using System;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Threading;
using MiniCore.UI;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 房间窗口 Presenter，投影房间 Model 并协调房间命令。
    /// </summary>
    public sealed class RoomWindowPresenter : AUIWindowPresenter<RoomWindowView>
    {
        #region Private 私有成员

        private static readonly int[] Durations = { 120, 300, 600 }; // 下拉索引对应的局时长。
        private readonly RoomWindowViewData viewData = new RoomWindowViewData(); // 复用房间窗口显示数据。
        private AccountSessionComponent account; // 账号会话组件。
        private RoomComponent room; // 房间组件。
        private MiniBomberClientFlowComponent flow; // 客户端流程组件。
        private bool commandRunning; // 是否已有房间命令执行中。
        private bool released; // Presenter 是否已经随窗口释放。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 获取业务依赖并绑定房间意图和 Model 变化事件。
        /// </summary>
        protected override void OnBind()
        {
            account = Global.Get<AccountSessionComponent>(this);
            room = Global.Get<RoomComponent>(this);
            flow = Global.Get<MiniBomberClientFlowComponent>(this);
            commandRunning = false;
            View.ShowPrompt(string.Empty);
            View.BindActions(Bindings, ApplySettings, ToggleReady, StartMatch, Leave);
            room.Changed += Render;
            Bindings.Add(() => room.Changed -= Render);
            Render();
            View.SetCommandInteractable(true, room.IsOwner);
        }

        /// <summary>
        /// 清空业务引用和复用显示列表。
        /// </summary>
        protected override void OnDispose()
        {
            released = true;
            commandRunning = false;
            account = null;
            room = null;
            flow = null;
            viewData.MutableMembers.Clear();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将当前房间 Model 投影为窗口专用显示数据。
        /// </summary>
        private void Render()
        {
            MiniBomberRoomModel source = room.Model;
            if (!source.HasRoom) return;
            viewData.RoomId = source.RoomId;
            viewData.RoomName = source.RoomName;
            int durationIndex = Array.IndexOf(Durations, source.DurationSeconds);
            viewData.DurationIndex = durationIndex >= 0 ? durationIndex : 0;
            viewData.IsOwner = room.IsOwner;
            viewData.LocalReady = false;
            while (viewData.MutableMembers.Count < source.Members.Count)
            {
                viewData.MutableMembers.Add(new RoomMemberViewData());
            }

            for (int index = 0; index < source.Members.Count; index++)
            {
                MiniBomberRoomMemberModel member = source.Members[index];
                RoomMemberViewData item = viewData.MutableMembers[index];
                item.PlayerName = member.PlayerName;
                item.IsOwner = member.IsOwner;
                item.IsReady = member.IsReady;
                item.IsOnline = member.IsOnline;
                if (member.PlayerId == account.Model.PlayerId) viewData.LocalReady = member.IsReady;
            }

            if (viewData.MutableMembers.Count > source.Members.Count)
            {
                viewData.MutableMembers.RemoveRange(source.Members.Count, viewData.MutableMembers.Count - source.Members.Count);
            }

            View.Refresh(viewData);
            View.SetCommandInteractable(!commandRunning, viewData.IsOwner);
        }

        /// <summary>
        /// 从按钮回调启动修改设置任务。
        /// </summary>
        private void ApplySettings()
        {
            if (!commandRunning) ApplySettingsAsync().Forget();
        }

        /// <summary>
        /// 提交房主设置并应用组件返回的 Model 更新。
        /// </summary>
        /// <returns>设置同步完成任务。</returns>
        private async MTask ApplySettingsAsync()
        {
            View.GetSettingsInput(out string roomName, out int durationIndex);
            int index = Mathf.Clamp(durationIndex, 0, Durations.Length - 1);
            BeginCommand("正在同步房间设置...");
            try
            {
                MiniBomberCommandResult result = await room.UpdateSettingsAsync(roomName, Durations[index]);
                if (!released) View.ShowPrompt(result.Message);
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 修改房间设置失败：{exception}");
                if (!released) View.ShowPrompt("修改房间设置失败，请检查网络连接后重试");
            }
            finally
            {
                FinishCommand();
            }
        }

        /// <summary>
        /// 从按钮回调启动准备状态切换任务。
        /// </summary>
        private void ToggleReady()
        {
            if (!commandRunning) ToggleReadyAsync().Forget();
        }

        /// <summary>
        /// 请求切换当前玩家准备状态。
        /// </summary>
        /// <returns>准备状态同步完成任务。</returns>
        private async MTask ToggleReadyAsync()
        {
            if (!room.Model.HasRoom)
            {
                View.ShowPrompt("房间状态尚未同步，请稍后重试");
                return;
            }

            bool ready = viewData.LocalReady;
            BeginCommand(ready ? "正在取消准备..." : "正在准备...");
            try
            {
                MiniBomberCommandResult result = await room.SetReadyAsync(!ready);
                if (!released) View.ShowPrompt(result.Message);
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 切换准备状态失败：{exception}");
                if (!released) View.ShowPrompt("准备状态同步失败，请检查网络连接后重试");
            }
            finally
            {
                FinishCommand();
            }
        }

        /// <summary>
        /// 从按钮回调启动比赛任务。
        /// </summary>
        private void StartMatch()
        {
            if (!commandRunning) StartMatchAsync().Forget();
        }

        /// <summary>
        /// 请求服务器验证并开始比赛。
        /// </summary>
        /// <returns>开局请求完成任务。</returns>
        private async MTask StartMatchAsync()
        {
            BeginCommand("正在请求开始比赛...");
            try
            {
                MiniBomberCommandResult result = await room.StartMatchAsync();
                if (!released) View.ShowPrompt(result.Message);
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 开始比赛请求失败：{exception}");
                if (!released) View.ShowPrompt("开始比赛失败，请检查网络连接后重试");
            }
            finally
            {
                FinishCommand();
            }
        }

        /// <summary>
        /// 从按钮回调启动离开房间任务。
        /// </summary>
        private void Leave()
        {
            if (!commandRunning) LeaveAsync().Forget();
        }

        /// <summary>
        /// 离开当前房间并返回大厅流程。
        /// </summary>
        /// <returns>离开流程完成任务。</returns>
        private async MTask LeaveAsync()
        {
            bool leftRoom = false;
            BeginCommand("正在离开房间...");
            try
            {
                MiniBomberCommandResult result = await room.LeaveAsync();
                if (released) return;
                View.ShowPrompt(result.Message);
                if (result.IsSuccess)
                {
                    leftRoom = true;
                    await flow.NavigateAsync(MiniBomberClientDestinationKind.Lobby);
                }
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 离开房间失败：{exception}");
                if (!released)
                {
                    View.ShowPrompt(leftRoom
                        ? "已经离开房间，但大厅界面切换失败，请重新登录"
                        : "离开房间失败，请检查网络连接后重试");
                }
            }
            finally
            {
                FinishCommand();
            }
        }

        /// <summary>
        /// 开始一个互斥房间命令并显示提示。
        /// </summary>
        /// <param name="message">命令开始提示。</param>
        private void BeginCommand(string message)
        {
            commandRunning = true;
            View.SetCommandInteractable(false, room.IsOwner);
            View.ShowPrompt(message);
        }

        /// <summary>
        /// 结束当前房间命令并恢复符合权限的交互状态。
        /// </summary>
        private void FinishCommand()
        {
            commandRunning = false;
            if (!released) View.SetCommandInteractable(true, room.IsOwner);
        }

        #endregion
    }
}
