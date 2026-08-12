#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Generic;
using MiniCore.Threading;
using WebSocketSharp;

namespace MiniCore.Model
{
    /// <summary>
    /// 封装单条原生 WebSocket 服务端连接并负责长度帧拼包。
    /// </summary>
    internal sealed class WebSocketServerSession : IServerSession
    {
        #region Private 私有成员

        private static readonly ExactSendBufferPool SendBufferPool = new ExactSendBufferPool(); // websocket-sharp 精确长度发送数组池。
        private readonly object receiveGate = new object(); // 保护拼包状态和单消费者队列。
        private readonly NativeWebSocketBehavior behavior; // 底层 websocket-sharp 会话行为。
        private readonly int maximumPacketSize; // 单个业务包正文大小上限。
        private readonly int maximumMessageSize; // 单条 WebSocket 二进制消息大小上限。
        private readonly int maximumPendingPacketCount; // 等待串行派发的业务包数量上限。
        private readonly Queue<ArraySegment<byte>> pendingPackets = new Queue<ArraySegment<byte>>(16); // 待串行派发的池化业务包。
        private byte[] receiveBuffer; // 仅保留一条跨消息未完成的长度帧。
        private int receiveCount; // 单帧残片的当前有效字节数。
        private int bufferedPacketBytes; // 队列和当前派发包合计占用的有效字节数。
        private int bufferedPacketCount; // 队列和当前派发包合计数量。
        private bool dispatching; // 是否已有唯一的收包派发泵运行。
        private bool closed; // 会话关闭状态。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 完成长帧拆包后触发。
        /// </summary>
        internal event Func<IServerSession, ReadOnlyMemory<byte>, MTask> DataReceived;

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取服务端会话标识。
        /// </summary>
        public string SessionId { get; }

        /// <summary>
        /// 获取底层连接是否仍可发送。
        /// </summary>
        public bool IsConnected
        {
            get
            {
                lock (receiveGate)
                {
                    return !closed && behavior.IsOpen;
                }
            }
        }

        /// <summary>
        /// 会话关闭时触发。
        /// </summary>
        public event Action OnDisconnected;

        /// <summary>
        /// 创建已经完成握手的服务端 WebSocket 会话。
        /// </summary>
        /// <param name="behavior">底层会话行为。</param>
        /// <param name="maximumPacketSize">单个业务包正文大小上限。</param>
        /// <param name="maximumMessageSize">单条 WebSocket 消息大小上限。</param>
        /// <param name="maximumPendingPacketCount">等待串行派发的业务包数量上限。</param>
        internal WebSocketServerSession(
            NativeWebSocketBehavior behavior,
            int maximumPacketSize,
            int maximumMessageSize,
            int maximumPendingPacketCount)
        {
            this.behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
            this.maximumPacketSize = maximumPacketSize;
            this.maximumMessageSize = maximumMessageSize;
            this.maximumPendingPacketCount = maximumPendingPacketCount;
            SessionId = behavior.SessionId;
            receiveBuffer = ByteBufferPool.Shared.Rent(
                Math.Min(64 * 1024, checked(maximumPacketSize + sizeof(int))));
        }

        /// <summary>
        /// 添加四字节大端长度头后发送一条完整二进制 WebSocket 消息。
        /// </summary>
        /// <param name="data">完整业务包正文。</param>
        /// <returns>底层发送回调完成或失败时结束的任务。</returns>
        public MTask SendAsync(ArraySegment<byte> data)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("WebSocket 服务端会话已经关闭。");
            }

            if (data.Array == null || data.Count <= 0 || data.Count > maximumPacketSize)
            {
                throw new ArgumentOutOfRangeException(nameof(data));
            }

            int frameLength = checked(data.Count + sizeof(int));
            byte[] frame = SendBufferPool.Rent(frameLength);
            NetBinaryCodec.WriteInt32BE(frame, 0, data.Count);
            Buffer.BlockCopy(data.Array, data.Offset, frame, sizeof(int), data.Count);
            var completion = new MTaskCompletionSource<bool>();
            int returned = 0;
            try
            {
                behavior.SendBinaryAsync(frame, succeeded =>
                {
                    if (System.Threading.Interlocked.Exchange(ref returned, 1) == 0)
                    {
                        SendBufferPool.Return(frame);
                    }

                    if (succeeded)
                    {
                        completion.TrySetResult(true);
                    }
                    else
                    {
                        completion.TrySetException(new InvalidOperationException("WebSocket 服务端消息发送失败。"));
                    }
                });
            }
            catch
            {
                if (System.Threading.Interlocked.Exchange(ref returned, 1) == 0)
                {
                    SendBufferPool.Return(frame);
                }

                throw;
            }

            return AwaitSentAsync(completion);
        }

        /// <summary>
        /// 使用正常关闭状态结束当前会话。
        /// </summary>
        public void Close()
        {
            lock (receiveGate)
            {
                if (!TryMarkClosedLocked())
                {
                    return;
                }
            }

            behavior.CloseSession(CloseStatusCode.Normal, "Closing");
            OnDisconnected?.Invoke();
        }

        /// <summary>
        /// 释放当前会话资源。
        /// </summary>
        public void Dispose()
        {
            Close();
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 追加一条已校验的二进制 WebSocket 消息并按长度帧派发业务包。
        /// </summary>
        /// <param name="message">一条完整二进制消息。</param>
        internal void PushBinaryMessage(byte[] message)
        {
            if (message == null || message.Length == 0)
            {
                return;
            }

            bool shouldClose = false;
            bool shouldStartDispatch = false;
            CloseStatusCode closeCode = CloseStatusCode.ProtocolError;
            string closeReason = "Invalid frame length.";
            lock (receiveGate)
            {
                if (closed || receiveBuffer == null)
                {
                    return;
                }

                if (message.Length > maximumMessageSize)
                {
                    closeCode = CloseStatusCode.TooBig;
                    closeReason = "Message is too large.";
                    shouldClose = TryMarkClosedLocked();
                }
                else if (!TryAppendMessageLocked(message, out closeCode, out closeReason))
                {
                    shouldClose = TryMarkClosedLocked();
                }
                else if (!dispatching && pendingPackets.Count > 0)
                {
                    dispatching = true;
                    shouldStartDispatch = true;
                }
            }

            if (shouldClose)
            {
                behavior.CloseSession(closeCode, closeReason);
                OnDisconnected?.Invoke();
            }
            else if (shouldStartDispatch)
            {
                DispatchQueuedPacketsAsync().Forget();
            }
        }

        /// <summary>
        /// 响应远端或监听器关闭，不再次发起关闭握手。
        /// </summary>
        internal void NotifyClosed()
        {
            lock (receiveGate)
            {
                if (!TryMarkClosedLocked())
                {
                    return;
                }
            }

            OnDisconnected?.Invoke();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 等待发送完成源并丢弃内部布尔结果。
        /// </summary>
        /// <param name="completion">发送完成源。</param>
        /// <returns>发送结果任务。</returns>
        private static async MTask AwaitSentAsync(MTaskCompletionSource<bool> completion)
        {
            await completion.Task;
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
                        if (closed || pendingPackets.Count == 0)
                        {
                            dispatching = false;
                            return;
                        }

                        packet = pendingPackets.Dequeue();
                    }

                    try
                    {
                        await TransportEventDispatcher.DispatchAsync(
                            DataReceived,
                            (IServerSession)this,
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
                CloseAfterDispatchFailure();
                throw;
            }
        }

        /// <summary>
        /// 流式解析一条 WebSocket 消息，可连续拆出多帧，也可保留一帧残片等待后续回调。
        /// </summary>
        /// <param name="message">当前二进制消息。</param>
        /// <param name="closeCode">解析失败时建议使用的关闭状态码。</param>
        /// <param name="closeReason">解析失败时发送给对端的简短原因。</param>
        /// <returns>所有长度合法且待派发缓冲未超过上限时返回 true。</returns>
        private bool TryAppendMessageLocked(
            byte[] message,
            out CloseStatusCode closeCode,
            out string closeReason)
        {
            closeCode = CloseStatusCode.ProtocolError;
            closeReason = "Invalid frame length.";
            int sourceOffset = 0;
            int sourceRemaining = message.Length;
            while (sourceRemaining > 0)
            {
                if (receiveCount == 0 && sourceRemaining >= sizeof(int))
                {
                    int packetLength = NetBinaryCodec.ReadInt32BE(message, sourceOffset);
                    if (!IsPacketLengthValid(packetLength))
                    {
                        return false;
                    }

                    int frameLength = checked(packetLength + sizeof(int));
                    if (sourceRemaining >= frameLength)
                    {
                        if (!TryQueuePacketLocked(message, sourceOffset + sizeof(int), packetLength))
                        {
                            closeCode = CloseStatusCode.TooBig;
                            closeReason = "Receive queue limit exceeded.";
                            return false;
                        }

                        sourceOffset += frameLength;
                        sourceRemaining -= frameLength;
                        continue;
                    }

                    EnsureReceiveCapacity(frameLength);
                    Buffer.BlockCopy(message, sourceOffset, receiveBuffer, 0, sourceRemaining);
                    receiveCount = sourceRemaining;
                    return true;
                }

                if (receiveCount < sizeof(int))
                {
                    int headerBytes = Math.Min(sizeof(int) - receiveCount, sourceRemaining);
                    Buffer.BlockCopy(message, sourceOffset, receiveBuffer, receiveCount, headerBytes);
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
                    Buffer.BlockCopy(message, sourceOffset, receiveBuffer, receiveCount, copyLength);
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
                    closeCode = CloseStatusCode.TooBig;
                    closeReason = "Receive queue limit exceeded.";
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

            int size = receiveBuffer.Length;
            while (size < required)
            {
                size = size > required / 2 ? required : size * 2;
            }

            byte[] expanded = ByteBufferPool.Shared.Rent(size);
            Buffer.BlockCopy(receiveBuffer, 0, expanded, 0, receiveCount);
            ByteBufferPool.Shared.Return(receiveBuffer);
            receiveBuffer = expanded;
        }

        /// <summary>
        /// 将会话标记为关闭并归还残片及队列中尚未派发的缓冲区。
        /// </summary>
        /// <returns>本次调用完成首次关闭时返回 true。</returns>
        private bool TryMarkClosedLocked()
        {
            if (closed)
            {
                return false;
            }

            closed = true;
            receiveCount = 0;
            ReleasePendingPacketsLocked();
            byte[] buffer = receiveBuffer;
            receiveBuffer = null;
            if (buffer != null)
            {
                ByteBufferPool.Shared.Return(buffer);
            }

            return true;
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
        /// 派发异常后关闭会话并保证关闭事件只触发一次。
        /// </summary>
        private void CloseAfterDispatchFailure()
        {
            lock (receiveGate)
            {
                dispatching = false;
                if (!TryMarkClosedLocked())
                {
                    return;
                }
            }

            behavior.CloseSession(CloseStatusCode.ServerError, "Receive handler failed.");
            OnDisconnected?.Invoke();
        }

        /// <summary>
        /// 为只接受完整数组的 websocket-sharp 发送接口复用精确长度缓冲区。
        /// </summary>
        private sealed class ExactSendBufferPool
        {
            #region Private 私有成员

            private const int MaximumRetainedCount = 64; // 最多保留的精确长度数组数量。
            private const int MaximumRetainedBytes = 16 * 1024 * 1024; // 最多保留的精确长度数组总字节数。
            private readonly object gate = new object(); // 保护精确长度桶和预算。
            private readonly Dictionary<int, Stack<byte[]>> buckets = new Dictionary<int, Stack<byte[]>>(); // 按精确长度保存可复用数组。
            private int retainedCount; // 当前保留数组数量。
            private int retainedBytes; // 当前保留数组总字节数。

            #endregion

            #region Public 公共成员

            /// <summary>
            /// 租用长度与请求值完全一致的发送数组。
            /// </summary>
            /// <param name="length">所需精确长度。</param>
            /// <returns>长度与请求值完全一致的字节数组。</returns>
            public byte[] Rent(int length)
            {
                lock (gate)
                {
                    if (buckets.TryGetValue(length, out Stack<byte[]> bucket) && bucket.Count > 0)
                    {
                        byte[] buffer = bucket.Pop();
                        retainedCount--;
                        retainedBytes -= buffer.Length;
                        if (bucket.Count == 0)
                        {
                            buckets.Remove(length);
                        }

                        return buffer;
                    }
                }

                return new byte[length];
            }

            /// <summary>
            /// 在数量和总字节预算允许时保留精确长度发送数组。
            /// </summary>
            /// <param name="buffer">发送回调已经释放所有权的数组。</param>
            public void Return(byte[] buffer)
            {
                if (buffer == null || buffer.Length == 0 || buffer.Length > MaximumRetainedBytes)
                {
                    return;
                }

                lock (gate)
                {
                    if (retainedCount >= MaximumRetainedCount
                        || retainedBytes > MaximumRetainedBytes - buffer.Length)
                    {
                        return;
                    }

                    if (!buckets.TryGetValue(buffer.Length, out Stack<byte[]> bucket))
                    {
                        bucket = new Stack<byte[]>(1);
                        buckets.Add(buffer.Length, bucket);
                    }

                    bucket.Push(buffer);
                    retainedCount++;
                    retainedBytes += buffer.Length;
                }
            }

            #endregion
        }

        #endregion
    }
}
#endif
