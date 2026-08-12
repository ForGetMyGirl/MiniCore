using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MiniCore.Threading
{

    /// <summary>
    /// 在调用线程立即执行续体的兜底执行器。
    /// </summary>
    internal sealed class MInlineExecutor : IMTaskExecutor
    {
        #region Public 公共成员

        /// <summary>
        /// 获取执行器名称。
        /// </summary>
        public string Name => "Inline";

        /// <summary>
        /// 获取当前线程始终属于同步执行器。
        /// </summary>
        public bool IsCurrentThread => true;

        /// <summary>
        /// 立即执行续体。
        /// </summary>
        /// <param name="continuation">要执行的续体。</param>
        public void Post(Action continuation)
        {
            continuation?.Invoke();
        }

        /// <summary>
        /// 注册兜底延迟回调。
        /// </summary>
        /// <param name="continuation">要执行的续体。</param>
        /// <param name="delay">延迟时间。</param>
        /// <returns>延迟回调句柄。</returns>
        public IMTaskScheduledHandle Schedule(Action continuation, TimeSpan delay)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (MTaskExecutors.TryGetUnityExecutor(out IMTaskExecutor executor) && !ReferenceEquals(executor, this))
            {
                return executor.Schedule(continuation, delay);
            }

            throw new PlatformNotSupportedException("WebGL 初始化主循环执行器前不能注册延迟任务。");
#else
            return new MTimerScheduledHandle(continuation, delay);
#endif
        }

        #endregion
    }
}
