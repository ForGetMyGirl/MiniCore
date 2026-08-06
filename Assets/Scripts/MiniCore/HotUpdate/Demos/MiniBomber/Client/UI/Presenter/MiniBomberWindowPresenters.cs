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
    /// <summary>登录窗口 Presenter。</summary>
    public sealed class LoginWindowPresenter : AUIWindowPresenter<LoginWindowView>
    {
        #region Private 私有成员

        private AccountSessionComponent account; // 账号会话组件。
        private MiniBomberClientFlowComponent flow; // 客户端流程组件。
        private bool commandRunning; // 是否已有登录或注册入口命令执行中。
        private bool released; // Presenter 是否已经随窗口释放。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 重置缓存窗口状态，绑定登录和注册按钮并填充最近服务器地址。
        /// </summary>
        protected override void OnBind()
        {
            account = Global.Get<AccountSessionComponent>(this);
            flow = Global.Get<MiniBomberClientFlowComponent>(this);
            commandRunning = false;
            View.PromptText.text = string.Empty;
            SetCommandInteractable(true);
            View.HostInput.text = string.IsNullOrEmpty(account.Host) ? "127.0.0.1" : account.Host;
            View.PortInput.text = account.Port.ToString();
            Bindings.Add(View.LoginButton, Login);
            Bindings.Add(View.RegisterButton, OpenRegister);
        }

        /// <summary>
        /// 清空业务引用。
        /// </summary>
        protected override void OnDispose()
        {
            released = true;
            commandRunning = false;
            account = null;
            flow = null;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 从按钮回调启动登录任务。
        /// </summary>
        private void Login()
        {
            if (commandRunning)
            {
                return;
            }

            LoginAsync().Forget();
        }

        /// <summary>
        /// 连接服务器、登录并进入服务器指定流程。
        /// </summary>
        /// <returns>登录流程完成任务。</returns>
        private async MTask LoginAsync()
        {
            if (!int.TryParse(View.PortInput.text, out int port))
            {
                View.PromptText.text = "端口格式不正确";
                return;
            }

            commandRunning = true;
            SetCommandInteractable(false);
            string host = View.HostInput.text;
            string accountName = View.AccountInput.text;
            string password = View.PasswordInput.text;
            bool authenticated = false;
            try
            {
                View.PromptText.text = "正在连接服务器...";
                if (!await account.ConnectAsync(host, port))
                {
                    if (!released)
                    {
                        View.PromptText.text = "连接服务器失败，请检查地址、端口和服务端状态";
                    }

                    return;
                }

                if (released)
                {
                    return;
                }

                View.PromptText.text = "连接成功，正在登录...";
                MiniBomberLoginResponse response = await account.LoginAsync(accountName, password);
                if (released)
                {
                    return;
                }

                View.PromptText.text = response.Msg;
                if (response.Code == MiniBomberErrorCode.Success)
                {
                    authenticated = true;
                    View.PromptText.text = "登录成功，正在进入游戏...";
                    await flow.NavigateAsync(response.Destination);
                }
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 登录流程失败：{exception}");
                if (!released)
                {
                    View.PromptText.text = authenticated
                        ? "登录已成功，但界面切换失败，请重试或重新进入客户端"
                        : "登录失败，请检查网络连接后重试";
                }
            }
            finally
            {
                commandRunning = false;
                if (!released)
                {
                    SetCommandInteractable(true);
                }
            }
        }

        /// <summary>
        /// 打开注册弹窗。
        /// </summary>
        private void OpenRegister()
        {
            if (commandRunning)
            {
                return;
            }

            OpenRegisterAsync().Forget();
        }

        /// <summary>
        /// 先验证服务器连接，再打开可直接提交的注册弹窗。
        /// </summary>
        /// <returns>连接检查和弹窗打开完成任务。</returns>
        private async MTask OpenRegisterAsync()
        {
            if (!int.TryParse(View.PortInput.text, out int port))
            {
                View.PromptText.text = "端口格式不正确";
                return;
            }

            commandRunning = true;
            SetCommandInteractable(false);
            string host = View.HostInput.text;
            try
            {
                View.PromptText.text = "正在连接服务器...";
                if (!await account.ConnectAsync(host, port))
                {
                    if (!released)
                    {
                        View.PromptText.text = "连接服务器失败，请检查地址、端口和服务端状态";
                    }

                    return;
                }

                if (released)
                {
                    return;
                }

                View.PromptText.text = "连接成功，正在打开注册界面...";
                await Context.Service.OpenAsync("RegisterWindow");
                if (!released)
                {
                    View.PromptText.text = string.Empty;
                }
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 打开注册界面失败：{exception}");
                if (!released)
                {
                    View.PromptText.text = "打开注册界面失败，请重试";
                }
            }
            finally
            {
                commandRunning = false;
                if (!released)
                {
                    SetCommandInteractable(true);
                }
            }
        }

        /// <summary>
        /// 统一切换登录界面的命令按钮，阻止连接和鉴权期间重复提交。
        /// </summary>
        /// <param name="interactable">是否允许点击。</param>
        private void SetCommandInteractable(bool interactable)
        {
            View.LoginButton.interactable = interactable;
            View.RegisterButton.interactable = interactable;
        }

        #endregion
    }

    /// <summary>注册弹窗 Presenter。</summary>
    public sealed class RegisterWindowPresenter : AUIWindowPresenter<RegisterWindowView>
    {
        #region Private 私有成员

        private AccountSessionComponent account; // 账号会话组件。
        private bool commandRunning; // 是否已有注册请求执行中。
        private bool released; // Presenter 是否已经随窗口释放。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 重置缓存弹窗状态并绑定提交和关闭按钮。
        /// </summary>
        protected override void OnBind()
        {
            account = Global.Get<AccountSessionComponent>(this);
            commandRunning = false;
            View.PromptText.text = string.Empty;
            SetCommandInteractable(true);
            Bindings.Add(View.SubmitButton, Submit);
            Bindings.Add(View.CloseButton, Close);
        }

        /// <summary>
        /// 清空账号引用。
        /// </summary>
        protected override void OnDispose()
        {
            released = true;
            commandRunning = false;
            account = null;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 从按钮回调启动注册任务。
        /// </summary>
        private void Submit()
        {
            if (commandRunning)
            {
                return;
            }

            SubmitAsync().Forget();
        }

        /// <summary>
        /// 校验确认密码并请求服务器注册。
        /// </summary>
        /// <returns>注册流程完成任务。</returns>
        private async MTask SubmitAsync()
        {
            if (!string.Equals(View.PasswordInput.text, View.ConfirmPasswordInput.text, StringComparison.Ordinal))
            {
                View.PromptText.text = "两次输入的密码不一致";
                return;
            }

            commandRunning = true;
            SetCommandInteractable(false);
            string accountName = View.AccountInput.text;
            string password = View.PasswordInput.text;
            string playerName = View.PlayerNameInput.text;
            try
            {
                View.PromptText.text = "正在注册账号...";
                MiniBomberRegisterResponse response = await account.RegisterAsync(accountName, password, playerName);
                if (!released)
                {
                    View.PromptText.text = response.Msg;
                }
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 注册请求失败：{exception}");
                if (!released)
                {
                    View.PromptText.text = "注册失败，请检查网络连接后重试";
                }
            }
            finally
            {
                commandRunning = false;
                if (!released)
                {
                    SetCommandInteractable(true);
                }
            }
        }

        /// <summary>
        /// 关闭注册弹窗。
        /// </summary>
        private void Close()
        {
            if (commandRunning)
            {
                return;
            }

            Context.Service.CloseAsync(Context.Handle).Forget();
        }

        /// <summary>
        /// 切换注册弹窗按钮，避免一个账号请求被重复发送。
        /// </summary>
        /// <param name="interactable">是否允许点击。</param>
        private void SetCommandInteractable(bool interactable)
        {
            View.SubmitButton.interactable = interactable;
            View.CloseButton.interactable = interactable;
        }

        #endregion
    }

    /// <summary>大厅窗口 Presenter。</summary>
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

    /// <summary>创建房间弹窗 Presenter。</summary>
    public sealed class CreateRoomPopupPresenter : AUIWindowPresenter<CreateRoomPopupView>
    {
        #region Private 私有成员

        private static readonly int[] Durations = { 120, 300, 600 }; // 下拉框索引对应时长。
        private RoomComponent room; // 房间组件。
        private MiniBomberClientFlowComponent flow; // 客户端流程组件。
        private bool commandRunning; // 是否已有创建房间请求执行中。
        private bool released; // Presenter 是否已经随窗口释放。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 重置缓存弹窗状态、配置时长选项并绑定按钮。
        /// </summary>
        protected override void OnBind()
        {
            room = Global.Get<RoomComponent>(this);
            flow = Global.Get<MiniBomberClientFlowComponent>(this);
            commandRunning = false;
            View.PromptText.text = string.Empty;
            SetCommandInteractable(true);
            View.DurationDropdown.ClearOptions();
            View.DurationDropdown.AddOptions(new System.Collections.Generic.List<string> { "2分钟", "5分钟", "10分钟" });
            Bindings.Add(View.SubmitButton, Submit);
            Bindings.Add(View.CancelButton, Close);
        }

        /// <summary>
        /// 清空业务引用。
        /// </summary>
        protected override void OnDispose()
        {
            released = true;
            commandRunning = false;
            room = null;
            flow = null;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 从按钮回调启动创建房间任务。
        /// </summary>
        private void Submit()
        {
            if (commandRunning)
            {
                return;
            }

            SubmitAsync().Forget();
        }

        /// <summary>
        /// 创建房间并进入房间界面。
        /// </summary>
        /// <returns>创建流程完成任务。</returns>
        private async MTask SubmitAsync()
        {
            int index = Mathf.Clamp(View.DurationDropdown.value, 0, Durations.Length - 1);
            string roomName = View.RoomNameInput.text;
            commandRunning = true;
            SetCommandInteractable(false);
            bool created = false;
            try
            {
                View.PromptText.text = "正在创建房间...";
                MiniBomberCreateRoomResponse response = await room.CreateAsync(roomName, Durations[index]);
                if (released)
                {
                    return;
                }

                View.PromptText.text = response.Msg;
                if (response.Code == MiniBomberErrorCode.Success)
                {
                    created = true;
                    MiniBomberClientFlowComponent flowComponent = flow;
                    IUIService service = Context.Service;
                    UIWindowHandle handle = Context.Handle;
                    View.PromptText.text = "创建成功，正在进入房间...";
                    await flowComponent.NavigateAsync(MiniBomberClientDestination.MiniBomberDestinationRoom, response.Room);
                    await service.CloseAsync(handle);
                }
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 创建房间失败：{exception}");
                if (!released)
                {
                    View.PromptText.text = created
                        ? "房间已经创建，但界面切换失败；重新登录可恢复房间状态"
                        : "创建房间失败，请检查网络连接后重试";
                }
            }
            finally
            {
                commandRunning = false;
                if (!released)
                {
                    SetCommandInteractable(true);
                }
            }
        }

        /// <summary>
        /// 关闭创建房间弹窗。
        /// </summary>
        private void Close()
        {
            if (commandRunning)
            {
                return;
            }

            Context.Service.CloseAsync(Context.Handle).Forget();
        }

        /// <summary>
        /// 切换创建房间弹窗按钮，防止同一请求重复提交。
        /// </summary>
        /// <param name="interactable">是否允许点击。</param>
        private void SetCommandInteractable(bool interactable)
        {
            View.SubmitButton.interactable = interactable;
            View.CancelButton.interactable = interactable;
        }

        #endregion
    }

    /// <summary>房间窗口 Presenter。</summary>
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

    /// <summary>战斗 HUD Presenter。</summary>
    public sealed class BattleHudWindowPresenter : AUIWindowPresenter<BattleHudWindowView>
    {
        #region Private 私有成员

        private readonly StringBuilder builder = new StringBuilder(256); // 排行格式化缓存。
        private BattleClientComponent battle; // 战斗状态组件。
        private INetworkService network; // HUD 网络往返延迟来源。
        private int lastPerformanceFrameCount; // 上一次性能采样时的累计渲染帧数。
        private double lastPerformanceSampleTime; // 上一次性能采样的未缩放时间。
        private bool performanceRefreshActive; // HUD 性能刷新任务是否仍然有效。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 绑定战斗快照和即时事件。
        /// </summary>
        protected override void OnBind()
        {
            battle = Global.Get<BattleClientComponent>(this);
            network = Global.GetService<INetworkService>(this);
            battle.SnapshotChanged += RenderSnapshot;
            battle.EventsChanged += RenderEvents;
            Bindings.Add(() => battle.SnapshotChanged -= RenderSnapshot);
            Bindings.Add(() => battle.EventsChanged -= RenderEvents);
            bool mobile = Application.platform == RuntimePlatform.Android;
            View.MobileControlRoot?.SetActive(mobile);
            View.DesktopHintRoot?.SetActive(!mobile);
            performanceRefreshActive = View.PerformanceText != null;
            lastPerformanceFrameCount = Time.frameCount;
            lastPerformanceSampleTime = Global.Time.UnscaledTime;
            if (performanceRefreshActive)
            {
                RefreshPerformanceLoopAsync().Forget();
            }

            RenderSnapshot();
        }

        /// <summary>
        /// 清空战斗引用和格式化缓存。
        /// </summary>
        protected override void OnDispose()
        {
            performanceRefreshActive = false;
            network = null;
            battle = null;
            builder.Clear();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 渲染剩余时间和服务器权威实时得分。
        /// </summary>
        private void RenderSnapshot()
        {
            MiniBomberBattleSnapshot snapshot = battle.Snapshot;
            if (snapshot == null)
            {
                return;
            }

            int seconds = Mathf.Max(0, snapshot.RemainingMilliseconds / 1000);
            View.RemainingTimeText.text = $"{seconds / 60:00}:{seconds % 60:00}";
            builder.Clear();
            for (int index = 0; index < snapshot.Players.Count; index++)
            {
                MiniBomberBattlePlayerDto player = snapshot.Players[index];
                builder.Append(player.PlayerName).Append("  ").Append(player.Score).AppendLine();
            }

            View.RankingText.text = builder.ToString();
        }

        /// <summary>
        /// 渲染最近一条服务器击杀事件。
        /// </summary>
        private void RenderEvents()
        {
            for (int index = battle.RecentEvents.Count - 1; index >= 0; index--)
            {
                MiniBomberBattleEventDto item = battle.RecentEvents[index];
                if (item.Type == MiniBomberBattleEventType.MiniBomberEventPlayerKilled)
                {
                    View.KillFeedText.text = item.ActorPlayerId == item.TargetPlayerId || item.ActorPlayerId == 0
                        ? $"{item.TargetName} 玩家被炸飞了"
                        : $"{item.ActorName} 玩家击杀了 {item.TargetName}";
                    ClearKillFeedAsync(View.KillFeedText.text).Forget();
                    return;
                }
            }
        }

        /// <summary>
        /// 两点五秒后清除仍未被新消息替换的击杀提示。
        /// </summary>
        /// <param name="expected">安排清除时的提示文本。</param>
        /// <returns>延迟清理完成任务。</returns>
        private async MTask ClearKillFeedAsync(string expected)
        {
            await MTask.Delay(2500);
            if (string.Equals(View.KillFeedText.text, expected, StringComparison.Ordinal))
            {
                View.KillFeedText.text = string.Empty;
            }
        }

        /// <summary>
        /// 每半秒计算平均渲染帧率并刷新 KCP 平滑往返延迟。
        /// </summary>
        /// <returns>窗口释放或任务域取消后结束的刷新任务。</returns>
        private async MTask RefreshPerformanceLoopAsync()
        {
            while (performanceRefreshActive)
            {
                await MTask.Delay(500);
                if (!performanceRefreshActive)
                {
                    return;
                }

                double now = Global.Time.UnscaledTime;
                int frameCount = Time.frameCount;
                double elapsedSeconds = now - lastPerformanceSampleTime;
                double framesPerSecond = elapsedSeconds > 0d
                    ? (frameCount - lastPerformanceFrameCount) / elapsedSeconds
                    : 0d;
                lastPerformanceFrameCount = frameCount;
                lastPerformanceSampleTime = now;

                int rttMilliseconds = 0;
                bool hasRtt = network != null && network.TryGetTransportRttMs(MiniBomberConstants.DefaultSessionId, out rttMilliseconds);
                View.PerformanceText.text = hasRtt
                    ? $"FPS: {framesPerSecond:F2}\nRTT: {rttMilliseconds} ms"
                    : $"FPS: {framesPerSecond:F2}\nRTT: --";
            }
        }

        #endregion
    }

    /// <summary>比赛成绩 Presenter。</summary>
    public sealed class MatchResultWindowPresenter : AUIWindowPresenter<MatchResultWindowView>
    {
        #region Private 私有成员

        private readonly StringBuilder builder = new StringBuilder(256); // 成绩格式化缓存。
        private BattleClientComponent battle; // 战斗状态组件。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 原样渲染服务器最终排名并绑定关闭按钮。
        /// </summary>
        protected override void OnBind()
        {
            battle = Global.Get<BattleClientComponent>(this);
            Bindings.Add(View.CloseButton, Close);
            MiniBomberMatchResultNotice result = battle.Result;
            builder.Clear();
            if (result != null)
            {
                for (int index = 0; index < result.Results.Count; index++)
                {
                    MiniBomberMatchResultEntryDto item = result.Results[index];
                    builder.Append(item.Rank).Append(". ").Append(item.PlayerName)
                        .Append("  得分:").Append(item.Score)
                        .Append("  击杀:").Append(item.Kills)
                        .Append("  死亡:").Append(item.Deaths).AppendLine();
                }

                View.ReturnCountdownText.text = $"{result.ReturnToRoomMilliseconds / 1000} 秒后返回房间";
            }

            View.ResultsText.text = builder.ToString();
        }

        /// <summary>
        /// 清空战斗引用。
        /// </summary>
        protected override void OnDispose()
        {
            battle = null;
            builder.Clear();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 关闭成绩弹窗。
        /// </summary>
        private void Close()
        {
            Context.Service.CloseAsync(Context.Handle).Forget();
        }

        #endregion
    }

    /// <summary>场景加载窗口 Presenter。</summary>
    public sealed class SceneLoadingWindowPresenter : AUIWindowPresenter<SceneLoadingWindowView>
    {
        #region Private 私有成员

        private ISceneService scenes; // 当前场景加载服务。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 显示当前场景服务进度。
        /// </summary>
        protected override void OnBind()
        {
            scenes = Global.GetService<ISceneService>(this);
            scenes.ProgressChanged += RenderProgress;
            Bindings.Add(() => scenes.ProgressChanged -= RenderProgress);
            RenderProgress(scenes.Progress);
            View.PromptText.text = "正在加载场景...";
        }

        /// <summary>
        /// 清空场景服务引用。
        /// </summary>
        protected override void OnDispose()
        {
            scenes = null;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将场景进度更新到进度条。
        /// </summary>
        /// <param name="progress">零到一的加载进度。</param>
        private void RenderProgress(float progress)
        {
            View.ProgressSlider.value = Mathf.Clamp01(progress);
        }

        #endregion
    }

    /// <summary>断线重连遮罩 Presenter。</summary>
    public sealed class ReconnectOverlayPresenter : AUIWindowPresenter<ReconnectOverlayView>
    {
        #region Private 私有成员

        private MiniBomberClientFlowComponent flow; // 客户端流程组件。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 绑定重连状态变化事件。
        /// </summary>
        protected override void OnBind()
        {
            flow = Global.Get<MiniBomberClientFlowComponent>(this);
            flow.Changed += Render;
            Bindings.Add(() => flow.Changed -= Render);
            Render();
        }

        /// <summary>
        /// 清空流程引用。
        /// </summary>
        protected override void OnDispose()
        {
            flow = null;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 渲染重连状态和尝试次数。
        /// </summary>
        private void Render()
        {
            View.StatusText.text = flow.Message;
        }

        #endregion
    }

    /// <summary>短时消息提示 Presenter。</summary>
    public sealed class MessageToastWindowPresenter : AUIWindowPresenter<MessageToastWindowView>
    {
        #region Protected 受保护成员

        /// <summary>
        /// 显示当前流程最近提示。
        /// </summary>
        protected override void OnBind()
        {
            MiniBomberClientFlowComponent flow = Global.Get<MiniBomberClientFlowComponent>(this);
            View.MessageText.text = flow.Message;
        }

        #endregion
    }

    /// <summary>网络诊断窗口 Presenter。</summary>
    public sealed class NetworkDebugWindowPresenter : AUIWindowPresenter<NetworkDebugWindowView>
    {
        #region Private 私有成员

        private INetworkService network; // 网络队列诊断服务。
        private BattleClientComponent battle; // 服务器 Tick 来源。
        private bool isActive; // 诊断窗口是否仍在活动。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 渲染当前网络队列和服务器 Tick 诊断值。
        /// </summary>
        protected override void OnBind()
        {
            network = Global.GetService<INetworkService>(this);
            battle = Global.Get<BattleClientComponent>(this);
            battle.SnapshotChanged += Render;
            Bindings.Add(() => battle.SnapshotChanged -= Render);
            isActive = true;
            Render();
            RefreshLoopAsync().Forget();
        }

        /// <summary>
        /// 清空网络和战斗状态引用。
        /// </summary>
        protected override void OnDispose()
        {
            isActive = false;
            network = null;
            battle = null;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 刷新服务器 Tick 与当前入站队列诊断。
        /// </summary>
        private void Render()
        {
            NetworkIncomingQueueSnapshot queue = network.GetIncomingQueueSnapshot();
            int rtt = 0;
            network.TryGetTransportRttMs(MiniBomberConstants.DefaultSessionId, out rtt);
            int snapshotAge = battle.LastSnapshotReceiveTime <= 0d
                ? 0
                : Mathf.Max(0, Mathf.RoundToInt((float)((Global.Time.UnscaledTime - battle.LastSnapshotReceiveTime) * 1000d)));
            View.DiagnosticsText.text = $"ServerTick: {battle.Snapshot?.ServerTick ?? 0}\nRTT: {rtt} ms\nSnapshotAge: {snapshotAge} ms\nQueued: {queue.PendingPacketCount}\nPeak: {queue.PeakPendingPacketCount}";
        }

        /// <summary>
        /// 每二百五十毫秒刷新 RTT、快照新鲜度和队列状态。
        /// </summary>
        /// <returns>窗口关闭后退出的诊断任务。</returns>
        private async MTask RefreshLoopAsync()
        {
            while (isActive)
            {
                await MTask.Delay(250);
                if (isActive)
                {
                    Render();
                }
            }
        }

        #endregion
    }
}
