using MiniCore.Core;
using UnityEngine;

namespace MiniCore.Unity
{
    /// <summary>
    /// 将 Unity 时间系统适配为 Runtime 时间来源。
    /// </summary>
    public sealed class UnityTimeProvider : ITimeProvider
    {
        /// <summary>
        /// 获取 Unity 非缩放运行秒数。
        /// </summary>
        public double UnscaledTime => Time.unscaledTimeAsDouble;

        /// <summary>
        /// 获取 Unity 缩放运行秒数。
        /// </summary>
        public double ScaledTime => Time.timeAsDouble;
    }
}
