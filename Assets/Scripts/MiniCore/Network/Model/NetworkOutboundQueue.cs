using System;
using System.Diagnostics;
using System.Threading;
using MiniCore.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// 每个逻辑会话独占的有界出站发送器。
    /// 高优先级包优先于高频普通包写入底层传输，并由单一发送循环保持同会话顺序。
    /// </summary>
    internal sealed class NetworkOutboundQueue : IDisposable
    {
        #region Private 私有成员

        private const int DataMaximumPacketCount = 1024; // 高频普通消息队列的固定槽位数量。
        private const int DataMaximumByteCount = 1024 * 1024; // 高频普通消息队列的总字节上限。
        private const int ReliableMaximumPacketCount = 32; // SendAsync、RPC 与心跳使用的保留槽位数量。
        private const int ReliableMaximumByteCount = 64 * 1024; // 保留队列的总字节上限。
        private static readonly long CongestionDisconnectTicks = Stopwatch.Frequency * 3L; // 持续满三秒后主动关闭会话。

        private readonly INetworkTransport transport; // 实际负责 socket/KCP 写入的底层传输。
        private readonly FixedCapacityPacketQueue<NetworkOutgoingPacket> dataPackets; // 高频普通消息队列。
        private readonly FixedCapacityPacketQueue<NetworkOutgoingPacket> reliablePackets; // 可靠、RPC 与心跳保留队列。
        private int draining; // 发送循环是否已经启动的原子标志。
        private int disposed; // 防止重复清理的原子标志。
        private int timingMetricsEnabled; // 是否记录仅供压测诊断使用的分段发送耗时。
        private long fullSinceTicks; // 当前数据队列持续满的起始时刻。
        private long timingSampleCount; // 当前统计周期内已完成分段耗时采样的包数量。
        private long totalQueueWaitTicks; // 包在出站队列中等待的累计 Stopwatch tick。
        private long maxQueueWaitTicks; // 包在出站队列中等待的最大 Stopwatch tick。
        private long totalTransportSendTicks; // 调用底层传输发送到完成的累计 Stopwatch tick。
        private long maxTransportSendTicks; // 调用底层传输发送到完成的最大 Stopwatch tick。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 创建使用默认数据与可靠队列预算的会话出站发送器。
        /// </summary>
        /// <param name="transport">需要被串行写入的底层传输。</param>
        public NetworkOutboundQueue(INetworkTransport transport)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            dataPackets = new FixedCapacityPacketQueue<NetworkOutgoingPacket>(DataMaximumPacketCount, DataMaximumByteCount);
            reliablePackets = new FixedCapacityPacketQueue<NetworkOutgoingPacket>(ReliableMaximumPacketCount, ReliableMaximumByteCount);
        }

        /// <summary>
        /// 将需要等待实际底层写入结果的数据包放入可靠保留队列。
        /// </summary>
        /// <param name="buffer">由调用方转交所有权的完整业务包数组。</param>
        /// <param name="length">数组中有效业务包长度。</param>
        /// <param name="returnToPool">发送或失败后是否归还共享缓冲池。</param>
        /// <returns>底层传输完成写入或出现异常时完成的任务。</returns>
        public MTask EnqueueReliableAsync(byte[] buffer, int length, bool returnToPool)
        {
            var completion = new MTaskCompletionSource<bool>();
            if (!TryEnqueue(reliablePackets, buffer, length, returnToPool, completion))
            {
                ReturnBuffer(buffer, returnToPool);
                completion.TrySetException(new InvalidOperationException("网络可靠出站队列已满或会话已断开。"));
            }

            return AwaitCompletionAsync(completion);
        }

        /// <summary>
        /// 尝试将无需等待写入完成的高频普通消息放入数据队列。
        /// </summary>
        /// <param name="buffer">由调用方转交所有权的完整业务包数组。</param>
        /// <param name="length">数组中有效业务包长度。</param>
        /// <param name="returnToPool">拒绝、发送或失败后是否归还共享缓冲池。</param>
        /// <returns>本次尝试的会话与队列状态。</returns>
        public NetworkSendResult TryEnqueueData(byte[] buffer, int length, bool returnToPool)
        {
            if (Volatile.Read(ref disposed) != 0 || !transport.IsConnected)
            {
                ReturnBuffer(buffer, returnToPool);
                return NetworkSendResult.Disconnected;
            }

            if (TryEnqueue(dataPackets, buffer, length, returnToPool, null))
            {
                Interlocked.Exchange(ref fullSinceTicks, 0);
                return NetworkSendResult.Accepted;
            }

            ReturnBuffer(buffer, returnToPool);
            CheckCongestionDisconnect();
            return NetworkSendResult.QueueFull;
        }

        /// <summary>
        /// 获取数据与可靠队列的当前统计快照。
        /// </summary>
        /// <returns>两条出站队列当前占用与累计拒绝次数。</returns>
        internal NetworkOutboundQueueSnapshot CaptureSnapshot()
        {
            dataPackets.CaptureSnapshot(out long dataPacketCount, out long dataByteCount, out _, out _, out long dataRejected);
            reliablePackets.CaptureSnapshot(out long reliablePacketCount, out long reliableByteCount, out _, out _, out long reliableRejected);
            long samples = Interlocked.Read(ref timingSampleCount);
            long queueWaitTicks = Interlocked.Read(ref totalQueueWaitTicks);
            long transportSendTicks = Interlocked.Read(ref totalTransportSendTicks);
            return new NetworkOutboundQueueSnapshot(
                dataPacketCount,
                dataByteCount,
                reliablePacketCount,
                reliableByteCount,
                dataRejected + reliableRejected,
                samples,
                ToAverageMilliseconds(queueWaitTicks, samples),
                ToMilliseconds(Interlocked.Read(ref maxQueueWaitTicks)),
                ToAverageMilliseconds(transportSendTicks, samples),
                ToMilliseconds(Interlocked.Read(ref maxTransportSendTicks)));
        }

        /// <summary>
        /// 启用或关闭出站分段耗时诊断，并在切换时清空上一周期统计。
        /// </summary>
        /// <param name="enabled">为 true 时记录排队等待与底层传输等待；仅建议在压测期间启用。</param>
        internal void SetTimingMetricsEnabled(bool enabled)
        {
            Interlocked.Exchange(ref timingMetricsEnabled, enabled ? 1 : 0);
            ResetTimingMetrics();
        }

        /// <summary>
        /// 清空出站分段耗时统计，但不影响已排队的数据包和拒绝计数。
        /// </summary>
        internal void ResetTimingMetrics()
        {
            Interlocked.Exchange(ref timingSampleCount, 0);
            Interlocked.Exchange(ref totalQueueWaitTicks, 0);
            Interlocked.Exchange(ref maxQueueWaitTicks, 0);
            Interlocked.Exchange(ref totalTransportSendTicks, 0);
            Interlocked.Exchange(ref maxTransportSendTicks, 0);
        }

        /// <summary>
        /// 停止发送器、失败等待者并归还尚未写出的所有缓冲区。
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
            {
                return;
            }

            DrainAndFail(reliablePackets, new ObjectDisposedException(nameof(NetworkOutboundQueue)));
            DrainAndFail(dataPackets, new ObjectDisposedException(nameof(NetworkOutboundQueue)));
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 等待可靠出站包的底层写入结果。
        /// </summary>
        /// <param name="completion">保存当前可靠出站结果的完成源。</param>
        /// <returns>写入成功或异常时完成的任务。</returns>
        private static async MTask AwaitCompletionAsync(MTaskCompletionSource<bool> completion)
        {
            await completion.Task;
        }

        /// <summary>
        /// 尝试将已封包的数据写入指定优先级队列并启动发送循环。
        /// </summary>
        /// <param name="queue">目标固定容量队列。</param>
        /// <param name="buffer">由队列接管的数组。</param>
        /// <param name="length">数组有效长度。</param>
        /// <param name="returnToPool">发送结束后是否归还数组。</param>
        /// <param name="completion">需要等待写入结果的调用者完成源；无需等待时为 null。</param>
        /// <returns>成功接管数组时返回 true。</returns>
        private bool TryEnqueue(FixedCapacityPacketQueue<NetworkOutgoingPacket> queue, byte[] buffer, int length, bool returnToPool, MTaskCompletionSource<bool> completion)
        {
            if (buffer == null || length <= 0 || length > buffer.Length || Volatile.Read(ref disposed) != 0 || !transport.IsConnected)
            {
                return false;
            }

            var packet = new NetworkOutgoingPacket
            {
                Buffer = buffer,
                Length = length,
                ReturnToPool = returnToPool,
                CompletionSource = completion,
                EnqueuedTicks = Volatile.Read(ref timingMetricsEnabled) != 0 ? Stopwatch.GetTimestamp() : 0
            };
            if (!queue.TryEnqueue(packet, length))
            {
                return false;
            }

            StartDrain();
            return true;
        }

        /// <summary>
        /// 在尚未存在发送循环时启动单一出站发送任务。
        /// </summary>
        private void StartDrain()
        {
            if (Interlocked.CompareExchange(ref draining, 1, 0) == 0)
            {
                DrainAsync().Forget();
            }
        }

        /// <summary>
        /// 按可靠优先、普通其次的顺序串行写入底层传输。
        /// </summary>
        /// <returns>当前发送循环结束任务。</returns>
        private async MTask DrainAsync()
        {
            try
            {
                while (TryDequeueNext(out NetworkOutgoingPacket packet))
                {
                    long transportStartedTicks = 0;
                    try
                    {
                        if (!transport.IsConnected)
                        {
                            throw new InvalidOperationException("底层传输已经断开。");
                        }

                        if (packet.EnqueuedTicks != 0)
                        {
                            long queueWaitTicks = Stopwatch.GetTimestamp() - packet.EnqueuedTicks;
                            Interlocked.Add(ref totalQueueWaitTicks, queueWaitTicks);
                            UpdateMaximum(ref maxQueueWaitTicks, queueWaitTicks);
                            transportStartedTicks = Stopwatch.GetTimestamp();
                        }

                        await transport.SendAsync(new ArraySegment<byte>(packet.Buffer, 0, packet.Length));
                        if (transportStartedTicks != 0)
                        {
                            long transportSendTicks = Stopwatch.GetTimestamp() - transportStartedTicks;
                            Interlocked.Increment(ref timingSampleCount);
                            Interlocked.Add(ref totalTransportSendTicks, transportSendTicks);
                            UpdateMaximum(ref maxTransportSendTicks, transportSendTicks);
                        }
                        packet.CompletionSource?.TrySetResult(true);
                    }
                    catch (Exception exception)
                    {
                        packet.CompletionSource?.TrySetException(exception);
                        transport.Disconnect();
                    }
                    finally
                    {
                        ReturnBuffer(packet.Buffer, packet.ReturnToPool);
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref draining, 0);
                if (HasPendingPackets() && Volatile.Read(ref disposed) == 0)
                {
                    StartDrain();
                }
            }
        }

        /// <summary>
        /// 先取可靠包，再取高频普通包。
        /// </summary>
        /// <param name="packet">成功时返回待发送数据包。</param>
        /// <returns>存在待发送数据包时返回 true。</returns>
        private bool TryDequeueNext(out NetworkOutgoingPacket packet)
        {
            if (reliablePackets.TryDequeue(out packet, out _))
            {
                return true;
            }

            return dataPackets.TryDequeue(out packet, out _);
        }

        /// <summary>
        /// 判断任一优先级队列是否仍有待写数据包。
        /// </summary>
        /// <returns>存在待写数据包时返回 true。</returns>
        private bool HasPendingPackets()
        {
            reliablePackets.CaptureSnapshot(out long reliableCount, out _, out _, out _, out _);
            if (reliableCount > 0)
            {
                return true;
            }

            dataPackets.CaptureSnapshot(out long dataCount, out _, out _, out _, out _);
            return dataCount > 0;
        }

        /// <summary>
        /// 在数据队列持续满时主动断开底层传输，避免无限积压。
        /// </summary>
        private void CheckCongestionDisconnect()
        {
            long now = Stopwatch.GetTimestamp();
            long started = Interlocked.Read(ref fullSinceTicks);
            if (started == 0)
            {
                Interlocked.CompareExchange(ref fullSinceTicks, now, 0);
                return;
            }

            if (now - started >= CongestionDisconnectTicks)
            {
                transport.Disconnect();
            }
        }

        /// <summary>
        /// 以无锁比较交换更新指定的最大值计数器。
        /// </summary>
        /// <param name="location">需要更新的最大值计数器。</param>
        /// <param name="candidate">本次观察到的候选值。</param>
        private static void UpdateMaximum(ref long location, long candidate)
        {
            long current = Interlocked.Read(ref location);
            while (candidate > current)
            {
                long observed = Interlocked.CompareExchange(ref location, candidate, current);
                if (observed == current)
                {
                    return;
                }

                current = observed;
            }
        }

        /// <summary>
        /// 将 Stopwatch tick 换算为毫秒。
        /// </summary>
        /// <param name="ticks">需要换算的 Stopwatch tick 数。</param>
        /// <returns>对应的毫秒数。</returns>
        private static double ToMilliseconds(long ticks)
        {
            return ticks * 1000d / Stopwatch.Frequency;
        }

        /// <summary>
        /// 将累计 Stopwatch tick 换算为平均毫秒。
        /// </summary>
        /// <param name="totalTicks">累计的 Stopwatch tick。</param>
        /// <param name="count">参与累计的样本数量。</param>
        /// <returns>没有样本时为零，否则返回平均毫秒。</returns>
        private static double ToAverageMilliseconds(long totalTicks, long count)
        {
            return count <= 0 ? 0d : ToMilliseconds(totalTicks) / count;
        }

        /// <summary>
        /// 清空指定队列，失败等待者并归还被队列接管的缓冲区。
        /// </summary>
        /// <param name="queue">需要清理的固定容量队列。</param>
        /// <param name="exception">等待调用者收到的失败原因。</param>
        private static void DrainAndFail(FixedCapacityPacketQueue<NetworkOutgoingPacket> queue, Exception exception)
        {
            while (queue.TryDequeue(out NetworkOutgoingPacket packet, out _))
            {
                packet.CompletionSource?.TrySetException(exception);
                ReturnBuffer(packet.Buffer, packet.ReturnToPool);
            }
        }

        /// <summary>
        /// 在数组所有权已转交缓冲池时归还该数组。
        /// </summary>
        /// <param name="buffer">可能需要归还的数组。</param>
        /// <param name="returnToPool">是否由当前发送器负责归还。</param>
        private static void ReturnBuffer(byte[] buffer, bool returnToPool)
        {
            if (returnToPool)
            {
                ByteBufferPool.Shared.Return(buffer);
            }
        }

        #endregion
    }
}
