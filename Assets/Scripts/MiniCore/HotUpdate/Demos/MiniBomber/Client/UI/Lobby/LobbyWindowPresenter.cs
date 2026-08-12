using System;
using System.Text;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using MiniCore.Threading;
using MiniCore.UI;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 大厅窗口 Presenter。
    /// </summary>
    public sealed class LobbyWindowPresenter : AUIWindowPresenter<LobbyWindowView>
    {
        #region Private 私有成员

        private readonly StringBuilder builder = new StringBuilder(512); // 房间列表格式化缓存。
        private AccountSessionComponent account; // 账号会话组件。
        private LobbyComponent lobby; // 大厅状态组件。
        private RoomComponent room; // 房间状态组件。
        private MiniBomberClientFlowComponent flow; // 客户端流程组件。
        private bool commandRunning; // 是否已有大厅命令执行中。
        private bool released; // Presenter 是否已经随窗口释放。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 重置缓存窗口状态并绑定大厅命令和变化事件。
        /// </summary>
        protected override void OnBind()
        {
            account = Global.Get<AccountSessionComponent>(this);
            lobby = Global.Get<LobbyComponent>(this);
            room = Global.Get<RoomComponent>(this);
            flow = Global.Get<MiniBomberClientFlowComponent>(this);
            commandRunning = false;
            View.PromptText.text = string.Empty;
            SetCommandInteractable(true);
            lobby.Changed += Render;
            Bindings.Add(() => lobby.Changed -= Render);
            Bindings.Add(View.RefreshButton, Refresh);
            Bindings.Add(View.CreateButton, OpenCreate);
            Bindings.Add(View.JoinButton, Join);
            Bindings.Add(View.LogoutButton, Logout);
            Render();
        }

        /// <summary>
        /// 清空业务引用。
        /// </summary>
        protected override void OnDispose()
        {
            released = true;
            commandRunning = false;
            account = null;
            lobby = null;
            room = null;
            flow = null;
            builder.Clear();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 渲染当前大厅权威列表。
        /// </summary>
        private void Render()
        {
            View.PlayerNameText.text = account.PlayerName;
            View.OnlineCountText.text = $"在线：{lobby.OnlinePlayerCount}";
            builder.Clear();
            for (int index = 0; index < lobby.Rooms.Count; index++)
            {
                MiniBomberRoomSummaryDto item = lobby.Rooms[index];
                builder.Append('#').Append(item.RoomId).Append(' ')
                    .Append(item.RoomName).Append("  ")
                    .Append(item.PlayerCount).Append('/').Append(item.MaxPlayerCount).Append("  ")
                    .Append(item.DurationSeconds / 60).Append("分钟  房主:").Append(item.OwnerName).AppendLine();
            }

            View.RoomListText.text = builder.Length == 0 ? "暂无房间" : builder.ToString();
        }

        /// <summary>
        /// 刷新大厅完整快照。
        /// </summary>
        private void Refresh()
        {
            if (commandRunning)
            {
                return;
            }

            RefreshAsync().Forget();
        }

        /// <summary>
        /// 请求刷新并显示响应。
        /// </summary>
        /// <returns>刷新完成任务。</returns>
        private async MTask RefreshAsync()
        {
            commandRunning = true;
            SetCommandInteractable(false);
            try
            {
                View.PromptText.text = "正在刷新房间列表...";
                MiniBomberLobbySnapshotResponse response = await lobby.RefreshAsync();
                if (!released)
                {
                    View.PromptText.text = response.Msg;
                }
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 刷新大厅失败：{exception}");
                if (!released)
                {
                    View.PromptText.text = "刷新失败，请检查网络连接后重试";
                }
            }
            finally
            {
                FinishCommand();
            }
        }

        /// <summary>
        /// 打开创建房间弹窗。
        /// </summary>
        private void OpenCreate()
        {
            if (commandRunning)
            {
                return;
            }

            OpenCreateAsync().Forget();
        }

        /// <summary>
        /// 打开创建房间弹窗，并在资源加载期间阻止重复点击。
        /// </summary>
        /// <returns>弹窗打开完成任务。</returns>
        private async MTask OpenCreateAsync()
        {
            commandRunning = true;
            SetCommandInteractable(false);
            try
            {
                View.PromptText.text = "正在打开创建房间界面...";
                await Context.Service.OpenAsync("CreateRoomPopup");
                if (!released)
                {
                    View.PromptText.text = string.Empty;
                }
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 打开创建房间界面失败：{exception}");
                if (!released)
                {
                    View.PromptText.text = "打开创建房间界面失败，请重试";
                }
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
            if (commandRunning)
            {
                return;
            }

            JoinAsync().Forget();
        }

        /// <summary>
        /// 按输入身份加入房间。
        /// </summary>
        /// <returns>加入流程完成任务。</returns>
        private async MTask JoinAsync()
        {
            if (!long.TryParse(View.JoinRoomIdInput.text, out long roomId))
            {
                View.PromptText.text = "房间编号格式不正确";
                return;
            }

            commandRunning = true;
            SetCommandInteractable(false);
            bool joined = false;
            try
            {
                View.PromptText.text = "正在加入房间...";
                MiniBomberJoinRoomResponse response = await room.JoinAsync(roomId);
                if (released)
                {
                    return;
                }

                View.PromptText.text = response.Msg;
                if (response.Code == MiniBomberErrorCode.Success)
                {
                    joined = true;
                    View.PromptText.text = "加入成功，正在进入房间...";
                    await flow.NavigateAsync(MiniBomberClientDestination.MiniBomberDestinationRoom, response.Room);
                }
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 加入房间失败：{exception}");
                if (!released)
                {
                    View.PromptText.text = joined
                        ? "已经加入房间，但界面切换失败；重新登录可恢复房间状态"
                        : "加入房间失败，请检查网络连接后重试";
                }
            }
            finally
            {
                FinishCommand();
            }
        }

        /// <summary>
        /// 注销并返回登录流程。
        /// </summary>
        private void Logout()
        {
            if (commandRunning)
            {
                return;
            }

            LogoutAsync().Forget();
        }

        /// <summary>
        /// 清理登录存档并返回登录场景。
        /// </summary>
        /// <returns>注销完成任务。</returns>
        private async MTask LogoutAsync()
        {
            commandRunning = true;
            SetCommandInteractable(false);
            try
            {
                View.PromptText.text = "正在退出登录...";
                await account.LogoutAsync();
                if (!released)
                {
                    await flow.NavigateAsync(MiniBomberClientDestination.MiniBomberDestinationLogin);
                }
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 退出登录失败：{exception}");
                if (!released)
                {
                    View.PromptText.text = "退出登录失败，请重试";
                }
            }
            finally
            {
                FinishCommand();
            }
        }

        /// <summary>
        /// 统一切换大厅命令按钮，阻止并发修改客户端流程。
        /// </summary>
        /// <param name="interactable">是否允许点击。</param>
        private void SetCommandInteractable(bool interactable)
        {
            View.RefreshButton.interactable = interactable;
            View.CreateButton.interactable = interactable;
            View.JoinButton.interactable = interactable;
            View.LogoutButton.interactable = interactable;
        }

        /// <summary>
        /// 结束当前大厅命令，并在窗口仍存活时恢复交互。
        /// </summary>
        private void FinishCommand()
        {
            commandRunning = false;
            if (!released)
            {
                SetCommandInteractable(true);
            }
        }

        #endregion
    }
}
