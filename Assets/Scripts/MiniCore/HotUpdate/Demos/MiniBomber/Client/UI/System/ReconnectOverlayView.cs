using TMPro;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 断线重连系统遮罩 View。
    /// </summary>
    public sealed class ReconnectOverlayView : MiniBomberWindowViewBase
    {
        #region UnityProperty Unity 引用属性

        [SerializeField] private TMP_Text StatusText; // 重连状态文本。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 显示格式化后的重连状态。
        /// </summary>
        /// <param name="message">重连状态文案。</param>
        public void ShowStatus(string message)
        {
            if (StatusText != null) StatusText.text = message ?? string.Empty;
        }

        #endregion
    }
}
