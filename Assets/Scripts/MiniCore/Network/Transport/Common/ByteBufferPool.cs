using System;
using System.Collections.Concurrent;
using System.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// 按二进制桶大小复用字节数组的线程安全缓冲池。
    /// 每个桶使用受容量限制的数组栈保存缓冲区，稳定运行时不会因归还数组而创建并发容器节点。
    /// </summary>
    public sealed class ByteBufferPool
    {
        #region Private 私有成员

        private const int MinimumBucketSize = 256; // 最小可复用缓冲区的桶大小。
        private const int InitialBucketSlotCount = 16; // 新桶初始创建的引用槽位数量。
        private const int MaximumRetainedBufferCountPerBucket = 16384; // 单个桶最多保留的缓冲区数量。
        private const int MaximumPooledBufferSize = 1024 * 1024; // 允许进入池的最大单个缓冲区大小。
        private const long MaximumRetainedBytesPerBucket = 8L * 1024L * 1024L; // 单个桶最多保留的数组总字节数。
        private const long MaximumRetainedBytes = 32L * 1024L * 1024L; // 整个共享池最多保留的数组总字节数。

        private readonly ConcurrentDictionary<int, BufferBucket> pools = new ConcurrentDictionary<int, BufferBucket>(); // 按桶大小索引的缓冲区桶。
        private long retainedByteCount; // 当前所有桶实际保留的字节数组总大小。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 全局共享的字节数组缓冲池。
        /// 网络传输和主线程均通过该实例租用与归还缓冲区。
        /// </summary>
        public static ByteBufferPool Shared { get; } = new ByteBufferPool();

        /// <summary>
        /// 创建使用默认容量、单桶上限和全局内存上限的字节数组缓冲池。
        /// </summary>
        public ByteBufferPool()
        {
        }

        /// <summary>
        /// 租用容量不小于指定大小的字节数组。
        /// 池中没有可用数组时会创建新数组；后续归还后可被相同桶大小的请求复用。
        /// </summary>
        /// <param name="size">调用方需要的最小有效字节长度。</param>
        /// <returns>容量不小于请求长度的字节数组。</returns>
        public byte[] Rent(int size)
        {
            int bucketSize = GetBucketSize(size);
            if (bucketSize > MaximumPooledBufferSize)
            {
                return new byte[bucketSize];
            }

            BufferBucket pool = GetOrCreateBucket(bucketSize);
            if (pool.TryRent(out byte[] buffer))
            {
                return buffer;
            }

            return new byte[bucketSize];
        }

        /// <summary>
        /// 归还由本池租用或大小与桶一致的字节数组。
        /// 桶或全局池已满时会放弃保留该数组，避免网络峰值后长期占用过多内存。
        /// </summary>
        /// <param name="buffer">需要归还的字节数组；空数组、超过池化上限或不匹配桶大小的数组不会被保留。</param>
        public void Return(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0 || buffer.Length > MaximumPooledBufferSize)
            {
                return;
            }

            int bucketSize = GetBucketSize(buffer.Length);
            if (buffer.Length != bucketSize)
            {
                return;
            }

            GetOrCreateBucket(bucketSize).TryReturn(buffer);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 获取已有桶，或在首次访问该大小时创建对应桶。
        /// </summary>
        /// <param name="bucketSize">需要获取的标准化桶大小。</param>
        /// <returns>与桶大小一一对应的缓冲区桶。</returns>
        private BufferBucket GetOrCreateBucket(int bucketSize)
        {
            if (pools.TryGetValue(bucketSize, out BufferBucket pool))
            {
                return pool;
            }

            return pools.GetOrAdd(bucketSize, CreateBucket);
        }

        /// <summary>
        /// 为首次访问的桶大小创建带有固定上限的缓冲区桶。
        /// </summary>
        /// <param name="bucketSize">新桶中每个数组的固定长度。</param>
        /// <returns>用于保存同一大小数组的线程安全桶。</returns>
        private BufferBucket CreateBucket(int bucketSize)
        {
            int maximumRetainedCount = GetMaximumRetainedCount(bucketSize);
            int initialSlotCount = Math.Min(InitialBucketSlotCount, maximumRetainedCount);
            return new BufferBucket(this, maximumRetainedCount, initialSlotCount);
        }

        /// <summary>
        /// 根据桶大小计算可保留数组数量，兼顾单桶字节预算与数量上限。
        /// </summary>
        /// <param name="bucketSize">桶内单个数组的固定长度。</param>
        /// <returns>该桶允许保留的最大数组数量。</returns>
        private static int GetMaximumRetainedCount(int bucketSize)
        {
            long countByByteBudget = MaximumRetainedBytesPerBucket / bucketSize;
            if (countByByteBudget <= 0)
            {
                return 1;
            }

            return (int)Math.Min(MaximumRetainedBufferCountPerBucket, countByByteBudget);
        }

        /// <summary>
        /// 为即将放入桶的数组预留全局内存预算。
        /// </summary>
        /// <param name="bufferLength">需要保留的数组长度。</param>
        /// <returns>成功预留预算时返回 true；全局池已满时返回 false。</returns>
        private bool TryReserveRetainedBytes(int bufferLength)
        {
            while (true)
            {
                long currentRetainedBytes = Interlocked.Read(ref retainedByteCount);
                if (currentRetainedBytes > MaximumRetainedBytes - bufferLength)
                {
                    return false;
                }

                long updatedRetainedBytes = currentRetainedBytes + bufferLength;
                if (Interlocked.CompareExchange(ref retainedByteCount, updatedRetainedBytes, currentRetainedBytes) == currentRetainedBytes)
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// 在数组从桶中租出或写入失败时归还已占用的全局内存预算。
        /// </summary>
        /// <param name="bufferLength">不再由池保留的数组长度。</param>
        private void ReleaseRetainedBytes(int bufferLength)
        {
            Interlocked.Add(ref retainedByteCount, -bufferLength);
        }

        /// <summary>
        /// 将请求长度向上归一到二进制桶大小。
        /// </summary>
        /// <param name="size">调用方请求的最小数组长度。</param>
        /// <returns>可用于复用的桶大小。</returns>
        private static int GetBucketSize(int size)
        {
            if (size <= 0)
            {
                return MinimumBucketSize;
            }

            int bucket = MinimumBucketSize;
            while (bucket < size && bucket > 0)
            {
                bucket <<= 1;
            }

            return bucket > 0 ? bucket : size;
        }

        /// <summary>
        /// 单个固定大小缓冲区桶。
        /// 该桶通过内部锁保护数组槽位与数量，不要求外部调用方额外加锁。
        /// </summary>
        private sealed class BufferBucket
        {
            #region Private 私有成员

            private readonly object syncRoot = new object(); // 保护当前桶的槽位数组和有效数量。
            private readonly ByteBufferPool owner; // 管理全局保留字节预算的所属缓冲池。
            private readonly int maximumRetainedCount; // 当前桶可保存数组的数量上限。
            private byte[][] buffers; // 保存可复用数组引用的栈式槽位数组。
            private int count; // 当前桶中可租用数组的数量。

            #endregion

            #region Public 公共成员

            /// <summary>
            /// 创建指定容量上限的缓冲区桶。
            /// </summary>
            /// <param name="owner">管理全局内存预算的所属池。</param>
            /// <param name="maximumRetainedCount">桶中允许保留的最大数组数量。</param>
            /// <param name="initialSlotCount">首次分配的数组引用槽位数量。</param>
            public BufferBucket(ByteBufferPool owner, int maximumRetainedCount, int initialSlotCount)
            {
                this.owner = owner;
                this.maximumRetainedCount = maximumRetainedCount;
                buffers = new byte[initialSlotCount][];
            }

            /// <summary>
            /// 从当前桶取出一个可复用数组。
            /// </summary>
            /// <param name="buffer">成功时返回已从桶中移除的数组。</param>
            /// <returns>桶中存在可复用数组时返回 true。</returns>
            public bool TryRent(out byte[] buffer)
            {
                lock (syncRoot)
                {
                    if (count == 0)
                    {
                        buffer = null;
                        return false;
                    }

                    int index = --count;
                    buffer = buffers[index];
                    buffers[index] = null;
                    owner.ReleaseRetainedBytes(buffer.Length);
                    return true;
                }
            }

            /// <summary>
            /// 将数组写入当前桶的预分配槽位。
            /// 桶已满或全局内存预算不足时不会保存该数组。
            /// </summary>
            /// <param name="buffer">需要保存到当前桶的字节数组。</param>
            /// <returns>数组被当前桶成功保留时返回 true。</returns>
            public bool TryReturn(byte[] buffer)
            {
                lock (syncRoot)
                {
                    if (count >= maximumRetainedCount || !owner.TryReserveRetainedBytes(buffer.Length))
                    {
                        return false;
                    }

                    bool isStored = false;
                    try
                    {
                        EnsureStorageCapacity();
                        buffers[count] = buffer;
                        count++;
                        isStored = true;
                        return true;
                    }
                    finally
                    {
                        if (!isStored)
                        {
                            owner.ReleaseRetainedBytes(buffer.Length);
                        }
                    }
                }
            }

            #endregion

            #region Private 私有成员

            /// <summary>
            /// 在桶仍可保留数组但引用槽位已满时扩容槽位数组。
            /// 扩容只发生在池容量增长阶段，稳定运行时不会进入该分支。
            /// </summary>
            private void EnsureStorageCapacity()
            {
                if (count < buffers.Length)
                {
                    return;
                }

                int nextCapacity = Math.Min(maximumRetainedCount, buffers.Length * 2);
                byte[][] expandedBuffers = new byte[nextCapacity][];
                Array.Copy(buffers, expandedBuffers, count);
                buffers = expandedBuffers;
            }

            #endregion
        }

        #endregion
    }
}
