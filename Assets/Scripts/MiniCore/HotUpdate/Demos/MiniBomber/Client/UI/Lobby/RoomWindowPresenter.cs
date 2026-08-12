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
    /// 房间窗口 Presenter。
    /// </summary>
    public sealed class RoomWindowPresenter : AUIWindowPresenter<RoomWindowView>
    {
        #region Private 私有成员

        private static readonly int[] Durations = { 120, 300, 600 }; // 下拉框索引对应时长。
        private readonly StringBuilder builder = new StringBuilder(256); // 成员列表格式化缓存。
        private AccountSessionComponent account; // 账号会话组件。
        private RoomComponent room; // 房间组件。
        private MiniBomberClientFlowComponent flow; // 客户端流程组件。
        private bool commandRunning; // 是否已有房间命令执行中。
        private bool released; // Presenter 是否已经随窗口释放。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 重置缓存窗口状态并绑定房间命令和权威快照变化事件。
        /// </summary>
        protected override void OnBind()
        {
            account = Global.Get<AccountSessionComponent>(this);
            room = Global.Get<RoomComponent>(this);
            flow = Global.Get<MiniBomberClientFlowComponent>(this);
            commandRunning = false;
            View.PromptText.text = string.Empty;
            View.DurationDropdown.ClearOptions();
            View.DurationDropdown.AddOptions(new System.Collections.Generic.List<string> { "2分钟", "5分钟", "10分钟" });
            room.Changed += Render;
            Bindings.Add(() => room.Changed -= Render);
            Bindings.Add(View.ApplySettingsButton, ApplySettings);
            Bindings.Add(View.ReadyButton, ToggleReady);
            Bindings.Add(View.StartButton, StartMatch);
            Bindings.Add(View.LeaveButton, Leave);
            Render();
            SetCommandInteractable(true);
        }

        /// <summary>
        /// 清空业务引用和格式化缓存。
        /// </summary>
        protected override void OnDispose()
        {
            released = true;
            commandRunning = false;
            account = null;
            room = null;
            flow = null;
            builder.Clear();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 渲染房间成员、房主权限和准备状态。
        /// </summary>
        private void Render()
        {
            MiniBomberRoomSnapshotDto snapshot = room.Current;
            if (snapshot == null)
            {
                return;
            }

            View.RoomTitleText.text = $"#{snapshot.RoomId}  {snapshot.RoomName}";
            View.RoomNameInput.text = snapshot.RoomName;
            int durationIndex = Array.IndexOf(Durations, snapshot.DurationSeconds);
            View.DurationDropdown.value = durationIndex >= 0 ? durationIndex : 0;
            View.RoomNameInput.interactable = room.IsOwner;
            View.DurationDropdown.interactable = room.IsOwner;
            View.ApplySettingsButton.gameObject.SetActive(room.IsOwner);
            View.StartButton.gameObject.SetActive(room.IsOwner);
            builder.Clear();
            bool localReady = false;
            for (int memberIndex = 0; memberIndex < snapshot.Members.Count; memberIndex++)
            {
                MiniBomberRoomMemberDto member = snapshot.Members[memberIndex];
                builder.Append(member.IsOwner ? "[房主] " : string.Empty)
                    .Append(member.PlayerName).Append(' ')
                    .Append(member.IsOnline ? string.Empty : "[离线] ")
                    .Append(member.IsReady ? "已准备" : "未准备").AppendLine();
                if (member.PlayerId == account.PlayerId)
                {
                    localReady = member.IsReady;
                }
            }

            View.MemberListText.text = builder.ToString();
            View.ReadyButton.GetComponentInChildren<TMPro.TMP_Text>().text = localReady ? "取消准备" : "准备";
        }

        /// <summary>
        /// 提交房主设置。
        /// </summary>
        private void ApplySettings()
        {
            if (commandRunning)
            {
                return;
            }

            ApplySettingsAsync().Forget();
        }

        /// <summary>
        /// 请求服务器修改房间设置。
        /// </summary>
        /// <returns>设置同步完成任务。</returns>
        private async MTask ApplySettingsAsync()
        {
            int index = Mathf.Clamp(View.DurationDropdown.value, 0, Durations.Length - 1);
            string roomName = View.RoomNameInput.text;
            commandRunning = true;
            SetCommandInteractable(false);
            try
            {
                View.PromptText.text = "正在同步房间设置...";
                MiniBomberUpdateRoomResponse response = await room.UpdateSettingsAsync(roomName, Durations[index]);
                if (!released)
                {
                    View.PromptText.text = response.Msg;
                }
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 修改房间设置失败：{exception}");
                if (!released)
                {
                    View.PromptText.text = "修改房间设置失败，请检查网络连接后重试";
                }
            }
            finally
            {
                FinishCommand();
            }
        }

        /// <summary>
        /// 切换本地成员准备状态。
        /// </summary>
        private void ToggleReady()
        {
            if (commandRunning)
            {
                return;
            }

            ToggleReadyAsync().Forget();
        }

        /// <summary>
        /// 请求服务器切换准备状态。
        /// </summary>
        /// <returns>准备同步完成任务。</returns>
        private async MTask ToggleReadyAsync()
        {
            bool ready = false;
            MiniBomberRoomSnapshotDto snapshot = room.Current;
            if (snapshot == null)
            {
                View.PromptText.text = "房间状态尚未同步，请稍后重试";
                return;
            }

            for (int index = 0; index < snapshot.Members.Count; index++)
            {
                if (snapshot.Members[index].PlayerId == account.PlayerId)
                {
                    ready = snapshot.Members[index].IsReady;
                    break;
                }
            }

            commandRunning = true;
            SetCommandInteractable(false);
            try
            {
                View.PromptText.text = ready ? "正在取消准备..." : "正在准备...";
                MiniBomberSetReadyResponse response = await room.SetReadyAsync(!ready);
                if (!released)
                {
                    View.PromptText.text = response.Msg;
                }
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 切换准备状态失败：{exception}");
                if (!released)
                {
                    View.PromptText.text = "准备状态同步失败，请检查网络连接后重试";
                }
            }
            finally
            {
                FinishCommand();
            }
        }

        /// <summary>
        /// 房主请求开始比赛。
        /// </summary>
        private void StartMatch()
        {
            if (commandRunning)
            {
                return;
            }

            StartMatchAsync().Forget();
        }

        /// <summary>
        /// 请求服务器验证开局条件。
        /// </summary>
        /// <returns>开局请求完成任务。</returns>
        private async MTask StartMatchAsync()
        {
            commandRunning = true;
            SetCommandInteractable(false);
            try
            {
                View.PromptText.text = "正在请求开始比赛...";
                MiniBomberStartMatchResponse response = await room.StartMatchAsync();
                if (!released)
                {
                    View.PromptText.text = response.Msg;
                }
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 开始比赛请求失败：{exception}");
                if (!released)
                {
                    View.PromptText.text = "开始比赛失败，请检查网络连接后重试";
                }
            }
            finally
            {
                FinishCommand();
            }
        }

        /// <summary>
        /// 离开房间并返回大厅。
        /// </summary>
        private void Leave()
        {
            if (commandRunning)
            {
                return;
            }

            LeaveAsync().Forget();
        }

        /// <summary>
        /// 请求离开房间并切换大厅流程。
        /// </summary>
        /// <returns>离开流程完成任务。</returns>
        private async MTask LeaveAsync()
        {
            commandRunning = true;
            SetCommandInteractable(false);
            bool leftRoom = false;
            try
            {
                View.PromptText.text = "正在离开房间...";
                MiniBomberLeaveRoomResponse response = await room.LeaveAsync();
                if (released)
                {
                    return;
                }

                View.PromptText.text = response.Msg;
                if (response.Code == MiniBomberErrorCode.Success)
                {
                    leftRoom = true;
                    await flow.NavigateAsync(MiniBomberClientDestination.MiniBomberDestinationLobby);
                }
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 离开房间失败：{exception}");
                if (!released)
                {
                    View.PromptText.text = leftRoom
                        ? "已经离开房间，但大厅界面切换失败，请重新登录"
                        : "离开房间失败，请检查网络连接后重试";
                }
            }
            finally
            {
                FinishCommand();
            }
        }

        /// <summary>
        /// 根据当前房主权限和命令状态切换房间按钮交互。
        /// </summary>
        /// <param name="interactable">是否允许执行新的房间命令。</param>
        private void SetCommandInteractable(bool interactable)
        {
            bool owner = room != null && room.IsOwner;
            View.ApplySettingsButton.interactable = interactable && owner;
            View.ReadyButton.interactable = interactable;
            View.StartButton.interactable = interactable && owner;
            View.LeaveButton.interactable = interactable;
        }

        /// <summary>
        /// 结束当前房间命令，并在窗口仍存活时恢复符合权限的交互状态。
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
