using Cysharp.Threading.Tasks;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MiniCore.Model
{
    public sealed class TcpServer : IDisposable
    {
        private Socket listener;
        private CancellationTokenSource cts;
        private int sessionIdSeed;

        public event Action<IServerSession> OnClientAccepted;

        public async UniTask StartAsync(string host, int port, CancellationToken token = default)
        {
            if (listener != null)
            {
                throw new InvalidOperationException("TcpServer already started.");
            }

            IPAddress ip = ResolveHost(host);
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(ip, port));
            listener.Listen(128);
            LogSwitch.Info($"[GM TCP] Listening on {ip}:{port}");

            cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _ = AcceptLoopAsync(cts.Token);
        }

        public void Stop()
        {
            try
            {
                cts?.Cancel();
            }
            catch { }

            if (listener != null)
            {
                try
                {
                    listener.Close();
                }
                catch { }
                listener = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private async UniTask AcceptLoopAsync(CancellationToken token)
        {
            try
            {
                await UniTask.SwitchToThreadPool();
                while (!token.IsCancellationRequested && listener != null)
                {
                    Socket client = await Task<Socket>.Factory.FromAsync(listener.BeginAccept, listener.EndAccept, null);
                    if (client == null)
                    {
                        continue;
                    }

                    client.NoDelay = true;
                    LogSwitch.Info($"[GM TCP] Accepted socket from {client.RemoteEndPoint}");
                    string sessionId = $"tcp:{Interlocked.Increment(ref sessionIdSeed)}:{client.RemoteEndPoint}";
                    var session = new TcpServerSession(sessionId, client);
                    OnClientAccepted?.Invoke(session);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                LogSwitch.Warning($"TcpServer accept loop error: {ex.Message}");
            }
            finally
            {
                Stop();
            }
        }

        private IPAddress ResolveHost(string host)
        {
            if (string.IsNullOrEmpty(host) || host == "0.0.0.0")
            {
                return IPAddress.Any;
            }

            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                return IPAddress.Loopback;
            }

            if (IPAddress.TryParse(host, out var ip))
            {
                return ip;
            }

            return IPAddress.Any;
        }
    }
}

