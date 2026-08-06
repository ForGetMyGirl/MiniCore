using TMPro;
using UnityEngine.UI;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>登录界面的被动 View。</summary>
    public sealed class LoginWindowView : MiniBomberWindowViewBase
    {
        #region UnityProperty Unity 引用属性

        /// <summary>服务器地址输入。</summary>
        public TMP_InputField HostInput;
        /// <summary>服务器端口输入。</summary>
        public TMP_InputField PortInput;
        /// <summary>账号输入。</summary>
        public TMP_InputField AccountInput;
        /// <summary>密码输入。</summary>
        public TMP_InputField PasswordInput;
        /// <summary>登录按钮。</summary>
        public Button LoginButton;
        /// <summary>打开注册按钮。</summary>
        public Button RegisterButton;
        /// <summary>请求状态文本。</summary>
        public TMP_Text PromptText;

        #endregion
    }
}
