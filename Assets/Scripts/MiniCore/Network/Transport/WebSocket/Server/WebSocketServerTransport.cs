using System;
using MiniCore.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// 将已完成 WebSocket 长度帧解析的服务端会话包装为统一传输。
    /// </summary>
    public sealed class WebSocketServerTransport : INetworkTransport
    {
        #region Private 私有成员

        private readonly IServerSession session; // 被包装的服务端会话。
        private bool closed; // 传输关闭状态。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 创建服务端 WebSocket 传输包装器。
        /// </summary>
        /// <param name="session">已完成握手的 WebSocket 服务端会话。</param>
        public WebSocketServerTransport(IServerSession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            session.OnDisconnected += HandleSessionDisconnected;
        }

        /// <summary>
        /// 获取底层会话是否保持连接。
        /// </summary>
        public bool IsConnected => !closed && session.IsConnected;

        /// <summary>
        /// 监听器完成长度帧解析后触发。
        /// </summary>
        public event Func<ReadOnlyMemory<byte>, MTask> OnDataReceived;

        /// <summary>
        /// 底层会话关闭时触发。
        /// </summary>
        public event Action OnDisconnected;

        /// <summary>
        /// 服务端传输不支持主动连接。
        /// </summary>
        /// <param name="host">未使用的主机。</param>
        /// <param name="port">未使用的端口。</param>
        /// <returns>此方法始终抛出异常。</returns>
        public MTask ConnectAsync(string host, int port)
        {
            throw new NotSupportedException("WebSocket 服务端传输不支持主动连接。");
        }

        /// <summary>
        /// 通过服务端会话发送业务包。
        /// </summary>
        /// <param name="data">完整业务包正文。</param>
        /// <returns>发送完成任务。</returns>
        public MTask SendAsync(ArraySegment<byte> data)
        {
            return session.SendAsync(data);
        }

        /// <summary>
        /// 关闭底层服务端会话。
        /// </summary>
        public void Disconnect()
        {
            if (closed)
            {
                return;
            }

            closed = true;
            session.Close();
            OnDisconnected?.Invoke();
        }

        /// <summary>
        /// 释放包装器。
        /// </summary>
        public void Dispose()
        {
            Disconnect();
            session.OnDisconnected -= HandleSessionDisconnected;
            OnDataReceived = null;
            OnDisconnected = null;
        }

        /// <summary>
        /// 将监听器收到的完整业务包派发给网络会话。
        /// </summary>
        /// <param name="data">完整业务包正文。</param>
        /// <returns>派发完成任务。</returns>
        public MTask PushReceivedAsync(ReadOnlyMemory<byte> data)
        {
            return TransportEventDispatcher.DispatchAsync(OnDataReceived, data);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 响应底层会话关闭。
        /// </summary>
        private void HandleSessionDisconnected()
        {
            if (closed)
            {
                return;
            }

            closed = true;
            OnDisconnected?.Invoke();
        }

        #endregion
    }
}
