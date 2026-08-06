using TMPro;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>断线重连系统遮罩的被动 View。</summary>
    public sealed class ReconnectOverlayView : MiniBomberWindowViewBase
    {
        #region UnityProperty Unity 引用属性

        /// <summary>重连状态和次数文本。</summary>
        public TMP_Text StatusText;

        #endregion
    }
}
