#if UNITY_WEBGL && !UNITY_EDITOR
using System;

namespace MiniCore.Threading
{
    /// <summary>
    /// WebGL 环境中的独占线程执行器占位类型；所有创建请求都会明确失败。
    /// </summary>
    public sealed class MSingleThreadExecutor : IMTaskOwnedExecutor
    {
        #region Public 公共成员

        /// <summary>
        /// 获取执行器诊断名称。
        /// </summary>
        public string Name => "Unsupported.SingleThread";

        /// <summary>
        /// 获取当前线程是否属于该执行器；WebGL 始终返回 false。
        /// </summary>
        public bool IsCurrentThread => false;

        /// <summary>
        /// 获取执行器是否已经释放；占位实现始终视为已释放。
        /// </summary>
        public bool IsDisposed => true;

        /// <summary>
        /// 阻止在 WebGL 环境构造独占线程执行器。
        /// </summary>
        /// <param name="name">仅用于保持跨平台构造签名一致的诊断名称。</param>
        internal MSingleThreadExecutor(string name)
        {
            throw new PlatformNotSupportedException("当前运行环境不支持独占后台线程执行器。");
        }

        /// <summary>
        /// 阻止向 WebGL 独占线程占位执行器派发续体。
        /// </summary>
        /// <param name="continuation">不会被执行的续体。</param>
        public void Post(Action continuation)
        {
            throw new PlatformNotSupportedException("当前运行环境不支持独占后台线程执行器。");
        }

        /// <summary>
        /// 阻止在 WebGL 独占线程占位执行器上注册延迟续体。
        /// </summary>
        /// <param name="continuation">不会被执行的续体。</param>
        /// <param name="delay">不会被使用的延迟。</param>
        /// <returns>该方法不会返回。</returns>
        public IMTaskScheduledHandle Schedule(Action continuation, TimeSpan delay)
        {
            throw new PlatformNotSupportedException("当前运行环境不支持独占后台线程执行器。");
        }

        /// <summary>
        /// 释放占位执行器；该操作没有副作用。
        /// </summary>
        public void Dispose()
        {
        }

        #endregion
    }
}
#endif
