using System;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;
using MiniCore.UI;

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
            View.ShowPrompt(string.Empty);
            View.SetCommandInteractable(true);
            View.BindActions(Bindings, Submit, Close);
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
            View.GetRegistrationInput(out string accountName, out string password, out string confirmPassword, out string playerName);
            if (string.IsNullOrEmpty(password))
            {
                View.ShowPrompt("请输入密码");
                return;
            }

            if (string.IsNullOrEmpty(confirmPassword))
            {
                View.ShowPrompt("请再次输入密码");
                return;
            }

            if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                View.ShowPrompt("两次输入的密码不一致");
                return;
            }

            commandRunning = true;
            View.SetCommandInteractable(false);
            try
            {
                View.ShowPrompt("正在注册账号...");
                MiniBomberCommandResult result = await account.RegisterAsync(accountName, password, playerName);
                if (!released)
                {
                    View.ShowPrompt(result.Message);
                }
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 注册请求失败：{exception}");
                if (!released)
                {
                    View.ShowPrompt("注册暂时失败，请稍后重试");
                }
            }
            finally
            {
                commandRunning = false;
                if (!released)
                {
                    View.SetCommandInteractable(true);
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

        #endregion
    }
}
