using System;
using System.Collections.Concurrent;

namespace MiniCore.Model
{
    /// <summary>
    /// 按二进制桶大小复用字节数组的线程安全缓冲池。
    /// </summary>
    public sealed class ByteBufferPool
    {
        /// <summary>
        /// 全局共享的字节数组缓冲池。
        /// </summary>
        public static ByteBufferPool Shared { get; } = new ByteBufferPool();

        private readonly ConcurrentDictionary<int, ConcurrentStack<byte[]>> pools = new ConcurrentDictionary<int, ConcurrentStack<byte[]>>(); // 按桶大小划分的字节数组池。

        /// <summary>
        /// 租用容量不小于指定大小的字节数组。
        /// </summary>
        /// <param name="size">执行该方法所需的 size 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public byte[] Rent(int size)
        {
            int bucketSize = GetBucketSize(size);
            var pool = pools.GetOrAdd(bucketSize, _ => new ConcurrentStack<byte[]>());
            if (pool.TryPop(out var buffer))
            {
                return buffer;
            }
            return new byte[bucketSize];
        }

        /// <summary>
        /// 归还由本池租用或可复用的字节数组。
        /// </summary>
        /// <param name="buffer">执行该方法所需的 buffer 参数。</param>
        public void Return(byte[] buffer)
        {
            if (buffer == null)
            {
                return;
            }

            var pool = pools.GetOrAdd(buffer.Length, _ => new ConcurrentStack<byte[]>());
            pool.Push(buffer);
        }

        /// <summary>
        /// 执行 GetBucketSize 相关处理。
        /// </summary>
        /// <param name="size">执行该方法所需的 size 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private static int GetBucketSize(int size)
        {
            if (size <= 0)
            {
                return 256;
            }

            int bucket = 256;
            while (bucket < size && bucket > 0)
            {
                bucket <<= 1;
            }

            if (bucket <= 0)
            {
                return size;
            }

            return bucket;
        }
    }
}
