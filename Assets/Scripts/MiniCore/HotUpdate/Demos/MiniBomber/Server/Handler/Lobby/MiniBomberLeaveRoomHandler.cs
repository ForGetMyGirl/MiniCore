using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 处理 MiniBomber 离开房间请求。
    /// </summary>
    [MiniBomberServerHandler(MiniBomberServerRole.Lobby)]
    public sealed class MiniBomberLeaveRoomHandler : ARpcHandler<MiniBomberLeaveRoomRequest, MiniBomberLeaveRoomResponse>
    {
        /// <summary>
        /// 让玩家离开等待状态房间。
        /// </summary>
        /// <param name="session">请求会话。</param>
        /// <param name="request">离开房间请求。</param>
        /// <param name="response">离开房间响应。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberLeaveRoomRequest request, MiniBomberLeaveRoomResponse response)
        {
            if (!MiniBomberServerHandlerUtility.TryGetRuntime(this, response, out MiniBomberServerRuntimeComponent runtime))
            {
                return MTask.CompletedTask;
            }

            try
            {
                runtime.LeaveRoom(session, request, response);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }
}
