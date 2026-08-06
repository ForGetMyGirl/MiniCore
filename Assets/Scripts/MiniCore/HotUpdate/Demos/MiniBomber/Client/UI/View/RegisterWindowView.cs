using TMPro;
using UnityEngine.UI;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>注册弹窗的被动 View。</summary>
    public sealed class RegisterWindowView : MiniBomberWindowViewBase
    {
        #region UnityProperty Unity 引用属性

        /// <summary>账号输入。</summary>
        public TMP_InputField AccountInput;
        /// <summary>密码输入。</summary>
        public TMP_InputField PasswordInput;
        /// <summary>确认密码输入。</summary>
        public TMP_InputField ConfirmPasswordInput;
        /// <summary>玩家姓名输入。</summary>
        public TMP_InputField PlayerNameInput;
        /// <summary>确认注册按钮。</summary>
        public Button SubmitButton;
        /// <summary>关闭按钮。</summary>
        public Button CloseButton;
        /// <summary>响应提示。</summary>
        public TMP_Text PromptText;

        #endregion
    }
}
