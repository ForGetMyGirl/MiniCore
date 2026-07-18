using System.Diagnostics;

namespace MiniCore.Core
{
    /// <summary>
    /// 使用 Stopwatch 为非 Unity 运行时提供时间。
    /// </summary>
    public sealed class SystemTimeProvider : ITimeProvider
    {
        #region Private 私有成员

        private readonly Stopwatch stopwatch = Stopwatch.StartNew(); // 进程内单调计时器。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取共享系统时间来源。
        /// </summary>
        public static SystemTimeProvider Shared { get; } = new SystemTimeProvider();

        /// <summary>
        /// 获取非缩放运行秒数。
        /// </summary>
        public double UnscaledTime => stopwatch.Elapsed.TotalSeconds;

        /// <summary>
        /// 获取缩放运行秒数；非 Unity 运行时保持与非缩放时间一致。
        /// </summary>
        public double ScaledTime => stopwatch.Elapsed.TotalSeconds;

        #endregion
    }
}
