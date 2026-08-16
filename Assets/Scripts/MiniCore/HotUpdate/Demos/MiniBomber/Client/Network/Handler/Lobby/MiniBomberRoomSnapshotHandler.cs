using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 接收当前房间权威快照。
    /// </summary>
    public sealed class MiniBomberRoomSnapshotHandler : AMHandler<MiniBomberRoomSnapshotNotice>
    {
        #region Public 公共成员

        /// <summary>
        /// 把服务器快照应用到房间组件。
        /// </summary>
        /// <param name="session">消息会话。</param>
        /// <param name="message">房间快照通知。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberRoomSnapshotNotice message)
        {
            RoomComponent room = Global.Get<RoomComponent>(this);
            try
            {
                room?.ApplySnapshot(message.Room);
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
