using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace MiniCore.Model
{
    public class KcpServerConfig
    {
        public int Mtu = 1400;
        public int SendWindow = 128;
        public int ReceiveWindow = 128;
        public int NoDelay = 1;
        public int Interval = 10;
        public int Resend = 2;
        public int NoCongestion = 1;
        public int MinRto = 30;
        public int FastResend = 2;
        public int FastAck = 1;
        public int DeadLink = 20;
        public bool Stream = false;
        public int SessionTimeoutMs = 30000;
    }

    public sealed class KcpServer
    {
        private const int MaxDatagramSize = 65507;

        private readonly KcpServerConfig config;
        private readonly Dictionary<string, KcpServerSession> sessions = new Dictionary<string, KcpServerSession>();
        private readonly object sessionLock = new object();

        private Socket socket;
        private CancellationTokenSource receiveCts;
        private CancellationTokenSource updateCts;
        private bool running;
        private long lastConnectionResetLogTicks;

        public event Action<IServerSession> OnSessionCreated;
        public event Action<IServerSession> OnSessionClosed;
        public event Func<IServerSession, ReadOnlyMemory<byte>, UniTask> OnDataReceived;

        public KcpServer(KcpServerConfig config = null)
        {
            this.config = config ?? new KcpServerConfig();
        }

        public UniTask StartAsync(string host, int port, CancellationToken token = default)
        {
            if (running)
            {
                throw new InvalidOperationException("KcpServer already running.");
            }

            running = true;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            TryDisableUdpConnReset(socket);
            socket.Bind(new IPEndPoint(ParseAddress(host), port));

            receiveCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            updateCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _ = ReceiveLoopAsync(receiveCts.Token);
            _ = UpdateLoopAsync(updateCts.Token);
            return UniTask.CompletedTask;
        }

        public void Stop()
        {
            if (!running)
            {
                return;
            }

            running = false;

            try
            {
                receiveCts?.Cancel();
                updateCts?.Cancel();
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

            List<KcpServerSession> toClose;
            lock (sessionLock)
            {
                toClose = new List<KcpServerSession>(sessions.Values);
                sessions.Clear();
            }

            foreach (var session in toClose)
            {
                session.Close();
                OnSessionClosed?.Invoke(session);
            }
        }

        public void CloseSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return;
            }

            KcpServerSession session;
            lock (sessionLock)
            {
                if (!sessions.TryGetValue(sessionId, out session))
                {
                    return;
                }
                sessions.Remove(sessionId);
            }

            session.Close();
            OnSessionClosed?.Invoke(session);
        }

        private async UniTask ReceiveLoopAsync(CancellationToken token)
        {
            byte[] buffer = ByteBufferPool.Shared.Rent(MaxDatagramSize);
            try
            {
                await UniTask.SwitchToThreadPool();
                EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                while (!token.IsCancellationRequested && running)
                {
                    SocketReceiveFromResult result;
                    try
                    {
                        result = await socket.ReceiveFromAsync(
                            new ArraySegment<byte>(buffer),
                            SocketFlags.None,
                            remote).ConfigureAwait(false);
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (SocketException ex)
                    {
                        if (ex.SocketErrorCode == SocketError.ConnectionReset)
                        {
                            // Ignore/reset-throttle noisy UDP connreset notifications on Windows.
                            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            if (now - lastConnectionResetLogTicks > 2000)
                            {
                                lastConnectionResetLogTicks = now;
                                LogSwitch.Warning($"KcpServer receive socket warning: {ex.SocketErrorCode}");
                            }
                            continue;
                        }

                        LogSwitch.Warning($"KcpServer receive socket error: {ex.SocketErrorCode}");
                        continue;
                    }
                    catch (Exception ex)
                    {
                        LogSwitch.Warning($"KcpServer receive error: {ex.Message}");
                        continue;
                    }

                    int received = result.ReceivedBytes;
                    if (received <= 0)
                    {
                        continue;
                    }

                    uint conv = Kcp.PeekConv(buffer, 0);
                    if (conv == 0)
                    {
                        continue;
                    }

                    var session = GetOrCreateSession(conv, result.RemoteEndPoint);
                    if (!session.Input(buffer, received))
                    {
                        continue;
                    }

                    while (session.TryReceive(out var packet))
                    {
                        try
                        {
                            await InvokeDataReceivedAsync(session, new ReadOnlyMemory<byte>(packet, 0, packet.Length));
                        }
                        catch (Exception ex)
                        {
                            LogSwitch.Error($"Server message handling error: {ex.Message}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                LogSwitch.Warning($"KcpServer receive loop error: {ex.Message}");
            }
            finally
            {
                ByteBufferPool.Shared.Return(buffer);
            }
        }

        private async UniTask UpdateLoopAsync(CancellationToken token)
        {
            try
            {
                await UniTask.SwitchToThreadPool();
                while (!token.IsCancellationRequested && running)
                {
                    uint now = CurrentMS();
                    List<KcpServerSession> snapshot;
                    lock (sessionLock)
                    {
                        snapshot = new List<KcpServerSession>(sessions.Values);
                    }

                    foreach (var session in snapshot)
                    {
                        session.Update(now);
                        if (session.IsDead || session.IsTimedOut(now, config.SessionTimeoutMs))
                        {
                            CloseSession(session, session.IsTimedOut(now, config.SessionTimeoutMs));
                        }
                    }

                    await Task.Delay(config.Interval, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                LogSwitch.Warning($"KcpServer update loop error: {ex.Message}");
            }
        }

        private void CloseSession(KcpServerSession session, bool timeout)
        {
            bool removed;
            lock (sessionLock)
            {
                removed = sessions.Remove(session.SessionId);
            }

            if (!removed)
            {
                return;
            }

            session.Close();
            if (timeout)
            {
                LogSwitch.Warning($"Server heartbeat timeout, kick session:{session.SessionId}");
            }
            else
            {
                LogSwitch.Warning($"Server session disconnected: {session.SessionId}");
            }
            OnSessionClosed?.Invoke(session);
        }

        private KcpServerSession GetOrCreateSession(uint conv, EndPoint remote)
        {
            string sessionId = $"{conv}:{remote}";
            KcpServerSession session;
            bool created = false;

            lock (sessionLock)
            {
                if (!sessions.TryGetValue(sessionId, out session))
                {
                    session = new KcpServerSession(conv, remote, socket, config);
                    sessions.Add(sessionId, session);
                    created = true;
                }
            }

            if (created)
            {
                OnSessionCreated?.Invoke(session);
            }

            return session;
        }

        private async UniTask InvokeDataReceivedAsync(IServerSession session, ReadOnlyMemory<byte> data)
        {
            await TransportEventDispatcher.DispatchAsync(OnDataReceived, session, data);
        }

        private static uint CurrentMS()
        {
            return unchecked((uint)Environment.TickCount);
        }

        private static IPAddress ParseAddress(string host)
        {
            if (string.IsNullOrWhiteSpace(host) || host == "0.0.0.0")
            {
                return IPAddress.Any;
            }
            if (IPAddress.TryParse(host, out var address))
            {
                return address;
            }
            return IPAddress.Any;
        }

        private static void TryDisableUdpConnReset(Socket udpSocket)
        {
            if (udpSocket == null)
            {
                return;
            }

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    const int SIO_UDP_CONNRESET = -1744830452;
                    udpSocket.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
                }
            }
            catch
            {
                // Ignore platform/socket capability differences.
            }
        }
    }
}
