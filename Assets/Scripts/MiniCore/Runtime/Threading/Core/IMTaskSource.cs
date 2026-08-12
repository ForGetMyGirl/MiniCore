using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// 无返回值 MTask 的底层结果源契约。
    /// </summary>
    public interface IMTaskSource
    {
        /// <summary>
        /// 获取指定版本任务的当前状态。
        /// </summary>
        /// <param name="token">结果源复用版本。</param>
        /// <returns>当前任务状态。</returns>
        MTaskStatus GetStatus(short token);

        /// <summary>
        /// 注册任务完成后的续体。
        /// </summary>
        /// <param name="continuation">任务完成后执行的续体。</param>
        /// <param name="token">结果源复用版本。</param>
        void OnCompleted(Action continuation, short token);

        /// <summary>
        /// 取得任务结果并完成单次消费。
        /// </summary>
        /// <param name="token">结果源复用版本。</param>
        void GetResult(short token);
    }
}
