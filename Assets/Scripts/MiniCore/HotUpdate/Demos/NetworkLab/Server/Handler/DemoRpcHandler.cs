using MiniCore.Threading;
using MiniCore.Core;
using MiniCore.Eventing;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// 回显示例 RPC 请求内容的处理器。
    /// </summary>
    [ServerHandler(DedicatedServerRole.All)]
    public class DemoRpcHandler : ARpcHandler<DemoRpcRequest, DemoRpcResponse>
    {
        #region Public 公共成员

        /// <summary>
        /// 处理示例 RPC 请求并写入回显响应。
        /// </summary>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        /// <param name="request">执行该方法所需的 request 参数。</param>
        /// <param name="response">执行该方法所需的 response 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public override MTask HandleAsync(NetworkSession session, DemoRpcRequest request, DemoRpcResponse response)
        {
            string text = $"收到RPC请求，会话:{session.SessionId} 内容:{request.Payload}";
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

            response.Code = 0;
            response.Msg = "RPC响应成功";
            response.Echo = request.Payload;
            return MTask.CompletedTask;
        }

        #endregion
    }
}
