using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// MTask 异步结果状态。
    /// </summary>
    public enum MTaskStatus : byte
    {
        /// <summary>
        /// 任务仍在运行。
        /// </summary>
        Pending = 0,

        /// <summary>
        /// 任务成功完成。
        /// </summary>
        Succeeded = 1,

        /// <summary>
        /// 任务因异常失败。
        /// </summary>
        Faulted = 2,

        /// <summary>
        /// 任务被协作式取消。
        /// </summary>
        Canceled = 3
    }

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

    /// <summary>
    /// 带返回值 MTask 的底层结果源契约。
    /// </summary>
    /// <typeparam name="T">任务返回值类型。</typeparam>
    public interface IMTaskSource<T>
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
        /// <returns>任务返回值。</returns>
        T GetResult(short token);
    }

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

    /// <summary>
    /// 无返回值 MTask 的自定义异步方法构建器。
    /// </summary>
    public struct MTaskMethodBuilder
    {
        #region Private 私有成员

        private MTaskPromise promise; // 状态机共享的池化结果源。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取当前异步方法返回的 MTask。
        /// </summary>
        public MTask Task => new MTask(promise, promise.Version);

        /// <summary>
        /// 创建异步方法构建器并建立结构化任务节点。
        /// </summary>
        /// <returns>初始化后的构建器。</returns>
        public static MTaskMethodBuilder Create()
        {
            return new MTaskMethodBuilder
            {
                promise = MTaskPromise.Rent()
            };
        }

        /// <summary>
        /// 启动编译器生成的异步状态机。
        /// </summary>
        /// <typeparam name="TStateMachine">状态机类型。</typeparam>
        /// <param name="stateMachine">状态机实例。</param>
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            promise.Start(ref stateMachine);
        }

        /// <summary>
        /// 通知构建器异步方法已经成功结束。
        /// </summary>
        public void SetResult()
        {
            promise.SetResult();
        }

        /// <summary>
        /// 通知构建器异步方法因异常结束。
        /// </summary>
        /// <param name="exception">状态机抛出的异常。</param>
        public void SetException(Exception exception)
        {
            promise.SetException(exception);
        }

        /// <summary>
        /// 注册实现安全完成通知的 awaiter。
        /// </summary>
        /// <typeparam name="TAwaiter">awaiter 类型。</typeparam>
        /// <typeparam name="TStateMachine">状态机类型。</typeparam>
        /// <param name="awaiter">当前等待器。</param>
        /// <param name="stateMachine">当前状态机。</param>
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(promise.GetStateMachineContinuation(ref stateMachine));
        }

        /// <summary>
        /// 注册不捕获 ExecutionContext 的 awaiter。
        /// </summary>
        /// <typeparam name="TAwaiter">awaiter 类型。</typeparam>
        /// <typeparam name="TStateMachine">状态机类型。</typeparam>
        /// <param name="awaiter">当前等待器。</param>
        /// <param name="stateMachine">当前状态机。</param>
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(promise.GetStateMachineContinuation(ref stateMachine));
        }

        /// <summary>
        /// 兼容 IAsyncStateMachine 的显式设置入口。
        /// </summary>
        /// <param name="stateMachine">编译器生成的状态机。</param>
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }

        #endregion
    }

    /// <summary>
    /// 带返回值 MTask 的自定义异步方法构建器。
    /// </summary>
    /// <typeparam name="T">异步方法返回值类型。</typeparam>
    public struct MTaskMethodBuilder<T>
    {
        #region Private 私有成员

        private MTaskPromise<T> promise; // 状态机共享的池化结果源。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取当前异步方法返回的 MTask。
        /// </summary>
        public MTask<T> Task => new MTask<T>(promise, promise.Version);

        /// <summary>
        /// 创建异步方法构建器并建立结构化任务节点。
        /// </summary>
        /// <returns>初始化后的构建器。</returns>
        public static MTaskMethodBuilder<T> Create()
        {
            return new MTaskMethodBuilder<T>
            {
                promise = MTaskPromise<T>.Rent()
            };
        }

        /// <summary>
        /// 启动编译器生成的异步状态机。
        /// </summary>
        /// <typeparam name="TStateMachine">状态机类型。</typeparam>
        /// <param name="stateMachine">状态机实例。</param>
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            promise.Start(ref stateMachine);
        }

        /// <summary>
        /// 通知构建器异步方法已经成功结束。
        /// </summary>
        /// <param name="result">异步方法返回值。</param>
        public void SetResult(T result)
        {
            promise.SetResult(result);
        }

        /// <summary>
        /// 通知构建器异步方法因异常结束。
        /// </summary>
        /// <param name="exception">状态机抛出的异常。</param>
        public void SetException(Exception exception)
        {
            promise.SetException(exception);
        }

        /// <summary>
        /// 注册实现安全完成通知的 awaiter。
        /// </summary>
        /// <typeparam name="TAwaiter">awaiter 类型。</typeparam>
        /// <typeparam name="TStateMachine">状态机类型。</typeparam>
        /// <param name="awaiter">当前等待器。</param>
        /// <param name="stateMachine">当前状态机。</param>
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(promise.GetStateMachineContinuation(ref stateMachine));
        }

        /// <summary>
        /// 注册不捕获 ExecutionContext 的 awaiter。
        /// </summary>
        /// <typeparam name="TAwaiter">awaiter 类型。</typeparam>
        /// <typeparam name="TStateMachine">状态机类型。</typeparam>
        /// <param name="awaiter">当前等待器。</param>
        /// <param name="stateMachine">当前状态机。</param>
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(promise.GetStateMachineContinuation(ref stateMachine));
        }

        /// <summary>
        /// 兼容 IAsyncStateMachine 的显式设置入口。
        /// </summary>
        /// <param name="stateMachine">编译器生成的状态机。</param>
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }

        #endregion
    }

    /// <summary>
    /// MTask.Yield 返回的无分配等待对象。
    /// </summary>
    public readonly struct MTaskYieldAwaitable
    {
        #region Private 私有成员

        private readonly IMTaskExecutor executor; // 当前任务的续体执行器。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建让出等待对象。
        /// </summary>
        /// <param name="executor">续体执行器。</param>
        internal MTaskYieldAwaitable(IMTaskExecutor executor)
        {
            this.executor = executor;
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取让出操作 awaiter。
        /// </summary>
        /// <returns>让出操作 awaiter。</returns>
        public MTaskYieldAwaiter GetAwaiter()
        {
            return new MTaskYieldAwaiter(executor);
        }

        #endregion
    }

    /// <summary>
    /// MTask.Yield 的 awaiter。
    /// </summary>
    public readonly struct MTaskYieldAwaiter : ICriticalNotifyCompletion
    {
        #region Private 私有成员

        private readonly IMTaskExecutor executor; // 续体执行器。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取让出操作始终需要异步调度。
        /// </summary>
        public bool IsCompleted => false;

        /// <summary>
        /// 创建让出操作 awaiter。
        /// </summary>
        /// <param name="executor">续体执行器。</param>
        public MTaskYieldAwaiter(IMTaskExecutor executor)
        {
            this.executor = executor;
        }

        /// <summary>
        /// 将续体放到执行器队列尾部。
        /// </summary>
        /// <param name="continuation">待执行续体。</param>
        public void OnCompleted(Action continuation)
        {
            executor.Post(continuation);
        }

        /// <summary>
        /// 将续体放到执行器队列尾部且不捕获 ExecutionContext。
        /// </summary>
        /// <param name="continuation">待执行续体。</param>
        public void UnsafeOnCompleted(Action continuation)
        {
            executor.Post(continuation);
        }

        /// <summary>
        /// 在恢复时检查当前任务取消状态。
        /// </summary>
        public void GetResult()
        {
            MTask.ThrowIfCancellationRequested();
        }

        #endregion
    }

    /// <summary>
    /// MTask.SwitchTo 返回的无分配等待对象。
    /// </summary>
    public readonly struct MTaskSwitchAwaitable
    {
        #region Private 私有成员

        private readonly IMTaskExecutor executor; // 目标执行器。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建执行器切换等待对象。
        /// </summary>
        /// <param name="executor">目标执行器。</param>
        internal MTaskSwitchAwaitable(IMTaskExecutor executor)
        {
            this.executor = executor;
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取执行器切换 awaiter。
        /// </summary>
        /// <returns>执行器切换 awaiter。</returns>
        public MTaskSwitchAwaiter GetAwaiter()
        {
            return new MTaskSwitchAwaiter(executor);
        }

        #endregion
    }

    /// <summary>
    /// MTask.SwitchTo 的 awaiter。
    /// </summary>
    public readonly struct MTaskSwitchAwaiter : ICriticalNotifyCompletion
    {
        #region Private 私有成员

        private readonly IMTaskExecutor executor; // 目标执行器。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取当前是否已经处于目标执行器线程。
        /// </summary>
        public bool IsCompleted => executor.IsCurrentThread;

        /// <summary>
        /// 创建执行器切换 awaiter。
        /// </summary>
        /// <param name="executor">目标执行器。</param>
        public MTaskSwitchAwaiter(IMTaskExecutor executor)
        {
            this.executor = executor;
        }

        /// <summary>
        /// 将续体派发到目标执行器。
        /// </summary>
        /// <param name="continuation">待执行续体。</param>
        public void OnCompleted(Action continuation)
        {
            MTaskRuntime.SwitchCurrentExecutor(executor);
            executor.Post(continuation);
        }

        /// <summary>
        /// 将续体派发到目标执行器且不捕获 ExecutionContext。
        /// </summary>
        /// <param name="continuation">待执行续体。</param>
        public void UnsafeOnCompleted(Action continuation)
        {
            MTaskRuntime.SwitchCurrentExecutor(executor);
            executor.Post(continuation);
        }

        /// <summary>
        /// 完成执行器切换并检查取消状态。
        /// </summary>
        public void GetResult()
        {
            MTaskRuntime.SwitchCurrentExecutor(executor);
            MTask.ThrowIfCancellationRequested();
        }

        #endregion
    }
}
