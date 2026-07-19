using MiniCore.Eventing;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// 表示示例网络处理器已经收到并格式化了一条业务消息。
    /// 该事件仅用于测试面板、冒烟检查等跨模块观察，不承载网络协议本身。
    /// </summary>
    public sealed class DemoMessageReceivedEvent : ISyncEvent
    {
        #region Public 公共成员

        /// <summary>
        /// 获取产生消息的逻辑会话标识。
        /// </summary>
        public string SessionId { get; }

        /// <summary>
        /// 获取展示和诊断使用的格式化文本。
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 使用会话标识与消息文本创建事件。
        /// </summary>
        /// <param name="sessionId">产生事件的逻辑会话标识。</param>
        /// <param name="message">展示和诊断使用的文本。</param>
        public DemoMessageReceivedEvent(string sessionId, string message)
        {
            SessionId = sessionId ?? string.Empty;
            Message = message ?? string.Empty;
        }

        #endregion
    }
}
