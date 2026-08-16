using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 处理其他 Dedicated Server 直连 Match Role 的玩家入队请求。
    /// </summary>
    [ServerHandler(DedicatedServerRole.Match)]
    public sealed class EnqueueMatchHandler : ARpcHandler<EnqueueMatchRequest, EnqueueMatchResponse>
    {
        #region Public 公共成员

        /// <summary>
        /// 创建唯一匹配票据。
        /// </summary>
        /// <param name="session">发起内网 RPC 的服务器会话。</param>
        /// <param name="request">玩家入队请求。</param>
        /// <param name="response">待填写的入队结果。</param>
        /// <returns>处理完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, EnqueueMatchRequest request, EnqueueMatchResponse response)
        {
            MiniBomberMatchServerComponent match = Global.Get<MiniBomberMatchServerComponent>(this);
            try
            {
                if (!match.TryEnqueue(request.PlayerId, request.Rating, out long ticketId))
                {
                    response.Code = 409;
                    response.Msg = "玩家已经在匹配队列中或参数无效";
                    return MTask.CompletedTask;
                }

                response.Code = 0;
                response.TicketId = ticketId;
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
