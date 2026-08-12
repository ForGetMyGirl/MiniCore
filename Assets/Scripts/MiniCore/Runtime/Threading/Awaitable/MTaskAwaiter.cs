using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// 无返回值 MTask 的 awaiter。
    /// </summary>
    public readonly struct MTaskAwaiter : ICriticalNotifyCompletion
    {
        #region Private 私有成员

        private readonly IMTaskSource source; // 等待的结果源。
        private readonly short token; // 结果源版本。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取任务是否已经完成。
        /// </summary>
        public bool IsCompleted => source == null || source.GetStatus(token) != MTaskStatus.Pending;

        /// <summary>
        /// 创建 MTask awaiter。
        /// </summary>
        /// <param name="source">任务结果源。</param>
        /// <param name="token">结果源版本。</param>
        public MTaskAwaiter(IMTaskSource source, short token)
        {
            this.source = source;
            this.token = token;
        }

        /// <summary>
        /// 注册安全续体。
        /// </summary>
        /// <param name="continuation">任务完成后的续体。</param>
        public void OnCompleted(Action continuation)
        {
            source.OnCompleted(continuation, token);
        }

        /// <summary>
        /// 注册不捕获 ExecutionContext 的续体。
        /// </summary>
        /// <param name="continuation">任务完成后的续体。</param>
        public void UnsafeOnCompleted(Action continuation)
        {
            source.OnCompleted(continuation, token);
        }

        /// <summary>
        /// 完成单次结果消费并传播异常或取消。
        /// </summary>
        public void GetResult()
        {
            source?.GetResult(token);
        }

        #endregion
    }
}
