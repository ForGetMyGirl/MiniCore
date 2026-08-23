using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 处理 MiniBomber 大厅完整快照请求。
    /// </summary>
    [MiniBomberServerHandler(MiniBomberServerRole.Lobby)]
    public sealed class MiniBomberLobbySnapshotHandler : ARpcHandler<MiniBomberLobbySnapshotRequest, MiniBomberLobbySnapshotResponse>
    {
        /// <summary>
        /// 返回权威大厅房间列表。
        /// </summary>
        /// <param name="session">请求会话。</param>
        /// <param name="request">大厅请求。</param>
        /// <param name="response">大厅响应。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberLobbySnapshotRequest request, MiniBomberLobbySnapshotResponse response)
        {
            if (!MiniBomberServerHandlerUtility.TryGetRuntime(this, response, out MiniBomberServerRuntimeComponent runtime))
            {
                return MTask.CompletedTask;
            }

            try
            {
                runtime.GetLobbySnapshot(session, request, response);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }
}
