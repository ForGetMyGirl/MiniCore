using System;
using System.Text;
using MiniCore.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 大厅窗口 View，负责房间列表与大厅控件表现。
    /// </summary>
    public sealed class LobbyWindowView : MiniBomberWindowViewBase
    {
        #region Private 私有成员

        private readonly StringBuilder roomListBuilder = new StringBuilder(512); // 房间列表显示缓存。

        #endregion

        #region UnityProperty Unity 引用属性

        [SerializeField] private TMP_Text PlayerNameText; // 当前玩家姓名。
        [SerializeField] private TMP_Text OnlineCountText; // 服务器在线人数。
        [SerializeField] private TMP_Text RoomListText; // 房间列表文本。
        [SerializeField] private TMP_InputField JoinRoomIdInput; // 房间编号输入。
        [SerializeField] private Button RefreshButton; // 刷新按钮。
        [SerializeField] private Button CreateButton; // 创建房间按钮。
        [SerializeField] private Button JoinButton; // 加入房间按钮。
        [SerializeField] private Button LogoutButton; // 注销按钮。
        [SerializeField] private TMP_Text PromptText; // 响应提示。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 绑定大厅窗口的用户意图。
        /// </summary>
        /// <param name="bindings">窗口周期绑定集合。</param>
        /// <param name="refresh">刷新意图。</param>
        /// <param name="create">创建房间意图。</param>
        /// <param name="join">加入房间意图。</param>
        /// <param name="logout">注销意图。</param>
        public void BindActions(UIBindingSet bindings, Action refresh, Action create, Action join, Action logout)
        {
            if (refresh != null) bindings.Add(RefreshButton, refresh.Invoke);
            if (create != null) bindings.Add(CreateButton, create.Invoke);
            if (join != null) bindings.Add(JoinButton, join.Invoke);
            if (logout != null) bindings.Add(LogoutButton, logout.Invoke);
        }

        /// <summary>
        /// 使用大厅专用显示数据刷新界面。
        /// </summary>
        /// <param name="data">大厅窗口显示数据。</param>
        public void Refresh(LobbyWindowViewData data)
        {
            if (data == null)
            {
                return;
            }

            if (PlayerNameText != null) PlayerNameText.text = data.PlayerName;
            if (OnlineCountText != null) OnlineCountText.text = $"在线：{data.OnlinePlayerCount}";
            roomListBuilder.Clear();
            for (int index = 0; index < data.Rooms.Count; index++)
            {
                LobbyRoomItemViewData item = data.Rooms[index];
                roomListBuilder.Append('#').Append(item.RoomId).Append(' ')
                    .Append(item.RoomName).Append("  ")
                    .Append(item.PlayerCount).Append('/').Append(item.MaxPlayerCount).Append("  ")
                    .Append(item.DurationSeconds / 60).Append("分钟  房主:").Append(item.OwnerName).AppendLine();
            }

            if (RoomListText != null)
            {
                RoomListText.text = roomListBuilder.Length == 0 ? "暂无房间" : roomListBuilder.ToString();
            }
        }

        /// <summary>
        /// 尝试读取合法的房间编号。
        /// </summary>
        /// <param name="roomId">解析后的房间编号。</param>
        /// <returns>输入是否为合法长整数。</returns>
        public bool TryGetJoinRoomId(out long roomId)
        {
            return long.TryParse(JoinRoomIdInput != null ? JoinRoomIdInput.text : string.Empty, out roomId);
        }

        /// <summary>
        /// 显示大厅命令提示。
        /// </summary>
        /// <param name="message">提示内容。</param>
        public void ShowPrompt(string message)
        {
            if (PromptText != null) PromptText.text = message ?? string.Empty;
        }

        /// <summary>
        /// 切换大厅命令按钮交互状态。
        /// </summary>
        /// <param name="interactable">是否允许交互。</param>
        public void SetCommandInteractable(bool interactable)
        {
            if (RefreshButton != null) RefreshButton.interactable = interactable;
            if (CreateButton != null) CreateButton.interactable = interactable;
            if (JoinButton != null) JoinButton.interactable = interactable;
            if (LogoutButton != null) LogoutButton.interactable = interactable;
        }

        #endregion
    }
}
