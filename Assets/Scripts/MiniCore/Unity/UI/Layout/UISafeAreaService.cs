using System;
using UnityEngine;

namespace MiniCore.UI
{
    /// <summary>
    /// 对外提供设备安全区域变化的 Root 级服务组件。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UISafeAreaService : MonoBehaviour
    {
        #region Private 私有成员

        private UIResolutionService resolutionService; // 安全区域数据来源。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 安全区域像素矩形发生变化时触发。
        /// </summary>
        public event Action<Rect> Changed;

        /// <summary>
        /// 获取最近一次设备安全区域像素矩形。
        /// </summary>
        public Rect Current { get; private set; }

        /// <summary>
        /// 绑定统一分辨率服务并发布当前安全区域。
        /// </summary>
        /// <param name="service">ApplicationUIRoot 分辨率服务。</param>
        public void Initialize(UIResolutionService service)
        {
            if (resolutionService != null)
            {
                resolutionService.Changed -= OnMetricsChanged;
            }

            resolutionService = service ?? throw new ArgumentNullException(nameof(service));
            resolutionService.Changed += OnMetricsChanged;
            OnMetricsChanged(resolutionService.Current);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 组件销毁时解除数据源订阅。
        /// </summary>
        private void OnDestroy()
        {
            if (resolutionService != null)
            {
                resolutionService.Changed -= OnMetricsChanged;
            }
        }

        /// <summary>
        /// 仅在安全区域值实际变化时广播。
        /// </summary>
        /// <param name="metrics">最新分辨率指标。</param>
        private void OnMetricsChanged(UIResolutionMetrics metrics)
        {
            if (Current == metrics.SafeArea)
            {
                return;
            }

            Current = metrics.SafeArea;
            Changed?.Invoke(Current);
        }

        #endregion
    }
}
