using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// MTask 续体执行器，定义即时派发、延迟派发和线程归属。
    /// </summary>
    public interface IMTaskExecutor
    {
        /// <summary>
        /// 获取执行器诊断名称。
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 获取调用线程是否为当前执行器线程。
        /// </summary>
        bool IsCurrentThread { get; }

        /// <summary>
        /// 派发一个续体。
        /// </summary>
        /// <param name="continuation">要执行的续体。</param>
        void Post(Action continuation);

        /// <summary>
        /// 在指定延迟后派发一个续体。
        /// </summary>
        /// <param name="continuation">要执行的续体。</param>
        /// <param name="delay">非负延迟时间。</param>
        /// <returns>可用于撤销延迟派发的句柄。</returns>
        IMTaskScheduledHandle Schedule(Action continuation, TimeSpan delay);
    }
}
