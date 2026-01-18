using Cysharp.Threading.Tasks;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MiniCore.Model
{
    /// <summary>
    /// TCP ???¨¨?¡°?????¡ã???????¡±?Socket + ¨¦???o|?¡ë???€??? ?-¡ª¨¨??????¡è¡ì??¡¥??¡ë?¡è?????2???¡­/?????¡­?€?
    /// </summary>
    public class TcpTransport : INetworkTransport
    {
        private Socket socket;
        private CancellationTokenSource receiveCts;

        public bool IsConnected => socket != null && socket.Connected;

        public event Func<ReadOnlyMemory<byte>, UniTask> OnDataReceived;
        public event Action OnDisconnected;

        public async UniTask ConnectAsync(string host, int port, CancellationToken token = default)
        {
            Disconnect();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(host, port);
            receiveCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _ = ReceiveLoopAsync(receiveCts.Token);
        }

        public async UniTask SendAsync(ArraySegment<byte> data, CancellationToken token = default)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("TCP ??a¨¨????£¤????¡ª??3???¡®¨¦€???¡ã????€?");
            }

            int length = data.Count;
            byte[] lengthBytes = ByteBufferPool.Shared.Rent(4);
            try
            {
                WriteInt32BE(lengthBytes, 0, length);
                await socket.SendAsync(new ArraySegment<byte>(lengthBytes, 0, 4), SocketFlags.None, token);
                await socket.SendAsync(data, SocketFlags.None, token);
            }
            finally
            {
                ByteBufferPool.Shared.Return(lengthBytes);
            }
        }

        private async UniTask ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                await UniTask.SwitchToThreadPool();
                while (!token.IsCancellationRequested && IsConnected)
                {
                    byte[] lengthBuf = ByteBufferPool.Shared.Rent(4);
                    try
                    {
                        if (!await ReadExactAsync(lengthBuf, 4, token))
                        {
                            break;
                        }
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(lengthBuf);
                        }
                        int bodyLength = BitConverter.ToInt32(lengthBuf, 0);
                        if (bodyLength <= 0)
                        {
                            break;
                        }

                        byte[] body = ByteBufferPool.Shared.Rent(bodyLength);
                        try
                        {
                            if (!await ReadExactAsync(body, bodyLength, token))
                            {
                                break;
                            }
                            await InvokeDataReceivedAsync(new ReadOnlyMemory<byte>(body, 0, bodyLength));
                        }
                        finally
                        {
                            ByteBufferPool.Shared.Return(body);
                        }
                    }
                    finally
                    {
                        ByteBufferPool.Shared.Return(lengthBuf);
                    }
                }
            }
            catch (Exception ex)
            {
                EventCenter.Broadcast(GameEvent.LogWarning, $"TcpTransport receive loop error: {ex.Message}");
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
                int n = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, read, size - read), SocketFlags.None, token).ConfigureAwait(false);
                if (n == 0)
                {
                    return false;
                }
                read += n;
            }
            return true;
        }

        public void Disconnect()
        {
            try
            {
                receiveCts?.Cancel();
            }
            catch { }

            if (socket != null)
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
                catch { }
                socket.Close();
                socket = null;
            }
            OnDisconnected?.Invoke();
        }

        public void Dispose()
        {
            Disconnect();
        }

        private async UniTask InvokeDataReceivedAsync(ReadOnlyMemory<byte> data)
        {
            var handler = OnDataReceived;
            if (handler == null)
            {
                return;
            }

            foreach (var del in handler.GetInvocationList())
            {
                var callback = (Func<ReadOnlyMemory<byte>, UniTask>)del;
                await callback(data);
            }
        }

        private static void WriteInt32BE(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)((value >> 24) & 0xFF);
            buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 3] = (byte)(value & 0xFF);
        }
    }
}