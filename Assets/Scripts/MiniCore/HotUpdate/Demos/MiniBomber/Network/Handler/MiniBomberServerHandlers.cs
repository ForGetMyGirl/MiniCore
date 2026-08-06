using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 服务端 Handler 获取运行时的公共辅助方法。
    /// </summary>
    internal static class MiniBomberServerHandlerUtility
    {
        #region Internal 内部成员

        /// <summary>
        /// 获取已经初始化的 Dedicated Server 运行时。
        /// </summary>
        /// <param name="owner">Global 引用所有者。</param>
        /// <param name="response">运行时不可用时写入错误的响应。</param>
        /// <param name="runtime">可用服务器运行时。</param>
        /// <returns>运行时存在且已经初始化时返回 true。</returns>
        internal static bool TryGetRuntime(object owner, IRpcResponse response, out MiniBomberServerRuntimeComponent runtime)
        {
            runtime = Global.Get<MiniBomberServerRuntimeComponent>(owner);
            if (runtime != null && runtime.IsInitialized)
            {
                return true;
            }

            response.Code = MiniBomberErrorCode.InvalidRoomState;
            response.Msg = "MiniBomber 服务器尚未就绪";
            Global.ReleaseAll(owner);
            return false;
        }

        #endregion
    }

    /// <summary>处理 MiniBomber 注册请求。</summary>
    public sealed class MiniBomberRegisterHandler : ARpcHandler<MiniBomberRegisterRequest, MiniBomberRegisterResponse>
    {
        /// <summary>
        /// 验证版本并持久化新账号。
        /// </summary>
        /// <param name="session">请求会话。</param>
        /// <param name="request">注册请求。</param>
        /// <param name="response">注册响应。</param>
        /// <returns>注册完成任务。</returns>
        public override async MTask HandleAsync(NetworkSession session, MiniBomberRegisterRequest request, MiniBomberRegisterResponse response)
        {
            if (!MiniBomberServerHandlerUtility.TryGetRuntime(this, response, out MiniBomberServerRuntimeComponent runtime))
            {
                return;
            }

            try
            {
                await runtime.RegisterAsync(session, request, response);
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }

    /// <summary>处理 MiniBomber 登录请求。</summary>
    public sealed class MiniBomberLoginHandler : ARpcHandler<MiniBomberLoginRequest, MiniBomberLoginResponse>
    {
        /// <summary>
        /// 验证账号并绑定服务器会话。
        /// </summary>
        /// <param name="session">请求会话。</param>
        /// <param name="request">登录请求。</param>
        /// <param name="response">登录响应。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberLoginRequest request, MiniBomberLoginResponse response)
        {
            if (!MiniBomberServerHandlerUtility.TryGetRuntime(this, response, out MiniBomberServerRuntimeComponent runtime))
            {
                return MTask.CompletedTask;
            }

            try
            {
                runtime.Login(session, request, response);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }

    /// <summary>处理 MiniBomber 断线恢复请求。</summary>
    public sealed class MiniBomberResumeSessionHandler : ARpcHandler<MiniBomberResumeSessionRequest, MiniBomberResumeSessionResponse>
    {
        /// <summary>
        /// 恢复认证、房间和比赛状态。
        /// </summary>
        /// <param name="session">新网络会话。</param>
        /// <param name="request">恢复请求。</param>
        /// <param name="response">恢复响应。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberResumeSessionRequest request, MiniBomberResumeSessionResponse response)
        {
            if (!MiniBomberServerHandlerUtility.TryGetRuntime(this, response, out MiniBomberServerRuntimeComponent runtime))
            {
                return MTask.CompletedTask;
            }

            try
            {
                runtime.ResumeSession(session, request, response);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }

    /// <summary>处理 MiniBomber 大厅完整快照请求。</summary>
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

    /// <summary>处理 MiniBomber 创建房间请求。</summary>
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

    /// <summary>处理 MiniBomber 加入房间请求。</summary>
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

    /// <summary>处理 MiniBomber 离开房间请求。</summary>
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

    /// <summary>处理 MiniBomber 房间设置请求。</summary>
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

    /// <summary>处理 MiniBomber 准备状态请求。</summary>
    public sealed class MiniBomberSetReadyHandler : ARpcHandler<MiniBomberSetReadyRequest, MiniBomberSetReadyResponse>
    {
        /// <summary>
        /// 更新单个成员准备状态。
        /// </summary>
        /// <param name="session">请求会话。</param>
        /// <param name="request">准备请求。</param>
        /// <param name="response">准备响应。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberSetReadyRequest request, MiniBomberSetReadyResponse response)
        {
            if (!MiniBomberServerHandlerUtility.TryGetRuntime(this, response, out MiniBomberServerRuntimeComponent runtime))
            {
                return MTask.CompletedTask;
            }

            try
            {
                runtime.SetReady(session, request, response);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }

    /// <summary>处理 MiniBomber 房主开始比赛请求。</summary>
    public sealed class MiniBomberStartMatchHandler : ARpcHandler<MiniBomberStartMatchRequest, MiniBomberStartMatchResponse>
    {
        /// <summary>
        /// 校验开局条件并进入场景加载阶段。
        /// </summary>
        /// <param name="session">请求会话。</param>
        /// <param name="request">开局请求。</param>
        /// <param name="response">开局响应。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberStartMatchRequest request, MiniBomberStartMatchResponse response)
        {
            if (!MiniBomberServerHandlerUtility.TryGetRuntime(this, response, out MiniBomberServerRuntimeComponent runtime))
            {
                return MTask.CompletedTask;
            }

            try
            {
                runtime.StartMatch(session, request, response);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }

    /// <summary>处理 MiniBomber 客户端战斗场景就绪请求。</summary>
    public sealed class MiniBomberSceneReadyHandler : ARpcHandler<MiniBomberSceneReadyRequest, MiniBomberSceneReadyResponse>
    {
        /// <summary>
        /// 标记加载完成并在全员就绪后创建权威比赛。
        /// </summary>
        /// <param name="session">请求会话。</param>
        /// <param name="request">场景就绪请求。</param>
        /// <param name="response">场景就绪响应。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberSceneReadyRequest request, MiniBomberSceneReadyResponse response)
        {
            if (!MiniBomberServerHandlerUtility.TryGetRuntime(this, response, out MiniBomberServerRuntimeComponent runtime))
            {
                return MTask.CompletedTask;
            }

            try
            {
                runtime.SetSceneReady(session, request, response);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }

    /// <summary>接收 MiniBomber 高频战斗输入批次。</summary>
    public sealed class MiniBomberBattleInputHandler : AMHandler<MiniBomberBattleInputBatch>
    {
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
    }

    /// <summary>处理客户端战斗基线不匹配后的重同步请求。</summary>
    public sealed class MiniBomberBattleResyncHandler : ARpcHandler<MiniBomberBattleResyncRequest, MiniBomberBattleResyncResponse>
    {
        /// <summary>
        /// 校验比赛身份并安排单会话完整关键帧。
        /// </summary>
        /// <param name="session">请求会话。</param>
        /// <param name="request">客户端同步基线。</param>
        /// <param name="response">请求接受状态。</param>
        /// <returns>已完成任务。</returns>
        public override MTask HandleAsync(NetworkSession session, MiniBomberBattleResyncRequest request, MiniBomberBattleResyncResponse response)
        {
            if (!MiniBomberServerHandlerUtility.TryGetRuntime(this, response, out MiniBomberServerRuntimeComponent runtime))
            {
                return MTask.CompletedTask;
            }

            try
            {
                runtime.RequestBattleResync(session, request, response);
                return MTask.CompletedTask;
            }
            finally
            {
                Global.ReleaseAll(this);
            }
        }
    }
}
