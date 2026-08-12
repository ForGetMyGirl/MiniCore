using TMPro;
using UnityEngine.UI;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 场景切换进度窗口的被动 View。
    /// </summary>
    public sealed class SceneLoadingWindowView : MiniBomberWindowViewBase
    {
        #region UnityProperty Unity 引用属性

        /// <summary>
        /// 加载进度条。
        /// </summary>
        public Slider ProgressSlider;
        /// <summary>
        /// 加载状态文本。
        /// </summary>
        public TMP_Text PromptText;

        #endregion
    }
}
