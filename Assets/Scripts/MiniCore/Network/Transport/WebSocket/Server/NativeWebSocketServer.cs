using System;
using MiniCore.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// 原生环境的 WebSocket 监听器；浏览器构建保留相同 API 并明确拒绝监听能力。
    /// </summary>
    public sealed class NativeWebSocketServer : IDisposable
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        #region Private 私有成员

        private readonly object sessionGate = new object(); // 保护服务端会话映射。
        private readonly System.Collections.Generic.Dictionary<string, WebSocketServerSession> sessions
            = new System.Collections.Generic.Dictionary<string, WebSocketServerSession>(); // 当前已完成握手的会话。
        private WebSocketSharp.Server.WebSocketServer server; // websocket-sharp 监听器。
        private WebSocketServerConfig config; // 当前启动配置。

        #endregion
#endif

        #region Public 公共成员

        /// <summary>
        /// 新连接完成握手并创建会话后触发。
        /// </summary>
        public event Action<IServerSession> OnSessionCreated;

        /// <summary>
        /// 服务端会话关闭并从监听器移除后触发。
        /// </summary>
        public event Action<IServerSession> OnSessionClosed;

        /// <summary>
        /// 收到完整长度帧业务包时触发。
        /// </summary>
        public event Func<IServerSession, ReadOnlyMemory<byte>, MTask> OnDataReceived;

        /// <summary>
        /// 启动 WS 或 WSS 监听。
        /// </summary>
        /// <param name="host">监听地址；空值或 0.0.0.0 表示任意 IPv4 地址。</param>
        /// <param name="port">监听端口。</param>
        /// <param name="config">路径、消息上限、握手校验和 TLS 配置。</param>
        /// <returns>监听成功后完成的任务。</returns>
        public MTask StartAsync(string host, int port, WebSocketServerConfig config = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new PlatformNotSupportedException("浏览器 WebGL 不支持创建 WebSocket 监听器。");
#else
            if (server != null)
            {
                throw new InvalidOperationException("WebSocket 服务端已经启动。");
            }

            if (port <= 0 || port > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            this.config = config ?? new WebSocketServerConfig();
            ValidateConfig(this.config);
            System.Net.IPAddress address = ParseAddress(host);
            server = new WebSocketSharp.Server.WebSocketServer(address, port, this.config.Secure)
            {
                ReuseAddress = false,
                WaitTime = TimeSpan.FromSeconds(5)
            };

            if (this.config.Secure)
            {
                server.SslConfiguration.ServerCertificate = this.config.ServerCertificate;
            }

            server.AddWebSocketService<NativeWebSocketBehavior>(this.config.Path, behavior =>
            {
                behavior.Configure(this.config, HandleOpened, HandleMessage, HandleClosed);
            });
            server.Start();
            return MTask.CompletedTask;
#endif
        }

        /// <summary>
        /// 停止监听并关闭全部现有会话。
        /// </summary>
        public void Stop()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            WebSocketSharp.Server.WebSocketServer current = server;
            server = null;
            if (current == null)
            {
                return;
            }

            current.Stop();
            WebSocketServerSession[] snapshot;
            lock (sessionGate)
            {
                snapshot = new WebSocketServerSession[sessions.Count];
                sessions.Values.CopyTo(snapshot, 0);
                sessions.Clear();
            }

            for (int index = 0; index < snapshot.Length; index++)
            {
                snapshot[index].NotifyClosed();
                OnSessionClosed?.Invoke(snapshot[index]);
            }
#endif
        }

        /// <summary>
        /// 释放监听器资源。
        /// </summary>
        public void Dispose()
        {
            Stop();
            OnSessionCreated = null;
            OnSessionClosed = null;
            OnDataReceived = null;
        }

        #endregion

#if !UNITY_WEBGL || UNITY_EDITOR
        #region Private 私有成员

        /// <summary>
        /// 校验路径、消息大小和 TLS 证书。
        /// </summary>
        /// <param name="value">需要校验的配置。</param>
        private static void ValidateConfig(WebSocketServerConfig value)
        {
            if (string.IsNullOrWhiteSpace(value.Path) || value.Path[0] != '/')
            {
                throw new ArgumentException("WebSocket 监听路径必须以 / 开头。", nameof(value));
            }

            if (value.MaximumPacketSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "WebSocket 最大业务包长度必须大于零。");
            }

            int maximumFrameSize = checked(value.MaximumPacketSize + sizeof(int));
            if (value.MaximumMessageSize < maximumFrameSize)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "WebSocket 消息上限不能小于单个完整业务帧。");
            }

            if (value.MaximumPendingPacketCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "WebSocket 待派发业务包数量上限必须大于零。");
            }

            if (value.Secure && value.ServerCertificate == null)
            {
                throw new ArgumentException("启用 WSS 时必须提供服务端证书。", nameof(value));
            }
        }

        /// <summary>
        /// 解析监听地址。
        /// </summary>
        /// <param name="host">主机名或 IP 地址。</param>
        /// <returns>可供监听器绑定的 IPv4 地址。</returns>
        private static System.Net.IPAddress ParseAddress(string host)
        {
            if (string.IsNullOrWhiteSpace(host) || host == "0.0.0.0")
            {
                return System.Net.IPAddress.Any;
            }

            if (System.Net.IPAddress.TryParse(host, out System.Net.IPAddress address))
            {
                return address;
            }

            System.Net.IPAddress[] addresses = System.Net.Dns.GetHostAddresses(host);
            for (int index = 0; index < addresses.Length; index++)
            {
                if (addresses[index].AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return addresses[index];
                }
            }

            throw new InvalidOperationException($"无法解析 WebSocket 监听地址：{host}");
        }

        /// <summary>
        /// 为完成握手的行为创建 MiniCore 服务端会话。
        /// </summary>
        /// <param name="behavior">已打开的 websocket-sharp 行为。</param>
        private void HandleOpened(NativeWebSocketBehavior behavior)
        {
            var session = new WebSocketServerSession(
                behavior,
                config.MaximumPacketSize,
                config.MaximumMessageSize,
                config.MaximumPendingPacketCount);
            session.DataReceived += ForwardDataReceivedAsync;
            lock (sessionGate)
            {
                sessions.Add(session.SessionId, session);
            }

            OnSessionCreated?.Invoke(session);
        }

        /// <summary>
        /// 校验消息类型和大小后交给会话长度帧解析器。
        /// </summary>
        /// <param name="behavior">消息所属行为。</param>
        /// <param name="args">消息参数。</param>
        private void HandleMessage(NativeWebSocketBehavior behavior, WebSocketSharp.MessageEventArgs args)
        {
            if (!args.IsBinary || args.RawData == null)
            {
                behavior.CloseSession(WebSocketSharp.CloseStatusCode.UnsupportedData, "Binary messages only.");
                return;
            }

            if (args.RawData.Length > config.MaximumMessageSize)
            {
                behavior.CloseSession(WebSocketSharp.CloseStatusCode.TooBig, "Message is too large.");
                return;
            }

            WebSocketServerSession session;
            lock (sessionGate)
            {
                sessions.TryGetValue(behavior.SessionId, out session);
            }

            session?.PushBinaryMessage(args.RawData);
        }

        /// <summary>
        /// 移除并通知已经关闭的会话。
        /// </summary>
        /// <param name="behavior">关闭的行为。</param>
        /// <param name="args">关闭状态。</param>
        private void HandleClosed(NativeWebSocketBehavior behavior, WebSocketSharp.CloseEventArgs args)
        {
            WebSocketServerSession session;
            lock (sessionGate)
            {
                if (!sessions.TryGetValue(behavior.SessionId, out session))
                {
                    return;
                }

                sessions.Remove(behavior.SessionId);
            }

            session.DataReceived -= ForwardDataReceivedAsync;
            session.NotifyClosed();
            OnSessionClosed?.Invoke(session);
        }

        /// <summary>
        /// 将会话拆出的业务包转发给监听器订阅者。
        /// </summary>
        /// <param name="session">消息所属服务端会话。</param>
        /// <param name="data">完整业务包正文。</param>
        /// <returns>全部监听器完成时结束的任务。</returns>
        private MTask ForwardDataReceivedAsync(IServerSession session, ReadOnlyMemory<byte> data)
        {
            return TransportEventDispatcher.DispatchAsync(OnDataReceived, session, data);
        }

        #endregion
#endif
    }
}
