using Cysharp.Threading.Tasks;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MiniCore.Model
{
    /// <summary>
    /// UDP transport implementation using connected datagrams.
    /// </summary>
    public class UdpTransport : INetworkTransport
    {
        private const int MaxDatagramSize = 65507;

        private Socket socket;
        private CancellationTokenSource receiveCts;

        public bool IsConnected => socket != null && socket.Connected;

        public event Func<ReadOnlyMemory<byte>, UniTask> OnDataReceived;
        public event Action OnDisconnected;

        public async UniTask ConnectAsync(string host, int port, CancellationToken token = default)
        {
            Disconnect();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            await socket.ConnectAsync(host, port);
            receiveCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _ = ReceiveLoopAsync(receiveCts.Token);
        }

        public async UniTask SendAsync(ArraySegment<byte> data, CancellationToken token = default)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("UDP is not connected; cannot send data.");
            }

            await socket.SendAsync(data, SocketFlags.None, token);
        }

        private async UniTask ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                await UniTask.SwitchToThreadPool();
                while (!token.IsCancellationRequested && IsConnected)
                {
                    byte[] buffer = ByteBufferPool.Shared.Rent(MaxDatagramSize);
                    try
                    {
                        int received = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None, token).ConfigureAwait(false);
                        if (received <= 0)
                        {
                            break;
                        }
                        await InvokeDataReceivedAsync(new ReadOnlyMemory<byte>(buffer, 0, received));
                    }
                    finally
                    {
                        ByteBufferPool.Shared.Return(buffer);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                EventCenter.Broadcast(GameEvent.LogWarning, $"UdpTransport receive loop error: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
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
                    socket.Close();
                }
                catch { }
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
    }
}
