using Cysharp.Threading.Tasks;
using MiniCore.Model;

namespace MiniCore.HotUpdate
{
    public class DemoNormalHandler : AMHandler<DemoNormalMessage>
    {
        public override async UniTask HandleAsync(NetworkSession session, DemoNormalMessage message)
        {
            await UniTask.SwitchToMainThread();
            string text = $"收到普通消息，会话:{session.SessionId} 内容:{message.Content}";
            EventCenter.Broadcast(GameEvent.LogInfo, text);
            EventCenter.Broadcast(HotEvent.KcpTestMessage, text);
        }
    }
}