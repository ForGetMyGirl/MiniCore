using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 接收比赛场景加载参数。
    /// </summary>
    public sealed class MiniBomberMatchPrepareHandler : AMHandler<MiniBomberMatchPrepareNotice>
    {
        #region Override 重写实现

        /// <summary>
        /// 将场景切换任务交给客户端流程监督，立即释放串行收包队列以接收后续就绪 RPC 响应。
        /// </summary>
        /// <param name="session">消息会话。</param>
        /// <param name="message">比赛准备通知。</param>
        /// <returns>通知接收完成任务；场景切换由流程组件的任务域继续监督。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberMatchPrepareNotice message)
        {
            MiniBomberClientFlowComponent flow = Global.Get<MiniBomberClientFlowComponent>(this);
            try
            {
                flow?.HandleMatchPrepareAsync(message).Forget();
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }

        #endregion
    }
}
