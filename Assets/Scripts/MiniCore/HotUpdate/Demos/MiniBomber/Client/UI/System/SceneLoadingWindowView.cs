using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 场景切换进度窗口 View。
    /// </summary>
    public sealed class SceneLoadingWindowView : MiniBomberWindowViewBase
    {
        #region UnityProperty Unity 引用属性

        [SerializeField] private Slider ProgressSlider; // 加载进度条。
        [SerializeField] private TMP_Text PromptText; // 加载状态文本。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 显示场景加载提示。
        /// </summary>
        /// <param name="message">加载提示。</param>
        public void ShowPrompt(string message)
        {
            if (PromptText != null) PromptText.text = message ?? string.Empty;
        }

        /// <summary>
        /// 刷新零到一的场景加载进度。
        /// </summary>
        /// <param name="progress">加载进度。</param>
        public void RefreshProgress(float progress)
        {
            if (ProgressSlider == null) return;
            float value = Mathf.Clamp01(progress);
            if (!Mathf.Approximately(ProgressSlider.value, value)) ProgressSlider.value = value;
        }

        #endregion
    }
}
