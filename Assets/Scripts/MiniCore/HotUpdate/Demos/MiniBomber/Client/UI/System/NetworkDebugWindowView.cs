using TMPro;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 网络 Tick、RTT、快照延迟和队列诊断窗口的被动 View。
    /// </summary>
    public sealed class NetworkDebugWindowView : MiniBomberWindowViewBase
    {
        #region UnityProperty Unity 引用属性

        /// <summary>
        /// 网络诊断文本。
        /// </summary>
        public TMP_Text DiagnosticsText;

        #endregion
    }
}
