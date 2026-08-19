using System;
using MiniCore.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 登录界面 View，封装输入、提示与按钮交互。
    /// </summary>
    public sealed class LoginWindowView : MiniBomberWindowViewBase
    {
        #region UnityProperty Unity 引用属性

        [SerializeField] private TMP_InputField AccountInput; // 账号输入。
        [SerializeField] private TMP_InputField PasswordInput; // 密码输入。
        [SerializeField] private Button LoginButton; // 登录按钮。
        [SerializeField] private Button RegisterButton; // 打开注册按钮。
        [SerializeField] private TMP_Text PromptText; // 请求状态文本。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 绑定登录与打开注册界面的用户意图。
        /// </summary>
        /// <param name="bindings">窗口周期绑定集合。</param>
        /// <param name="login">登录意图。</param>
        /// <param name="register">打开注册界面意图。</param>
        public void BindActions(UIBindingSet bindings, Action login, Action register)
        {
            if (login != null)
            {
                bindings.Add(LoginButton, login.Invoke);
            }

            if (register != null)
            {
                bindings.Add(RegisterButton, register.Invoke);
            }
        }

        /// <summary>
        /// 读取当前账号和密码输入。
        /// </summary>
        /// <param name="account">账号文本。</param>
        /// <param name="password">密码文本。</param>
        public void GetCredentials(out string account, out string password)
        {
            account = AccountInput != null ? AccountInput.text : string.Empty;
            password = PasswordInput != null ? PasswordInput.text : string.Empty;
        }

        /// <summary>
        /// 显示登录流程提示。
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
        /// 切换登录入口按钮的交互状态。
        /// </summary>
        /// <param name="interactable">是否允许交互。</param>
        public void SetCommandInteractable(bool interactable)
        {
            if (LoginButton != null)
            {
                LoginButton.interactable = interactable;
            }

            if (RegisterButton != null)
            {
                RegisterButton.interactable = interactable;
            }
        }

        #endregion
    }
}
