namespace MiniCore.Model
{
    /// <summary>
    /// 表示网络收包队列在某一时刻的积压、处理耗时和累计处理情况。
    /// 该快照用于 Development 与性能压测诊断，不参与协议序列化。
    /// </summary>
    public struct NetworkIncomingQueueSnapshot
    {
        #region Public 公共成员

        /// <summary>
        /// 获取当前等待主线程处理的数据包数量。
        /// </summary>
        public long PendingPacketCount { get; }

        /// <summary>
        /// 获取当前等待主线程处理的数据总字节数。
        /// </summary>
        public long PendingByteCount { get; }

        /// <summary>
        /// 获取自上次重置后观察到的最大积压包数量。
        /// </summary>
        public long PeakPendingPacketCount { get; }

        /// <summary>
        /// 获取自上次重置后观察到的最大积压字节数。
        /// </summary>
        public long PeakPendingByteCount { get; }

        /// <summary>
        /// 获取自上次重置后已由主线程处理的数据包总数。
        /// </summary>
        public long ProcessedPacketCount { get; }

        /// <summary>
        /// 获取单个收包从开始处理到完成的最大耗时，单位为毫秒。
        /// </summary>
        public double MaxPacketProcessMilliseconds { get; }

        /// <summary>
        /// 获取主线程处理单个收包的 P50 耗时，单位为毫秒。
        /// 仅在压测诊断启用时记录，采用固定 0.25ms 时间桶精度。
        /// </summary>
        public double PacketProcessP50Milliseconds { get; }

        /// <summary>
        /// 获取主线程处理单个收包的 P95 耗时，单位为毫秒。
        /// 仅在压测诊断启用时记录，采用固定 0.25ms 时间桶精度。
        /// </summary>
        public double PacketProcessP95Milliseconds { get; }

        /// <summary>
        /// 获取主线程处理单个收包的 P99 耗时，单位为毫秒。
        /// 仅在压测诊断启用时记录，采用固定 0.25ms 时间桶精度。
        /// </summary>
        public double PacketProcessP99Milliseconds { get; }

        /// <summary>
        /// 获取当前统计周期内参与入站队列等待耗时采样的数据包数量。
        /// </summary>
        public long QueueWaitSampleCount { get; }

        /// <summary>
        /// 获取网络线程入队到主线程开始处理的平均等待时间，单位为毫秒。
        /// 未启用诊断或没有样本时为零。
        /// </summary>
        public double AverageQueueWaitMilliseconds { get; }

        /// <summary>
        /// 获取网络线程入队到主线程开始处理的最大等待时间，单位为毫秒。
        /// 未启用诊断或没有样本时为零。
        /// </summary>
        public double MaxQueueWaitMilliseconds { get; }

        /// <summary>
        /// 获取网络线程入队到主线程开始处理的 P50 等待时间，单位为毫秒。
        /// 仅在压测诊断启用时记录，采用固定 0.25ms 时间桶精度。
        /// </summary>
        public double QueueWaitP50Milliseconds { get; }

        /// <summary>
        /// 获取网络线程入队到主线程开始处理的 P95 等待时间，单位为毫秒。
        /// 仅在压测诊断启用时记录，采用固定 0.25ms 时间桶精度。
        /// </summary>
        public double QueueWaitP95Milliseconds { get; }

        /// <summary>
        /// 获取网络线程入队到主线程开始处理的 P99 等待时间，单位为毫秒。
        /// 仅在压测诊断启用时记录，采用固定 0.25ms 时间桶精度。
        /// </summary>
        public double QueueWaitP99Milliseconds { get; }

        /// <summary>
        /// 获取统计周期内被 Ping、Pong 或 RPC 保留队列拒绝的数据包数量。
        /// </summary>
        public long ControlRejectedPacketCount { get; }

        /// <summary>
        /// 获取统计周期内被普通业务数据队列拒绝的数据包数量。
        /// </summary>
        public long DataRejectedPacketCount { get; }

        /// <summary>
        /// 获取统计周期内两条入站队列累计拒绝的数据包数量。
        /// </summary>
        public long RejectedPacketCount => ControlRejectedPacketCount + DataRejectedPacketCount;

        /// <summary>
        /// 使用当前队列统计值创建诊断快照。
        /// </summary>
        /// <param name="pendingPacketCount">当前等待处理的数据包数量。</param>
        /// <param name="pendingByteCount">当前等待处理的数据总字节数。</param>
        /// <param name="peakPendingPacketCount">统计周期内的积压包数量峰值。</param>
        /// <param name="peakPendingByteCount">统计周期内的积压字节数峰值。</param>
        /// <param name="processedPacketCount">统计周期内已处理的数据包数量。</param>
        /// <param name="maxPacketProcessMilliseconds">统计周期内单包处理最大耗时。</param>
        /// <param name="packetProcessP50Milliseconds">主线程单包处理 P50 耗时。</param>
        /// <param name="packetProcessP95Milliseconds">主线程单包处理 P95 耗时。</param>
        /// <param name="packetProcessP99Milliseconds">主线程单包处理 P99 耗时。</param>
        /// <param name="queueWaitSampleCount">参与入站队列等待耗时采样的数据包数量。</param>
        /// <param name="averageQueueWaitMilliseconds">入站队列平均等待时间。</param>
        /// <param name="maxQueueWaitMilliseconds">入站队列最大等待时间。</param>
        /// <param name="queueWaitP50Milliseconds">入站队列等待 P50 耗时。</param>
        /// <param name="queueWaitP95Milliseconds">入站队列等待 P95 耗时。</param>
        /// <param name="queueWaitP99Milliseconds">入站队列等待 P99 耗时。</param>
        /// <param name="controlRejectedPacketCount">统计周期内控制保留队列拒绝的数据包数量。</param>
        /// <param name="dataRejectedPacketCount">统计周期内普通数据队列拒绝的数据包数量。</param>
        public NetworkIncomingQueueSnapshot(
            long pendingPacketCount,
            long pendingByteCount,
            long peakPendingPacketCount,
            long peakPendingByteCount,
            long processedPacketCount,
            double maxPacketProcessMilliseconds,
            double packetProcessP50Milliseconds,
            double packetProcessP95Milliseconds,
            double packetProcessP99Milliseconds,
            long queueWaitSampleCount,
            double averageQueueWaitMilliseconds,
            double maxQueueWaitMilliseconds,
            double queueWaitP50Milliseconds,
            double queueWaitP95Milliseconds,
            double queueWaitP99Milliseconds,
            long controlRejectedPacketCount,
            long dataRejectedPacketCount)
        {
            PendingPacketCount = pendingPacketCount;
            PendingByteCount = pendingByteCount;
            PeakPendingPacketCount = peakPendingPacketCount;
            PeakPendingByteCount = peakPendingByteCount;
            ProcessedPacketCount = processedPacketCount;
            MaxPacketProcessMilliseconds = maxPacketProcessMilliseconds;
            PacketProcessP50Milliseconds = packetProcessP50Milliseconds;
            PacketProcessP95Milliseconds = packetProcessP95Milliseconds;
            PacketProcessP99Milliseconds = packetProcessP99Milliseconds;
            QueueWaitSampleCount = queueWaitSampleCount;
            AverageQueueWaitMilliseconds = averageQueueWaitMilliseconds;
            MaxQueueWaitMilliseconds = maxQueueWaitMilliseconds;
            QueueWaitP50Milliseconds = queueWaitP50Milliseconds;
            QueueWaitP95Milliseconds = queueWaitP95Milliseconds;
            QueueWaitP99Milliseconds = queueWaitP99Milliseconds;
            ControlRejectedPacketCount = controlRejectedPacketCount;
            DataRejectedPacketCount = dataRejectedPacketCount;
        }

        #endregion
    }
}
