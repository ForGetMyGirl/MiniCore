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
    /// 注册弹窗 Presenter。
    /// </summary>
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
                AuthenticationRegisterResponse response = await account.RegisterAsync(accountName, password, playerName);
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
}
