using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiniCore.UI
{

    /// <summary>
    /// 只在屏幕、安全区域或响应式断点变化时广播布局指标。
    /// </summary>
    public sealed partial class UIResolutionService : MonoBehaviour
    {
        #region Private 私有成员

        private UIProjectProfile profile; // 当前项目 Profile。
        private int lastWidth = -1; // 上次屏幕宽度。
        private int lastHeight = -1; // 上次屏幕高度。
        private Rect lastSafeArea; // 上次安全区域。
        private UIResolutionMetrics current; // 当前布局指标。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 分辨率、安全区域或断点发生变化时触发。
        /// </summary>
        public event Action<UIResolutionMetrics> Changed;

        /// <summary>
        /// 获取最近一次计算结果。
        /// </summary>
        public UIResolutionMetrics Current => current;

        /// <summary>
        /// 使用项目 Profile 初始化并立即计算布局指标。
        /// </summary>
        /// <param name="projectProfile">当前项目 UI Profile。</param>
        public void Initialize(UIProjectProfile projectProfile)
        {
            profile = projectProfile ?? throw new ArgumentNullException(nameof(projectProfile));
            Refresh(true);
        }

        /// <summary>
        /// 强制重新计算并广播当前布局指标。
        /// </summary>
        public void Refresh()
        {
            Refresh(true);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 每帧只比较值类型屏幕指标，未变化时不执行布局计算。
        /// </summary>
        private void Update()
        {
            Refresh(false);
        }

        /// <summary>
        /// 在输入值变化或强制请求时重新计算布局指标。
        /// </summary>
        /// <param name="force">是否忽略缓存强制广播。</param>
        private void Refresh(bool force)
        {
            if (profile == null)
            {
                return;
            }

            int width = Mathf.Max(1, Screen.width);
            int height = Mathf.Max(1, Screen.height);
            Rect safeArea = Screen.safeArea;
            if (!force && width == lastWidth && height == lastHeight && safeArea == lastSafeArea)
            {
                return;
            }

            lastWidth = width;
            lastHeight = height;
            lastSafeArea = safeArea;
            float aspectRatio = width / (float)height;
            bool portrait = height > width;
            current = new UIResolutionMetrics(new Vector2(width, height), safeArea, aspectRatio, portrait, profile.ResolveBreakpoint(aspectRatio, portrait));
            Changed?.Invoke(current);
        }

        #endregion
    }
}
