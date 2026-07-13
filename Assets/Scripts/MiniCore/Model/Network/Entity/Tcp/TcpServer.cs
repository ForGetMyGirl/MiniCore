using Cysharp.Threading.Tasks;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MiniCore.Model
{
    /// <summary>
    /// 基于 Socket 的 TCP 服务端监听器。
    /// </summary>
    public sealed class TcpServer : IDisposable
    {
        private Socket listener; // 服务端监听套接字。
        private CancellationTokenSource cts; // 接入循环取消令牌源。
        private int sessionIdSeed; // TCP 服务端会话标识递增序号。

        /// <summary>
        /// 接受到 TCP 客户端并创建服务端会话时触发。
        /// </summary>
        public event Action<IServerSession> OnClientAccepted;

        /// <summary>
        /// 开始在指定地址和端口监听 TCP 连接。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <param name="token">执行该方法所需的 token 参数。</param>
        /// <returns>执行处理后的结果。</returns>
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

        /// <summary>
        /// 停止监听并取消接入循环。
        /// </summary>
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

        /// <summary>
        /// 释放监听资源。
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// 执行 AcceptLoopAsync 相关处理。
        /// </summary>
        /// <param name="token">执行该方法所需的 token 参数。</param>
        /// <returns>执行处理后的结果。</returns>
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

        /// <summary>
        /// 执行 ResolveHost 相关处理。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <returns>执行处理后的结果。</returns>
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
