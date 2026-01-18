using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

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

        public event Action<KcpServerSession> OnSessionCreated;
        public event Action<KcpServerSession> OnSessionClosed;
        public event Func<KcpServerSession, ReadOnlyMemory<byte>, UniTask> OnDataReceived;

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
                    SocketReceiveFromResult result = await socket.ReceiveFromAsync(
                        new ArraySegment<byte>(buffer),
                        SocketFlags.None,
                        remote).ConfigureAwait(false);

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
                            EventCenter.Broadcast(GameEvent.LogError, $"服务端处理消息异常：{ex.Message}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                EventCenter.Broadcast(GameEvent.LogWarning, $"KcpServer receive loop error: {ex.Message}");
            }
            finally
            {
                ByteBufferPool.Shared.Return(buffer);
                if (running)
                {
                    Stop();
                }
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
                EventCenter.Broadcast(GameEvent.LogWarning, $"KcpServer update loop error: {ex.Message}");
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
                EventCenter.Broadcast(GameEvent.LogWarning, $"服务端心跳超时，踢出连接，会话:{session.SessionId}");
            }
            else
            {
                EventCenter.Broadcast(GameEvent.LogWarning, $"服务端会话已断开：{session.SessionId}");
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

        private async UniTask InvokeDataReceivedAsync(KcpServerSession session, ReadOnlyMemory<byte> data)
        {
            var handler = OnDataReceived;
            if (handler == null)
            {
                return;
            }

            foreach (var del in handler.GetInvocationList())
            {
                var callback = (Func<KcpServerSession, ReadOnlyMemory<byte>, UniTask>)del;
                await callback(session, data);
            }
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
    }
}
