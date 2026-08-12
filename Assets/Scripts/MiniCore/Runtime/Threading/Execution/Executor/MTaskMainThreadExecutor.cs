using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MiniCore.Threading
{

    /// <summary>
    /// 由宿主线程主动抽取的单线程 MTask 执行器。
    /// </summary>
    public sealed class MTaskMainThreadExecutor : IMTaskExecutor
    {
        #region Private 私有成员

        private readonly MTaskWorkQueue continuations = new MTaskWorkQueue(); // 跨线程进入的池化 MPSC 续体队列。
        private readonly object timerGate = new object(); // 保护延迟任务集合。
        private readonly List<MScheduledContinuation> timers = new List<MScheduledContinuation>(16); // 复用的延迟任务集合。
        private readonly int threadId; // 主线程托管线程标识。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取执行器诊断名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 获取调用线程是否为创建执行器的线程。
        /// </summary>
        public bool IsCurrentThread => Thread.CurrentThread.ManagedThreadId == threadId;

        /// <summary>
        /// 在当前线程创建一个需要主动抽取的执行器。
        /// </summary>
        /// <param name="name">执行器诊断名称。</param>
        public MTaskMainThreadExecutor(string name)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Main" : name;
            threadId = Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        /// 将续体加入主线程队列。
        /// </summary>
        /// <param name="continuation">要执行的续体。</param>
        public void Post(Action continuation)
        {
            if (continuation == null)
            {
                throw new ArgumentNullException(nameof(continuation));
            }

            continuations.Enqueue(continuation);
        }

        /// <summary>
        /// 注册一个延迟续体。
        /// </summary>
        /// <param name="continuation">要执行的续体。</param>
        /// <param name="delay">非负延迟时间。</param>
        /// <returns>延迟派发句柄。</returns>
        public IMTaskScheduledHandle Schedule(Action continuation, TimeSpan delay)
        {
            MScheduledContinuation scheduled = MScheduledContinuation.Rent(continuation, delay);
            lock (timerGate)
            {
                timers.Add(scheduled);
            }

            return scheduled;
        }

        /// <summary>
        /// 抽取当前已经到期的延迟项和等待执行的续体。
        /// </summary>
        /// <param name="maxContinuations">单次允许执行的最大续体数量。</param>
        /// <returns>本次实际执行的续体数量。</returns>
        public int Drain(int maxContinuations = 4096)
        {
            if (!IsCurrentThread)
            {
                throw new InvalidOperationException($"执行器 {Name} 只能由所属线程抽取。");
            }

            CollectDueTimers();
            int count = 0;
            while (count < maxContinuations && continuations.TryDequeue(out Action continuation))
            {
                continuation();
                count++;
            }

            return count;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将已经到期的延迟回调转移到续体队列。
        /// </summary>
        private void CollectDueTimers()
        {
            long now = MTaskClock.Timestamp;
            lock (timerGate)
            {
                for (int i = timers.Count - 1; i >= 0; i--)
                {
                    MScheduledContinuation scheduled = timers[i];
                    if (!scheduled.IsCanceled && scheduled.DueTimestamp > now)
                    {
                        continue;
                    }

                    timers.RemoveAt(i);
                    if (scheduled.TryTake(out Action continuation))
                    {
                        continuations.Enqueue(continuation);
                    }

                    scheduled.Return();
                }
            }
        }

        #endregion
    }
}
