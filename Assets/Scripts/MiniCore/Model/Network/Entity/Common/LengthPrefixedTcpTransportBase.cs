using Cysharp.Threading.Tasks;
using System;
using System.Net.Sockets;
using System.Threading;

namespace MiniCore.Model
{
    public abstract class LengthPrefixedTcpTransportBase : INetworkTransport
    {
        private readonly int maxPacketSize;
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);

        private Socket socket;
        private CancellationTokenSource receiveCts;
        private int disconnected = 1;

        protected LengthPrefixedTcpTransportBase(int maxPacketSize = 4 * 1024 * 1024)
        {
            this.maxPacketSize = maxPacketSize;
        }

        protected Socket Socket => socket;

        public bool IsConnected => socket != null && socket.Connected;

        public event Func<ReadOnlyMemory<byte>, UniTask> OnDataReceived;
        public event Action OnDisconnected;

        public abstract UniTask ConnectAsync(string host, int port, CancellationToken token = default);

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

        public void Dispose()
        {
            Disconnect();
        }

        protected void AttachConnectedSocket(Socket connectedSocket, CancellationToken token = default)
        {
            socket = connectedSocket ?? throw new ArgumentNullException(nameof(connectedSocket));
            Interlocked.Exchange(ref disconnected, 0);

            receiveCts = token == default
                ? new CancellationTokenSource()
                : CancellationTokenSource.CreateLinkedTokenSource(token);
            _ = ReceiveLoopAsync(receiveCts.Token);
        }

        protected UniTask DispatchDataReceivedAsync(ReadOnlyMemory<byte> data)
        {
            return TransportEventDispatcher.DispatchAsync(OnDataReceived, data);
        }

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
            catch (SocketException)
            {
            }
            catch (Exception ex)
            {
                LogSwitch.Warning($"{GetType().Name} receive loop error: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

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

