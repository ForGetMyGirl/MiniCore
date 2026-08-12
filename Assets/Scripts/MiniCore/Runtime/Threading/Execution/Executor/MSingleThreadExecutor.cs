#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// 拥有独占后台线程的顺序 MTask 执行器。
    /// </summary>
    public sealed class MSingleThreadExecutor : IMTaskOwnedExecutor
    {
        #region Private 私有成员

        private readonly MTaskWorkQueue continuations = new MTaskWorkQueue(); // 等待后台线程执行的池化 MPSC 续体队列。
        private readonly object timerGate = new object(); // 保护延迟任务集合。
        private readonly List<MScheduledContinuation> timers = new List<MScheduledContinuation>(16); // 后台线程的延迟任务。
        private readonly AutoResetEvent signal = new AutoResetEvent(false); // 唤醒后台线程。
        private readonly Thread thread; // 独占工作线程。
        private int disposed; // 是否已经请求退出。
        private int fastShutdown; // 是否应在退出时丢弃未执行工作并避免主线程等待。
        private int signalDisposed; // 等待句柄是否已经释放。
        private int threadId; // 实际工作线程标识。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取执行器诊断名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 获取调用线程是否为执行器的独占线程。
        /// </summary>
        public bool IsCurrentThread => Thread.CurrentThread.ManagedThreadId == Volatile.Read(ref threadId);

        /// <summary>
        /// 获取执行器是否已经收到释放请求。
        /// </summary>
        public bool IsDisposed => Volatile.Read(ref disposed) != 0;

        /// <summary>
        /// 创建并立即启动一个独占后台线程执行器。
        /// </summary>
        /// <param name="name">线程和执行器诊断名称。</param>
        internal MSingleThreadExecutor(string name)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "MTask.Worker" : name;
            thread = new Thread(Run)
            {
                IsBackground = true,
                Name = Name
            };
            thread.Start();
        }

        /// <summary>
        /// 向独占线程派发续体。
        /// </summary>
        /// <param name="continuation">要执行的续体。</param>
        public void Post(Action continuation)
        {
            if (continuation == null)
            {
                throw new ArgumentNullException(nameof(continuation));
            }

            if (Volatile.Read(ref disposed) != 0)
            {
                if (MTaskRuntime.IsFastShutdown)
                {
                    return;
                }

                throw new ObjectDisposedException(Name);
            }

            continuations.Enqueue(continuation);
            Signal();
        }

        /// <summary>
        /// 在独占线程注册延迟续体。
        /// </summary>
        /// <param name="continuation">要执行的续体。</param>
        /// <param name="delay">非负延迟时间。</param>
        /// <returns>延迟派发句柄。</returns>
        public IMTaskScheduledHandle Schedule(Action continuation, TimeSpan delay)
        {
            ThrowIfDisposed();
            MScheduledContinuation scheduled = MScheduledContinuation.Rent(continuation, delay);
            lock (timerGate)
            {
                timers.Add(scheduled);
            }

            Signal();
            return scheduled;
        }

        /// <summary>
        /// 请求后台线程退出并最多等待三秒。
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
            {
                return;
            }

            MTaskExecutorRegistry.Unregister(this);
            bool isFastShutdown = MTaskRuntime.IsFastShutdown;
            if (isFastShutdown)
            {
                Volatile.Write(ref fastShutdown, 1);
            }

            Signal();
            if (isFastShutdown)
            {
                return;
            }

            bool canDisposeSignal = true;
            if (!IsCurrentThread)
            {
                canDisposeSignal = thread.Join(TimeSpan.FromSeconds(3));
                if (!canDisposeSignal)
                {
                    MTaskSupervisor.Report(
                        new TimeoutException($"独占执行器 {Name} 在 3 秒内未退出。"),
                        Name);
                }
            }

            if (canDisposeSignal)
            {
                DisposeSignal();
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 运行独占线程的事件循环。
        /// </summary>
        private void Run()
        {
            Volatile.Write(ref threadId, Thread.CurrentThread.ManagedThreadId);
            while (Volatile.Read(ref disposed) == 0)
            {
                DrainContinuations();
                if (Volatile.Read(ref disposed) != 0)
                {
                    break;
                }

                int waitMilliseconds = CollectDueTimers();
                if (waitMilliseconds == 0)
                {
                    continue;
                }

                signal.WaitOne(waitMilliseconds);
            }

            if (Volatile.Read(ref fastShutdown) == 0)
            {
                DrainContinuations();
            }
            else
            {
                continuations.Clear();
            }

            CancelTimers();
            if (Volatile.Read(ref fastShutdown) != 0)
            {
                DisposeSignal();
            }
        }

        /// <summary>
        /// 执行当前队列中的全部续体。
        /// </summary>
        private void DrainContinuations()
        {
            while (continuations.TryDequeue(out Action continuation))
            {
                try
                {
                    continuation();
                }
                catch (Exception exception)
                {
                    MTaskSupervisor.Report(exception, Name);
                }
            }
        }

        /// <summary>
        /// 派发到期计时器并计算下次唤醒间隔。
        /// </summary>
        /// <returns>零表示已有就绪续体，正数表示等待下一枚计时器，<see cref="Timeout.Infinite"/> 表示等待外部信号。</returns>
        private int CollectDueTimers()
        {
            long now = MTaskClock.Timestamp;
            long nearest = long.MaxValue;
            bool hasReadyContinuation = false;
            lock (timerGate)
            {
                for (int i = timers.Count - 1; i >= 0; i--)
                {
                    MScheduledContinuation scheduled = timers[i];
                    if (scheduled.IsCanceled || scheduled.DueTimestamp <= now)
                    {
                        timers.RemoveAt(i);
                        if (scheduled.TryTake(out Action continuation))
                        {
                            continuations.Enqueue(continuation);
                            hasReadyContinuation = true;
                        }

                        scheduled.Return();
                        continue;
                    }

                    if (scheduled.DueTimestamp < nearest)
                    {
                        nearest = scheduled.DueTimestamp;
                    }
                }
            }

            if (hasReadyContinuation)
            {
                return 0;
            }

            if (nearest == long.MaxValue)
            {
                return Timeout.Infinite;
            }

            return Math.Max(1, MTaskClock.ToMilliseconds(nearest - now));
        }

        /// <summary>
        /// 取消并回收尚未触发的全部计时器。
        /// </summary>
        private void CancelTimers()
        {
            lock (timerGate)
            {
                for (int i = 0; i < timers.Count; i++)
                {
                    timers[i].Cancel();
                    timers[i].Return();
                }

                timers.Clear();
            }
        }

        /// <summary>
        /// 安全唤醒独占线程；快速退出与线程退出并发时允许等待句柄已经释放。
        /// </summary>
        private void Signal()
        {
            if (Volatile.Read(ref signalDisposed) != 0)
            {
                return;
            }

            try
            {
                signal.Set();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        /// <summary>
        /// 只释放一次后台线程等待句柄。
        /// </summary>
        private void DisposeSignal()
        {
            if (Interlocked.Exchange(ref signalDisposed, 1) == 0)
            {
                signal.Dispose();
            }
        }

        /// <summary>
        /// 在执行器关闭后阻止继续派发任务。
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref disposed) != 0)
            {
                throw new ObjectDisposedException(Name);
            }
        }

        #endregion
    }
}
#endif
