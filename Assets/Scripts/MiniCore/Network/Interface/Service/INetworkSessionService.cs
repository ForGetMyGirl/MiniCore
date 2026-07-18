using Cysharp.Threading.Tasks;
using MiniCore.Model;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MiniCore.Core
{
    /// <summary>
    /// 网络会话创建、查询、断开及服务端监听的统一服务接口。
    /// </summary>
    public interface INetworkSessionService
    {
        /// <summary>
        /// 服务端创建逻辑会话后触发。
        /// </summary>
        event Action<NetworkSession> OnServerSessionCreated;
        /// <summary>
        /// 服务端逻辑会话关闭后触发。
        /// </summary>
        event Action<string> OnServerSessionClosed;

        /// <summary>
        /// 创建 TCP 客户端逻辑会话。
        /// </summary>
        UniTask<NetworkSession> CreateTcpSessionAsync(string sessionId, string host, int port, CancellationToken token = default);
        /// <summary>
        /// 创建 KCP 客户端逻辑会话。
        /// </summary>
        UniTask<NetworkSession> CreateKcpSessionAsync(string sessionId, string host, int port, uint conv, KcpTransportConfig config = null, CancellationToken token = default);
        /// <summary>
        /// 创建 UDP 客户端逻辑会话。
        /// </summary>
        UniTask<NetworkSession> CreateUdpSessionAsync(string sessionId, string host, int port, CancellationToken token = default);

        /// <summary>
        /// 启动 KCP 服务端监听。
        /// </summary>
        UniTask StartKcpServerAsync(string host, int port, KcpServerConfig config = null, CancellationToken token = default);
        /// <summary>
        /// 启动 TCP 服务端监听。
        /// </summary>
        UniTask StartTcpServerAsync(string host, int port, CancellationToken token = default);
        /// <summary>
        /// 启动 UDP 服务端监听。
        /// </summary>
        UniTask StartUdpServerAsync(string host, int port, UdpServerConfig config = null, CancellationToken token = default);

        /// <summary>
        /// 停止 KCP 服务端监听。
        /// </summary>
        void StopKcpServer();
        /// <summary>
        /// 停止 TCP 服务端监听。
        /// </summary>
        void StopTcpServer();
        /// <summary>
        /// 停止 UDP 服务端监听。
        /// </summary>
        void StopUdpServer();

        /// <summary>
        /// 按标识查询逻辑会话。
        /// </summary>
        NetworkSession GetSession(string sessionId);
        /// <summary>
        /// 获取服务端逻辑会话快照。
        /// </summary>
        List<NetworkSession> GetServerSessionsSnapshot();
        /// <summary>
        /// 断开指定逻辑会话。
        /// </summary>
        void DisconnectSession(string sessionId);
        /// <summary>
        /// 移除并释放指定逻辑会话。
        /// </summary>
        void RemoveSession(string sessionId);
    }
}
