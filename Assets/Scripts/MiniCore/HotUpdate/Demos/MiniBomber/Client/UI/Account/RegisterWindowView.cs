using System;
using MiniCore.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 注册弹窗 View，封装输入、提示与按钮交互。
    /// </summary>
    public sealed class RegisterWindowView : MiniBomberWindowViewBase
    {
        #region UnityProperty Unity 引用属性

        [SerializeField] private TMP_InputField AccountInput; // 账号输入。
        [SerializeField] private TMP_InputField PasswordInput; // 密码输入。
        [SerializeField] private TMP_InputField ConfirmPasswordInput; // 确认密码输入。
        [SerializeField] private TMP_InputField PlayerNameInput; // 玩家姓名输入。
        [SerializeField] private Button SubmitButton; // 确认注册按钮。
        [SerializeField] private Button CloseButton; // 关闭按钮。
        [SerializeField] private TMP_Text PromptText; // 响应提示。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 绑定提交注册与关闭弹窗意图。
        /// </summary>
        /// <param name="bindings">窗口周期绑定集合。</param>
        /// <param name="submit">提交注册意图。</param>
        /// <param name="close">关闭弹窗意图。</param>
        public void BindActions(UIBindingSet bindings, Action submit, Action close)
        {
            if (submit != null)
            {
                bindings.Add(SubmitButton, submit.Invoke);
            }

            if (close != null)
            {
                bindings.Add(CloseButton, close.Invoke);
            }
        }

        /// <summary>
        /// 读取当前注册输入。
        /// </summary>
        /// <param name="account">账号文本。</param>
        /// <param name="password">密码文本。</param>
        /// <param name="confirmPassword">确认密码文本。</param>
        /// <param name="playerName">玩家名称。</param>
        public void GetRegistrationInput(out string account, out string password, out string confirmPassword, out string playerName)
        {
            account = AccountInput != null ? AccountInput.text : string.Empty;
            password = PasswordInput != null ? PasswordInput.text : string.Empty;
            confirmPassword = ConfirmPasswordInput != null ? ConfirmPasswordInput.text : string.Empty;
            playerName = PlayerNameInput != null ? PlayerNameInput.text : string.Empty;
        }

        /// <summary>
        /// 显示注册流程提示。
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
        /// 切换注册弹窗按钮的交互状态。
        /// </summary>
        /// <param name="interactable">是否允许交互。</param>
        public void SetCommandInteractable(bool interactable)
        {
            if (SubmitButton != null)
            {
                SubmitButton.interactable = interactable;
            }

            if (CloseButton != null)
            {
                CloseButton.interactable = interactable;
            }
        }

        #endregion
    }
}
