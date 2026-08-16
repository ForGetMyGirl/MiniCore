using System.Collections.Generic;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 处理 Lobby 或房间分配逻辑对一组已匹配玩家的提取请求。
    /// </summary>
    [ServerHandler(DedicatedServerRole.Match)]
    public sealed class TakeMatchHandler : ARpcHandler<TakeMatchRequest, TakeMatchResponse>
    {
        #region Private 私有成员

        private readonly List<long> playerIds = new List<long>(16); // 复用单 Handler 的低频结果缓冲。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 在候选人数足够时按顺序取出指定数量玩家。
        /// </summary>
        /// <param name="session">发起内网 RPC 的服务器会话。</param>
        /// <param name="request">组队人数请求。</param>
        /// <param name="response">待填写的成组玩家结果。</param>
        /// <returns>处理完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, TakeMatchRequest request, TakeMatchResponse response)
        {
            MiniBomberMatchServerComponent match = Global.Get<MiniBomberMatchServerComponent>(this);
            try
            {
                if (!match.TryTake(request.PlayerCount, playerIds))
                {
                    response.Code = 404;
                    response.Msg = "等待人数不足或请求人数无效";
                    return MTask.CompletedTask;
                }

                response.Code = 0;
                response.PlayerIds.Add(playerIds);
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
