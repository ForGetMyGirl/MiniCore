using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 接收 MiniBomber 高频战斗输入批次。
    /// </summary>
    [ServerHandler(DedicatedServerRole.Game)]
    public sealed class MiniBomberBattleInputHandler : AMHandler<MiniBomberBattleInputBatch>
    {
        #region Public 公共成员

        /// <summary>
        /// 把已认证玩家输入提交给权威模拟。
        /// </summary>
        /// <param name="session">输入所属会话。</param>
        /// <param name="message">输入批次。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberBattleInputBatch message)
        {
            MiniBomberServerRuntimeComponent runtime = Global.Get<MiniBomberServerRuntimeComponent>(this);
            try
            {
                runtime?.SubmitBattleInput(session, message);
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
