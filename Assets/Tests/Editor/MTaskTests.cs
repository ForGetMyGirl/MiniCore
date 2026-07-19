using System;
using System.Diagnostics;
using System.Threading;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Threading;
using NUnit.Framework;

namespace MiniCore.EditorTests
{
    /// <summary>
    /// 验证 MTask 的单次消费、结构化取消、共享与执行器切换。
    /// </summary>
    public sealed class MTaskTests
    {
        #region Private 私有成员

        private MTaskMainThreadExecutor mainExecutor; // 测试线程主动抽取的执行器。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 为每个用例创建独立的 MTask 应用域。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            mainExecutor = new MTaskMainThreadExecutor("MTask.Tests");
            MTaskExecutors.Unity = mainExecutor;
            MTaskRuntime.Initialize(mainExecutor);
        }

        /// <summary>
        /// 关闭用例应用域，避免任务状态泄漏到后续用例。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            MTaskRuntime.Shutdown();
        }

        /// <summary>
        /// 验证自定义 Builder 能够在 Yield 后恢复并返回结果。
        /// </summary>
        [Test]
        public void AsyncBuilder_Yield_ReturnsResult()
        {
            int result = Run(YieldAndReturnAsync(42));

            Assert.AreEqual(42, result);
        }

        /// <summary>
        /// 验证普通 MTask 被第二次消费时给出明确异常。
        /// </summary>
        [Test]
        public void MTask_SecondConsumption_Throws()
        {
            MTask task = YieldOnceAsync();
            Run(task);

            Assert.Throws<InvalidOperationException>(() => Run(task));
        }

        /// <summary>
        /// 验证 Share 只消费一次底层任务并向多个等待者广播同一结果。
        /// </summary>
        [Test]
        public void SharedTask_MultipleConsumers_ReceiveSameResult()
        {
            MTaskCompletionSource<int> completion = new MTaskCompletionSource<int>();
            MSharedTask<int> shared = AwaitCompletionAsync(completion).Share();
            MTask<int> first = AwaitSharedAsync(shared);
            MTask<int> second = AwaitSharedAsync(shared);

            completion.TrySetResult(17);

            Assert.AreEqual(17, Run(first));
            Assert.AreEqual(17, Run(second));
            Assert.AreEqual(17, Run(AwaitSharedAsync(shared)));
        }

        /// <summary>
        /// 验证父方法结束时会取消未 Forget 的子任务并等待 finally 退场。
        /// </summary>
        [Test]
        public void ParentCompletion_UnawaitedChild_RunsFinallyBeforeParentCompletes()
        {
            MTaskCompletionSource<bool> completion = new MTaskCompletionSource<bool>();
            bool finallyRan = false;

            Run(StartUnawaitedChildAsync(completion, () => finallyRan = true));

            Assert.IsTrue(finallyRan);
        }

        /// <summary>
        /// 验证未 await 且未 Forget 的子任务异常会使父任务失败并取消兄弟。
        /// </summary>
        [Test]
        public void UnobservedChildFault_FailsParent_AndCancelsSibling()
        {
            MTaskCompletionSource<bool> siblingSignal = new MTaskCompletionSource<bool>();
            bool siblingFinally = false;

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => Run(StartFaultAndSiblingAsync(siblingSignal, () => siblingFinally = true)));

            Assert.AreEqual("child fault", exception.Message);
            Assert.IsTrue(siblingFinally);
        }

        /// <summary>
        /// 验证 Forget 任务从方法父节点转移到最近 Owner，不会被父方法结束取消。
        /// </summary>
        [Test]
        public void Forget_DetachesFromMethod_RemainsOwnedByComponent()
        {
            TestOwnerComponent owner = new TestOwnerComponent();
            MTaskCompletionSource<bool> completion = new MTaskCompletionSource<bool>();
            bool finished = false;
            using (MTaskRuntime.EnterOwner(owner))
            {
                Run(StartForgottenChildAsync(completion, () => finished = true));
            }

            Assert.IsFalse(finished);
            completion.TrySetResult(true);
            DrainUntil(() => finished);
            Assert.IsTrue(finished);

            owner.Dispose();
            DrainUntil(() => owner.IsDisposed);
        }

        /// <summary>
        /// 验证 AComponent 释放会取消它名下的全部任务，并在 finally 完成后才执行 OnDispose。
        /// </summary>
        [Test]
        public void ComponentDispose_CancelsDomain_ThenFinalizes()
        {
            TestOwnerComponent owner = new TestOwnerComponent();
            MTask pending;
            using (MTaskRuntime.EnterOwner(owner))
            {
                pending = owner.WaitForeverAsync();
            }

            owner.Dispose();
            DrainUntil(() => owner.IsDisposed);

            Assert.IsTrue(owner.FinallyRan);
            Assert.IsTrue(owner.OnDisposeRan);
            Assert.Throws<MTaskCanceledException>(() => Run(pending));
        }

        /// <summary>
        /// 验证 Global 在旧组件的异步 finally 退场前保留墓碑，不会创建同类型替代实例。
        /// </summary>
        [Test]
        public void GlobalDisposal_AsyncFinally_BlocksReplacementUntilDrained()
        {
            object owner = new object();
            Global.Shutdown();
            Global.Initialize();
            AsyncDisposalComponent component = Global.GetOrAdd<AsyncDisposalComponent>(owner);

            Global.Remove<AsyncDisposalComponent>(owner);

            Assert.IsTrue(component.IsDisposing);
            Assert.Throws<InvalidOperationException>(() => Global.GetOrAdd<AsyncDisposalComponent>(owner));
            DrainUntil(() => component.IsDisposed);
            AsyncDisposalComponent replacement = Global.GetOrAdd<AsyncDisposalComponent>(owner);
            Assert.AreNotSame(component, replacement);

            Global.Remove<AsyncDisposalComponent>(owner);
            DrainUntil(() => replacement.IsDisposed);
            Global.Shutdown();
        }

        /// <summary>
        /// 验证任务可从 Unity 执行器切到独占线程并再回到原主线程。
        /// </summary>
        [Test]
        public void SwitchTo_DedicatedAndMain_UsesExpectedThreads()
        {
            int mainThreadId = Thread.CurrentThread.ManagedThreadId;
            using MDedicatedThreadExecutor worker = MTaskExecutors.CreateDedicated("MTask.Tests.Worker");

            (int workerThreadId, int resumedThreadId) = Run(SwitchExecutorsAsync(worker, mainExecutor));

            Assert.AreNotEqual(mainThreadId, workerThreadId);
            Assert.AreEqual(mainThreadId, resumedThreadId);
        }

        /// <summary>
        /// 验证线程池执行器不会创建固定线程，且能够切回主执行器。
        /// </summary>
        [Test]
        public void SwitchTo_ThreadPoolAndMain_UsesExpectedThreads()
        {
            int mainThreadId = Thread.CurrentThread.ManagedThreadId;

            (int workerThreadId, int resumedThreadId) = Run(SwitchExecutorsAsync(MTaskExecutors.ThreadPool, mainExecutor));

            Assert.AreNotEqual(mainThreadId, workerThreadId);
            Assert.AreEqual(mainThreadId, resumedThreadId);
        }

        /// <summary>
        /// 验证不同模块持有的独占执行器不会共享同一条工作线程。
        /// </summary>
        [Test]
        public void CreateDedicated_MultipleExecutors_UseIndependentThreads()
        {
            using MDedicatedThreadExecutor first = MTaskExecutors.CreateDedicated("MTask.Tests.First");
            using MDedicatedThreadExecutor second = MTaskExecutors.CreateDedicated("MTask.Tests.Second");

            (int firstThreadId, int secondThreadId) = Run(SwitchBetweenDedicatedExecutorsAsync(first, second));

            Assert.AreNotEqual(firstThreadId, secondThreadId);
        }

        /// <summary>
        /// 验证组件同步停机钩子在任务域取消前执行。
        /// </summary>
        [Test]
        public void ComponentDispose_OnDisposing_RunsBeforeDomainCancellation()
        {
            DisposingOrderComponent component = new DisposingOrderComponent();
            using (MTaskRuntime.EnterOwner(component))
            {
                component.WaitForeverAsync().Forget();
            }

            component.Dispose();
            DrainUntil(() => component.IsDisposed);

            Assert.IsTrue(component.OnDisposingRan);
            Assert.IsFalse(component.DomainWasCanceledDuringOnDisposing);
        }

        /// <summary>
        /// 验证快速退出不会等待仍在执行的独占线程工作。
        /// </summary>
        [Test]
        public void FastShutdown_DedicatedExecutor_DoesNotJoinWorker()
        {
            using ManualResetEventSlim started = new ManualResetEventSlim(false);
            using ManualResetEventSlim finished = new ManualResetEventSlim(false);
            MDedicatedThreadExecutor worker = MTaskExecutors.CreateDedicated("MTask.Tests.FastShutdown");
            worker.Post(() =>
            {
                started.Set();
                try
                {
                    Thread.Sleep(250);
                }
                finally
                {
                    finished.Set();
                }
            });

            Assert.IsTrue(started.Wait(TimeSpan.FromSeconds(1)));
            MTaskRuntime.BeginFastShutdown();
            Stopwatch stopwatch = Stopwatch.StartNew();
            worker.Dispose();

            Assert.Less(stopwatch.Elapsed, TimeSpan.FromMilliseconds(100));
            Assert.IsTrue(finished.Wait(TimeSpan.FromSeconds(1)));
        }

        /// <summary>
        /// 验证快速退出后创建的根 MTask 会直接以取消状态结束。
        /// </summary>
        [Test]
        public void FastShutdown_NewRootTask_IsCanceled()
        {
            MTaskRuntime.BeginFastShutdown();

            MTask task = YieldOnceAsync();

            Assert.Throws<MTaskCanceledException>(() => Run(task));
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 在主执行器上抽取一个无返回值 MTask 直至完成。
        /// </summary>
        /// <param name="task">待完成任务。</param>
        private void Run(MTask task)
        {
            MTaskAwaiter awaiter = task.GetAwaiter();
            bool completed = awaiter.IsCompleted;
            if (!completed)
            {
                awaiter.UnsafeOnCompleted(() => completed = true);
                DrainUntil(() => completed);
            }

            awaiter.GetResult();
        }

        /// <summary>
        /// 在主执行器上抽取一个带返回值 MTask 直至完成。
        /// </summary>
        /// <typeparam name="T">任务返回值类型。</typeparam>
        /// <param name="task">待完成任务。</param>
        /// <returns>任务最终结果。</returns>
        private T Run<T>(MTask<T> task)
        {
            MTaskAwaiter<T> awaiter = task.GetAwaiter();
            bool completed = awaiter.IsCompleted;
            if (!completed)
            {
                awaiter.UnsafeOnCompleted(() => completed = true);
                DrainUntil(() => completed);
            }

            return awaiter.GetResult();
        }

        /// <summary>
        /// 持续抽取主执行器，直到条件成立或超时。
        /// </summary>
        /// <param name="condition">完成条件。</param>
        private void DrainUntil(Func<bool> condition)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (!condition())
            {
                mainExecutor.Drain();
                if (stopwatch.Elapsed > TimeSpan.FromSeconds(3))
                {
                    Assert.Fail("MTask 测试在 3 秒内未完成。");
                }

                Thread.Yield();
            }

            mainExecutor.Drain();
        }

        /// <summary>
        /// 让出一次执行器后返回指定数值。
        /// </summary>
        /// <param name="value">返回数值。</param>
        /// <returns>指定数值。</returns>
        private static async MTask<int> YieldAndReturnAsync(int value)
        {
            await MTask.Yield();
            return value;
        }

        /// <summary>
        /// 让出一次执行器后完成。
        /// </summary>
        /// <returns>让出后完成的任务。</returns>
        private static async MTask YieldOnceAsync()
        {
            await MTask.Yield();
        }

        /// <summary>
        /// 等待手动完成源并返回其结果。
        /// </summary>
        /// <param name="completion">手动完成源。</param>
        /// <returns>手动完成结果。</returns>
        private static async MTask<int> AwaitCompletionAsync(MTaskCompletionSource<int> completion)
        {
            return await completion.Task;
        }

        /// <summary>
        /// 等待带整数结果的共享任务。
        /// </summary>
        /// <param name="shared">共享任务。</param>
        /// <returns>共享结果。</returns>
        private static async MTask<int> AwaitSharedAsync(MSharedTask<int> shared)
        {
            return await shared;
        }

        /// <summary>
        /// 启动一个不等待的子任务并立即结束父方法。
        /// </summary>
        /// <param name="completion">子任务等待的手动完成源。</param>
        /// <param name="finallyAction">子任务退场标记。</param>
        /// <returns>会等待子任务退场的父任务。</returns>
        private static async MTask StartUnawaitedChildAsync(MTaskCompletionSource<bool> completion, Action finallyAction)
        {
            WaitWithFinallyAsync(completion, finallyAction);
            await MTask.Yield();
        }

        /// <summary>
        /// 启动一个转移到 Owner 的后台任务。
        /// </summary>
        /// <param name="completion">后台任务等待的手动完成源。</param>
        /// <param name="completedAction">后台任务完成标记。</param>
        /// <returns>启动后台任务的父任务。</returns>
        private static async MTask StartForgottenChildAsync(MTaskCompletionSource<bool> completion, Action completedAction)
        {
            CompleteAfterSignalAsync(completion, completedAction).Forget();
            await MTask.Yield();
        }

        /// <summary>
        /// 启动一个将要失败的子任务和一个挂起兄弟任务。
        /// </summary>
        /// <param name="siblingSignal">兄弟任务等待的信号。</param>
        /// <param name="siblingFinally">兄弟任务退场标记。</param>
        /// <returns>应被未观察子异常标记失败的父任务。</returns>
        private static async MTask StartFaultAndSiblingAsync(
            MTaskCompletionSource<bool> siblingSignal,
            Action siblingFinally)
        {
            FaultAfterYieldAsync();
            WaitWithFinallyAsync(siblingSignal, siblingFinally);
            await MTask.Yield();
        }

        /// <summary>
        /// 让出一次执行器后以固定异常失败。
        /// </summary>
        /// <returns>必然失败的子任务。</returns>
        private static async MTask FaultAfterYieldAsync()
        {
            await MTask.Yield();
            throw new InvalidOperationException("child fault");
        }

        /// <summary>
        /// 等待手动信号并在退场时执行 finally。
        /// </summary>
        /// <param name="completion">手动信号。</param>
        /// <param name="finallyAction">退场标记。</param>
        /// <returns>可取消的等待任务。</returns>
        private static async MTask WaitWithFinallyAsync(MTaskCompletionSource<bool> completion, Action finallyAction)
        {
            try
            {
                await completion.Task;
            }
            finally
            {
                finallyAction();
            }
        }

        /// <summary>
        /// 等待信号后标记后台任务完成。
        /// </summary>
        /// <param name="completion">手动信号。</param>
        /// <param name="completedAction">完成标记。</param>
        /// <returns>后台任务。</returns>
        private static async MTask CompleteAfterSignalAsync(MTaskCompletionSource<bool> completion, Action completedAction)
        {
            await completion.Task;
            completedAction();
        }

        /// <summary>
        /// 依次切换到独占执行器和主执行器并返回实际线程。
        /// </summary>
        /// <param name="worker">独占线程执行器。</param>
        /// <param name="main">主线程执行器。</param>
        /// <returns>两个执行阶段的线程标识。</returns>
        private static async MTask<(int workerThreadId, int resumedThreadId)> SwitchExecutorsAsync(
            IMTaskExecutor worker,
            IMTaskExecutor main)
        {
            await MTask.SwitchTo(worker);
            int workerThreadId = Thread.CurrentThread.ManagedThreadId;
            await MTask.SwitchTo(main);
            return (workerThreadId, Thread.CurrentThread.ManagedThreadId);
        }

        /// <summary>
        /// 依次切换到两个独占执行器并返回它们各自的实际线程标识。
        /// </summary>
        /// <param name="first">第一个独占执行器。</param>
        /// <param name="second">第二个独占执行器。</param>
        /// <returns>两个独占执行器各自的工作线程标识。</returns>
        private static async MTask<(int firstThreadId, int secondThreadId)> SwitchBetweenDedicatedExecutorsAsync(
            IMTaskExecutor first,
            IMTaskExecutor second)
        {
            await MTask.SwitchTo(first);
            int firstThreadId = Thread.CurrentThread.ManagedThreadId;
            await MTask.SwitchTo(second);
            return (firstThreadId, Thread.CurrentThread.ManagedThreadId);
        }

        #endregion

        /// <summary>
        /// 用于验证 AComponent 两阶段释放的 MTask Owner。
        /// </summary>
        private sealed class TestOwnerComponent : AComponent
        {
            #region Public 公共成员

            /// <summary>
            /// 获取挂起任务的 finally 是否已经运行。
            /// </summary>
            public bool FinallyRan { get; private set; }

            /// <summary>
            /// 获取组件最终释放钩子是否已经运行。
            /// </summary>
            public bool OnDisposeRan { get; private set; }

            /// <summary>
            /// 启动一个只能通过 Owner 取消的挂起任务。
            /// </summary>
            /// <returns>持续到 Owner 释放的任务。</returns>
            public async MTask WaitForeverAsync()
            {
                try
                {
                    await MTask.Delay(TimeSpan.FromHours(1));
                }
                finally
                {
                    FinallyRan = true;
                }
            }

            #endregion

            #region Protected 受保护成员

            /// <summary>
            /// 记录组件已在任务退场后执行最终清理。
            /// </summary>
            protected override void OnDispose()
            {
                OnDisposeRan = true;
            }

            #endregion
        }

        /// <summary>
        /// 在取消退场时额外 Yield 一次，用于验证 Global 释放墓碑。
        /// </summary>
        private sealed class AsyncDisposalComponent : AComponent
        {
            #region Public 公共成员

            /// <summary>
            /// 启动与组件任务域绑定的长时间挂起任务。
            /// </summary>
            public override void Awake()
            {
                WaitForDisposeAsync().Forget();
            }

            #endregion

            #region Private 私有成员

            /// <summary>
            /// 在组件取消时让出一次执行器，人为保留两阶段释放窗口。
            /// </summary>
            /// <returns>组件释放时退场的任务。</returns>
            private static async MTask WaitForDisposeAsync()
            {
                try
                {
                    await MTask.Delay(TimeSpan.FromHours(1));
                }
                finally
                {
                    await MTask.Yield();
                }
            }

            #endregion
        }

        /// <summary>
        /// 用于验证同步停机阶段和任务域取消顺序的组件。
        /// </summary>
        private sealed class DisposingOrderComponent : AComponent
        {
            #region Public 公共成员

            /// <summary>
            /// 获取同步停机钩子是否已经执行。
            /// </summary>
            public bool OnDisposingRan { get; private set; }

            /// <summary>
            /// 获取同步停机钩子执行时任务域是否已经取消。
            /// </summary>
            public bool DomainWasCanceledDuringOnDisposing { get; private set; }

            /// <summary>
            /// 启动一个等待组件域取消的挂起任务。
            /// </summary>
            /// <returns>组件释放时结束的任务。</returns>
            public async MTask WaitForeverAsync()
            {
                await MTask.Delay(TimeSpan.FromHours(1));
            }

            #endregion

            #region Protected 受保护成员

            /// <summary>
            /// 记录任务域取消前的同步停机状态。
            /// </summary>
            protected override void OnDisposing()
            {
                OnDisposingRan = true;
                DomainWasCanceledDuringOnDisposing = GetMTaskDomain().IsCancellationRequested;
            }

            #endregion
        }
    }
}
