using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniCore.UI
{
    /// <summary>
    /// 按宽高比和方向选择响应式布局的项目断点。
    /// </summary>
    [Serializable]
    public sealed class UIResponsiveBreakpoint
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置断点名称。
        /// </summary>
        public string Name = "Default";

        /// <summary>
        /// 获取或设置允许的最小宽高比。
        /// </summary>
        public float MinAspectRatio;

        /// <summary>
        /// 获取或设置允许的最大宽高比；零表示不限制。
        /// </summary>
        public float MaxAspectRatio;

        /// <summary>
        /// 获取或设置是否只匹配竖屏。
        /// </summary>
        public bool PortraitOnly;

        /// <summary>
        /// 获取或设置是否只匹配横屏。
        /// </summary>
        public bool LandscapeOnly;

        /// <summary>
        /// 判断给定屏幕指标是否命中当前断点。
        /// </summary>
        /// <param name="aspectRatio">当前宽高比。</param>
        /// <param name="portrait">当前是否为竖屏。</param>
        /// <returns>满足方向和宽高比限制时返回 true。</returns>
        public bool Matches(float aspectRatio, bool portrait)
        {
            if (PortraitOnly && !portrait || LandscapeOnly && portrait)
            {
                return false;
            }

            return aspectRatio >= MinAspectRatio && (MaxAspectRatio <= 0f || aspectRatio < MaxAspectRatio);
        }

        #endregion
    }

    /// <summary>
    /// 项目统一的 Root、安全区域、缓存、响应式断点和加载过渡配置。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniCore/UI/Project Profile", fileName = "UIProjectProfile")]
    public sealed class UIProjectProfile : ScriptableObject
    {
        #region Private 私有成员

        [SerializeField] private string applicationRootAddress = "ApplicationUIRoot"; // ApplicationUIRoot 资源地址。
        [SerializeField, Min(0)] private int loadingDelayMilliseconds = 120; // Loading 延迟显示时间。
        [SerializeField, Min(0)] private int loadingMinimumMilliseconds = 200; // Loading 最短显示时间。
        [SerializeField, Min(0)] private int defaultCacheCount = 1; // 未覆盖时的默认 View 缓存数量。
        [SerializeField] private UISafeAreaPolicy defaultSafeAreaPolicy = UISafeAreaPolicy.ConstrainContent; // 默认安全区域策略。
        [SerializeField] private List<UIResponsiveBreakpoint> breakpoints = new List<UIResponsiveBreakpoint>(); // 项目自定义响应式断点。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取 ApplicationUIRoot 资源地址。
        /// </summary>
        public string ApplicationRootAddress => applicationRootAddress;

        /// <summary>
        /// 获取 Loading 延迟显示毫秒数。
        /// </summary>
        public int LoadingDelayMilliseconds => loadingDelayMilliseconds;

        /// <summary>
        /// 获取 Loading 最短显示毫秒数。
        /// </summary>
        public int LoadingMinimumMilliseconds => loadingMinimumMilliseconds;

        /// <summary>
        /// 获取窗口默认缓存数量。
        /// </summary>
        public int DefaultCacheCount => defaultCacheCount;

        /// <summary>
        /// 获取项目默认安全区域策略。
        /// </summary>
        public UISafeAreaPolicy DefaultSafeAreaPolicy => defaultSafeAreaPolicy;

        /// <summary>
        /// 获取项目响应式断点列表。
        /// </summary>
        public IReadOnlyList<UIResponsiveBreakpoint> Breakpoints => breakpoints;

        /// <summary>
        /// 校验运行所需的 Root 地址和缓存配置。
        /// </summary>
        /// <param name="error">校验失败说明。</param>
        /// <returns>配置可用于初始化时返回 true。</returns>
        public bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(applicationRootAddress))
            {
                error = "UIProjectProfile 未配置 ApplicationUIRoot 地址。";
                return false;
            }

            if (defaultCacheCount < 0)
            {
                error = "UIProjectProfile 的默认缓存数量不能小于零。";
                return false;
            }

            if (defaultSafeAreaPolicy == UISafeAreaPolicy.Inherit)
            {
                error = "UIProjectProfile 的默认安全区域策略不能继续使用 Inherit。";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// 按配置顺序查找当前屏幕命中的响应式断点。
        /// </summary>
        /// <param name="aspectRatio">当前宽高比。</param>
        /// <param name="portrait">当前是否为竖屏。</param>
        /// <returns>命中名称；未配置或未命中时返回 Default。</returns>
        public string ResolveBreakpoint(float aspectRatio, bool portrait)
        {
            for (int i = 0; i < breakpoints.Count; i++)
            {
                UIResponsiveBreakpoint breakpoint = breakpoints[i];
                if (breakpoint != null && breakpoint.Matches(aspectRatio, portrait))
                {
                    return string.IsNullOrWhiteSpace(breakpoint.Name) ? "Default" : breakpoint.Name;
                }
            }

            return "Default";
        }

        #endregion
    }
}
