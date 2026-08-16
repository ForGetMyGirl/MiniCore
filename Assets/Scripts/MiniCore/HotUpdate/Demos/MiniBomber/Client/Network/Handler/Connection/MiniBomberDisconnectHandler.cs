using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 接收服务器主动断开原因。
    /// </summary>
    public sealed class MiniBomberDisconnectHandler : AMHandler<MiniBomberDisconnectNotice>
    {
        /// <summary>
        /// 根据服务器标记进入重连或登录流程。
        /// </summary>
        /// <param name="session">消息会话。</param>
        /// <param name="message">断开通知。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberDisconnectNotice message)
        {
            MiniBomberClientFlowComponent flow = Global.Get<MiniBomberClientFlowComponent>(this);
            try
            {
            flow?.HandleDisconnected(message.Reason, message.MayResume);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }
}
