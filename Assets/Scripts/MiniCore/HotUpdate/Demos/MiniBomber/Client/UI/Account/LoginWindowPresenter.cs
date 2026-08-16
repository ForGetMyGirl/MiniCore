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
    /// 登录窗口 Presenter。
    /// </summary>
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
        /// 重置缓存窗口状态并绑定登录和注册按钮。
        /// </summary>
        protected override void OnBind()
        {
            account = Global.Get<AccountSessionComponent>(this);
            flow = Global.Get<MiniBomberClientFlowComponent>(this);
            commandRunning = false;
            View.PromptText.text = string.Empty;
            SetCommandInteractable(true);
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
            commandRunning = true;
            SetCommandInteractable(false);
            string accountName = View.AccountInput.text;
            string password = View.PasswordInput.text;
            bool authenticated = false;
            try
            {
                View.PromptText.text = "正在连接服务器...";
                if (!await account.ConnectAsync())
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
                MiniBomberResumeSessionResponse response = await account.LoginAsync(accountName, password);
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
            commandRunning = true;
            SetCommandInteractable(false);
            try
            {
                View.PromptText.text = "正在连接服务器...";
                if (!await account.ConnectAsync())
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
}
