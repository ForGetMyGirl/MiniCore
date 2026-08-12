using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 处理 MiniBomber 加入房间请求。
    /// </summary>
    public sealed class MiniBomberJoinRoomHandler : ARpcHandler<MiniBomberJoinRoomRequest, MiniBomberJoinRoomResponse>
    {
        /// <summary>
        /// 把玩家加入等待状态房间。
        /// </summary>
        /// <param name="session">请求会话。</param>
        /// <param name="request">加入房间请求。</param>
        /// <param name="response">加入房间响应。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberJoinRoomRequest request, MiniBomberJoinRoomResponse response)
        {
            if (!MiniBomberServerHandlerUtility.TryGetRuntime(this, response, out MiniBomberServerRuntimeComponent runtime))
            {
                return MTask.CompletedTask;
            }

            try
            {
                runtime.JoinRoom(session, request, response);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }
}
