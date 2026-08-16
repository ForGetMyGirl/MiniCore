using MiniCore.Threading;
using MiniCore.Core;
using MiniCore.Eventing;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// 记录并广播普通示例消息的处理器。
    /// </summary>
    public class DemoNormalHandler : AMHandler<DemoNormalMessage>
    {
        /// <summary>
        /// 处理收到的普通示例消息。
        /// </summary>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        /// <param name="message">执行该方法所需的 message 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public override MTask HandleAsync(NetworkSession session, DemoNormalMessage message)
        {
            string text = $"收到普通消息，会话:{session.SessionId} 内容:{message.Content}";
            LogSwitch.Info(text);
            IApplicationEventBus eventBus = Global.GetOrAddModule<IApplicationEventBus>(this);
            try
            {
                eventBus.Publish(new DemoMessageReceivedEvent(session.SessionId, text));
            }
            finally
            {
                Global.ReleaseAll(this);
            }
            return MTask.CompletedTask;
        }
    }
}
