using System;
using System.Collections.Generic;
using MiniCore.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 创建房间弹窗 View，封装创建参数与交互表现。
    /// </summary>
    public sealed class CreateRoomPopupView : MiniBomberWindowViewBase
    {
        #region Private 私有成员

        private static readonly List<string> DurationOptions = new List<string> { "2分钟", "5分钟", "10分钟" }; // 固定局时长选项。

        #endregion

        #region UnityProperty Unity 引用属性

        [SerializeField] private TMP_InputField RoomNameInput; // 房间名输入。
        [SerializeField] private TMP_Dropdown DurationDropdown; // 局时长下拉框。
        [SerializeField] private Button SubmitButton; // 创建按钮。
        [SerializeField] private Button CancelButton; // 取消按钮。
        [SerializeField] private TMP_Text PromptText; // 响应提示。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 初始化时长选项并绑定创建与取消意图。
        /// </summary>
        /// <param name="bindings">窗口周期绑定集合。</param>
        /// <param name="submit">创建房间意图。</param>
        /// <param name="cancel">取消意图。</param>
        public void BindActions(UIBindingSet bindings, Action submit, Action cancel)
        {
            if (DurationDropdown != null)
            {
                DurationDropdown.ClearOptions();
                DurationDropdown.AddOptions(DurationOptions);
            }

            if (submit != null)
            {
                bindings.Add(SubmitButton, submit.Invoke);
            }

            if (cancel != null)
            {
                bindings.Add(CancelButton, cancel.Invoke);
            }
        }

        /// <summary>
        /// 读取房间名称和时长选项索引。
        /// </summary>
        /// <param name="roomName">房间名称。</param>
        /// <param name="durationIndex">局时长选项索引。</param>
        public void GetCreateInput(out string roomName, out int durationIndex)
        {
            roomName = RoomNameInput != null ? RoomNameInput.text : string.Empty;
            durationIndex = DurationDropdown != null ? DurationDropdown.value : 0;
        }

        /// <summary>
        /// 显示创建房间流程提示。
        /// </summary>
        /// <param name="message">提示内容。</param>
        public void ShowPrompt(string message)
        {
            if (PromptText != null)
            {
                PromptText.text = message ?? string.Empty;
            }
        }

        /// <summary>
        /// 切换创建房间弹窗按钮的交互状态。
        /// </summary>
        /// <param name="interactable">是否允许交互。</param>
        public void SetCommandInteractable(bool interactable)
        {
            if (SubmitButton != null)
            {
                SubmitButton.interactable = interactable;
            }

            if (CancelButton != null)
            {
                CancelButton.interactable = interactable;
            }
        }

        #endregion
    }
}
