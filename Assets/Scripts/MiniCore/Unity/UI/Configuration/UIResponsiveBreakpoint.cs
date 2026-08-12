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
}
