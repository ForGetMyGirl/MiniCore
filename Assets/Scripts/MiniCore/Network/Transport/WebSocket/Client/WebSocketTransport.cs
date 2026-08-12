using System;
using System.Collections.Generic;
using System.Threading;
using MiniCore.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// 以 WebSocket 二进制消息承载四字节大端长度帧的统一客户端传输。
    /// </summary>
    public sealed class WebSocketTransport : INetworkTransport
    {
        #region Private 私有成员

        private const int DefaultMaximumPacketSize = 4 * 1024 * 1024; // 默认单个业务包上限。
        private const int DefaultMaximumMessageSize = 16 * 1024 * 1024; // 默认单条 WebSocket 消息上限。
        private const int DefaultMaximumPendingPacketCount = 1024; // 默认待派发业务包数量上限。
        private const int InitialReceiveBufferSize = 64 * 1024; // 流式拼包缓冲区初始大小。
        private readonly object receiveGate = new object(); // 保护跨回调拼包缓冲区。
        private readonly IWebSocketClientAdapter clientAdapter; // 当前平台的 WebSocket 客户端底层适配器。
        private readonly int maximumPacketSize; // 单个业务包正文允许的最大字节数。
        private readonly int maximumMessageSize; // 单条 WebSocket 二进制消息允许的最大字节数。
        private readonly int maximumPendingPacketCount; // 等待串行派发的业务包数量上限。
        private readonly Queue<ArraySegment<byte>> pendingPackets = new Queue<ArraySegment<byte>>(16); // 等待串行派发的池化业务包。
        private byte[] receiveBuffer; // 跨 WebSocket 回调保留的长度帧字节。
        private int receiveCount; // 拼包缓冲区当前有效字节数。
        private int bufferedPacketBytes; // 队列和当前派发包合计占用的有效字节数。
        private int bufferedPacketCount; // 队列和当前派发包合计数量。
        private bool dispatching; // 是否已有唯一的收包派发泵运行。
        private int disconnected = 1; // 断开状态的原子标志。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 创建使用当前平台客户端适配器的 WebSocket 传输。
        /// </summary>
        /// <param name="clientAdapter">客户端底层适配器；为空时从注册表创建。</param>
        /// <param name="maximumPacketSize">单个业务包正文允许的最大字节数。</param>
        /// <param name="maximumMessageSize">单条 WebSocket 消息上限，可容纳多个业务帧。</param>
        /// <param name="maximumPendingPacketCount">等待串行派发的业务包数量上限。</param>
        public WebSocketTransport(
            IWebSocketClientAdapter clientAdapter = null,
            int maximumPacketSize = DefaultMaximumPacketSize,
            int maximumMessageSize = DefaultMaximumMessageSize,
            int maximumPendingPacketCount = DefaultMaximumPendingPacketCount)
        {
            if (maximumPacketSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumPacketSize));
            }

            int maximumFrameSize = checked(maximumPacketSize + sizeof(int));
            if (maximumMessageSize < maximumFrameSize)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumMessageSize),
                    "WebSocket 消息上限不能小于单个完整业务帧。");
            }

            if (maximumPendingPacketCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumPendingPacketCount));
            }

            this.clientAdapter = clientAdapter ?? WebSocketClientAdapterRegistry.CreateClient();
            this.maximumPacketSize = maximumPacketSize;
            this.maximumMessageSize = maximumMessageSize;
            this.maximumPendingPacketCount = maximumPendingPacketCount;
            receiveBuffer = ByteBufferPool.Shared.Rent(Math.Min(InitialReceiveBufferSize, maximumFrameSize));
            this.clientAdapter.BinaryMessageReceived += HandleBinaryMessageReceived;
            this.clientAdapter.Closed += HandleAdapterClosed;
        }

        /// <summary>
        /// 获取底层 WebSocket 是否已经打开。
        /// </summary>
        public bool IsConnected => Volatile.Read(ref disconnected) == 0 && clientAdapter.IsOpen;

        /// <summary>
        /// 完成长帧拆包后触发。
        /// </summary>
        public event Func<ReadOnlyMemory<byte>, MTask> OnDataReceived;

        /// <summary>
        /// 底层连接关闭时触发。
        /// </summary>
        public event Action OnDisconnected;

        /// <summary>
        /// 连接由主机和端口组成的 WS 地址。
        /// 完整路径场景请使用 <see cref="ConnectAsync(string)"/>。
        /// </summary>
        /// <param name="host">远端主机名或 IP。</param>
        /// <param name="port">远端端口。</param>
        /// <returns>握手完成或失败时结束的任务。</returns>
        public MTask ConnectAsync(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentException("WebSocket 主机不能为空。", nameof(host));
            }

            return ConnectAsync($"ws://{host}:{port}/");
        }

        /// <summary>
        /// 连接包含协议、主机、端口和路径的完整 WS/WSS 地址。
        /// </summary>
        /// <param name="url">完整 WebSocket 地址。</param>
        /// <returns>握手完成或失败时结束的任务。</returns>
        public async MTask ConnectAsync(string url)
        {
            Disconnect();
            Interlocked.Exchange(ref disconnected, 0);
            try
            {
                await clientAdapter.ConnectAsync(url, maximumMessageSize);
            }
            catch
            {
                Interlocked.Exchange(ref disconnected, 1);
                throw;
            }
        }

        /// <summary>
        /// 为业务包添加四字节大端长度头并作为一条二进制 WebSocket 消息发送。
        /// </summary>
        /// <param name="data">完整业务包正文。</param>
        /// <returns>底层发送完成或失败时结束的任务。</returns>
        public async MTask SendAsync(ArraySegment<byte> data)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("WebSocket 尚未连接。");
            }

            if (data.Array == null || data.Count <= 0 || data.Count > maximumPacketSize)
            {
                throw new ArgumentOutOfRangeException(nameof(data), "WebSocket 业务包长度无效。");
            }

            int frameLength = checked(data.Count + sizeof(int));
            byte[] frame = ByteBufferPool.Shared.Rent(frameLength);
            try
            {
                NetBinaryCodec.WriteInt32BE(frame, 0, data.Count);
                Buffer.BlockCopy(data.Array, data.Offset, frame, sizeof(int), data.Count);
                await clientAdapter.SendAsync(new ArraySegment<byte>(frame, 0, frameLength));
            }
            finally
            {
                ByteBufferPool.Shared.Return(frame);
            }
        }

        /// <summary>
        /// 关闭底层连接并清空尚未形成完整业务包的字节。
        /// </summary>
        public void Disconnect()
        {
            if (Interlocked.Exchange(ref disconnected, 1) != 0)
            {
                return;
            }

            lock (receiveGate)
            {
                ResetReceiveStateLocked();
            }

            clientAdapter.Close();
            OnDisconnected?.Invoke();
        }

        /// <summary>
        /// 释放客户端适配器和拼包缓冲区。
        /// </summary>
        public void Dispose()
        {
            Disconnect();
            clientAdapter.BinaryMessageReceived -= HandleBinaryMessageReceived;
            clientAdapter.Closed -= HandleAdapterClosed;
            clientAdapter.Dispose();

            byte[] buffer;
            lock (receiveGate)
            {
                ResetReceiveStateLocked();
                buffer = receiveBuffer;
                receiveBuffer = null;
            }

            if (buffer != null)
            {
                ByteBufferPool.Shared.Return(buffer);
            }

            OnDataReceived = null;
            OnDisconnected = null;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将平台回调字节追加到流式缓冲区，拆出全部完整长度帧后异步派发。
        /// </summary>
        /// <param name="message">一条完整 WebSocket 二进制消息。</param>
        private void HandleBinaryMessageReceived(ArraySegment<byte> message)
        {
            if (message.Array == null || message.Count <= 0 || Volatile.Read(ref disconnected) != 0)
            {
                return;
            }

            bool shouldClose = false;
            bool shouldStartDispatch = false;
            lock (receiveGate)
            {
                if (Volatile.Read(ref disconnected) != 0 || receiveBuffer == null)
                {
                    return;
                }

                if (!TryAppendMessageLocked(message))
                {
                    Interlocked.Exchange(ref disconnected, 1);
                    ResetReceiveStateLocked();
                    shouldClose = true;
                }
                else if (!dispatching && pendingPackets.Count > 0)
                {
                    dispatching = true;
                    shouldStartDispatch = true;
                }
            }

            if (shouldClose)
            {
                clientAdapter.Close();
                OnDisconnected?.Invoke();
            }
            else if (shouldStartDispatch)
            {
                DispatchQueuedPacketsAsync().Forget();
            }
        }

        /// <summary>
        /// 从唯一队列按连接到达顺序派发业务包并归还池化数组。
        /// </summary>
        /// <returns>队列清空、连接关闭或派发失败时结束的任务。</returns>
        private async MTask DispatchQueuedPacketsAsync()
        {
            try
            {
                while (true)
                {
                    ArraySegment<byte> packet;
                    lock (receiveGate)
                    {
                        if (Volatile.Read(ref disconnected) != 0 || pendingPackets.Count == 0)
                        {
                            dispatching = false;
                            return;
                        }

                        packet = pendingPackets.Dequeue();
                    }

                    try
                    {
                        await TransportEventDispatcher.DispatchAsync(
                            OnDataReceived,
                            new ReadOnlyMemory<byte>(packet.Array, packet.Offset, packet.Count));
                    }
                    finally
                    {
                        lock (receiveGate)
                        {
                            bufferedPacketBytes -= packet.Count;
                            bufferedPacketCount--;
                        }

                        ByteBufferPool.Shared.Return(packet.Array);
                    }
                }
            }
            catch
            {
                Disconnect();
                lock (receiveGate)
                {
                    dispatching = false;
                    ReleasePendingPacketsLocked();
                }

                throw;
            }
        }

        /// <summary>
        /// 流式解析一条 WebSocket 消息，可连续拆出多帧，也可保留一帧残片等待后续回调。
        /// </summary>
        /// <param name="message">当前二进制消息。</param>
        /// <returns>所有长度合法且待派发缓冲未超过上限时返回 true。</returns>
        private bool TryAppendMessageLocked(ArraySegment<byte> message)
        {
            int sourceOffset = message.Offset;
            int sourceRemaining = message.Count;
            while (sourceRemaining > 0)
            {
                if (receiveCount == 0 && sourceRemaining >= sizeof(int))
                {
                    int packetLength = NetBinaryCodec.ReadInt32BE(message.Array, sourceOffset);
                    if (!IsPacketLengthValid(packetLength))
                    {
                        return false;
                    }

                    int frameLength = checked(packetLength + sizeof(int));
                    if (sourceRemaining >= frameLength)
                    {
                        if (!TryQueuePacketLocked(message.Array, sourceOffset + sizeof(int), packetLength))
                        {
                            return false;
                        }

                        sourceOffset += frameLength;
                        sourceRemaining -= frameLength;
                        continue;
                    }

                    EnsureReceiveCapacity(frameLength);
                    Buffer.BlockCopy(message.Array, sourceOffset, receiveBuffer, 0, sourceRemaining);
                    receiveCount = sourceRemaining;
                    return true;
                }

                if (receiveCount < sizeof(int))
                {
                    int headerBytes = Math.Min(sizeof(int) - receiveCount, sourceRemaining);
                    Buffer.BlockCopy(message.Array, sourceOffset, receiveBuffer, receiveCount, headerBytes);
                    receiveCount += headerBytes;
                    sourceOffset += headerBytes;
                    sourceRemaining -= headerBytes;
                    if (receiveCount < sizeof(int))
                    {
                        return true;
                    }
                }

                int bufferedPacketLength = NetBinaryCodec.ReadInt32BE(receiveBuffer, 0);
                if (!IsPacketLengthValid(bufferedPacketLength))
                {
                    return false;
                }

                int bufferedFrameLength = checked(bufferedPacketLength + sizeof(int));
                EnsureReceiveCapacity(bufferedFrameLength);
                int copyLength = Math.Min(bufferedFrameLength - receiveCount, sourceRemaining);
                if (copyLength > 0)
                {
                    Buffer.BlockCopy(message.Array, sourceOffset, receiveBuffer, receiveCount, copyLength);
                    receiveCount += copyLength;
                    sourceOffset += copyLength;
                    sourceRemaining -= copyLength;
                }

                if (receiveCount < bufferedFrameLength)
                {
                    return true;
                }

                if (!TryQueuePacketLocked(receiveBuffer, sizeof(int), bufferedPacketLength))
                {
                    return false;
                }

                receiveCount = 0;
            }

            return true;
        }

        /// <summary>
        /// 校验单个业务包长度是否位于配置范围内。
        /// </summary>
        /// <param name="packetLength">长度头声明的正文大小。</param>
        /// <returns>长度有效时返回 true。</returns>
        private bool IsPacketLengthValid(int packetLength)
        {
            return packetLength > 0 && packetLength <= maximumPacketSize;
        }

        /// <summary>
        /// 复制并登记一个待派发业务包，同时执行数量与字节双重背压检查。
        /// </summary>
        /// <param name="source">业务包来源数组。</param>
        /// <param name="offset">正文起始偏移。</param>
        /// <param name="packetLength">正文有效长度。</param>
        /// <returns>成功进入有界队列时返回 true。</returns>
        private bool TryQueuePacketLocked(byte[] source, int offset, int packetLength)
        {
            if (bufferedPacketCount >= maximumPendingPacketCount
                || bufferedPacketBytes > maximumMessageSize - packetLength)
            {
                return false;
            }

            byte[] packet = ByteBufferPool.Shared.Rent(packetLength);
            try
            {
                Buffer.BlockCopy(source, offset, packet, 0, packetLength);
                pendingPackets.Enqueue(new ArraySegment<byte>(packet, 0, packetLength));
                bufferedPacketBytes += packetLength;
                bufferedPacketCount++;
                return true;
            }
            catch
            {
                ByteBufferPool.Shared.Return(packet);
                throw;
            }
        }

        /// <summary>
        /// 扩大单帧残片缓冲区，容量始终不超过单帧上限。
        /// </summary>
        /// <param name="required">当前残片所属完整帧需要的容量。</param>
        private void EnsureReceiveCapacity(int required)
        {
            if (receiveBuffer.Length >= required)
            {
                return;
            }

            int newSize = receiveBuffer.Length;
            while (newSize < required)
            {
                newSize = newSize > required / 2 ? required : newSize * 2;
            }

            byte[] expanded = ByteBufferPool.Shared.Rent(newSize);
            Buffer.BlockCopy(receiveBuffer, 0, expanded, 0, receiveCount);
            ByteBufferPool.Shared.Return(receiveBuffer);
            receiveBuffer = expanded;
        }

        /// <summary>
        /// 清空单帧残片和尚未派发的池化业务包。
        /// </summary>
        private void ResetReceiveStateLocked()
        {
            receiveCount = 0;
            ReleasePendingPacketsLocked();
        }

        /// <summary>
        /// 归还队列内尚未由派发泵取得所有权的池化业务包。
        /// </summary>
        private void ReleasePendingPacketsLocked()
        {
            while (pendingPackets.Count > 0)
            {
                ArraySegment<byte> packet = pendingPackets.Dequeue();
                bufferedPacketBytes -= packet.Count;
                bufferedPacketCount--;
                ByteBufferPool.Shared.Return(packet.Array);
            }
        }

        /// <summary>
        /// 响应客户端适配器关闭事件并保证上层只收到一次断开通知。
        /// </summary>
        /// <param name="code">RFC 6455 关闭状态码。</param>
        /// <param name="reason">远端关闭原因。</param>
        private void HandleAdapterClosed(ushort code, string reason)
        {
            bool shouldNotify = Interlocked.Exchange(ref disconnected, 1) == 0;
            lock (receiveGate)
            {
                ResetReceiveStateLocked();
            }

            if (shouldNotify)
            {
                OnDisconnected?.Invoke();
            }
        }

        #endregion
    }
}
