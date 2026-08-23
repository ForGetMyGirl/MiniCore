using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 处理 MiniBomber 房间设置请求。
    /// </summary>
    [MiniBomberServerHandler(MiniBomberServerRole.Lobby)]
    public sealed class MiniBomberUpdateRoomHandler : ARpcHandler<MiniBomberUpdateRoomRequest, MiniBomberUpdateRoomResponse>
    {
        /// <summary>
        /// 校验房主权限并同步房间设置。
        /// </summary>
        /// <param name="session">请求会话。</param>
        /// <param name="request">更新请求。</param>
        /// <param name="response">更新响应。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberUpdateRoomRequest request, MiniBomberUpdateRoomResponse response)
        {
            if (!MiniBomberServerHandlerUtility.TryGetRuntime(this, response, out MiniBomberServerRuntimeComponent runtime))
            {
                return MTask.CompletedTask;
            }

            try
            {
                runtime.UpdateRoom(session, request, response);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }
}
