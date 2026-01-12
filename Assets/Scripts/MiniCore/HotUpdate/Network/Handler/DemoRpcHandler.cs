using Cysharp.Threading.Tasks;
using MiniCore.Model;

namespace MiniCore.HotUpdate
{
    public class DemoRpcHandler : ARpcHandler<DemoRpcRequest, DemoRpcResponse>
    {
        public override UniTask HandleAsync(NetworkSession session, DemoRpcRequest request, DemoRpcResponse response)
        {
            string text = $"收到RPC请求，会话:{session.SessionId} 内容:{request.Payload}";
            EventCenter.Broadcast(GameEvent.LogInfo, text);
            EventCenter.Broadcast(HotEvent.KcpTestMessage, text);

            response.ErrorCode = 0;
            response.Message = "RPC响应成功";
            response.Echo = request.Payload;
            return UniTask.CompletedTask;
        }
    }
}
