using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// MiniCore 的无返回值低分配异步任务。
    /// </summary>
    [AsyncMethodBuilder(typeof(MTaskMethodBuilder))]
    public readonly partial struct MTask
    {
        #region Private 私有成员

        private readonly IMTaskSource source; // 非同步任务使用的结果源。
        private readonly short token; // 结果源复用版本。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取已经同步完成的 MTask。
        /// </summary>
        public static MTask CompletedTask => default;

        /// <summary>
        /// 创建同步完成的带返回值 MTask。
        /// </summary>
        /// <typeparam name="T">返回值类型。</typeparam>
        /// <param name="result">同步返回值。</param>
        /// <returns>内联保存结果的已完成任务。</returns>
        public static MTask<T> FromResult<T>(T result)
        {
            return new MTask<T>(result);
        }

        /// <summary>
        /// 创建由指定结果源驱动的 MTask。
        /// </summary>
        /// <param name="source">任务结果源。</param>
        /// <param name="token">结果源复用版本。</param>
        public MTask(IMTaskSource source, short token)
        {
            this.source = source;
            this.token = token;
        }

        /// <summary>
        /// 获取 C# await 使用的等待器。
        /// </summary>
        /// <returns>MTask 等待器。</returns>
        public MTaskAwaiter GetAwaiter()
        {
            return new MTaskAwaiter(source, token);
        }

        /// <summary>
        /// 创建由当前执行器驱动的延迟任务。
        /// </summary>
        /// <param name="delay">非负延迟时间。</param>
        /// <returns>延迟结束或当前任务取消时完成的 MTask。</returns>
        public static MTask Delay(TimeSpan delay)
        {
            return MTaskDelaySource.Create(delay);
        }

        /// <summary>
        /// 创建由当前执行器驱动的毫秒延迟任务。
        /// </summary>
        /// <param name="millisecondsDelay">非负延迟毫秒数。</param>
        /// <returns>延迟结束或当前任务取消时完成的 MTask。</returns>
        public static MTask Delay(int millisecondsDelay)
        {
            if (millisecondsDelay < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(millisecondsDelay));
            }

            return Delay(TimeSpan.FromMilliseconds(millisecondsDelay));
        }

        /// <summary>
        /// 将当前任务续体让出到执行器队列尾部。
        /// </summary>
        /// <returns>可直接 await 的让出操作。</returns>
        public static MTaskYieldAwaitable Yield()
        {
            return new MTaskYieldAwaitable(MTaskRuntime.CurrentExecutor ?? MTaskExecutors.Inline);
        }

        /// <summary>
        /// 将当前任务后续执行切换到指定执行器。
        /// </summary>
        /// <param name="executor">目标执行器。</param>
        /// <returns>可直接 await 的执行器切换操作。</returns>
        public static MTaskSwitchAwaitable SwitchTo(IMTaskExecutor executor)
        {
            return new MTaskSwitchAwaitable(executor ?? throw new ArgumentNullException(nameof(executor)));
        }

        /// <summary>
        /// 在当前任务已经取消时抛出域级复用取消异常。
        /// </summary>
        public static void ThrowIfCancellationRequested()
        {
            MTaskNode node = MTaskRuntime.CurrentNode;
            if (node != null && node.IsCancellationRequested)
            {
                throw node.Domain.CancellationException;
            }
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
            MTaskForgetObserver.Observe(this);
        }

        /// <summary>
        /// 将单次消费任务转换为可被多个调用方等待的共享任务。
        /// </summary>
        /// <returns>显式持有共享状态的任务。</returns>
        public MSharedTask Share()
        {
            return new MSharedTask(this);
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 获取任务底层结果源。
        /// </summary>
        internal IMTaskSource Source => source;

        /// <summary>
        /// 获取结果源版本。
        /// </summary>
        internal short Token => token;

        #endregion
    }
}
