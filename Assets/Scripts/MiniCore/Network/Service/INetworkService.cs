using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MiniCore.Model;

namespace MiniCore.Service
{
    /// <summary>
    /// 网络服务对业务层公开的完整会话、传输、RPC、消息和 Handler 能力。
    /// </summary>
    public interface INetworkService : IAppService
    {
        /// <summary>默认会话标识。</summary>
        string DefaultSessionId { get; set; }
        /// <summary>服务端会话创建事件。</summary>
        event Action<NetworkSession> OnServerSessionCreated;
        /// <summary>服务端会话关闭事件。</summary>
        event Action<string> OnServerSessionClosed;
        /// <summary>设置消息序列化器。</summary>
        void SetSerializer(INetworkSerializer customSerializer);
        /// <summary>初始化默认 TCP 会话。</summary>
        UniTask InitializeDefaultSessionAsync(string host, int port, CancellationToken token = default);
        /// <summary>连接默认 KCP 会话。</summary>
        UniTask<bool> ConnectDefaultKcpSessionAsync(string host, int port, uint conv, TimeSpan probeTimeout = default, KcpTransportConfig config = null, CancellationToken token = default);
        /// <summary>连接默认 TCP 会话。</summary>
        UniTask<bool> ConnectDefaultTcpSessionAsync(string host, int port, TimeSpan probeTimeout = default, CancellationToken token = default);
        /// <summary>连接默认 UDP 会话。</summary>
        UniTask<bool> ConnectDefaultUdpSessionAsync(string host, int port, TimeSpan probeTimeout = default, CancellationToken token = default);
        /// <summary>连接指定 TCP 会话。</summary>
        UniTask<bool> ConnectTcpSessionAsync(string sessionId, string host, int port, TimeSpan probeTimeout = default, CancellationToken token = default);
        /// <summary>连接指定 KCP 会话。</summary>
        UniTask<bool> ConnectKcpSessionAsync(string sessionId, string host, int port, uint conv, TimeSpan probeTimeout = default, KcpTransportConfig config = null, CancellationToken token = default);
        /// <summary>连接指定 UDP 会话。</summary>
        UniTask<bool> ConnectUdpSessionAsync(string sessionId, string host, int port, TimeSpan probeTimeout = default, CancellationToken token = default);
        /// <summary>启动 KCP 服务端。</summary>
        UniTask StartKcpServerAsync(string host, int port, KcpServerConfig config = null, CancellationToken token = default);
        /// <summary>启动 TCP 服务端。</summary>
        UniTask StartTcpServerAsync(string host, int port, CancellationToken token = default);
        /// <summary>启动 UDP 服务端。</summary>
        UniTask StartUdpServerAsync(string host, int port, UdpServerConfig config = null, CancellationToken token = default);
        /// <summary>停止 KCP 服务端。</summary>
        void StopKcpServer();
        /// <summary>停止 TCP 服务端。</summary>
        void StopTcpServer();
        /// <summary>停止 UDP 服务端。</summary>
        void StopUdpServer();
        /// <summary>断开指定会话。</summary>
        void DisconnectSession(string sessionId);
        /// <summary>获取服务端会话快照。</summary>
        List<NetworkSession> GetServerSessionsSnapshot();
        /// <summary>获取指定会话。</summary>
        NetworkSession GetSession(string sessionId);
        /// <summary>探测会话可用性。</summary>
        UniTask<bool> ProbeSessionAsync(string sessionId, TimeSpan timeout, CancellationToken token = default);
        /// <summary>发送普通消息。</summary>
        UniTask SendAsync<TMessage>(TMessage message, CancellationToken token = default) where TMessage : INormalMessage;
        /// <summary>向指定会话发送普通消息。</summary>
        UniTask SendAsync<TMessage>(string sessionId, TMessage message, CancellationToken token = default) where TMessage : INormalMessage;
        /// <summary>调用默认会话 RPC。</summary>
        UniTask<TResponse> CallAsync<TRequest, TResponse>(TRequest request, CancellationToken token = default) where TRequest : IRpcRequest where TResponse : IRpcResponse;
        /// <summary>调用指定会话 RPC。</summary>
        UniTask<TResponse> CallAsync<TRequest, TResponse>(string sessionId, TRequest request, CancellationToken token = default) where TRequest : IRpcRequest where TResponse : IRpcResponse;
        /// <summary>注册普通 Handler。</summary>
        void RegisterHandler(INetworkMessageHandlerInvoker invoker);
        /// <summary>注册 RPC Handler。</summary>
        void RegisterHandler(INetworkRpcHandlerInvoker invoker);
    }
}
