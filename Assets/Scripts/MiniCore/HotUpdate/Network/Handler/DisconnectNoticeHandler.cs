using Cysharp.Threading.Tasks;
using MiniCore.Core;
using MiniCore.Model;

namespace MiniCore.HotUpdate
{
    public class DisconnectNoticeHandler : AMHandler<DisconnectNotice>
    {
        public override UniTask HandleAsync(NetworkSession session, DisconnectNotice message)
        {
            string reason = string.IsNullOrWhiteSpace(message.Reason) ? string.Empty : $" 原因:{message.Reason}";
            string text = message.IsServerShutdown
                ? $"服务端通知断开，会话:{session.SessionId}{reason}"
                : $"对端请求断开，会话:{session.SessionId}{reason}";

            EventCenter.Broadcast(GameEvent.LogInfo, text);
            EventCenter.Broadcast(HotEvent.KcpTestMessage, text);

            Global.Com.Get<NetworkSessionComponent>().DisconnectSession(session.SessionId);
            return UniTask.CompletedTask;
        }
    }
}
