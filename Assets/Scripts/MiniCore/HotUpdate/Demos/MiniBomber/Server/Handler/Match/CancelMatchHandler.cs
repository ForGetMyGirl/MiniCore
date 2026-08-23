using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 处理其他 Dedicated Server 对等待票据的取消请求。
    /// </summary>
    [MiniBomberServerHandler(MiniBomberServerRole.Match)]
    public sealed class CancelMatchHandler : ARpcHandler<CancelMatchRequest, CancelMatchResponse>
    {
        #region Public 公共成员

        /// <summary>
        /// 仅在玩家和票据都匹配时移除等待项。
        /// </summary>
        /// <param name="session">发起内网 RPC 的服务器会话。</param>
        /// <param name="request">匹配取消请求。</param>
        /// <param name="response">待填写的取消结果。</param>
        /// <returns>处理完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, CancelMatchRequest request, CancelMatchResponse response)
        {
            MiniBomberMatchServerComponent match = Global.Get<MiniBomberMatchServerComponent>(this);
            try
            {
                response.Code = match.TryCancel(request.PlayerId, request.TicketId) ? 0 : 404;
                response.Msg = response.Code == 0 ? string.Empty : "匹配票据不存在";
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
