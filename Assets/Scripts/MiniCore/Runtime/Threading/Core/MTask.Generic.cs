using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// MiniCore 的带返回值低分配异步任务。
    /// </summary>
    /// <typeparam name="T">任务返回值类型。</typeparam>
    [AsyncMethodBuilder(typeof(MTaskMethodBuilder<>))]
    public readonly struct MTask<T>
    {
        #region Private 私有成员

        private readonly IMTaskSource<T> source; // 非同步任务使用的结果源。
        private readonly short token; // 结果源复用版本。
        private readonly T result; // 同步完成任务直接内联的结果。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 创建同步完成的带返回值 MTask。
        /// </summary>
        /// <param name="result">同步返回值。</param>
        public MTask(T result)
        {
            source = null;
            token = 0;
            this.result = result;
        }

        /// <summary>
        /// 创建由指定结果源驱动的 MTask。
        /// </summary>
        /// <param name="source">任务结果源。</param>
        /// <param name="token">结果源复用版本。</param>
        public MTask(IMTaskSource<T> source, short token)
        {
            this.source = source;
            this.token = token;
            result = default;
        }

        /// <summary>
        /// 获取 C# await 使用的等待器。
        /// </summary>
        /// <returns>带返回值 MTask 等待器。</returns>
        public MTaskAwaiter<T> GetAwaiter()
        {
            return new MTaskAwaiter<T>(source, token, result);
        }

        /// <summary>
        /// 将任务交给所属 Owner 的监督器，不再要求调用方 await。
        /// </summary>
        public void Forget()
        {
            if (source == null)
            {
                return;
            }

            (source as MTaskNode)?.DetachForForget();
            MTaskForgetObserver<T>.Observe(this);
        }

        /// <summary>
        /// 将单次消费任务转换为可被多个调用方等待的共享任务。
        /// </summary>
        /// <returns>显式持有共享结果的任务。</returns>
        public MSharedTask<T> Share()
        {
            return new MSharedTask<T>(this);
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 获取任务底层结果源。
        /// </summary>
        internal IMTaskSource<T> Source => source;

        /// <summary>
        /// 获取结果源版本。
        /// </summary>
        internal short Token => token;

        #endregion
    }
}
