using System;
using System.Collections.Generic;
using System.Threading;
using MiniCore.Threading;
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
        MTask InitializeDefaultSessionAsync(string host, int port);
        /// <summary>连接默认 KCP 会话。</summary>
        MTask<bool> ConnectDefaultKcpSessionAsync(string host, int port, uint conv, TimeSpan probeTimeout = default, KcpTransportConfig config = null);
        /// <summary>连接默认 TCP 会话。</summary>
        MTask<bool> ConnectDefaultTcpSessionAsync(string host, int port, TimeSpan probeTimeout = default);
        /// <summary>连接默认 UDP 会话。</summary>
        MTask<bool> ConnectDefaultUdpSessionAsync(string host, int port, TimeSpan probeTimeout = default);
        /// <summary>连接指定 TCP 会话。</summary>
        MTask<bool> ConnectTcpSessionAsync(string sessionId, string host, int port, TimeSpan probeTimeout = default);
        /// <summary>连接指定 KCP 会话。</summary>
        MTask<bool> ConnectKcpSessionAsync(string sessionId, string host, int port, uint conv, TimeSpan probeTimeout = default, KcpTransportConfig config = null);
        /// <summary>连接指定 UDP 会话。</summary>
        MTask<bool> ConnectUdpSessionAsync(string sessionId, string host, int port, TimeSpan probeTimeout = default);
        /// <summary>启动 KCP 服务端。</summary>
        MTask StartKcpServerAsync(string host, int port, KcpServerConfig config = null);
        /// <summary>启动 TCP 服务端。</summary>
        MTask StartTcpServerAsync(string host, int port);
        /// <summary>启动 UDP 服务端。</summary>
        MTask StartUdpServerAsync(string host, int port, UdpServerConfig config = null);
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
        /// <summary>
        /// 获取当前收包队列的诊断快照。
        /// </summary>
        NetworkIncomingQueueSnapshot GetIncomingQueueSnapshot();
        /// <summary>
        /// 启用或关闭入站队列等待耗时诊断；仅建议在性能测试期间启用。
        /// </summary>
        /// <param name="enabled">为 true 时记录网络线程入队到主线程开始处理的等待时间。</param>
        void SetIncomingQueueTimingMetricsEnabled(bool enabled);
        /// <summary>
        /// 重置收包队列的峰值与累计诊断统计。
        /// </summary>
        void ResetIncomingQueueMetrics();
        /// <summary>探测会话可用性。</summary>
        MTask<bool> ProbeSessionAsync(string sessionId, TimeSpan timeout);
        /// <summary>发送普通消息。</summary>
        MTask SendAsync<TMessage>(TMessage message) where TMessage : INormalMessage;
        /// <summary>向指定会话发送普通消息。</summary>
        MTask SendAsync<TMessage>(string sessionId, TMessage message) where TMessage : INormalMessage;
        /// <summary>
        /// 尝试将默认会话的高频普通消息放入有界出站队列，不等待底层 socket 写入。
        /// </summary>
        /// <typeparam name="TMessage">需要发送的普通消息类型。</typeparam>
        /// <param name="message">需要发送的高频普通消息。</param>
        /// <returns>当前会话和队列接受或拒绝该消息的原因。</returns>
        NetworkSendResult TrySend<TMessage>(TMessage message) where TMessage : INormalMessage;
        /// <summary>
        /// 尝试将指定会话的高频普通消息放入有界出站队列，不等待底层 socket 写入。
        /// </summary>
        /// <typeparam name="TMessage">需要发送的普通消息类型。</typeparam>
        /// <param name="sessionId">目标逻辑会话标识。</param>
        /// <param name="message">需要发送的高频普通消息。</param>
        /// <returns>当前会话和队列接受或拒绝该消息的原因。</returns>
        NetworkSendResult TrySend<TMessage>(string sessionId, TMessage message) where TMessage : INormalMessage;
        /// <summary>调用默认会话 RPC。</summary>
        MTask<TResponse> CallAsync<TRequest, TResponse>(TRequest request) where TRequest : IRpcRequest where TResponse : IRpcResponse;
        /// <summary>调用指定会话 RPC。</summary>
        MTask<TResponse> CallAsync<TRequest, TResponse>(string sessionId, TRequest request) where TRequest : IRpcRequest where TResponse : IRpcResponse;
        /// <summary>注册普通 Handler。</summary>
        void RegisterHandler(INetworkMessageHandlerInvoker invoker);
        /// <summary>注册 RPC Handler。</summary>
        void RegisterHandler(INetworkRpcHandlerInvoker invoker);
    }
}
