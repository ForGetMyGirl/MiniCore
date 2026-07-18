namespace MiniCore.Core
{
    /// <summary>
    /// 为 Runtime 提供单调递增的缩放与非缩放时间。
    /// </summary>
    public interface ITimeProvider
    {
        /// <summary>
        /// 获取非缩放运行秒数。
        /// </summary>
        double UnscaledTime { get; }

        /// <summary>
        /// 获取缩放后的运行秒数。
        /// </summary>
        double ScaledTime { get; }
    }
}
