using TMPro;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>短时业务响应提示窗口的被动 View。</summary>
    public sealed class MessageToastWindowView : MiniBomberWindowViewBase
    {
        #region UnityProperty Unity 引用属性

        /// <summary>提示内容文本。</summary>
        public TMP_Text MessageText;

        #endregion
    }
}
