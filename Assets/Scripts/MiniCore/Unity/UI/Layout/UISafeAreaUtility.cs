using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniCore.UI
{

    /// <summary>
    /// 提供设备安全区域到 UGUI 归一化锚点的无状态换算。
    /// </summary>
    public static class UISafeAreaUtility
    {
        #region Public 公共成员

        /// <summary>
        /// 将设备像素安全区域换算为零到一锚点矩形。
        /// </summary>
        /// <param name="safeArea">设备安全区域像素矩形。</param>
        /// <param name="pixelSize">完整屏幕像素尺寸。</param>
        /// <returns>归一化安全区域矩形。</returns>
        public static Rect Normalize(Rect safeArea, Vector2 pixelSize)
        {
            float width = Mathf.Max(1f, pixelSize.x);
            float height = Mathf.Max(1f, pixelSize.y);
            return Rect.MinMaxRect(safeArea.xMin / width, safeArea.yMin / height, safeArea.xMax / width, safeArea.yMax / height);
        }

        /// <summary>
        /// 将安全区域指标直接应用到目标 RectTransform。
        /// </summary>
        /// <param name="target">需要限制在安全区域内的布局节点。</param>
        /// <param name="metrics">最新分辨率指标。</param>
        public static void Apply(RectTransform target, UIResolutionMetrics metrics)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            Rect normalized = Normalize(metrics.SafeArea, metrics.PixelSize);
            target.anchorMin = normalized.min;
            target.anchorMax = normalized.max;
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }

        #endregion
    }
}
