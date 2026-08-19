using System;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;
using MiniCore.UI;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 大厅窗口 Presenter，投影大厅 Model 并协调大厅命令。
    /// </summary>
    public sealed class LobbyWindowPresenter : AUIWindowPresenter<LobbyWindowView>
    {
        #region Private 私有成员

        private readonly LobbyWindowViewData viewData = new LobbyWindowViewData(); // 复用大厅窗口显示数据。
        private AccountSessionComponent account; // 账号会话组件。
        private LobbyComponent lobby; // 大厅状态组件。
        private RoomComponent room; // 房间状态组件。
        private MiniBomberClientFlowComponent flow; // 客户端流程组件。
        private bool commandRunning; // 是否已有大厅命令执行中。
        private bool released; // Presenter 是否已经随窗口释放。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 获取业务依赖并绑定大厅意图和 Model 变化事件。
        /// </summary>
        protected override void OnBind()
        {
            account = Global.Get<AccountSessionComponent>(this);
            lobby = Global.Get<LobbyComponent>(this);
            room = Global.Get<RoomComponent>(this);
            flow = Global.Get<MiniBomberClientFlowComponent>(this);
            commandRunning = false;
            View.ShowPrompt(string.Empty);
            View.SetCommandInteractable(true);
            View.BindActions(Bindings, Refresh, OpenCreate, Join, Logout);
            lobby.Changed += Render;
            Bindings.Add(() => lobby.Changed -= Render);
            Render();
        }

        /// <summary>
        /// 清空业务引用和复用显示列表。
        /// </summary>
        protected override void OnDispose()
        {
            released = true;
            commandRunning = false;
            account = null;
            lobby = null;
            room = null;
            flow = null;
            viewData.MutableRooms.Clear();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将账号与大厅 Model 投影为窗口专用显示数据。
        /// </summary>
        private void Render()
        {
            MiniBomberLobbyModel source = lobby.Model;
            viewData.PlayerName = account.Model.PlayerName;
            viewData.OnlinePlayerCount = source.OnlinePlayerCount;
            while (viewData.MutableRooms.Count < source.Rooms.Count)
            {
                viewData.MutableRooms.Add(new LobbyRoomItemViewData());
            }

            for (int index = 0; index < source.Rooms.Count; index++)
            {
                MiniBomberLobbyRoomModel roomModel = source.Rooms[index];
                LobbyRoomItemViewData item = viewData.MutableRooms[index];
                item.RoomId = roomModel.RoomId;
                item.RoomName = roomModel.RoomName;
                item.PlayerCount = roomModel.PlayerCount;
                item.MaxPlayerCount = roomModel.MaxPlayerCount;
                item.DurationSeconds = roomModel.DurationSeconds;
                item.OwnerName = roomModel.OwnerName;
            }

            if (viewData.MutableRooms.Count > source.Rooms.Count)
            {
                viewData.MutableRooms.RemoveRange(source.Rooms.Count, viewData.MutableRooms.Count - source.Rooms.Count);
            }

            View.Refresh(viewData);
        }

        /// <summary>
        /// 从按钮回调启动大厅刷新任务。
        /// </summary>
        private void Refresh()
        {
            if (!commandRunning) RefreshAsync().Forget();
        }

        /// <summary>
        /// 请求刷新大厅 Model 并显示业务结果。
        /// </summary>
        /// <returns>刷新完成任务。</returns>
        private async MTask RefreshAsync()
        {
            BeginCommand("正在刷新房间列表...");
            try
            {
                MiniBomberCommandResult result = await lobby.RefreshAsync();
                if (!released) View.ShowPrompt(result.Message);
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 刷新大厅失败：{exception}");
                if (!released) View.ShowPrompt("刷新失败，请检查网络连接后重试");
            }
            finally
            {
                FinishCommand();
            }
        }

        /// <summary>
        /// 从按钮回调启动创建房间弹窗任务。
        /// </summary>
        private void OpenCreate()
        {
            if (!commandRunning) OpenCreateAsync().Forget();
        }

        /// <summary>
        /// 打开创建房间弹窗并阻止重复点击。
        /// </summary>
        /// <returns>弹窗打开完成任务。</returns>
        private async MTask OpenCreateAsync()
        {
            BeginCommand("正在打开创建房间界面...");
            try
            {
                await Context.Service.OpenAsync("CreateRoomPopup");
                if (!released) View.ShowPrompt(string.Empty);
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 打开创建房间界面失败：{exception}");
                if (!released) View.ShowPrompt("打开创建房间界面失败，请重试");
            }
            finally
            {
                FinishCommand();
            }
        }

        /// <summary>
        /// 从按钮回调启动加入房间任务。
        /// </summary>
        private void Join()
        {
            if (!commandRunning) JoinAsync().Forget();
        }

        /// <summary>
        /// 按输入编号加入房间并进入房间流程。
        /// </summary>
        /// <returns>加入流程完成任务。</returns>
        private async MTask JoinAsync()
        {
            if (!View.TryGetJoinRoomId(out long roomId))
            {
                View.ShowPrompt("房间编号格式不正确");
                return;
            }

            bool joined = false;
            BeginCommand("正在加入房间...");
            try
            {
                MiniBomberCommandResult result = await room.JoinAsync(roomId);
                if (released) return;
                View.ShowPrompt(result.Message);
                if (result.IsSuccess)
                {
                    joined = true;
                    View.ShowPrompt("加入成功，正在进入房间...");
                    await flow.NavigateAsync(MiniBomberClientDestinationKind.Room);
                }
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 加入房间失败：{exception}");
                if (!released)
                {
                    View.ShowPrompt(joined
                        ? "已经加入房间，但界面切换失败；重新登录可恢复房间状态"
                        : "加入房间失败，请检查网络连接后重试");
                }
            }
            finally
            {
                FinishCommand();
            }
        }

        /// <summary>
        /// 从按钮回调启动注销任务。
        /// </summary>
        private void Logout()
        {
            if (!commandRunning) LogoutAsync().Forget();
        }

        /// <summary>
        /// 清理账号会话并返回登录流程。
        /// </summary>
        /// <returns>注销完成任务。</returns>
        private async MTask LogoutAsync()
        {
            BeginCommand("正在退出登录...");
            try
            {
                await account.LogoutAsync();
                if (!released) await flow.NavigateAsync(MiniBomberClientDestinationKind.Login);
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 退出登录失败：{exception}");
                if (!released) View.ShowPrompt("退出登录失败，请重试");
            }
            finally
            {
                FinishCommand();
            }
        }

        /// <summary>
        /// 开始一个互斥大厅命令并显示提示。
        /// </summary>
        /// <param name="message">命令开始提示。</param>
        private void BeginCommand(string message)
        {
            commandRunning = true;
            View.SetCommandInteractable(false);
            View.ShowPrompt(message);
        }

        /// <summary>
        /// 结束当前大厅命令并恢复交互。
        /// </summary>
        private void FinishCommand()
        {
            commandRunning = false;
            if (!released) View.SetCommandInteractable(true);
        }

        #endregion
    }
}
