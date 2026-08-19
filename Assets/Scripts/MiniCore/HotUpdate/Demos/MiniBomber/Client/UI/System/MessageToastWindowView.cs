using TMPro;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 短时业务响应提示窗口 View。
    /// </summary>
    public sealed class MessageToastWindowView : MiniBomberWindowViewBase
    {
        #region UnityProperty Unity 引用属性

        [SerializeField] private TMP_Text MessageText; // 提示内容文本。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 显示短时业务提示。
        /// </summary>
        /// <param name="message">提示内容。</param>
        public void ShowMessage(string message)
        {
            if (MessageText != null) MessageText.text = message ?? string.Empty;
        }

        #endregion
    }
}
