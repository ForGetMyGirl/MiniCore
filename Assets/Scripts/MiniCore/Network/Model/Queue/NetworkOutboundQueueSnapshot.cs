namespace MiniCore.Model
{
    /// <summary>
    /// 表示单个逻辑会话两条固定容量出站队列的当前占用、累计拒绝次数和可选的分段耗时。
    /// 快照仅用于诊断和调度决策，不转移任何待发送缓冲区的所有权。
    /// </summary>
    public readonly struct NetworkOutboundQueueSnapshot
    {
        #region Public 公共成员

        /// <summary>
        /// 高频普通消息数据队列当前等待写入的数据包数量。
        /// </summary>
        public long DataPacketCount { get; }

        /// <summary>
        /// 高频普通消息数据队列当前等待写入的有效字节数。
        /// </summary>
        public long DataByteCount { get; }

        /// <summary>
        /// SendAsync、RPC 与心跳保留队列当前等待写入的数据包数量。
        /// </summary>
        public long ReliablePacketCount { get; }

        /// <summary>
        /// SendAsync、RPC 与心跳保留队列当前等待写入的有效字节数。
        /// </summary>
        public long ReliableByteCount { get; }

        /// <summary>
        /// 两条出站队列自上次重置以来拒绝的数据包总数。
        /// </summary>
        public long RejectedPacketCount { get; }

        /// <summary>
        /// 获取当前统计周期内参与出站耗时采样的数据包数量。
        /// </summary>
        public long TimingSampleCount { get; }

        /// <summary>
        /// 获取当前统计周期内实际调用底层传输写入的次数。
        /// TCP 普通消息批量时该值可小于参与耗时采样的数据包数量。
        /// </summary>
        public long TransportWriteCount { get; }

        /// <summary>
        /// 获取包进入出站队列到开始调用传输发送的平均等待时间，单位为毫秒。
        /// 未启用诊断或没有样本时为零。
        /// </summary>
        public double AverageQueueWaitMilliseconds { get; }

        /// <summary>
        /// 获取包进入出站队列到开始调用传输发送的最大等待时间，单位为毫秒。
        /// 未启用诊断或没有样本时为零。
        /// </summary>
        public double MaxQueueWaitMilliseconds { get; }

        /// <summary>
        /// 获取调用底层传输发送到该异步操作完成的平均等待时间，单位为毫秒。
        /// 未启用诊断或没有样本时为零。
        /// </summary>
        public double AverageTransportSendMilliseconds { get; }

        /// <summary>
        /// 获取调用底层传输发送到该异步操作完成的最大等待时间，单位为毫秒。
        /// 未启用诊断或没有样本时为零。
        /// </summary>
        public double MaxTransportSendMilliseconds { get; }

        /// <summary>
        /// 使用当前数据与可靠队列状态创建不可变诊断快照。
        /// </summary>
        /// <param name="dataPacketCount">普通数据队列当前包数量。</param>
        /// <param name="dataByteCount">普通数据队列当前有效字节数。</param>
        /// <param name="reliablePacketCount">可靠保留队列当前包数量。</param>
        /// <param name="reliableByteCount">可靠保留队列当前有效字节数。</param>
        /// <param name="rejectedPacketCount">两条队列累计拒绝包数量。</param>
        /// <param name="timingSampleCount">参与耗时统计的数据包数量。</param>
        /// <param name="transportWriteCount">实际调用底层传输写入的次数。</param>
        /// <param name="averageQueueWaitMilliseconds">出站队列平均等待时间。</param>
        /// <param name="maxQueueWaitMilliseconds">出站队列最大等待时间。</param>
        /// <param name="averageTransportSendMilliseconds">底层传输平均发送等待时间。</param>
        /// <param name="maxTransportSendMilliseconds">底层传输最大发送等待时间。</param>
        public NetworkOutboundQueueSnapshot(
            long dataPacketCount,
            long dataByteCount,
            long reliablePacketCount,
            long reliableByteCount,
            long rejectedPacketCount,
            long timingSampleCount,
            long transportWriteCount,
            double averageQueueWaitMilliseconds,
            double maxQueueWaitMilliseconds,
            double averageTransportSendMilliseconds,
            double maxTransportSendMilliseconds)
        {
            DataPacketCount = dataPacketCount;
            DataByteCount = dataByteCount;
            ReliablePacketCount = reliablePacketCount;
            ReliableByteCount = reliableByteCount;
            RejectedPacketCount = rejectedPacketCount;
            TimingSampleCount = timingSampleCount;
            TransportWriteCount = transportWriteCount;
            AverageQueueWaitMilliseconds = averageQueueWaitMilliseconds;
            MaxQueueWaitMilliseconds = maxQueueWaitMilliseconds;
            AverageTransportSendMilliseconds = averageTransportSendMilliseconds;
            MaxTransportSendMilliseconds = maxTransportSendMilliseconds;
        }

        #endregion
    }
}
