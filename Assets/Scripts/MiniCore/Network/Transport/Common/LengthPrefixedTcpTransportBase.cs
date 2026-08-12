using MiniCore.Threading;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using MiniCore.Core;

namespace MiniCore.Model
{
    /// <summary>
    /// 为 TCP 传输提供四字节大端长度前缀拆包和粘包处理的基类。
    /// </summary>
    public abstract class LengthPrefixedTcpTransportBase : INetworkTransport, IFramedBatchNetworkTransport, ITransportDiagnosticsNetworkTransport
    {
        #region Private 私有成员

        private const int MaximumCoalescedFrameSize = 1024 * 1024; // 小包合并长度头与正文时允许使用共享池的最大完整帧大小。
        private const int InitialReceiveBufferSize = 64 * 1024; // TCP 连续收包缓冲区的初始容量；一次 Socket 读取可解析多个小帧。
        private readonly int maxPacketSize; // 允许接收的单个业务包最大字节数。
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1); // 保证完整长度帧在同一 TCP 字节流中连续发送的异步锁。
        private readonly IMTaskExecutor networkExecutor; // 接收循环使用的执行器。

        private Socket socket; // 当前已连接的 TCP 套接字。
        private CancellationTokenSource receiveCts; // 接收循环取消令牌源。
        private int disconnected = 1; // 传输断开状态的原子标志。
        private int receiveDiagnosticsEnabled; // 是否记录仅供压测与排障使用的收包边界计数。
        private long framedPacketCount; // 已完成长度头与正文读取的完整 TCP 业务帧数量。
        private long dispatchedPacketCount; // 已完成 OnDataReceived 回调派发的完整 TCP 业务帧数量。
        private long receiveOperationCount; // 底层 Socket 接收操作完成次数。
        private long totalReceiveOperationTicks; // 底层 Socket 接收操作从发起到完成的累计等待时间。
        private long maxReceiveOperationTicks; // 单次底层 Socket 接收操作的最大等待时间。
        private long sendOperationCount; // 底层 Socket 发送操作完成次数。
        private long totalSendOperationTicks; // 底层 Socket 发送操作从发起到完成的累计等待时间。
        private long maxSendOperationTicks; // 单次底层 Socket 发送操作的最大等待时间。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 使用指定最大业务包大小创建 TCP 传输基类。
        /// </summary>
        /// <param name="maxPacketSize">单个业务包正文允许接收的最大字节数。</param>
        /// <param name="executor">接收循环使用的执行器；为空时按当前环境选择默认执行器。</param>
        protected LengthPrefixedTcpTransportBase(int maxPacketSize = 4 * 1024 * 1024, IMTaskExecutor executor = null)
        {
            this.maxPacketSize = maxPacketSize;
            networkExecutor = NetworkExecutorResolver.Resolve(executor);
        }

        /// <summary>
        /// 当前已连接套接字，供派生传输访问。
        /// </summary>
        protected Socket Socket => socket;

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 当前套接字是否已连接。
        /// </summary>
        public bool IsConnected => socket != null && socket.Connected;

        /// <summary>
        /// 接收到完整业务包时触发。
        /// </summary>
        public event Func<ReadOnlyMemory<byte>, MTask> OnDataReceived;
        /// <summary>
        /// 传输关闭时触发。
        /// </summary>
        public event Action OnDisconnected;

        /// <summary>
        /// 由派生类实现到目标主机的连接过程。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public abstract MTask ConnectAsync(string host, int port);

        /// <summary>
        /// 以长度前缀格式发送完整业务包。
        /// 小于共享池上限的业务包会将长度头和正文复制为一个完整帧后一次写入；大包仍分段写入，避免为优化小包而放大峰值内存。
        /// </summary>
        /// <param name="data">需要发送的业务包正文。</param>
        /// <returns>完整长度帧被底层 TCP 套接字写入或发生异常时完成的任务。</returns>
        public async MTask SendAsync(ArraySegment<byte> data)
        {
            CancellationToken token = MTaskExternal.GetCancellationToken();
            if (!IsConnected)
            {
                throw new InvalidOperationException($"{GetType().Name} is not connected.");
            }

            if (data.Array == null)
            {
                throw new ArgumentException("ArraySegment has no backing array.", nameof(data));
            }

            int frameLength = checked(data.Count + sizeof(int));
            if (frameLength > MaximumCoalescedFrameSize)
            {
                await SendLargeFrameAsync(data, token);
                return;
            }

            byte[] frameBuffer = ByteBufferPool.Shared.Rent(frameLength);
            try
            {
                NetBinaryCodec.WriteInt32BE(frameBuffer, 0, data.Count);
                Buffer.BlockCopy(data.Array, data.Offset, frameBuffer, sizeof(int), data.Count);
                await sendLock.WaitAsync(token);
                try
                {
                    await SendAllAsync(new ArraySegment<byte>(frameBuffer, 0, frameLength), token);
                }
                finally
                {
                    sendLock.Release();
                }
            }
            finally
            {
                ByteBufferPool.Shared.Return(frameBuffer);
            }
        }

        /// <summary>
        /// 取消接收循环、关闭套接字并通知断开事件。
        /// </summary>
        public virtual void Disconnect()
        {
            if (Interlocked.Exchange(ref disconnected, 1) != 0)
            {
                return;
            }

            var currentCts = receiveCts;
            receiveCts = null;
            try
            {
                currentCts?.Cancel();
            }
            catch
            {
            }
            finally
            {
                currentCts?.Dispose();
            }

            var currentSocket = socket;
            socket = null;
            if (currentSocket != null)
            {
                try
                {
                    currentSocket.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                }
                currentSocket.Close();
            }

            var disconnectedHandler = OnDisconnected;
            OnDisconnected = null;
            disconnectedHandler?.Invoke();
        }

        /// <summary>
        /// 释放传输资源。
        /// </summary>
        public void Dispose()
        {
            Disconnect();
        }

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 附加已连接套接字并启动后台接收循环。
        /// </summary>
        /// <param name="connectedSocket">执行该方法所需的 connectedSocket 参数。</param>
        protected void AttachConnectedSocket(Socket connectedSocket)
        {
            CancellationToken token = MTaskExternal.GetCancellationToken();
            socket = connectedSocket ?? throw new ArgumentNullException(nameof(connectedSocket));
            Interlocked.Exchange(ref disconnected, 0);

            receiveCts = token == default
                ? new CancellationTokenSource()
                : CancellationTokenSource.CreateLinkedTokenSource(token);
            ReceiveLoopAsync(receiveCts.Token).Forget();
        }

        /// <summary>
        /// 向订阅者派发已完成拆包的数据。
        /// </summary>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        protected MTask DispatchDataReceivedAsync(ReadOnlyMemory<byte> data)
        {
            return TransportEventDispatcher.DispatchAsync(OnDataReceived, data);
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 写入已由会话发送器完成长度前缀封装的一组连续 TCP 帧。
        /// 该显式接口实现不对外暴露，避免其他传输或普通调用方跳过长度前缀协议。
        /// </summary>
        /// <param name="frames">按顺序连续排列的一个或多个完整长度帧。</param>
        /// <returns>全部帧写入完成或出现异常时完成的任务。</returns>
        async MTask IFramedBatchNetworkTransport.SendFramedBatchAsync(ArraySegment<byte> frames)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException($"{GetType().Name} is not connected.");
            }

            if (frames.Array == null || frames.Count <= 0)
            {
                throw new ArgumentException("批量 TCP 帧必须具有有效的底层数组和长度。", nameof(frames));
            }

            CancellationToken token = MTaskExternal.GetCancellationToken();
            await sendLock.WaitAsync(token);
            try
            {
                await SendAllAsync(frames, token);
            }
            finally
            {
                sendLock.Release();
            }
        }

        /// <summary>
        /// 启用或关闭 TCP 收包边界诊断，并清空上一统计周期的完整帧与回调计数。
        /// </summary>
        /// <param name="enabled">为 true 时记录完整帧读取和收包回调派发完成数量。</param>
        void ITransportDiagnosticsNetworkTransport.SetTransportDiagnosticsEnabled(bool enabled)
        {
            Interlocked.Exchange(ref receiveDiagnosticsEnabled, enabled ? 1 : 0);
            Interlocked.Exchange(ref framedPacketCount, 0);
            Interlocked.Exchange(ref dispatchedPacketCount, 0);
            Interlocked.Exchange(ref receiveOperationCount, 0);
            Interlocked.Exchange(ref totalReceiveOperationTicks, 0);
            Interlocked.Exchange(ref maxReceiveOperationTicks, 0);
            Interlocked.Exchange(ref sendOperationCount, 0);
            Interlocked.Exchange(ref totalSendOperationTicks, 0);
            Interlocked.Exchange(ref maxSendOperationTicks, 0);
        }

        /// <summary>
        /// 获取当前 TCP 收包边界诊断快照。
        /// </summary>
        /// <returns>不转移任何接收缓冲区所有权的完整帧与回调计数。</returns>
        NetworkTransportReceiveSnapshot ITransportDiagnosticsNetworkTransport.CaptureReceiveDiagnostics()
        {
            long operations = Interlocked.Read(ref receiveOperationCount);
            long totalTicks = Interlocked.Read(ref totalReceiveOperationTicks);
            return new NetworkTransportReceiveSnapshot(
                Interlocked.Read(ref framedPacketCount),
                Interlocked.Read(ref dispatchedPacketCount),
                operations,
                operations == 0 ? 0d : totalTicks * 1000d / Stopwatch.Frequency / operations,
                Interlocked.Read(ref maxReceiveOperationTicks) * 1000d / Stopwatch.Frequency);
        }

        /// <summary>
        /// 获取当前 TCP 底层 Socket 发送操作诊断快照。
        /// </summary>
        /// <returns>不转移任何发送缓冲区所有权的 Socket 发送操作计数与等待时间。</returns>
        NetworkTransportSendSnapshot ITransportDiagnosticsNetworkTransport.CaptureSendDiagnostics()
        {
            long operations = Interlocked.Read(ref sendOperationCount);
            long totalTicks = Interlocked.Read(ref totalSendOperationTicks);
            return new NetworkTransportSendSnapshot(
                operations,
                operations == 0 ? 0d : totalTicks * 1000d / Stopwatch.Frequency / operations,
                Interlocked.Read(ref maxSendOperationTicks) * 1000d / Stopwatch.Frequency);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 执行 ReceiveLoopAsync 相关处理。
        /// </summary>
        /// <returns>执行处理后的结果。</returns>
        private async MTask ReceiveLoopAsync(CancellationToken token)
        {
            byte[] receiveBuffer = ByteBufferPool.Shared.Rent(InitialReceiveBufferSize);
            int bufferedByteCount = 0;
            try
            {
                await MTask.SwitchTo(networkExecutor);
                while (!token.IsCancellationRequested && IsConnected)
                {
                    if (bufferedByteCount == receiveBuffer.Length
                        && !TryExpandReceiveBuffer(ref receiveBuffer, bufferedByteCount))
                    {
                        break;
                    }

                    var currentSocket = socket;
                    if (currentSocket == null)
                    {
                        break;
                    }

                    long startedTicks = Volatile.Read(ref receiveDiagnosticsEnabled) != 0 ? Stopwatch.GetTimestamp() : 0;
                    int received = await currentSocket.ReceiveAsync(
                        new ArraySegment<byte>(receiveBuffer, bufferedByteCount, receiveBuffer.Length - bufferedByteCount),
                        SocketFlags.None,
                        token).ConfigureAwait(false);
                    RecordReceiveOperation(startedTicks);
                    if (received == 0)
                    {
                        break;
                    }

                    bufferedByteCount += received;
                    int consumedByteCount = await DispatchBufferedFramesAsync(receiveBuffer, bufferedByteCount);
                    if (consumedByteCount < 0)
                    {
                        break;
                    }

                    if (consumedByteCount > 0)
                    {
                        int remainingByteCount = bufferedByteCount - consumedByteCount;
                        if (remainingByteCount > 0)
                        {
                            Buffer.BlockCopy(receiveBuffer, consumedByteCount, receiveBuffer, 0, remainingByteCount);
                        }

                        bufferedByteCount = remainingByteCount;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException ex) when (IsExpectedSocketClosure(ex))
            {
            }
            catch (SocketException ex)
            {
                if (!IsActiveDisconnect())
                {
                    LogSwitch.Warning($"{GetType().Name} receive loop error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                if (!IsActiveDisconnect())
                {
                    LogSwitch.Warning($"{GetType().Name} receive loop error: {ex.Message}");
                }
            }
            finally
            {
                ByteBufferPool.Shared.Return(receiveBuffer);
                Disconnect();
            }
        }

        /// <summary>
        /// 在成功读取一条完整 TCP 业务帧后记录诊断计数。
        /// </summary>
        private void RecordFramedPacket()
        {
            if (Volatile.Read(ref receiveDiagnosticsEnabled) != 0)
            {
                Interlocked.Increment(ref framedPacketCount);
            }
        }

        /// <summary>
        /// 在当前完整 TCP 业务帧的收包回调全部完成后记录诊断计数。
        /// </summary>
        private void RecordDispatchedPacket()
        {
            if (Volatile.Read(ref receiveDiagnosticsEnabled) != 0)
            {
                Interlocked.Increment(ref dispatchedPacketCount);
            }
        }

        /// <summary>
        /// 在一次底层 Socket 接收操作成功完成后记录其等待时间。
        /// </summary>
        /// <param name="startedTicks">发起 Socket 接收操作时的 Stopwatch tick；零表示未启用诊断。</param>
        private void RecordReceiveOperation(long startedTicks)
        {
            if (startedTicks == 0)
            {
                return;
            }

            long elapsedTicks = Stopwatch.GetTimestamp() - startedTicks;
            Interlocked.Increment(ref receiveOperationCount);
            Interlocked.Add(ref totalReceiveOperationTicks, elapsedTicks);
            UpdateMaximum(ref maxReceiveOperationTicks, elapsedTicks);
        }

        /// <summary>
        /// 在一次底层 Socket 发送操作成功完成后记录其等待时间。
        /// </summary>
        /// <param name="startedTicks">发起 Socket 发送操作时的 Stopwatch tick；零表示未启用诊断。</param>
        private void RecordSendOperation(long startedTicks)
        {
            if (startedTicks == 0)
            {
                return;
            }

            long elapsedTicks = Stopwatch.GetTimestamp() - startedTicks;
            Interlocked.Increment(ref sendOperationCount);
            Interlocked.Add(ref totalSendOperationTicks, elapsedTicks);
            UpdateMaximum(ref maxSendOperationTicks, elapsedTicks);
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
        /// 执行 IsActiveDisconnect 相关处理。
        /// </summary>
        /// <returns>执行处理后的结果。</returns>
        private bool IsActiveDisconnect()
        {
            return Interlocked.CompareExchange(ref disconnected, 0, 0) != 0;
        }

        /// <summary>
        /// 执行 IsExpectedSocketClosure 相关处理。
        /// </summary>
        /// <param name="ex">执行该方法所需的 ex 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private static bool IsExpectedSocketClosure(SocketException ex)
        {
            return ex.SocketErrorCode == SocketError.OperationAborted
                || ex.SocketErrorCode == SocketError.Interrupted
                || ex.SocketErrorCode == SocketError.ConnectionAborted
                || ex.SocketErrorCode == SocketError.ConnectionReset
                || ex.SocketErrorCode == SocketError.Shutdown
                || ex.SocketErrorCode == SocketError.NotSocket;
        }

        /// <summary>
        /// 从当前连续接收缓冲区依次解析全部完整长度帧，并在回调结束前保持业务正文缓冲区有效。
        /// </summary>
        /// <param name="receiveBuffer">包含一个或多个 TCP 粘包帧的连续接收缓冲区。</param>
        /// <param name="bufferedByteCount">缓冲区中当前有效字节数。</param>
        /// <returns>已从缓冲区头部消费的字节数；协议长度非法时返回负数。</returns>
        private async MTask<int> DispatchBufferedFramesAsync(byte[] receiveBuffer, int bufferedByteCount)
        {
            int consumedByteCount = 0;
            while (bufferedByteCount - consumedByteCount >= sizeof(int))
            {
                int bodyLength = NetBinaryCodec.ReadInt32BE(receiveBuffer, consumedByteCount);
                if (bodyLength <= 0 || bodyLength > maxPacketSize)
                {
                    return -1;
                }

                int frameLength = checked(sizeof(int) + bodyLength);
                if (bufferedByteCount - consumedByteCount < frameLength)
                {
                    break;
                }

                byte[] bodyBuffer = ByteBufferPool.Shared.Rent(bodyLength);
                try
                {
                    Buffer.BlockCopy(receiveBuffer, consumedByteCount + sizeof(int), bodyBuffer, 0, bodyLength);
                    RecordFramedPacket();
                    await DispatchDataReceivedAsync(new ReadOnlyMemory<byte>(bodyBuffer, 0, bodyLength));
                    RecordDispatchedPacket();
                }
                finally
                {
                    ByteBufferPool.Shared.Return(bodyBuffer);
                }

                consumedByteCount += frameLength;
            }

            return consumedByteCount;
        }

        /// <summary>
        /// 当连续接收缓冲区被未完成的大帧占满时扩容，并保留其中尚未解析的字节。
        /// </summary>
        /// <param name="receiveBuffer">当前连续接收缓冲区；成功时替换为容量更大的缓冲区。</param>
        /// <param name="bufferedByteCount">当前缓冲区中尚未解析的有效字节数。</param>
        /// <returns>成功扩容时返回 true；协议长度非法或达到单包上限时返回 false。</returns>
        private bool TryExpandReceiveBuffer(ref byte[] receiveBuffer, int bufferedByteCount)
        {
            if (bufferedByteCount < sizeof(int))
            {
                return false;
            }

            int bodyLength = NetBinaryCodec.ReadInt32BE(receiveBuffer, 0);
            if (bodyLength <= 0 || bodyLength > maxPacketSize)
            {
                return false;
            }

            int requiredFrameLength = checked(sizeof(int) + bodyLength);
            if (requiredFrameLength <= receiveBuffer.Length)
            {
                return false;
            }

            int expandedSize = receiveBuffer.Length;
            while (expandedSize < requiredFrameLength)
            {
                if (expandedSize > maxPacketSize / 2)
                {
                    expandedSize = maxPacketSize + sizeof(int);
                    break;
                }

                expandedSize <<= 1;
            }

            byte[] expandedBuffer = ByteBufferPool.Shared.Rent(expandedSize);
            Buffer.BlockCopy(receiveBuffer, 0, expandedBuffer, 0, bufferedByteCount);
            ByteBufferPool.Shared.Return(receiveBuffer);
            receiveBuffer = expandedBuffer;
            return true;
        }

        /// <summary>
        /// 对超过共享缓冲池帧上限的大包保持长度头与正文分段写入，避免额外复制和大数组保留。
        /// </summary>
        /// <param name="data">需要发送的业务包正文。</param>
        /// <param name="token">控制发送等待的取消令牌。</param>
        /// <returns>长度头与正文均写入完成或出现异常时完成的任务。</returns>
        private async MTask SendLargeFrameAsync(ArraySegment<byte> data, CancellationToken token)
        {
            byte[] lengthBytes = ByteBufferPool.Shared.Rent(sizeof(int));
            try
            {
                NetBinaryCodec.WriteInt32BE(lengthBytes, 0, data.Count);
                await sendLock.WaitAsync(token);
                try
                {
                    await SendAllAsync(new ArraySegment<byte>(lengthBytes, 0, sizeof(int)), token);
                    await SendAllAsync(data, token);
                }
                finally
                {
                    sendLock.Release();
                }
            }
            finally
            {
                ByteBufferPool.Shared.Return(lengthBytes);
            }
        }

        /// <summary>
        /// 执行 SendAllAsync 相关处理。
        /// </summary>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private async MTask SendAllAsync(ArraySegment<byte> data, CancellationToken token)
        {
            if (data.Array == null)
            {
                throw new ArgumentException("ArraySegment has no backing array.", nameof(data));
            }

            int sent = 0;
            while (sent < data.Count)
            {
                var currentSocket = socket;
                if (currentSocket == null)
                {
                    throw new InvalidOperationException($"{GetType().Name} socket is null.");
                }

                long startedTicks = Volatile.Read(ref receiveDiagnosticsEnabled) != 0 ? Stopwatch.GetTimestamp() : 0;
                int written = await currentSocket.SendAsync(
                    new ArraySegment<byte>(data.Array, data.Offset + sent, data.Count - sent),
                    SocketFlags.None,
                    token).ConfigureAwait(false);
                RecordSendOperation(startedTicks);

                if (written <= 0)
                {
                    throw new SocketException((int)SocketError.ConnectionAborted);
                }

                sent += written;
            }
        }

        #endregion
    }
}
