#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace MiniCore.Model
{
    /// <summary>
    /// 将 websocket-sharp 行为回调桥接到 MiniCore 原生 WebSocket 监听器。
    /// </summary>
    internal sealed class NativeWebSocketBehavior : WebSocketBehavior
    {
        #region Private 私有成员

        private Action<NativeWebSocketBehavior> opened; // 握手完成回调。
        private Action<NativeWebSocketBehavior, MessageEventArgs> messageReceived; // 消息回调。
        private Action<NativeWebSocketBehavior, CloseEventArgs> closed; // 关闭回调。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 获取当前行为对应的稳定会话标识。
        /// </summary>
        internal string SessionId => $"ws:{ID}";

        /// <summary>
        /// 获取当前连接是否保持打开。
        /// </summary>
        internal bool IsOpen => ReadyState == WebSocketState.Open;

        /// <summary>
        /// 配置握手限制和生命周期回调。
        /// </summary>
        /// <param name="config">服务端握手配置。</param>
        /// <param name="opened">握手完成回调。</param>
        /// <param name="messageReceived">消息到达回调。</param>
        /// <param name="closed">连接关闭回调。</param>
        internal void Configure(
            WebSocketServerConfig config,
            Action<NativeWebSocketBehavior> opened,
            Action<NativeWebSocketBehavior, MessageEventArgs> messageReceived,
            Action<NativeWebSocketBehavior, CloseEventArgs> closed)
        {
            IgnoreExtensions = true;
            NoDelay = true;
            HostValidator = config.HostValidator;
            OriginValidator = config.OriginValidator;
            this.opened = opened;
            this.messageReceived = messageReceived;
            this.closed = closed;
        }

        /// <summary>
        /// 异步发送完整二进制消息。
        /// </summary>
        /// <param name="data">待发送消息。</param>
        /// <param name="completion">发送结果回调。</param>
        internal void SendBinaryAsync(byte[] data, Action<bool> completion)
        {
            SendAsync(data, completion);
        }

        /// <summary>
        /// 使用指定状态码关闭当前会话。
        /// </summary>
        /// <param name="code">RFC 6455 关闭状态码。</param>
        /// <param name="reason">关闭原因。</param>
        internal void CloseSession(CloseStatusCode code, string reason)
        {
            CloseAsync(code, reason);
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 通知监听器握手已经完成。
        /// </summary>
        protected override void OnOpen()
        {
            opened?.Invoke(this);
        }

        /// <summary>
        /// 将收到的完整 WebSocket 消息转交监听器校验。
        /// </summary>
        /// <param name="args">消息参数。</param>
        protected override void OnMessage(MessageEventArgs args)
        {
            messageReceived?.Invoke(this, args);
        }

        /// <summary>
        /// 将连接关闭状态转交监听器清理会话。
        /// </summary>
        /// <param name="args">关闭参数。</param>
        protected override void OnClose(CloseEventArgs args)
        {
            closed?.Invoke(this, args);
        }

        #endregion
    }
}
#endif
