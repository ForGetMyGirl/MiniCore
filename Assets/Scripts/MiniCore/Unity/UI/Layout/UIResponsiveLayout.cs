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
}
