using System;

namespace MiniCore.Model
{
    /// <summary>
    /// 使用预分配数组和互斥锁保存网络数据包的固定容量环形队列。
    /// 队列创建后不会扩容；调用方必须在拒绝、出队和清理路径维护缓冲区所有权。
    /// </summary>
    public sealed class FixedCapacityPacketQueue<TPacket> where TPacket : struct
    {
        #region Private 私有成员

        private readonly object syncRoot = new object(); // 保护环形槽位、索引与字节计数。
        private readonly TPacket[] packets; // 预分配的数据包槽位。
        private readonly int[] packetLengths; // 与槽位一一对应的有效字节长度。
        private readonly int maximumPacketCount; // 允许入队的最大数据包数量。
        private readonly long maximumByteCount; // 允许入队的最大有效字节数。
        private int head; // 当前队首索引。
        private int tail; // 下一个写入索引。
        private int count; // 当前队列数据包数量。
        private long byteCount; // 当前队列有效字节总数。
        private long peakPacketCount; // 统计周期内数据包数量峰值。
        private long peakByteCount; // 统计周期内字节数量峰值。
        private long rejectedPacketCount; // 统计周期内被容量限制拒绝的数据包数量。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 使用固定的消息数和字节数预算创建环形队列。
        /// </summary>
        /// <param name="maximumPacketCount">队列最多容纳的数据包数量。</param>
        /// <param name="maximumByteCount">队列最多容纳的有效字节数。</param>
        public FixedCapacityPacketQueue(int maximumPacketCount, long maximumByteCount)
        {
            if (maximumPacketCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumPacketCount));
            }

            if (maximumByteCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumByteCount));
            }

            this.maximumPacketCount = maximumPacketCount;
            this.maximumByteCount = maximumByteCount;
            packets = new TPacket[maximumPacketCount];
            packetLengths = new int[maximumPacketCount];
        }

        /// <summary>
        /// 尝试将数据包放入预分配的环形槽位。
        /// </summary>
        /// <param name="packet">需要保存的数据包。</param>
        /// <param name="byteLength">该数据包占用的有效字节数。</param>
        /// <returns>容量允许且已成功入队时返回 true。</returns>
        public bool TryEnqueue(TPacket packet, int byteLength)
        {
            if (byteLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byteLength));
            }

            lock (syncRoot)
            {
                if (count >= maximumPacketCount || byteLength > maximumByteCount - byteCount)
                {
                    rejectedPacketCount++;
                    return false;
                }

                packets[tail] = packet;
                packetLengths[tail] = byteLength;
                tail = (tail + 1) % maximumPacketCount;
                count++;
                byteCount += byteLength;
                if (count > peakPacketCount)
                {
                    peakPacketCount = count;
                }

                if (byteCount > peakByteCount)
                {
                    peakByteCount = byteCount;
                }

                return true;
            }
        }

        /// <summary>
        /// 判断指定字节长度是否可在不触发任何扩容的前提下进入当前队列。
        /// </summary>
        /// <param name="byteLength">计划入队的数据包有效字节数。</param>
        /// <returns>消息数与字节数预算均允许时返回 true。</returns>
        public bool CanAccept(int byteLength)
        {
            if (byteLength < 0)
            {
                return false;
            }

            lock (syncRoot)
            {
                return count < maximumPacketCount && byteLength <= maximumByteCount - byteCount;
            }
        }

        /// <summary>
        /// 尝试取出最早入队的数据包。
        /// </summary>
        /// <param name="packet">成功时返回队首数据包。</param>
        /// <param name="byteLength">成功时返回该数据包的有效字节数。</param>
        /// <returns>队列非空且已成功出队时返回 true。</returns>
        public bool TryDequeue(out TPacket packet, out int byteLength)
        {
            lock (syncRoot)
            {
                if (count == 0)
                {
                    packet = default;
                    byteLength = 0;
                    return false;
                }

                packet = packets[head];
                packets[head] = default;
                byteLength = packetLengths[head];
                packetLengths[head] = 0;
                head = (head + 1) % maximumPacketCount;
                count--;
                byteCount -= byteLength;
                return true;
            }
        }

        /// <summary>
        /// 获取不改变队列状态的当前计数和统计峰值。
        /// </summary>
        /// <param name="packetCount">返回当前包数量。</param>
        /// <param name="currentByteCount">返回当前有效字节数。</param>
        /// <param name="peakPackets">返回统计周期内包数量峰值。</param>
        /// <param name="peakBytes">返回统计周期内字节数峰值。</param>
        /// <param name="rejectedPackets">返回统计周期内拒绝次数。</param>
        public void CaptureSnapshot(out long packetCount, out long currentByteCount, out long peakPackets, out long peakBytes, out long rejectedPackets)
        {
            lock (syncRoot)
            {
                packetCount = count;
                currentByteCount = byteCount;
                peakPackets = peakPacketCount;
                peakBytes = peakByteCount;
                rejectedPackets = rejectedPacketCount;
            }
        }

        /// <summary>
        /// 重置统计峰值和拒绝次数，不清除仍在队列中的数据包。
        /// </summary>
        public void ResetMetrics()
        {
            lock (syncRoot)
            {
                peakPacketCount = count;
                peakByteCount = byteCount;
                rejectedPacketCount = 0;
            }
        }

        #endregion

    }
}
