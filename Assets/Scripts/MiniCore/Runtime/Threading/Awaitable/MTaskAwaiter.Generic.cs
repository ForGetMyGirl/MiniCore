using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// 带返回值 MTask 的 awaiter。
    /// </summary>
    /// <typeparam name="T">任务返回值类型。</typeparam>
    public readonly struct MTaskAwaiter<T> : ICriticalNotifyCompletion
    {
        #region Private 私有成员

        private readonly IMTaskSource<T> source; // 等待的结果源。
        private readonly short token; // 结果源版本。
        private readonly T result; // 同步完成时的内联结果。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取任务是否已经完成。
        /// </summary>
        public bool IsCompleted => source == null || source.GetStatus(token) != MTaskStatus.Pending;

        /// <summary>
        /// 创建带返回值 MTask awaiter。
        /// </summary>
        /// <param name="source">任务结果源。</param>
        /// <param name="token">结果源版本。</param>
        /// <param name="result">同步完成时的内联结果。</param>
        public MTaskAwaiter(IMTaskSource<T> source, short token, T result)
        {
            this.source = source;
            this.token = token;
            this.result = result;
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
        /// 完成单次结果消费并返回结果。
        /// </summary>
        /// <returns>异步任务的返回值。</returns>
        public T GetResult()
        {
            return source == null ? result : source.GetResult(token);
        }

        #endregion
    }
}
