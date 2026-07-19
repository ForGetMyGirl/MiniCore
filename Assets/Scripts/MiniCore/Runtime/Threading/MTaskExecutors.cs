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

    /// <summary>
    /// 延迟派发句柄。
    /// </summary>
    public interface IMTaskScheduledHandle
    {
        /// <summary>
        /// 尝试取消尚未执行的延迟回调。
        /// </summary>
        void Cancel();
    }

    /// <summary>
    /// MTask 内置执行器集合。
    /// </summary>
    public static class MTaskExecutors
    {
        #region Private 私有成员

        private static readonly MInlineExecutor InlineInstance = new MInlineExecutor(); // 无运行时环境时使用的同步执行器。
        private static readonly MThreadPoolExecutor ThreadPoolInstance = new MThreadPoolExecutor(); // 复用 CLR 线程池的无亲和性执行器。
        private static IMTaskExecutor unity; // Unity 主线程执行器。
        private static IMTaskExecutor network; // 网络专用执行器。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取同步执行的兜底执行器。
        /// </summary>
        public static IMTaskExecutor Inline => InlineInstance;

        /// <summary>
        /// 获取复用 CLR 线程池的后台执行器。
        /// </summary>
        /// <remarks>
        /// 此执行器不会创建固定线程，也不保证两次续体运行在同一条工作线程。需要串行线程亲和性时请创建独占执行器。
        /// </remarks>
        public static IMTaskExecutor ThreadPool => ThreadPoolInstance;

        /// <summary>
        /// 获取或设置 Unity 主线程执行器。
        /// </summary>
        public static IMTaskExecutor Unity
        {
            get => Volatile.Read(ref unity) ?? InlineInstance;
            set => Volatile.Write(ref unity, value ?? throw new ArgumentNullException(nameof(value)));
        }

        /// <summary>
        /// 获取或设置网络模块持有的专用执行器。
        /// </summary>
        /// <remarks>
        /// 该属性只保存网络模块已创建的执行器，不会创建线程。新模块请调用 <see cref="CreateDedicated"/> 并自行持有返回值。
        /// </remarks>
        public static IMTaskExecutor Network
        {
            get => Volatile.Read(ref network) ?? Unity;
            set => Volatile.Write(ref network, value ?? throw new ArgumentNullException(nameof(value)));
        }

        /// <summary>
        /// 创建由调用模块自行持有和释放的独占后台线程执行器。
        /// </summary>
        /// <param name="name">用于线程名称和诊断输出的稳定名称。</param>
        /// <returns>已经启动的独占执行器；调用方必须在所属模块释放时调用 <see cref="IDisposable.Dispose"/>。</returns>
        public static MDedicatedThreadExecutor CreateDedicated(string name)
        {
            return new MDedicatedThreadExecutor(name);
        }

        #endregion
    }

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

    /// <summary>
    /// 拥有独占后台线程的顺序 MTask 执行器。
    /// </summary>
    public sealed class MDedicatedThreadExecutor : IMTaskExecutor, IDisposable
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
        /// 创建并立即启动一个独占后台线程执行器。
        /// </summary>
        /// <param name="name">线程和执行器诊断名称。</param>
        public MDedicatedThreadExecutor(string name)
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
                int waitMilliseconds = CollectDueTimers();
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
        /// <returns>下次等待的毫秒数。</returns>
        private int CollectDueTimers()
        {
            long now = MTaskClock.Timestamp;
            long nearest = long.MaxValue;
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

            if (nearest == long.MaxValue)
            {
                return 1000;
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

    /// <summary>
    /// 将无状态短续体投递到 CLR 线程池的 MTask 执行器。
    /// </summary>
    public sealed class MThreadPoolExecutor : IMTaskExecutor
    {
        #region Private 私有成员

        private static readonly WaitCallback ExecuteWorkItem = Execute; // 避免每次投递创建 WaitCallback 委托。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取执行器诊断名称。
        /// </summary>
        public string Name => "ThreadPool";

        /// <summary>
        /// CLR 线程池没有固定线程归属，因此始终要求异步投递。
        /// </summary>
        public bool IsCurrentThread => false;

        /// <summary>
        /// 将续体投递到 CLR 线程池。
        /// </summary>
        /// <param name="continuation">需要在线程池工作线程执行的续体。</param>
        public void Post(Action continuation)
        {
            if (continuation == null)
            {
                throw new ArgumentNullException(nameof(continuation));
            }

            MThreadPoolWorkItem workItem = MThreadPoolWorkItem.Rent(continuation);
            ThreadPool.QueueUserWorkItem(ExecuteWorkItem, workItem);
        }

        /// <summary>
        /// 在延迟到期后将续体投递到 CLR 线程池。
        /// </summary>
        /// <param name="continuation">需要在线程池工作线程执行的续体。</param>
        /// <param name="delay">非负延迟时间。</param>
        /// <returns>可撤销延迟派发的句柄。</returns>
        public IMTaskScheduledHandle Schedule(Action continuation, TimeSpan delay)
        {
            if (continuation == null)
            {
                throw new ArgumentNullException(nameof(continuation));
            }

            return MThreadPoolScheduledHandle.Rent(this, continuation, delay);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 在线程池工作线程执行并回收工作项。
        /// </summary>
        /// <param name="state">池化的工作项。</param>
        private static void Execute(object state)
        {
            MThreadPoolWorkItem workItem = (MThreadPoolWorkItem)state;
            try
            {
                workItem.Invoke();
            }
            catch (Exception exception)
            {
                MTaskSupervisor.Report(exception, "ThreadPool");
            }
            finally
            {
                workItem.Return();
            }
        }

        #endregion
    }

    /// <summary>
    /// CLR 线程池执行器使用的可复用工作项。
    /// </summary>
    internal sealed class MThreadPoolWorkItem
    {
        #region Private 私有成员

        private Action continuation; // 当前工作项持有的续体。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 从共享池中获取工作项。
        /// </summary>
        /// <param name="value">需要在线程池执行的续体。</param>
        /// <returns>已绑定续体的工作项。</returns>
        internal static MThreadPoolWorkItem Rent(Action value)
        {
            if (!MTaskObjectPool<MThreadPoolWorkItem>.TryRent(out MThreadPoolWorkItem item))
            {
                item = new MThreadPoolWorkItem();
            }

            item.continuation = value;
            return item;
        }

        /// <summary>
        /// 调用当前工作项绑定的续体。
        /// </summary>
        internal void Invoke()
        {
            continuation?.Invoke();
        }

        /// <summary>
        /// 清理对业务续体的引用并回收到共享池。
        /// </summary>
        internal void Return()
        {
            continuation = null;
            MTaskObjectPool<MThreadPoolWorkItem>.Return(this);
        }

        #endregion
    }

    /// <summary>
    /// CLR 线程池执行器使用的一次性延迟派发句柄。
    /// </summary>
    internal sealed class MThreadPoolScheduledHandle : IMTaskScheduledHandle
    {
        #region Private 私有成员

        private static readonly TimerCallback TimerCallback = OnTimer; // 避免每次延迟任务创建计时器回调委托。

        private MThreadPoolExecutor executor; // 到期后负责投递续体的线程池执行器。
        private Action continuation; // 延迟到期后需要执行的续体。
        private Timer timer; // 一次性系统计时器。
        private int canceled; // 是否已经取消或执行完毕。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建延迟句柄并启动计时器。
        /// </summary>
        /// <param name="executor">到期后负责投递续体的执行器。</param>
        /// <param name="value">到期后需要执行的续体。</param>
        /// <param name="delay">非负延迟时间。</param>
        /// <returns>已启动的一次性延迟句柄。</returns>
        internal static MThreadPoolScheduledHandle Rent(MThreadPoolExecutor executor, Action value, TimeSpan delay)
        {
            MThreadPoolScheduledHandle handle = new MThreadPoolScheduledHandle();

            handle.executor = executor;
            handle.continuation = value;
            handle.canceled = 0;
            handle.timer = new Timer(TimerCallback, handle, delay < TimeSpan.Zero ? TimeSpan.Zero : delay, Timeout.InfiniteTimeSpan);
            return handle;
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 取消尚未到期的延迟派发。
        /// </summary>
        public void Cancel()
        {
            if (Interlocked.Exchange(ref canceled, 1) != 0)
            {
                return;
            }

            Release();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 处理计时器到期事件并将续体投递到 CLR 线程池。
        /// </summary>
        /// <param name="state">当前延迟句柄。</param>
        private static void OnTimer(object state)
        {
            MThreadPoolScheduledHandle handle = (MThreadPoolScheduledHandle)state;
            if (Interlocked.Exchange(ref handle.canceled, 1) == 0)
            {
                MThreadPoolExecutor target = handle.executor;
                Action callback = handle.continuation;
                handle.Release();
                target?.Post(callback);
            }
        }

        /// <summary>
        /// 释放计时器和业务引用。
        /// </summary>
        private void Release()
        {
            Interlocked.Exchange(ref timer, null)?.Dispose();
            executor = null;
            continuation = null;
        }

        #endregion
    }

    /// <summary>
    /// 使用池化链表节点的多生产者单消费者续体队列。
    /// </summary>
    internal sealed class MTaskWorkQueue
    {
        #region Private 私有成员

        private readonly object gate = new object(); // 保护多线程入队和消费线程出队。
        private MTaskWorkItem head; // 当前队首节点。
        private MTaskWorkItem tail; // 当前队尾节点。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 将续体放入队尾。
        /// </summary>
        /// <param name="continuation">待执行续体。</param>
        internal void Enqueue(Action continuation)
        {
            if (continuation == null)
            {
                throw new ArgumentNullException(nameof(continuation));
            }

            MTaskWorkItem item = MTaskWorkItem.Rent(continuation);
            lock (gate)
            {
                if (tail == null)
                {
                    head = item;
                    tail = item;
                }
                else
                {
                    tail.Next = item;
                    tail = item;
                }
            }
        }

        /// <summary>
        /// 尝试从队首取出一个续体。
        /// </summary>
        /// <param name="continuation">成功取出的续体。</param>
        /// <returns>队列非空时返回 true。</returns>
        internal bool TryDequeue(out Action continuation)
        {
            MTaskWorkItem item;
            lock (gate)
            {
                item = head;
                if (item == null)
                {
                    continuation = null;
                    return false;
                }

                head = item.Next;
                if (head == null)
                {
                    tail = null;
                }

                item.Next = null;
            }

            continuation = item.TakeContinuation();
            MTaskObjectPool<MTaskWorkItem>.Return(item);
            return true;
        }

        /// <summary>
        /// 丢弃当前队列中的全部续体并回收工作节点。
        /// </summary>
        internal void Clear()
        {
            while (TryDequeue(out _))
            {
            }
        }

        #endregion
    }

    /// <summary>
    /// MTaskWorkQueue 使用的可复用链表节点。
    /// </summary>
    internal sealed class MTaskWorkItem
    {
        #region Private 私有成员

        private Action continuation; // 当前节点携带的续体。

        #endregion

        #region Internal 内部成员

        internal MTaskWorkItem Next; // 队列中的下一个节点。

        /// <summary>
        /// 从共享池中获取工作节点并绑定续体。
        /// </summary>
        /// <param name="value">待执行续体。</param>
        /// <returns>初始化后的工作节点。</returns>
        internal static MTaskWorkItem Rent(Action value)
        {
            if (!MTaskObjectPool<MTaskWorkItem>.TryRent(out MTaskWorkItem item))
            {
                item = new MTaskWorkItem();
            }

            item.continuation = value;
            item.Next = null;
            return item;
        }

        /// <summary>
        /// 取出续体并清除节点对业务对象的引用。
        /// </summary>
        /// <returns>节点中的续体。</returns>
        internal Action TakeContinuation()
        {
            Action value = continuation;
            continuation = null;
            return value;
        }

        #endregion
    }

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
            return new MTimerScheduledHandle(continuation, delay);
        }

        #endregion
    }

    /// <summary>
    /// 同步兜底执行器使用的一次性系统计时器句柄。
    /// </summary>
    internal sealed class MTimerScheduledHandle : IMTaskScheduledHandle
    {
        #region Private 私有成员

        private Timer timer; // 兜底环境的一次性计时器。
        private Action continuation; // 到期后执行的回调。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建一次性计时器。
        /// </summary>
        /// <param name="continuation">到期回调。</param>
        /// <param name="delay">延迟时间。</param>
        internal MTimerScheduledHandle(Action continuation, TimeSpan delay)
        {
            this.continuation = continuation ?? throw new ArgumentNullException(nameof(continuation));
            timer = new Timer(OnTimer, null, delay < TimeSpan.Zero ? TimeSpan.Zero : delay, Timeout.InfiniteTimeSpan);
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 取消一次性计时器。
        /// </summary>
        public void Cancel()
        {
            Interlocked.Exchange(ref continuation, null);
            Interlocked.Exchange(ref timer, null)?.Dispose();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 执行计时器回调并释放底层资源。
        /// </summary>
        /// <param name="state">未使用的计时器状态。</param>
        private void OnTimer(object state)
        {
            Action callback = Interlocked.Exchange(ref continuation, null);
            Interlocked.Exchange(ref timer, null)?.Dispose();
            callback?.Invoke();
        }

        #endregion
    }

    /// <summary>
    /// 可池化的延迟续体记录。
    /// </summary>
    internal sealed class MScheduledContinuation : IMTaskScheduledHandle
    {
        #region Private 私有成员

        private Action continuation; // 到期后执行的续体。
        private int canceled; // 是否已经取消。

        #endregion

        #region Internal 内部成员

        internal long DueTimestamp; // 到期时间戳。

        /// <summary>
        /// 从池中获取一条延迟记录。
        /// </summary>
        /// <param name="continuation">到期后执行的续体。</param>
        /// <param name="delay">延迟时间。</param>
        /// <returns>初始化后的延迟记录。</returns>
        internal static MScheduledContinuation Rent(Action continuation, TimeSpan delay)
        {
            if (!MTaskObjectPool<MScheduledContinuation>.TryRent(out MScheduledContinuation scheduled))
            {
                scheduled = new MScheduledContinuation();
            }

            scheduled.continuation = continuation ?? throw new ArgumentNullException(nameof(continuation));
            scheduled.canceled = 0;
            scheduled.DueTimestamp = MTaskClock.Timestamp + MTaskClock.FromTimeSpan(delay < TimeSpan.Zero ? TimeSpan.Zero : delay);
            MTaskDiagnostics.OnTimerActivated();
            return scheduled;
        }

        /// <summary>
        /// 获取延迟项是否已经取消。
        /// </summary>
        internal bool IsCanceled => Volatile.Read(ref canceled) != 0;

        /// <summary>
        /// 尝试取得尚未取消的续体。
        /// </summary>
        /// <param name="callback">成功取得的回调。</param>
        /// <returns>存在可执行回调时返回 true。</returns>
        internal bool TryTake(out Action callback)
        {
            if (IsCanceled)
            {
                callback = null;
                return false;
            }

            callback = Interlocked.Exchange(ref continuation, null);
            return callback != null;
        }

        /// <summary>
        /// 清理状态并归还共享池。
        /// </summary>
        internal void Return()
        {
            continuation = null;
            DueTimestamp = 0;
            MTaskDiagnostics.OnTimerCompleted();
            MTaskObjectPool<MScheduledContinuation>.Return(this);
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 取消尚未触发的延迟项。
        /// </summary>
        public void Cancel()
        {
            Interlocked.Exchange(ref canceled, 1);
            Interlocked.Exchange(ref continuation, null);
        }

        #endregion
    }

    /// <summary>
    /// 将 TimeSpan 转换为 Stopwatch 单调时钟刻度。
    /// </summary>
    internal static class MTaskClock
    {
        #region Internal 内部成员

        /// <summary>
        /// 获取当前单调时钟刻度。
        /// </summary>
        internal static long Timestamp => Stopwatch.GetTimestamp();

        /// <summary>
        /// 将时间间隔转换为单调时钟刻度。
        /// </summary>
        /// <param name="timeSpan">要转换的时间间隔。</param>
        /// <returns>对应的 Stopwatch 刻度。</returns>
        internal static long FromTimeSpan(TimeSpan timeSpan)
        {
            return (long)(timeSpan.TotalSeconds * Stopwatch.Frequency);
        }

        /// <summary>
        /// 将 Stopwatch 刻度转换为毫秒。
        /// </summary>
        /// <param name="ticks">Stopwatch 刻度。</param>
        /// <returns>向上取整后的毫秒数。</returns>
        internal static int ToMilliseconds(long ticks)
        {
            double milliseconds = ticks * 1000d / Stopwatch.Frequency;
            return milliseconds >= int.MaxValue ? int.MaxValue : (int)Math.Ceiling(milliseconds);
        }

        #endregion
    }
}
