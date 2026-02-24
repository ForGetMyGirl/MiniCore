using Cysharp.Threading.Tasks;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// UDP transport implementation using unconnected datagrams.
    /// </summary>
    public class UdpTransport : INetworkTransport
    {
        private const int MaxDatagramSize = 65507;
        private static readonly TimeSpan DefaultInitTimeout = TimeSpan.FromSeconds(3);

        private Socket socket;
        private EndPoint remoteEndPoint;
        private CancellationTokenSource receiveCts;
        private int disconnected;

        public bool IsConnected => socket != null;

        public event Func<ReadOnlyMemory<byte>, UniTask> OnDataReceived;
        public event Action OnDisconnected;

        public async UniTask ConnectAsync(string host, int port, CancellationToken token = default)
        {
            Disconnect();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            UniTask resolveTask = ResolveRemoteEndPointAsync(host, port);
            int winner = await UniTask.WhenAny(
                resolveTask,
                UniTask.Delay(DefaultInitTimeout, cancellationToken: token));

            if (winner != 0)
            {
                TryCloseSocket();
                throw new TimeoutException($"UDP init timeout to {host}:{port}");
            }

            await resolveTask;
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

            if (remoteEndPoint == null)
            {
                throw new InvalidOperationException("UDP remote endpoint is not initialized.");
            }

            await socket.SendToAsync(data, SocketFlags.None, remoteEndPoint).ConfigureAwait(false);
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
                        EndPoint from = new IPEndPoint(IPAddress.Any, 0);
                        SocketReceiveFromResult result = await socket.ReceiveFromAsync(
                            new ArraySegment<byte>(buffer),
                            SocketFlags.None,
                            from).ConfigureAwait(false);

                        int received = result.ReceivedBytes;
                        if (received <= 0)
                        {
                            break;
                        }

                        if (!IsExpectedRemote(result.RemoteEndPoint))
                        {
                            continue;
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
                    LogSwitch.Warning($"UdpTransport receive loop error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                if (!IsActiveDisconnect())
                {
                    LogSwitch.Warning($"UdpTransport receive loop error: {ex.Message}");
                }
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
            catch
            {
            }
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
            remoteEndPoint = null;
        }

        private async UniTask InvokeDataReceivedAsync(ReadOnlyMemory<byte> data)
        {
            await TransportEventDispatcher.DispatchAsync(OnDataReceived, data);
        }

        private async UniTask ResolveRemoteEndPointAsync(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentException("Host is empty.", nameof(host));
            }

            if (IPAddress.TryParse(host, out var ip))
            {
                remoteEndPoint = new IPEndPoint(ip, port);
                return;
            }

            IPAddress[] addresses = await Dns.GetHostAddressesAsync(host);
            foreach (var address in addresses)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    remoteEndPoint = new IPEndPoint(address, port);
                    return;
                }
            }

            throw new SocketException((int)SocketError.AddressFamilyNotSupported);
        }

        private bool IsExpectedRemote(EndPoint remote)
        {
            if (remoteEndPoint == null)
            {
                return true;
            }

            if (!(remoteEndPoint is IPEndPoint expected) || !(remote is IPEndPoint actual))
            {
                return Equals(remoteEndPoint, remote);
            }

            if (expected.Port != actual.Port)
            {
                return false;
            }

            IPAddress expectedIp = expected.Address.MapToIPv4();
            IPAddress actualIp = actual.Address.MapToIPv4();
            return expectedIp.Equals(actualIp);
        }

        private bool IsActiveDisconnect()
        {
            return Volatile.Read(ref disconnected) != 0;
        }

        private static bool IsExpectedSocketClosure(SocketException ex)
        {
            return ex.SocketErrorCode == SocketError.OperationAborted
                || ex.SocketErrorCode == SocketError.Interrupted
                || ex.SocketErrorCode == SocketError.ConnectionAborted
                || ex.SocketErrorCode == SocketError.ConnectionReset
                || ex.SocketErrorCode == SocketError.NotSocket;
        }

    }
}
