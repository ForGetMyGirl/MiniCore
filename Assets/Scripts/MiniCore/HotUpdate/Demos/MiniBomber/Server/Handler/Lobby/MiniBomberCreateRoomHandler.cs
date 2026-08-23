using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 处理 MiniBomber 创建房间请求。
    /// </summary>
    [MiniBomberServerHandler(MiniBomberServerRole.Lobby)]
    public sealed class MiniBomberCreateRoomHandler : ARpcHandler<MiniBomberCreateRoomRequest, MiniBomberCreateRoomResponse>
    {
        /// <summary>
        /// 创建房间并设置房主。
        /// </summary>
        /// <param name="session">请求会话。</param>
        /// <param name="request">创建房间请求。</param>
        /// <param name="response">创建房间响应。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberCreateRoomRequest request, MiniBomberCreateRoomResponse response)
        {
            if (!MiniBomberServerHandlerUtility.TryGetRuntime(this, response, out MiniBomberServerRuntimeComponent runtime))
            {
                return MTask.CompletedTask;
            }

            try
            {
                runtime.CreateRoom(session, request, response);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }
}
