using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 接收大厅房间列表修订通知。
    /// </summary>
    public sealed class MiniBomberLobbyChangedHandler : AMHandler<MiniBomberLobbyChangedNotice>
    {
        #region Public 公共成员

        /// <summary>
        /// 通知大厅组件服务器修订号已经变化。
        /// </summary>
        /// <param name="session">消息会话。</param>
        /// <param name="message">大厅修订通知。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberLobbyChangedNotice message)
        {
            LobbyComponent lobby = Global.Get<LobbyComponent>(this);
            try
            {
                lobby?.ApplyChangedNotice(message.Revision);
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
