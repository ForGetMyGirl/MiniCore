using System;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// MiniBomber 客户端场景、房间、战斗与断线重连编排组件。
    /// </summary>
    public sealed class MiniBomberClientFlowComponent : AComponent
    {
        #region Private 私有成员

        private static readonly int[] ReconnectDelayMilliseconds = { 1000, 2000, 4000, 4000, 4000 }; // 计划约定的重连退避序列。
        private ISceneService sceneService; // YooAsset 单场景服务。
        private MiniCore.UI.IUIService uiService; // 应用级 UI 服务。
        private INetworkService network; // 网络服务。
        private AccountSessionComponent account; // 账号会话组件。
        private LobbyComponent lobby; // 大厅组件。
        private RoomComponent room; // 房间组件。
        private BattleClientComponent battle; // 战斗组件。
        private MiniBomberRuntimeConfig runtimeConfig; // 可选运行时配置资产。
        private MiniBomberMatchPrepareNotice pendingPrepare; // 当前加载中的比赛参数。
        private MiniCore.UI.UIWindowHandle reconnectHandle; // 当前重连系统遮罩句柄。
        private MiniCore.UI.UIWindowHandle resultHandle; // 当前成绩弹窗句柄。
        private MiniCore.UI.UIWindowHandle battleHudHandle; // 当前战斗 HUD 精确句柄。
        private bool returningToRoom; // 是否正在从成绩状态返回房间。
        private bool reconnecting; // 是否已有重连循环运行。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 流程状态变化事件。
        /// </summary>
        public event Action Changed;

        /// <summary>
        /// 当前客户端流程状态。
        /// </summary>
        public MiniBomberClientFlowState State { get; private set; }

        /// <summary>
        /// 当前比赛倒计时消息。
        /// </summary>
        public MiniBomberMatchCountdownNotice Countdown { get; private set; }

        /// <summary>
        /// 当前重连尝试序号，从一开始。
        /// </summary>
        public int ReconnectAttempt { get; private set; }

        /// <summary>
        /// 最近流程提示或错误。
        /// </summary>
        public string Message { get; private set; } = string.Empty;

        /// <summary>
        /// 取得客户端组件依赖并进入登录场景。
        /// </summary>
        /// <param name="config">可选运行时配置；为空时使用默认场景地址。</param>
        /// <returns>登录场景加载完成任务。</returns>
        public async MTask InitializeAsync(MiniBomberRuntimeConfig config)
        {
            runtimeConfig = config;
            sceneService = Global.GetService<ISceneService>(this);
            uiService = Global.GetService<MiniCore.UI.IUIService>(this);
            network = Global.GetService<INetworkService>(this);
            account = Global.Get<AccountSessionComponent>(this);
            lobby = Global.Get<LobbyComponent>(this);
            room = Global.Get<RoomComponent>(this);
            battle = Global.Get<BattleClientComponent>(this);
            battle.ResultChanged += HandleResultChanged;
            room.Changed += HandleRoomChanged;
            account.Disconnected += HandleTransportDisconnected;
            State = MiniBomberClientFlowState.Login;
            MiniCore.UI.UIWindowHandle loadingHandle = await OpenWindowIfAvailableAsync(MiniBomberConstants.SceneLoadingWindowRoute);
            try
            {
                await LoadSceneAsync(runtimeConfig != null ? runtimeConfig.LoginSceneAddress : "LoginScene");
                await NavigateWindowIfAvailableAsync(MiniBomberConstants.LoginWindowRoute);
            }
            finally
            {
                await CloseLoadingAsync(loadingHandle);
            }

            Changed?.Invoke();
        }

        /// <summary>
        /// 根据登录或恢复响应切换到大厅、房间或战斗流程。
        /// </summary>
        /// <param name="destination">服务器权威目标状态。</param>
        /// <param name="roomSnapshot">可选恢复房间快照。</param>
        /// <returns>场景切换和状态应用完成任务。</returns>
        public async MTask NavigateAsync(MiniBomberClientDestination destination, MiniBomberRoomSnapshotDto roomSnapshot = null)
        {
            if (roomSnapshot != null)
            {
                room.ApplySnapshot(roomSnapshot);
            }

            MiniCore.UI.UIWindowHandle loadingHandle = await OpenWindowIfAvailableAsync(MiniBomberConstants.SceneLoadingWindowRoute);
            try
            {
                if (destination != MiniBomberClientDestination.MiniBomberDestinationBattle)
                {
                    await CloseBattleHudAsync();
                }

                switch (destination)
                {
                    case MiniBomberClientDestination.MiniBomberDestinationBattle:
                        State = MiniBomberClientFlowState.LoadingBattle;
                        Message = "正在恢复战斗场景";
                        Changed?.Invoke();
                        await uiService.CloseNavigationAsync(MiniBomberConstants.MainNavigationGroup);
                        await LoadSceneAsync(runtimeConfig != null ? runtimeConfig.BattleSceneAddress : "BattleScene");
                        await OpenBattleHudAsync();
                        State = MiniBomberClientFlowState.Battle;
                        break;
                    case MiniBomberClientDestination.MiniBomberDestinationRoom:
                        Message = "正在进入房间";
                        Changed?.Invoke();
                        await LoadLobbySceneAsync();
                        State = MiniBomberClientFlowState.Room;
                        await NavigateWindowIfAvailableAsync(MiniBomberConstants.RoomWindowRoute);
                        break;
                    case MiniBomberClientDestination.MiniBomberDestinationLobby:
                        Message = "正在进入大厅";
                        Changed?.Invoke();
                        await LoadLobbySceneAsync();
                        State = MiniBomberClientFlowState.Lobby;
                        await lobby.RefreshAsync();
                        await NavigateWindowIfAvailableAsync(MiniBomberConstants.LobbyWindowRoute);
                        break;
                    default:
                        Message = "正在返回登录界面";
                        Changed?.Invoke();
                        State = MiniBomberClientFlowState.Login;
                        await LoadSceneAsync(runtimeConfig != null ? runtimeConfig.LoginSceneAddress : "LoginScene");
                        await NavigateWindowIfAvailableAsync(MiniBomberConstants.LoginWindowRoute);
                        break;
                }

                Message = string.Empty;
                Changed?.Invoke();
            }
            finally
            {
                await CloseLoadingAsync(loadingHandle);
            }
        }

        /// <summary>
        /// 处理服务器比赛准备消息，切换战斗场景并报告加载完成。
        /// </summary>
        /// <param name="notice">服务器比赛准备参数。</param>
        /// <returns>场景切换和就绪 RPC 完成任务。</returns>
        public async MTask HandleMatchPrepareAsync(MiniBomberMatchPrepareNotice notice)
        {
            if (notice == null || notice.MatchId <= 0)
            {
                return;
            }

            pendingPrepare = notice;
            State = MiniBomberClientFlowState.LoadingBattle;
            Message = "正在加载战斗场景";
            battle.ResetBattle();
            Changed?.Invoke();
            MiniCore.UI.UIWindowHandle loadingHandle = await OpenWindowIfAvailableAsync(MiniBomberConstants.SceneLoadingWindowRoute);
            try
            {
                await uiService.CloseNavigationAsync(MiniBomberConstants.MainNavigationGroup);
                await LoadSceneAsync(notice.BattleSceneAddress);
                await OpenBattleHudAsync();
                MiniBomberSceneReadyResponse response = await network.CallAsync<MiniBomberSceneReadyRequest, MiniBomberSceneReadyResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberSceneReadyRequest
                {
                    PlayerId = account.PlayerId,
                    RoomId = notice.RoomId,
                    MatchId = notice.MatchId
                }, timeoutSeconds: 15);
                if (response.Code != MiniBomberErrorCode.Success)
                {
                    Message = response.Msg;
                    Changed?.Invoke();
                }
            }
            finally
            {
                await CloseLoadingAsync(loadingHandle);
            }
        }

        /// <summary>
        /// 应用服务器统一倒计时并进入战斗状态。
        /// </summary>
        /// <param name="notice">倒计时消息。</param>
        public void ApplyCountdown(MiniBomberMatchCountdownNotice notice)
        {
            Countdown = notice;
            State = MiniBomberClientFlowState.Battle;
            Message = string.Empty;
            Changed?.Invoke();
        }

        /// <summary>
        /// 处理网络断开并按 1、2、4、4、4 秒退避恢复会话。
        /// </summary>
        /// <param name="reason">断开原因。</param>
        public void HandleDisconnected(string reason, bool mayResume = true)
        {
            account.MarkDisconnected();
            Message = string.IsNullOrWhiteSpace(reason) ? "网络连接已断开" : reason;
            if (!mayResume)
            {
                HandleNonResumableDisconnectAsync().Forget();
            }
            else if (!reconnecting && account.IsAuthenticated)
            {
                OpenReconnectOverlayAsync().Forget();
                ReconnectAsync().Forget();
            }
            else if (!account.IsAuthenticated)
            {
                State = MiniBomberClientFlowState.Login;
                Changed?.Invoke();
            }
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 解除成绩事件并释放全部 Global 引用。
        /// </summary>
        protected override void OnDispose()
        {
            if (battle != null)
            {
                battle.ResultChanged -= HandleResultChanged;
            }

            if (room != null)
            {
                room.Changed -= HandleRoomChanged;
            }
            if (account != null)
            {
                account.Disconnected -= HandleTransportDisconnected;
            }

            Changed = null;
            sceneService = null;
            uiService = null;
            network = null;
            account = null;
            lobby = null;
            room = null;
            battle = null;
            runtimeConfig = null;
            pendingPrepare = null;
            reconnectHandle = null;
            resultHandle = null;
            battleHudHandle = null;
            Global.ReleaseAll(this);
            base.OnDispose();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 执行有上限的断线重连循环并恢复服务器目的地。
        /// </summary>
        /// <returns>重连流程完成任务。</returns>
        private async MTask ReconnectAsync()
        {
            reconnecting = true;
            State = MiniBomberClientFlowState.Reconnecting;
            try
            {
                for (int index = 0; index < ReconnectDelayMilliseconds.Length; index++)
                {
                    ReconnectAttempt = index + 1;
                    Message = $"连接中断，{ReconnectDelayMilliseconds[index] / 1000} 秒后进行第 {ReconnectAttempt} 次恢复";
                    Changed?.Invoke();
                    await MTask.Delay(ReconnectDelayMilliseconds[index]);
                    bool connected = await account.ConnectAsync();
                    if (!connected)
                    {
                        continue;
                    }

                    MiniBomberResumeSessionResponse response = await account.ResumeAsync();
                    if (response.Code == MiniBomberErrorCode.Success)
                    {
                        await NavigateAsync(response.Destination, response.Room);
                        return;
                    }
                }

                Message = "重连超时，请重新登录";
                await NavigateAsync(MiniBomberClientDestination.MiniBomberDestinationLogin);
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 重连失败：{exception}");
                Message = "重连失败，请重新登录";
                State = MiniBomberClientFlowState.Login;
            }
            finally
            {
                reconnecting = false;
                ReconnectAttempt = 0;
                if (reconnectHandle != null && reconnectHandle.IsValid)
                {
                    await uiService.CloseAsync(reconnectHandle);
                    reconnectHandle = null;
                }
                Changed?.Invoke();
            }
        }

        /// <summary>
        /// 打开全局重连遮罩并保存精确窗口句柄。
        /// </summary>
        /// <returns>重连遮罩打开完成任务。</returns>
        private async MTask OpenReconnectOverlayAsync()
        {
            reconnectHandle = await OpenWindowIfAvailableAsync(MiniBomberConstants.ReconnectOverlayRoute);
        }

        /// <summary>
        /// 处理服务器最终成绩并切换流程状态。
        /// </summary>
        private void HandleResultChanged()
        {
            State = MiniBomberClientFlowState.Result;
            OpenResultAsync().Forget();
            Changed?.Invoke();
        }

        /// <summary>
        /// 将底层 KCP 断开事件转入统一重连流程。
        /// </summary>
        private void HandleTransportDisconnected()
        {
            HandleDisconnected("网络连接已断开");
        }

        /// <summary>
        /// 保存成绩弹窗句柄，供服务器恢复房间状态后精确关闭。
        /// </summary>
        /// <returns>成绩弹窗打开完成任务。</returns>
        private async MTask OpenResultAsync()
        {
            resultHandle = await OpenWindowIfAvailableAsync(MiniBomberConstants.MatchResultWindowRoute);
        }

        /// <summary>
        /// 服务器把结果房间恢复为等待状态后返回房间场景和界面。
        /// </summary>
        private void HandleRoomChanged()
        {
            if (!returningToRoom && State == MiniBomberClientFlowState.Result && room.Current != null && room.Current.State == MiniBomberRoomState.MiniBomberRoomWaiting)
            {
                ReturnToRoomAsync().Forget();
            }
            else if (!returningToRoom && State == MiniBomberClientFlowState.LoadingBattle && room.Current != null && room.Current.State == MiniBomberRoomState.MiniBomberRoomWaiting)
            {
                Message = "有玩家战斗场景加载超时，已返回房间";
                ReturnToRoomAsync().Forget();
            }
        }

        /// <summary>
        /// 关闭成绩弹窗并返回房间流程。
        /// </summary>
        /// <returns>场景和窗口切换完成任务。</returns>
        private async MTask ReturnToRoomAsync()
        {
            returningToRoom = true;
            try
            {
                if (resultHandle != null && resultHandle.IsValid)
                {
                    await uiService.CloseAsync(resultHandle);
                    resultHandle = null;
                }

                await NavigateAsync(MiniBomberClientDestination.MiniBomberDestinationRoom, room.Current);
            }
            finally
            {
                returningToRoom = false;
            }
        }

        /// <summary>
        /// 打开战斗 HUD 并保存精确句柄；已有有效 HUD 时保持当前实例。
        /// </summary>
        /// <returns>战斗 HUD 可用后的完成任务。</returns>
        private async MTask OpenBattleHudAsync()
        {
            if (battleHudHandle != null && battleHudHandle.IsValid)
            {
                return;
            }

            battleHudHandle = await OpenWindowIfAvailableAsync(MiniBomberConstants.BattleHudWindowRoute);
        }

        /// <summary>
        /// 关闭当前战斗 HUD，使非战斗场景不保留 Hud Layer 内容。
        /// </summary>
        /// <returns>战斗 HUD 关闭完成任务；没有有效句柄时立即完成。</returns>
        private async MTask CloseBattleHudAsync()
        {
            MiniCore.UI.UIWindowHandle handle = battleHudHandle;
            battleHudHandle = null;
            if (handle != null && handle.IsValid)
            {
                await uiService.CloseAsync(handle);
            }
        }

        /// <summary>
        /// 加载大厅和房间共用的非战斗场景。
        /// </summary>
        /// <returns>大厅场景加载完成任务。</returns>
        private MTask LoadLobbySceneAsync()
        {
            return LoadSceneAsync(runtimeConfig != null ? runtimeConfig.LobbySceneAddress : "LobbyScene");
        }

        /// <summary>
        /// 通过场景服务完成一次单场景切换；Loading 生命周期由完整业务流程持有。
        /// </summary>
        /// <param name="address">YooAsset 场景地址。</param>
        /// <returns>场景切换完成任务。</returns>
        private async MTask LoadSceneAsync(string address)
        {
            await sceneService.LoadSingleAsync(address);
        }

        /// <summary>
        /// 精确关闭当前流程打开的 Loading 窗口；路由缺失时保持无操作。
        /// </summary>
        /// <param name="loadingHandle">当前流程持有的 Loading 窗口句柄。</param>
        /// <returns>Loading 窗口关闭完成任务。</returns>
        private async MTask CloseLoadingAsync(MiniCore.UI.UIWindowHandle loadingHandle)
        {
            if (loadingHandle != null && loadingHandle.IsValid)
            {
                await uiService.CloseAsync(loadingHandle);
            }
        }

        /// <summary>
        /// 处理被新登录替换或服务器明确拒绝恢复的断开。
        /// </summary>
        /// <returns>本地令牌清理和登录场景切换完成任务。</returns>
        private async MTask HandleNonResumableDisconnectAsync()
        {
            await account.LogoutAsync();
            await NavigateAsync(MiniBomberClientDestination.MiniBomberDestinationLogin);
        }

        /// <summary>
        /// 打开已生成的可叠加窗口；美术 Prefab 尚未完成时仅记录提示，不阻断场景开发。
        /// </summary>
        /// <param name="routeName">稳定路由名称。</param>
        /// <returns>窗口句柄；路由尚未生成时返回空句柄。</returns>
        private async MTask<MiniCore.UI.UIWindowHandle> OpenWindowIfAvailableAsync(string routeName)
        {
            try
            {
                return await uiService.OpenAsync(routeName);
            }
            catch (InvalidOperationException exception)
            {
                LogSwitch.Warning($"MiniBomber UI 路由尚未生成：{routeName}。完成对应 Prefab 后重新生成 UI 注册表。{exception.Message}");
                return null;
            }
        }

        /// <summary>
        /// 导航到已生成的 Screen 窗口；美术 Prefab 尚未完成时不阻断场景开发。
        /// </summary>
        /// <param name="routeName">稳定路由名称。</param>
        /// <returns>导航完成任务。</returns>
        private async MTask NavigateWindowIfAvailableAsync(string routeName)
        {
            try
            {
                await uiService.NavigateAsync(routeName);
            }
            catch (InvalidOperationException exception)
            {
                LogSwitch.Warning($"MiniBomber UI 路由尚未生成：{routeName}。完成对应 Prefab 后重新生成 UI 注册表。{exception.Message}");
            }
        }

        #endregion
    }
}
