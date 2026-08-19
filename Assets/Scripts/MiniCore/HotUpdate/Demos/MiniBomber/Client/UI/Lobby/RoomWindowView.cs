using System;
using System.Collections.Generic;
using System.Text;
using MiniCore.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 房间窗口 View，负责成员列表、房主设置与按钮表现。
    /// </summary>
    public sealed class RoomWindowView : MiniBomberWindowViewBase
    {
        #region Private 私有成员

        private static readonly List<string> DurationOptions = new List<string> { "2分钟", "5分钟", "10分钟" }; // 固定局时长选项。
        private readonly StringBuilder memberListBuilder = new StringBuilder(256); // 成员列表显示缓存。
        private TMP_Text readyButtonText; // 准备按钮文本缓存。

        #endregion

        #region UnityProperty Unity 引用属性

        [SerializeField] private TMP_Text RoomTitleText; // 房间编号和名称。
        [SerializeField] private TMP_Text MemberListText; // 成员状态列表。
        [SerializeField] private TMP_InputField RoomNameInput; // 房间名输入。
        [SerializeField] private TMP_Dropdown DurationDropdown; // 局时长下拉框。
        [SerializeField] private Button ApplySettingsButton; // 应用设置按钮。
        [SerializeField] private Button ReadyButton; // 准备按钮。
        [SerializeField] private Button StartButton; // 开始比赛按钮。
        [SerializeField] private Button LeaveButton; // 离开房间按钮。
        [SerializeField] private TMP_Text PromptText; // 响应提示。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 初始化时长选项并绑定房间用户意图。
        /// </summary>
        /// <param name="bindings">窗口周期绑定集合。</param>
        /// <param name="applySettings">应用设置意图。</param>
        /// <param name="toggleReady">切换准备意图。</param>
        /// <param name="startMatch">开始比赛意图。</param>
        /// <param name="leave">离开房间意图。</param>
        public void BindActions(UIBindingSet bindings, Action applySettings, Action toggleReady, Action startMatch, Action leave)
        {
            if (DurationDropdown != null)
            {
                DurationDropdown.ClearOptions();
                DurationDropdown.AddOptions(DurationOptions);
            }

            readyButtonText = ReadyButton != null ? ReadyButton.GetComponentInChildren<TMP_Text>() : null;
            if (applySettings != null) bindings.Add(ApplySettingsButton, applySettings.Invoke);
            if (toggleReady != null) bindings.Add(ReadyButton, toggleReady.Invoke);
            if (startMatch != null) bindings.Add(StartButton, startMatch.Invoke);
            if (leave != null) bindings.Add(LeaveButton, leave.Invoke);
        }

        /// <summary>
        /// 使用房间专用显示数据刷新界面。
        /// </summary>
        /// <param name="data">房间窗口显示数据。</param>
        public void Refresh(RoomWindowViewData data)
        {
            if (data == null)
            {
                return;
            }

            if (RoomTitleText != null) RoomTitleText.text = $"#{data.RoomId}  {data.RoomName}";
            if (RoomNameInput != null)
            {
                if (!string.Equals(RoomNameInput.text, data.RoomName, StringComparison.Ordinal)) RoomNameInput.text = data.RoomName;
                RoomNameInput.interactable = data.IsOwner;
            }

            if (DurationDropdown != null)
            {
                if (DurationDropdown.value != data.DurationIndex) DurationDropdown.value = data.DurationIndex;
                DurationDropdown.interactable = data.IsOwner;
            }

            if (ApplySettingsButton != null) ApplySettingsButton.gameObject.SetActive(data.IsOwner);
            if (StartButton != null) StartButton.gameObject.SetActive(data.IsOwner);
            if (readyButtonText != null) readyButtonText.text = data.LocalReady ? "取消准备" : "准备";

            memberListBuilder.Clear();
            for (int index = 0; index < data.Members.Count; index++)
            {
                RoomMemberViewData member = data.Members[index];
                memberListBuilder.Append(member.IsOwner ? "[房主] " : string.Empty)
                    .Append(member.PlayerName).Append(' ')
                    .Append(member.IsOnline ? string.Empty : "[离线] ")
                    .Append(member.IsReady ? "已准备" : "未准备").AppendLine();
            }

            if (MemberListText != null) MemberListText.text = memberListBuilder.ToString();
        }

        /// <summary>
        /// 读取当前房间设置输入。
        /// </summary>
        /// <param name="roomName">房间名称。</param>
        /// <param name="durationIndex">局时长选项索引。</param>
        public void GetSettingsInput(out string roomName, out int durationIndex)
        {
            roomName = RoomNameInput != null ? RoomNameInput.text : string.Empty;
            durationIndex = DurationDropdown != null ? DurationDropdown.value : 0;
        }

        /// <summary>
        /// 显示房间命令提示。
        /// </summary>
        /// <param name="message">提示内容。</param>
        public void ShowPrompt(string message)
        {
            if (PromptText != null) PromptText.text = message ?? string.Empty;
        }

        /// <summary>
        /// 根据房主权限切换房间命令按钮交互状态。
        /// </summary>
        /// <param name="interactable">是否允许发起新命令。</param>
        /// <param name="isOwner">当前玩家是否为房主。</param>
        public void SetCommandInteractable(bool interactable, bool isOwner)
        {
            if (ApplySettingsButton != null) ApplySettingsButton.interactable = interactable && isOwner;
            if (ReadyButton != null) ReadyButton.interactable = interactable;
            if (StartButton != null) StartButton.interactable = interactable && isOwner;
            if (LeaveButton != null) LeaveButton.interactable = interactable;
        }

        #endregion
    }
}
