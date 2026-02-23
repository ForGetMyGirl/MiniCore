using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MiniCore.Model
{
    public class UdpServerConfig
    {
        public int MaxDatagramSize = 65507;
    }

    public sealed class UdpServer
    {
        private readonly UdpServerConfig config;
        private readonly Dictionary<string, UdpServerSession> sessions = new Dictionary<string, UdpServerSession>();
        private readonly object sessionLock = new object();

        private Socket socket;
        private CancellationTokenSource receiveCts;
        private bool running;

        public event Action<IServerSession> OnSessionCreated;
        public event Action<IServerSession> OnSessionClosed;
        public event Func<IServerSession, ReadOnlyMemory<byte>, UniTask> OnDataReceived;

        public UdpServer(UdpServerConfig config = null)
        {
            this.config = config ?? new UdpServerConfig();
        }

        public UniTask StartAsync(string host, int port, CancellationToken token = default)
        {
            if (running)
            {
                throw new InvalidOperationException("UdpServer already running.");
            }

            running = true;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(ParseAddress(host), port));

            receiveCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _ = ReceiveLoopAsync(receiveCts.Token);
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
            }
            catch
            {
            }
            finally
            {
                receiveCts?.Dispose();
                receiveCts = null;
            }

            if (socket != null)
            {
                try
                {
                    socket.Close();
                }
                catch
                {
                }
                socket = null;
            }

            List<UdpServerSession> toClose;
            lock (sessionLock)
            {
                toClose = new List<UdpServerSession>(sessions.Values);
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

            UdpServerSession session;
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
            var buffer = ByteBufferPool.Shared.Rent(config.MaxDatagramSize);
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
                    catch (SocketException)
                    {
                        continue;
                    }

                    int received = result.ReceivedBytes;
                    if (received <= 0)
                    {
                        continue;
                    }

                    LogSwitch.Info($"UDP raw received from {result.RemoteEndPoint}, len:{received}");

                    var session = GetOrCreateSession(result.RemoteEndPoint);
                    byte[] packet = ByteBufferPool.Shared.Rent(received);
                    Buffer.BlockCopy(buffer, 0, packet, 0, received);
                    try
                    {
                        await TransportEventDispatcher.DispatchAsync(OnDataReceived, (IServerSession)session, new ReadOnlyMemory<byte>(packet, 0, received));
                    }
                    finally
                    {
                        ByteBufferPool.Shared.Return(packet);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                LogSwitch.Warning($"UdpServer receive loop error: {ex.Message}");
            }
            finally
            {
                ByteBufferPool.Shared.Return(buffer);
            }
        }

        private UdpServerSession GetOrCreateSession(EndPoint remote)
        {
            string sessionId = $"udp:{remote}";
            bool created = false;
            UdpServerSession session;

            lock (sessionLock)
            {
                if (!sessions.TryGetValue(sessionId, out session))
                {
                    session = new UdpServerSession(remote, socket);
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

        private static IPAddress ParseAddress(string host)
        {
            if (string.IsNullOrWhiteSpace(host) || host == "0.0.0.0")
            {
                return IPAddress.Any;
            }

            if (IPAddress.TryParse(host, out var ip))
            {
                return ip;
            }

            return IPAddress.Any;
        }
    }
}
