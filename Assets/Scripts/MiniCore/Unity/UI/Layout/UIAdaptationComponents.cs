using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniCore.UI
{
    /// <summary>
    /// 根据当前响应式断点启用唯一布局变体。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class UIResponsiveLayout : MonoBehaviour
    {
        #region UnityProperty Unity 引用属性

        [SerializeField] private UIResponsiveVariant[] variants = Array.Empty<UIResponsiveVariant>(); // 断点对应的布局根节点。

        #endregion

        #region Private 私有成员

        private UIResolutionService resolutionService; // 当前 Root 的分辨率服务。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 绑定分辨率服务并立即应用当前断点。
        /// </summary>
        /// <param name="service">当前 ApplicationUIRoot 的分辨率服务。</param>
        public void Bind(UIResolutionService service)
        {
            if (resolutionService == service)
            {
                return;
            }

            Unbind();
            resolutionService = service ?? throw new ArgumentNullException(nameof(service));
            resolutionService.Changed += Apply;
            Apply(resolutionService.Current);
        }

        /// <summary>
        /// 解除当前分辨率服务订阅。
        /// </summary>
        public void Unbind()
        {
            if (resolutionService != null)
            {
                resolutionService.Changed -= Apply;
                resolutionService = null;
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 组件失活时解除服务订阅。
        /// </summary>
        private void OnDisable()
        {
            Unbind();
        }

        /// <summary>
        /// 启用与当前断点匹配的布局节点。
        /// </summary>
        /// <param name="metrics">最新分辨率指标。</param>
        private void Apply(UIResolutionMetrics metrics)
        {
            bool matched = false;
            for (int i = 0; i < variants.Length; i++)
            {
                UIResponsiveVariant variant = variants[i];
                bool active = !matched && variant.Root != null && string.Equals(variant.Breakpoint, metrics.Breakpoint, StringComparison.Ordinal);
                if (variant.Root != null && variant.Root.activeSelf != active)
                {
                    variant.Root.SetActive(active);
                }

                matched |= active;
            }
        }

        #endregion
    }

    /// <summary>
    /// 响应式断点与布局节点的序列化映射。
    /// </summary>
    [Serializable]
    public struct UIResponsiveVariant
    {
        #region Public 公共成员

        /// <summary>
        /// Profile 中的断点名称。
        /// </summary>
        public string Breakpoint;

        /// <summary>
        /// 命中断点时启用的布局节点。
        /// </summary>
        public GameObject Root;

        #endregion
    }

    /// <summary>
    /// 在锚点布局之后限制内容的设计坐标尺寸。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed partial class UIContentConstraint : MonoBehaviour
    {
        #region UnityProperty Unity 引用属性

        [SerializeField] private Vector2 minimumSize; // 允许的最小设计尺寸。
        [SerializeField] private Vector2 maximumSize; // 允许的最大设计尺寸，零表示不限制。

        #endregion

        #region Private 私有成员

        private RectTransform rectTransform; // 当前布局节点。
        private Vector2 lastParentSize = new Vector2(-1f, -1f); // 上次父节点尺寸。

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 父节点尺寸改变后重新应用限制。
        /// </summary>
        private void OnRectTransformDimensionsChange()
        {
            RectTransform current = rectTransform ??= GetComponent<RectTransform>();
            RectTransform parent = current.parent as RectTransform;
            if (parent == null || parent.rect.size == lastParentSize)
            {
                return;
            }

            lastParentSize = parent.rect.size;
            Vector2 size = lastParentSize;
            size.x = Mathf.Max(minimumSize.x, maximumSize.x > 0f ? Mathf.Min(size.x, maximumSize.x) : size.x);
            size.y = Mathf.Max(minimumSize.y, maximumSize.y > 0f ? Mathf.Min(size.y, maximumSize.y) : size.y);
            current.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            current.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        }

        #endregion
    }

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
