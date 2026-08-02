using MiniCore.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MiniCore.UI;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// KCP 网络功能测试窗口的被动 View 示例。
    /// </summary>
    public sealed class KcpTestWindowView : AUIWindowView
    {
        #region UnityProperty Unity 引用属性

        public Button startServerBtn;
        public Button stopServerBtn;
        public Button connectClientBtn;
        public Button disconnectClientBtn;
        public Button sendRpcBtn;
        public Button sendNormalBtn;
        public TMP_Text promptText;
        public TMP_InputField hostInput;
        public TMP_InputField portInput;
        public TMP_InputField convInput;
        public TMP_InputField messageInput;

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 追加一行窗口诊断文本。
        /// </summary>
        /// <param name="prompt">待显示文本。</param>
        public void UpdatePrompt(string prompt)
        {
            if (promptText != null)
            {
                promptText.text += $"{prompt}\n";
            }
        }

        /// <summary>
        /// 获取主机输入；空值时返回回退地址。
        /// </summary>
        /// <param name="fallback">回退主机地址。</param>
        /// <returns>有效主机地址。</returns>
        public string GetHostOrDefault(string fallback)
        {
            if (hostInput == null)
            {
                return fallback;
            }
            string text = hostInput.text;
            return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
        }

        /// <summary>
        /// 读取端口输入。
        /// </summary>
        /// <param name="fallback">空输入使用的默认端口。</param>
        /// <param name="port">解析后的端口。</param>
        /// <returns>输入为空或解析成功时返回 true。</returns>
        public bool TryGetPort(int fallback, out int port)
        {
            port = fallback;
            if (portInput == null)
            {
                return true;
            }
            string text = portInput.text;
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }
            return int.TryParse(text.Trim(), out port);
        }

        /// <summary>
        /// 读取 KCP Conv 输入。
        /// </summary>
        /// <param name="fallback">空输入使用的默认 Conv。</param>
        /// <param name="conv">解析后的 Conv。</param>
        /// <returns>输入为空或解析成功时返回 true。</returns>
        public bool TryGetConv(uint fallback, out uint conv)
        {
            conv = fallback;
            if (convInput == null)
            {
                return true;
            }
            string text = convInput.text;
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }
            return uint.TryParse(text.Trim(), out conv);
        }

        /// <summary>
        /// 获取消息输入；空值时返回回退文本。
        /// </summary>
        /// <param name="fallback">回退消息。</param>
        /// <returns>有效消息文本。</returns>
        public string GetMessageOrDefault(string fallback)
        {
            if (messageInput == null)
            {
                return fallback;
            }
            string text = messageInput.text;
            return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
        }

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 窗口进入 Staging 时保持现有控件状态。
        /// </summary>
        /// <returns>同步完成任务。</returns>
        protected override MTask OnOpenAsync()
        {
            return MTask.CompletedTask;
        }

        /// <summary>
        /// 窗口关闭时不执行额外异步操作。
        /// </summary>
        /// <returns>同步完成任务。</returns>
        protected override MTask OnCloseAsync()
        {
            return MTask.CompletedTask;
        }

        #endregion
    }
}
