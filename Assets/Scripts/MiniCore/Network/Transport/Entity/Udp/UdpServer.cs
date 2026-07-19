using MiniCore.Threading;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// UDP 服务端接收行为配置。
    /// </summary>
    public class UdpServerConfig
    {
        /// <summary>
        /// 单个 UDP 数据报允许接收的最大字节数。
        /// </summary>
        public int MaxDatagramSize = 65507;
    }

    /// <summary>
    /// 按远端地址维护逻辑会话的 UDP 服务端。
    /// </summary>
    public sealed class UdpServer
    {
        private readonly UdpServerConfig config; // UDP 服务端配置。
        private readonly Dictionary<string, UdpServerSession> sessions = new Dictionary<string, UdpServerSession>(); // 远端地址对应的服务端会话。
        private readonly object sessionLock = new object(); // 服务端会话表同步锁。

        private Socket socket; // UDP 监听套接字。
        private CancellationTokenSource receiveCts; // 接收循环取消令牌源。
        private bool running; // 服务端运行状态。

        /// <summary>
        /// 首次收到某远端数据报并创建会话时触发。
        /// </summary>
        public event Action<IServerSession> OnSessionCreated;
        /// <summary>
        /// 服务端会话关闭时触发。
        /// </summary>
        public event Action<IServerSession> OnSessionClosed;
        /// <summary>
        /// 接收到 UDP 业务数据报时触发。
        /// </summary>
        public event Func<IServerSession, ReadOnlyMemory<byte>, MTask> OnDataReceived;

        /// <summary>
        /// 使用指定配置创建 UDP 服务端。
        /// </summary>
        /// <param name="config">执行该方法所需的 config 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public UdpServer(UdpServerConfig config = null)
        {
            this.config = config ?? new UdpServerConfig();
        }

        /// <summary>
        /// 绑定指定地址和端口并启动 UDP 接收循环。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public MTask StartAsync(string host, int port)
        {
            CancellationToken token = MTaskExternal.GetCancellationToken();
            if (running)
            {
                throw new InvalidOperationException("UdpServer already running.");
            }

            running = true;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(ParseAddress(host), port));

            receiveCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            ReceiveLoopAsync(receiveCts.Token).Forget();
            return MTask.CompletedTask;
        }

        /// <summary>
        /// 停止接收、关闭套接字并释放全部 UDP 服务端会话。
        /// </summary>
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

        /// <summary>
        /// 关闭并移除指定远端地址对应的服务端会话。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
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

        /// <summary>
        /// 执行 ReceiveLoopAsync 相关处理。
        /// </summary>
        /// <returns>执行处理后的结果。</returns>
        private async MTask ReceiveLoopAsync(CancellationToken token)
        {
            var buffer = ByteBufferPool.Shared.Rent(config.MaxDatagramSize);
            try
            {
                await MTask.SwitchTo(MTaskExecutors.Network);
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

        /// <summary>
        /// 执行 GetOrCreateSession 相关处理。
        /// </summary>
        /// <param name="remote">执行该方法所需的 remote 参数。</param>
        /// <returns>执行处理后的结果。</returns>
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

        /// <summary>
        /// 执行 ParseAddress 相关处理。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <returns>执行处理后的结果。</returns>
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
