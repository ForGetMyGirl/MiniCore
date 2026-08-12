using System;
using System.Diagnostics;
using System.Threading;
using MiniCore.Threading;
using MiniCore.Core;

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
        private const int TcpBatchMaximumPacketCount = 32; // 单次 TCP 普通消息批量最多合并的业务包数量，限制可靠包的最长等待。
        private const int TcpBatchMaximumByteCount = 32 * 1024; // 单次 TCP 普通消息批量的完整长度帧字节上限。
        private const int UdpBatchMaximumPacketCount = 16; // 单次 UDP 高频数据报最多承载的逻辑业务包数量，限制同一数据报丢失影响范围。
        private const int UdpBatchMaximumDatagramByteCount = 1200; // UDP 高频批量数据报的安全 MTU 预算，避免公网路径发生 IP 分片。
        private static readonly long CongestionDisconnectTicks = Stopwatch.Frequency * 3L; // 持续满三秒后主动关闭会话。

        private readonly INetworkTransport transport; // 实际负责 socket/KCP 写入的底层传输。
        private readonly IMTaskExecutor networkExecutor; // 当前会话发送循环使用的执行器。
        private readonly FixedCapacityPacketQueue<NetworkOutgoingPacket> dataPackets; // 高频普通消息队列。
        private readonly FixedCapacityPacketQueue<NetworkOutgoingPacket> reliablePackets; // 可靠、RPC 与心跳保留队列。
        private readonly NetworkOutgoingPacket[] tcpBatchPackets = new NetworkOutgoingPacket[TcpBatchMaximumPacketCount]; // 单会话发送器独占复用的 TCP 普通消息批量槽位。
        private readonly NetworkOutgoingPacket[] udpBatchPackets = new NetworkOutgoingPacket[UdpBatchMaximumPacketCount]; // 单会话发送器独占复用的 UDP 高频数据报批量槽位。
        private int draining; // 发送循环是否已经启动的原子标志。
        private int disposed; // 防止重复清理的原子标志。
        private int timingMetricsEnabled; // 是否记录仅供压测诊断使用的分段发送耗时。
        private long fullSinceTicks; // 当前数据队列持续满的起始时刻。
        private long timingSampleCount; // 当前统计周期内已完成分段耗时采样的包数量。
        private long transportWriteCount; // 当前统计周期内实际调用底层传输写入的次数。
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
        /// <param name="executor">发送循环使用的执行器；为空时按当前环境选择默认执行器。</param>
        public NetworkOutboundQueue(INetworkTransport transport, IMTaskExecutor executor = null)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            networkExecutor = NetworkExecutorResolver.Resolve(executor);
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
        /// 尝试将无需等待实际写入完成的可靠业务包放入保留队列。
        /// 调用成功后队列接管数组并保持发送顺序；调用失败时当前方法归还数组，调用方必须显式处理返回状态。
        /// </summary>
        /// <param name="buffer">由调用方转交所有权的完整业务包数组。</param>
        /// <param name="length">数组中有效业务包长度。</param>
        /// <param name="returnToPool">拒绝、发送或失败后是否归还共享缓冲池。</param>
        /// <returns>本次尝试的会话与可靠队列状态。</returns>
        public NetworkSendResult TryEnqueueReliable(byte[] buffer, int length, bool returnToPool)
        {
            if (Volatile.Read(ref disposed) != 0 || !transport.IsConnected)
            {
                ReturnBuffer(buffer, returnToPool);
                return NetworkSendResult.Disconnected;
            }

            if (TryEnqueue(reliablePackets, buffer, length, returnToPool, null))
            {
                return NetworkSendResult.Accepted;
            }

            ReturnBuffer(buffer, returnToPool);
            return NetworkSendResult.QueueFull;
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
            long writes = Interlocked.Read(ref transportWriteCount);
            long queueWaitTicks = Interlocked.Read(ref totalQueueWaitTicks);
            long transportSendTicks = Interlocked.Read(ref totalTransportSendTicks);
            return new NetworkOutboundQueueSnapshot(
                dataPacketCount,
                dataByteCount,
                reliablePacketCount,
                reliableByteCount,
                dataRejected + reliableRejected,
                samples,
                writes,
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
            Interlocked.Exchange(ref transportWriteCount, 0);
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
            await MTask.SwitchTo(networkExecutor);
            try
            {
                while (true)
                {
                    if (reliablePackets.TryDequeue(out NetworkOutgoingPacket reliablePacket, out _))
                    {
                        await SendSinglePacketAsync(reliablePacket);
                        continue;
                    }

                    if (!dataPackets.TryDequeue(out NetworkOutgoingPacket dataPacket, out _))
                    {
                        break;
                    }

                    if (transport is IFramedBatchNetworkTransport framedBatchTransport
                        && dataPacket.Length + sizeof(int) <= TcpBatchMaximumByteCount)
                    {
                        await SendTcpDataBatchAsync(framedBatchTransport, dataPacket);
                    }
                    else if (transport is IDatagramBatchNetworkTransport datagramBatchTransport
                             && dataPacket.Length + UdpBatchDatagramCodec.PacketLengthPrefixByteCount
                                <= UdpBatchMaximumDatagramByteCount - UdpBatchDatagramCodec.HeaderByteCount)
                    {
                        await SendUdpDataBatchAsync(datagramBatchTransport, dataPacket);
                    }
                    else
                    {
                        await SendSinglePacketAsync(dataPacket);
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
        /// 串行发送一个未参与 TCP 批量的业务包，并完成其等待者和缓冲区归还。
        /// </summary>
        /// <param name="packet">已从可靠或普通队列取出的待发送业务包。</param>
        /// <returns>当前包成功写入或失败清理完成时完成的任务。</returns>
        private async MTask SendSinglePacketAsync(NetworkOutgoingPacket packet)
        {
            long transportStartedTicks = 0;
            try
            {
                if (!transport.IsConnected)
                {
                    throw new InvalidOperationException("底层传输已经断开。");
                }

                transportStartedTicks = RecordQueueWait(packet);
                await transport.SendAsync(new ArraySegment<byte>(packet.Buffer, 0, packet.Length));
                Interlocked.Increment(ref transportWriteCount);
                RecordTransportSend(packet, transportStartedTicks);
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

        /// <summary>
        /// 将同一会话的普通 TCP 业务包合成为有限大小的连续长度帧并一次写入。
        /// 可靠、RPC 与心跳包不进入本方法，且最多连续占用三十二个普通包，保证它们不会被无限延后。
        /// </summary>
        /// <param name="framedBatchTransport">支持写入已完成长度前缀帧的当前 TCP 传输。</param>
        /// <param name="firstPacket">已从普通数据队列取出的批量首包。</param>
        /// <returns>当前批量成功写入或全部失败清理完成时完成的任务。</returns>
        private async MTask SendTcpDataBatchAsync(IFramedBatchNetworkTransport framedBatchTransport, NetworkOutgoingPacket firstPacket)
        {
            int packetCount = CollectTcpDataBatch(firstPacket, out int frameByteCount);
            byte[] frameBuffer = null;
            try
            {
                if (!transport.IsConnected)
                {
                    throw new InvalidOperationException("底层传输已经断开。");
                }

                frameBuffer = ByteBufferPool.Shared.Rent(frameByteCount);
                int offset = 0;
                for (int index = 0; index < packetCount; index++)
                {
                    NetworkOutgoingPacket packet = tcpBatchPackets[index];
                    NetBinaryCodec.WriteInt32BE(frameBuffer, offset, packet.Length);
                    offset += sizeof(int);
                    Buffer.BlockCopy(packet.Buffer, 0, frameBuffer, offset, packet.Length);
                    offset += packet.Length;
                }

                long transportStartedTicks = 0;
                for (int index = 0; index < packetCount; index++)
                {
                    long currentStartedTicks = RecordQueueWait(tcpBatchPackets[index]);
                    if (currentStartedTicks != 0)
                    {
                        transportStartedTicks = currentStartedTicks;
                    }
                }

                await framedBatchTransport.SendFramedBatchAsync(new ArraySegment<byte>(frameBuffer, 0, frameByteCount));
                Interlocked.Increment(ref transportWriteCount);
                for (int index = 0; index < packetCount; index++)
                {
                    NetworkOutgoingPacket packet = tcpBatchPackets[index];
                    RecordTransportSend(packet, transportStartedTicks);
                    packet.CompletionSource?.TrySetResult(true);
                }
            }
            catch (Exception exception)
            {
                for (int index = 0; index < packetCount; index++)
                {
                    tcpBatchPackets[index].CompletionSource?.TrySetException(exception);
                }

                transport.Disconnect();
            }
            finally
            {
                if (frameBuffer != null)
                {
                    ByteBufferPool.Shared.Return(frameBuffer);
                }

                for (int index = 0; index < packetCount; index++)
                {
                    NetworkOutgoingPacket packet = tcpBatchPackets[index];
                    ReturnBuffer(packet.Buffer, packet.ReturnToPool);
                    tcpBatchPackets[index] = default;
                }
            }
        }

        /// <summary>
        /// 从普通队列连续取得不超过包数与字节预算的 TCP 数据包，用于保持协议顺序的单次批量写入。
        /// </summary>
        /// <param name="firstPacket">已被发送循环取出的批量首包。</param>
        /// <param name="frameByteCount">返回所有长度头和业务正文合计的连续帧字节数。</param>
        /// <returns>实际写入批量槽位的业务包数量，至少为一。</returns>
        private int CollectTcpDataBatch(NetworkOutgoingPacket firstPacket, out int frameByteCount)
        {
            tcpBatchPackets[0] = firstPacket;
            int packetCount = 1;
            frameByteCount = firstPacket.Length + sizeof(int);
            while (packetCount < TcpBatchMaximumPacketCount
                && dataPackets.TryPeek(out NetworkOutgoingPacket nextPacket, out _)
                && nextPacket.Length + sizeof(int) <= TcpBatchMaximumByteCount - frameByteCount)
            {
                dataPackets.TryDequeue(out NetworkOutgoingPacket packet, out _);
                tcpBatchPackets[packetCount] = packet;
                packetCount++;
                frameByteCount += packet.Length + sizeof(int);
            }

            return packetCount;
        }

        /// <summary>
        /// 将同一会话中已有的高频普通业务包封装为一个受 MTU 约束的 UDP 数据报。
        /// 仅有首包时沿用单数据报发送，绝不为了等待第二包而延迟当前消息；可靠队列、RPC 与心跳不进入本方法。
        /// </summary>
        /// <param name="datagramBatchTransport">支持发送完整 UDP 批量数据报的当前传输。</param>
        /// <param name="firstPacket">已从普通数据队列取出的批量首包。</param>
        /// <returns>当前数据报或首包成功写入、或者对应失败清理完成时完成的任务。</returns>
        private async MTask SendUdpDataBatchAsync(IDatagramBatchNetworkTransport datagramBatchTransport, NetworkOutgoingPacket firstPacket)
        {
            int packetCount = CollectUdpDataBatch(firstPacket, out int datagramByteCount);
            if (packetCount == 1)
            {
                udpBatchPackets[0] = default;
                await SendSinglePacketAsync(firstPacket);
                return;
            }

            byte[] datagramBuffer = null;
            try
            {
                if (!transport.IsConnected)
                {
                    throw new InvalidOperationException("底层传输已经断开。");
                }

                datagramBuffer = ByteBufferPool.Shared.Rent(datagramByteCount);
                UdpBatchDatagramCodec.WriteHeader(datagramBuffer, 0, packetCount);
                int offset = UdpBatchDatagramCodec.HeaderByteCount;
                for (int index = 0; index < packetCount; index++)
                {
                    NetworkOutgoingPacket packet = udpBatchPackets[index];
                    UdpBatchDatagramCodec.WritePacketLength(datagramBuffer, offset, packet.Length);
                    offset += UdpBatchDatagramCodec.PacketLengthPrefixByteCount;
                    Buffer.BlockCopy(packet.Buffer, 0, datagramBuffer, offset, packet.Length);
                    offset += packet.Length;
                }

                long transportStartedTicks = 0;
                for (int index = 0; index < packetCount; index++)
                {
                    long currentStartedTicks = RecordQueueWait(udpBatchPackets[index]);
                    if (currentStartedTicks != 0)
                    {
                        transportStartedTicks = currentStartedTicks;
                    }
                }

                await datagramBatchTransport.SendDatagramBatchAsync(new ArraySegment<byte>(datagramBuffer, 0, datagramByteCount));
                Interlocked.Increment(ref transportWriteCount);
                for (int index = 0; index < packetCount; index++)
                {
                    NetworkOutgoingPacket packet = udpBatchPackets[index];
                    RecordTransportSend(packet, transportStartedTicks);
                    packet.CompletionSource?.TrySetResult(true);
                }
            }
            catch (Exception exception)
            {
                for (int index = 0; index < packetCount; index++)
                {
                    udpBatchPackets[index].CompletionSource?.TrySetException(exception);
                }

                transport.Disconnect();
            }
            finally
            {
                if (datagramBuffer != null)
                {
                    ByteBufferPool.Shared.Return(datagramBuffer);
                }

                for (int index = 0; index < packetCount; index++)
                {
                    NetworkOutgoingPacket packet = udpBatchPackets[index];
                    ReturnBuffer(packet.Buffer, packet.ReturnToPool);
                    udpBatchPackets[index] = default;
                }
            }
        }

        /// <summary>
        /// 从普通数据队列连续取得当前已经存在且不超过 MTU 与包数预算的业务包。
        /// 不等待后续生产者入队，保证低频 TrySend 的首包不会因凑包增加额外延迟。
        /// </summary>
        /// <param name="firstPacket">已被发送循环取出的批量首包。</param>
        /// <param name="datagramByteCount">返回批量头、每包长度前缀和所有业务包正文合计的有效字节数。</param>
        /// <returns>实际写入 UDP 批量槽位的业务包数量，至少为一。</returns>
        private int CollectUdpDataBatch(NetworkOutgoingPacket firstPacket, out int datagramByteCount)
        {
            udpBatchPackets[0] = firstPacket;
            int packetCount = 1;
            datagramByteCount = UdpBatchDatagramCodec.HeaderByteCount
                + UdpBatchDatagramCodec.PacketLengthPrefixByteCount
                + firstPacket.Length;
            while (packetCount < UdpBatchMaximumPacketCount
                && dataPackets.TryPeek(out NetworkOutgoingPacket nextPacket, out _)
                && nextPacket.Length + UdpBatchDatagramCodec.PacketLengthPrefixByteCount
                    <= UdpBatchMaximumDatagramByteCount - datagramByteCount)
            {
                dataPackets.TryDequeue(out NetworkOutgoingPacket packet, out _);
                udpBatchPackets[packetCount] = packet;
                packetCount++;
                datagramByteCount += UdpBatchDatagramCodec.PacketLengthPrefixByteCount + packet.Length;
            }

            return packetCount;
        }

        /// <summary>
        /// 记录一个业务包从入队到开始底层写入前的等待时间。
        /// </summary>
        /// <param name="packet">需要记录队列等待时间的业务包。</param>
        /// <returns>已启用采样时返回本次开始写入的 Stopwatch tick；未启用时返回零。</returns>
        private long RecordQueueWait(NetworkOutgoingPacket packet)
        {
            if (packet.EnqueuedTicks == 0)
            {
                return 0;
            }

            long queueWaitTicks = Stopwatch.GetTimestamp() - packet.EnqueuedTicks;
            Interlocked.Add(ref totalQueueWaitTicks, queueWaitTicks);
            UpdateMaximum(ref maxQueueWaitTicks, queueWaitTicks);
            return Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// 在底层写入成功后记录一个业务包的传输等待时间。
        /// </summary>
        /// <param name="packet">需要记录传输等待时间的业务包。</param>
        /// <param name="transportStartedTicks">开始调用底层传输时的 Stopwatch tick；零表示该包未启用采样。</param>
        private void RecordTransportSend(NetworkOutgoingPacket packet, long transportStartedTicks)
        {
            if (packet.EnqueuedTicks == 0 || transportStartedTicks == 0)
            {
                return;
            }

            long transportSendTicks = Stopwatch.GetTimestamp() - transportStartedTicks;
            Interlocked.Increment(ref timingSampleCount);
            Interlocked.Add(ref totalTransportSendTicks, transportSendTicks);
            UpdateMaximum(ref maxTransportSendTicks, transportSendTicks);
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
