using Cysharp.Threading.Tasks;
using System;
using System.Net.Sockets;
using System.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// 为 TCP 传输提供四字节大端长度前缀拆包和粘包处理的基类。
    /// </summary>
    public abstract class LengthPrefixedTcpTransportBase : INetworkTransport
    {
        private readonly int maxPacketSize; // 允许接收的单个业务包最大字节数。
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1); // 保证长度头和包体连续发送的异步锁。

        private Socket socket; // 当前已连接的 TCP 套接字。
        private CancellationTokenSource receiveCts; // 接收循环取消令牌源。
        private int disconnected = 1; // 传输断开状态的原子标志。

        /// <summary>
        /// 使用指定最大业务包大小创建 TCP 传输基类。
        /// </summary>
        /// <param name="maxPacketSize">执行该方法所需的 maxPacketSize 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        protected LengthPrefixedTcpTransportBase(int maxPacketSize = 4 * 1024 * 1024)
        {
            this.maxPacketSize = maxPacketSize;
        }

        /// <summary>
        /// 当前已连接套接字，供派生传输访问。
        /// </summary>
        protected Socket Socket => socket;

        /// <summary>
        /// 当前套接字是否已连接。
        /// </summary>
        public bool IsConnected => socket != null && socket.Connected;

        /// <summary>
        /// 接收到完整业务包时触发。
        /// </summary>
        public event Func<ReadOnlyMemory<byte>, UniTask> OnDataReceived;
        /// <summary>
        /// 传输关闭时触发。
        /// </summary>
        public event Action OnDisconnected;

        /// <summary>
        /// 由派生类实现到目标主机的连接过程。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <param name="token">执行该方法所需的 token 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public abstract UniTask ConnectAsync(string host, int port, CancellationToken token = default);

        /// <summary>
        /// 以长度前缀格式发送完整业务包。
        /// </summary>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <param name="token">执行该方法所需的 token 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public async UniTask SendAsync(ArraySegment<byte> data, CancellationToken token = default)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException($"{GetType().Name} is not connected.");
            }

            byte[] lengthBytes = ByteBufferPool.Shared.Rent(4);
            try
            {
                NetBinaryCodec.WriteInt32BE(lengthBytes, 0, data.Count);
                await sendLock.WaitAsync(token);
                try
                {
                    await SendAllAsync(new ArraySegment<byte>(lengthBytes, 0, 4), token);
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

        /// <summary>
        /// 附加已连接套接字并启动后台接收循环。
        /// </summary>
        /// <param name="connectedSocket">执行该方法所需的 connectedSocket 参数。</param>
        /// <param name="token">执行该方法所需的 token 参数。</param>
        protected void AttachConnectedSocket(Socket connectedSocket, CancellationToken token = default)
        {
            socket = connectedSocket ?? throw new ArgumentNullException(nameof(connectedSocket));
            Interlocked.Exchange(ref disconnected, 0);

            receiveCts = token == default
                ? new CancellationTokenSource()
                : CancellationTokenSource.CreateLinkedTokenSource(token);
            _ = ReceiveLoopAsync(receiveCts.Token);
        }

        /// <summary>
        /// 向订阅者派发已完成拆包的数据。
        /// </summary>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        protected UniTask DispatchDataReceivedAsync(ReadOnlyMemory<byte> data)
        {
            return TransportEventDispatcher.DispatchAsync(OnDataReceived, data);
        }

        /// <summary>
        /// 执行 ReceiveLoopAsync 相关处理。
        /// </summary>
        /// <param name="token">执行该方法所需的 token 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private async UniTask ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                await UniTask.SwitchToThreadPool();
                while (!token.IsCancellationRequested && IsConnected)
                {
                    byte[] lengthBuffer = ByteBufferPool.Shared.Rent(4);
                    try
                    {
                        if (!await ReadExactAsync(lengthBuffer, 4, token))
                        {
                            break;
                        }

                        int bodyLength = NetBinaryCodec.ReadInt32BE(lengthBuffer, 0);
                        if (bodyLength <= 0 || bodyLength > maxPacketSize)
                        {
                            break;
                        }

                        byte[] bodyBuffer = ByteBufferPool.Shared.Rent(bodyLength);
                        try
                        {
                            if (!await ReadExactAsync(bodyBuffer, bodyLength, token))
                            {
                                break;
                            }

                            await DispatchDataReceivedAsync(new ReadOnlyMemory<byte>(bodyBuffer, 0, bodyLength));
                        }
                        finally
                        {
                            ByteBufferPool.Shared.Return(bodyBuffer);
                        }
                    }
                    finally
                    {
                        ByteBufferPool.Shared.Return(lengthBuffer);
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
                Disconnect();
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
        /// 执行 ReadExactAsync 相关处理。
        /// </summary>
        /// <param name="buffer">执行该方法所需的 buffer 参数。</param>
        /// <param name="size">执行该方法所需的 size 参数。</param>
        /// <param name="token">执行该方法所需的 token 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private async UniTask<bool> ReadExactAsync(byte[] buffer, int size, CancellationToken token)
        {
            int read = 0;
            while (read < size)
            {
                var currentSocket = socket;
                if (currentSocket == null)
                {
                    return false;
                }

                int received = await currentSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer, read, size - read),
                    SocketFlags.None,
                    token).ConfigureAwait(false);

                if (received == 0)
                {
                    return false;
                }

                read += received;
            }

            return true;
        }

        /// <summary>
        /// 执行 SendAllAsync 相关处理。
        /// </summary>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <param name="token">执行该方法所需的 token 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private async UniTask SendAllAsync(ArraySegment<byte> data, CancellationToken token)
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

                int written = await currentSocket.SendAsync(
                    new ArraySegment<byte>(data.Array, data.Offset + sent, data.Count - sent),
                    SocketFlags.None,
                    token).ConfigureAwait(false);

                if (written <= 0)
                {
                    throw new SocketException((int)SocketError.ConnectionAborted);
                }

                sent += written;
            }
        }
    }
}
