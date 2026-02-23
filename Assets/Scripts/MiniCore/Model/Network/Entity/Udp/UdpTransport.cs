using Cysharp.Threading.Tasks;
using System;
using System.Net.Sockets;
using System.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// UDP transport implementation using connected datagrams.
    /// </summary>
    public class UdpTransport : INetworkTransport
    {
        private const int MaxDatagramSize = 65507;
        private static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromSeconds(3);

        private Socket socket;
        private CancellationTokenSource receiveCts;
        private int disconnected;

        public bool IsConnected => socket != null;

        public event Func<ReadOnlyMemory<byte>, UniTask> OnDataReceived;
        public event Action OnDisconnected;

        public async UniTask ConnectAsync(string host, int port, CancellationToken token = default)
        {
            Disconnect();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            var connectTask = socket.ConnectAsync(host, port);
            int winner = await UniTask.WhenAny(
                connectTask.AsUniTask(),
                UniTask.Delay(DefaultConnectTimeout, cancellationToken: token));

            if (winner != 0)
            {
                TryCloseSocket();
                throw new TimeoutException($"UDP connect timeout to {host}:{port}");
            }

            await connectTask;
            disconnected = 0;
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
                LogSwitch.Warning($"UdpTransport receive loop error: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        public void Disconnect()
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
            catch { }
            finally
            {
                currentCts?.Dispose();
            }

            TryCloseSocket();

            var handler = OnDisconnected;
            OnDisconnected = null;
            handler?.Invoke();
        }

        public void Dispose()
        {
            Disconnect();
        }

        private void TryCloseSocket()
        {
            if (socket == null)
            {
                return;
            }

            try
            {
                socket.Close();
            }
            catch
            {
            }

            socket = null;
        }

        private async UniTask InvokeDataReceivedAsync(ReadOnlyMemory<byte> data)
        {
            await TransportEventDispatcher.DispatchAsync(OnDataReceived, data);
        }
    }
}

