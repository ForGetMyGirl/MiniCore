using System;
using MiniCore.Threading;

namespace MiniCore.Core
{
    /// <summary>
    /// 为独立使用的网络对象选择可用执行器；由网络服务创建的对象应显式传入其私有执行器。
    /// </summary>
    internal static class NetworkExecutorResolver
    {
        #region Internal 内部成员

        /// <summary>
        /// 返回调用方传入的执行器；未传入时按当前运行环境选择线程池或 Unity 主循环。
        /// </summary>
        /// <param name="executor">调用模块显式提供的执行器。</param>
        /// <returns>当前环境可用的网络异步执行器。</returns>
        internal static IMTaskExecutor Resolve(IMTaskExecutor executor)
        {
            if (executor != null)
            {
                return executor;
            }

            if (MTaskExecutors.TryGetThreadPool(out IMTaskExecutor threadPool))
            {
                return threadPool;
            }

            return MTaskExecutors.Unity;
        }

        #endregion
    }
}
