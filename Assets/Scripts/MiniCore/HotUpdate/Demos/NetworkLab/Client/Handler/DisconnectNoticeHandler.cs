using MiniCore.Threading;
using MiniCore.Core;
using MiniCore.Eventing;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// 处理对端断开通知并关闭本地逻辑会话。
    /// </summary>
    public class DisconnectNoticeHandler : AMHandler<DisconnectNotice>
    {
        /// <summary>
        /// 记录断开原因、广播事件并断开对应会话。
        /// </summary>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        /// <param name="message">执行该方法所需的 message 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public override MTask HandleAsync(NetworkSession session, DisconnectNotice message)
        {
            string reason = string.IsNullOrWhiteSpace(message.Reason) ? string.Empty : $" 原因:{message.Reason}";
            string text = message.IsServerShutdown
                ? $"服务端通知断开，会话:{session.SessionId}{reason}"
                : $"对端请求断开，会话:{session.SessionId}{reason}";

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

            INetworkService networkMessageComponent = Global.GetService<INetworkService>(this);
            try
            {
                networkMessageComponent.DisconnectSession(session.SessionId);
            }
            finally
            {
                Global.ReleaseAll(this);
            }

            return MTask.CompletedTask;
        }
    }
}
