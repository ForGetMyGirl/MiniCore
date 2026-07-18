using Cysharp.Threading.Tasks;
using MiniCore.Model;
using MiniCore.Protocol.Generated;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// 回显示例 RPC 请求内容的处理器。
    /// </summary>
    public class DemoRpcHandler : ARpcHandler<DemoRpcRequest, DemoRpcResponse>
    {
        /// <summary>
        /// 处理示例 RPC 请求并写入回显响应。
        /// </summary>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        /// <param name="request">执行该方法所需的 request 参数。</param>
        /// <param name="response">执行该方法所需的 response 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public override UniTask HandleAsync(NetworkSession session, DemoRpcRequest request, DemoRpcResponse response)
        {
            string text = $"收到RPC请求，会话:{session.SessionId} 内容:{request.Payload}";
            LogSwitch.Info(text);
            EventCenter.Broadcast(HotEvent.KcpTestMessage, text);

            response.Code = 0;
            response.Msg = "RPC响应成功";
            response.Echo = request.Payload;
            return UniTask.CompletedTask;
        }
    }
}
