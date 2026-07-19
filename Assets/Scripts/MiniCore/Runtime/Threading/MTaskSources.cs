using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// 结构化 MTask 的运行节点基类。
    /// </summary>
    internal abstract class MTaskNode
    {
        #region Private 私有成员

        private readonly object gate = new object(); // 保护子节点和完成状态。
        private readonly List<MTaskNode> children = new List<MTaskNode>(2); // 当前仍存活的直接子任务。
        private Action cancellationContinuation; // 当前挂起点的取消唤醒回调。
        private CancellationTokenSource externalCancellationSource; // 仅外部 BCL 边界需要时惰性创建的 CTS。
        private Exception structuralException; // 未被观察子任务传播的第一个失败。
        private bool bodyCompleted; // 异步方法主体是否已经退出。
        private bool executionCompleted; // 当前节点执行和全部子节点是否已经退场。
        private bool nodeCompleted; // 节点是否已经最终完成。
        private int cancellationRequested; // 是否已请求取消。

        #endregion

        #region Internal 内部成员

        internal MTaskNode Parent; // 结构化父节点。
        internal MTaskDomain Domain; // 所属生命周期域。
        internal IMTaskOwner Owner; // 最近的生命周期 Owner。
        internal IMTaskExecutor Executor; // 当前状态机续体执行器。

        /// <summary>
        /// 获取节点是否已经收到取消请求。
        /// </summary>
        internal bool IsCancellationRequested => Volatile.Read(ref cancellationRequested) != 0 || Domain.IsCancellationRequested;

        /// <summary>
        /// 获取节点自身是否已经执行过取消传播。
        /// </summary>
        internal bool HasNodeCancellationRequest => Volatile.Read(ref cancellationRequested) != 0;

        /// <summary>
        /// 获取当前节点的执行与子任务是否已经全部退场。
        /// </summary>
        internal bool IsExecutionCompleted => Volatile.Read(ref executionCompleted);

        /// <summary>
        /// 初始化节点并加入当前结构化父节点或 Owner 域。
        /// </summary>
        internal void InitializeNode()
        {
            Parent = MTaskRuntime.CurrentNode;
            Owner = Parent?.Owner ?? MTaskRuntime.CurrentOwner;
            Domain = Parent?.Domain ?? Owner?.GetMTaskDomain() ?? MTaskRuntime.ApplicationDomain;
            Executor = MTaskRuntime.CurrentExecutor ?? Domain.Executor ?? MTaskExecutors.Inline;
            bodyCompleted = false;
            executionCompleted = false;
            nodeCompleted = false;
            structuralException = null;
            cancellationRequested = 0;
            cancellationContinuation = null;
            children.Clear();
            MTaskDiagnostics.OnNodeActivated();

            if (Parent != null)
            {
                Parent.AddChild(this);
            }
            else
            {
                Domain.AddRoot(this);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (Owner == null)
                {
                    MTaskSupervisor.ReportOrphan(GetType());
                }
#endif
            }

            if (Domain.IsCancellationRequested || (Parent == null && MTaskRuntime.ShouldCancelNewRoot))
            {
                Cancel();
            }
        }

        /// <summary>
        /// 请求取消当前节点及其所有后代。
        /// </summary>
        internal void Cancel()
        {
            if (Interlocked.Exchange(ref cancellationRequested, 1) != 0)
            {
                return;
            }

            Action callback;
            CancellationTokenSource externalSource;
            lock (gate)
            {
                callback = cancellationContinuation;
                externalSource = externalCancellationSource;
                CancelChildrenNoLock();
            }

            externalSource?.Cancel();
            callback?.Invoke();
        }

        /// <summary>
        /// 获取当前节点惰性创建的外部 CancellationToken。
        /// </summary>
        /// <returns>与当前节点生命周期一致的 Token。</returns>
        internal CancellationToken GetExternalCancellationToken()
        {
            CancellationTokenSource source;
            bool cancel;
            lock (gate)
            {
                source = externalCancellationSource ??= new CancellationTokenSource();
                cancel = IsCancellationRequested;
            }

            if (cancel)
            {
                source.Cancel();
            }

            return source.Token;
        }

        /// <summary>
        /// 设置当前挂起点的取消唤醒回调。
        /// </summary>
        /// <param name="continuation">取消时执行的回调。</param>
        internal void SetCancellationContinuation(Action continuation)
        {
            bool invoke;
            lock (gate)
            {
                cancellationContinuation = continuation;
                invoke = IsCancellationRequested && continuation != null;
            }

            if (invoke)
            {
                continuation();
            }
        }

        /// <summary>
        /// 清除与指定挂起点匹配的取消回调。
        /// </summary>
        /// <param name="continuation">即将离开的挂起点回调。</param>
        internal void ClearCancellationContinuation(Action continuation)
        {
            lock (gate)
            {
                if (ReferenceEquals(cancellationContinuation, continuation))
                {
                    cancellationContinuation = null;
                }
            }
        }

        /// <summary>
        /// 标记异步方法主体结束，并取消未完成的非等待子任务。
        /// </summary>
        internal void CompleteBody()
        {
            bool finalize;
            lock (gate)
            {
                bodyCompleted = true;
                CancelOrObserveChildrenNoLock();
                finalize = children.Count == 0;
            }

            if (finalize)
            {
                CompleteNode();
            }
        }

        /// <summary>
        /// 将后台任务从当前方法父节点转移为 Owner 域根任务。
        /// </summary>
        internal void DetachForForget()
        {
            MTaskNode parent;
            lock (gate)
            {
                if (nodeCompleted || Parent == null)
                {
                    return;
                }

                parent = Parent;
                Parent = null;
            }

            parent.RemoveChild(this);
            Domain.AddRoot(this);
        }

        /// <summary>
        /// 在调用方消费结果时将已完成子节点从父任务解除。
        /// </summary>
        internal void MarkObserved()
        {
            MTaskNode parent = Parent;
            if (parent == null)
            {
                return;
            }

            Parent = null;
            parent.RemoveChild(this);
        }

        /// <summary>
        /// 由具体结果源完成状态与续体派发。
        /// </summary>
        protected abstract void CompleteSource();

        /// <summary>
        /// 获取当前节点需要向未观察父任务传播的异常。
        /// </summary>
        /// <returns>节点失败异常；成功时为 null。</returns>
        protected abstract Exception GetNodeException();

        /// <summary>
        /// 获取未观察子任务传播的结构化失败。
        /// </summary>
        /// <returns>第一个子任务失败；不存在时为 null。</returns>
        protected Exception GetStructuralException()
        {
            lock (gate)
            {
                return structuralException;
            }
        }

        /// <summary>
        /// 清理节点关系，供结果源归还对象池前调用。
        /// </summary>
        protected void ResetNode()
        {
            externalCancellationSource?.Dispose();
            externalCancellationSource = null;
            Parent = null;
            Domain = null;
            Owner = null;
            Executor = null;
            cancellationContinuation = null;
            structuralException = null;
            children.Clear();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将直接子节点加入结构化任务树。
        /// </summary>
        /// <param name="child">新建的子任务。</param>
        private void AddChild(MTaskNode child)
        {
            bool cancel;
            lock (gate)
            {
                cancel = bodyCompleted || IsCancellationRequested;
                if (!cancel)
                {
                    children.Add(child);
                }
            }

            if (cancel)
            {
                child.Cancel();
            }
        }

        /// <summary>
        /// 在已持有节点锁时取消所有尚未传播取消的直接子节点。
        /// </summary>
        private void CancelChildrenNoLock()
        {
            while (true)
            {
                MTaskNode child = null;
                for (int i = children.Count - 1; i >= 0; i--)
                {
                    MTaskNode candidate = children[i];
                    if (!candidate.HasNodeCancellationRequest)
                    {
                        child = candidate;
                        break;
                    }
                }

                if (child == null)
                {
                    return;
                }

                child.Cancel();
            }
        }

        /// <summary>
        /// 在方法主体退出时观察已完成子节点，并取消仍在运行的子节点。
        /// </summary>
        private void CancelOrObserveChildrenNoLock()
        {
            while (true)
            {
                MTaskNode child = null;
                bool observe = false;
                for (int i = children.Count - 1; i >= 0; i--)
                {
                    MTaskNode candidate = children[i];
                    if (candidate.IsExecutionCompleted)
                    {
                        child = candidate;
                        observe = true;
                        break;
                    }

                    if (!candidate.HasNodeCancellationRequest)
                    {
                        child = candidate;
                        break;
                    }
                }

                if (child == null)
                {
                    return;
                }

                if (observe)
                {
                    CompleteUnobservedChild(child);
                }
                else
                {
                    child.Cancel();
                }
            }
        }

        /// <summary>
        /// 从结构化任务树移除已经完成的直接子节点。
        /// </summary>
        /// <param name="child">已经完成的子节点。</param>
        private void RemoveChild(MTaskNode child)
        {
            bool finalize;
            lock (gate)
            {
                children.Remove(child);
                finalize = bodyCompleted && children.Count == 0;
            }

            if (finalize)
            {
                CompleteNode();
            }
        }

        /// <summary>
        /// 处理一个已经退场但没有被 await 消费的子节点。
        /// </summary>
        /// <param name="child">未被观察的子节点。</param>
        private void CompleteUnobservedChild(MTaskNode child)
        {
            bool finalize;
            bool removed;
            Exception childException = child.GetNodeException();
            lock (gate)
            {
                removed = children.Remove(child);
                if (!removed)
                {
                    return;
                }

                if (childException != null && !(childException is OperationCanceledException) && structuralException == null)
                {
                    structuralException = childException;
                    CancelChildrenNoLock();
                }

                finalize = bodyCompleted && children.Count == 0;
            }

            child.Parent = null;
            if (finalize)
            {
                CompleteNode();
            }
        }

        /// <summary>
        /// 在子节点执行完成时决定继续等待结果消费或按未观察失败处理。
        /// </summary>
        /// <param name="child">已经退场的子节点。</param>
        private void OnChildExecutionCompleted(MTaskNode child)
        {
            bool abandon;
            lock (gate)
            {
                abandon = bodyCompleted && children.Contains(child);
            }

            if (abandon)
            {
                CompleteUnobservedChild(child);
            }
        }

        /// <summary>
        /// 完成节点、解除父子关系并通知结果源。
        /// </summary>
        private void CompleteNode()
        {
            lock (gate)
            {
                if (nodeCompleted)
                {
                    return;
                }

                nodeCompleted = true;
                executionCompleted = true;
                cancellationContinuation = null;
            }

            MTaskNode parent = Parent;
            if (parent != null)
            {
                parent.OnChildExecutionCompleted(this);
            }
            else
            {
                Domain.RemoveRoot(this);
            }

            MTaskDiagnostics.OnNodeCompleted();
            CompleteSource();
        }

        #endregion
    }

    /// <summary>
    /// MTask async 状态机 Runner 的非泛型接口。
    /// </summary>
    internal interface IMTaskStateMachineRunner
    {
        /// <summary>
        /// 获取 awaiter 应注册的复用续体。
        /// </summary>
        Action Continuation { get; }

        /// <summary>
        /// 清理状态机并归还对应类型的对象池。
        /// </summary>
        void Return();
    }

    /// <summary>
    /// 按具体状态机类型池化的 MTask Runner。
    /// </summary>
    /// <typeparam name="TStateMachine">编译器生成的状态机类型。</typeparam>
    internal sealed class MTaskStateMachineRunner<TStateMachine> : IMTaskStateMachineRunner
        where TStateMachine : IAsyncStateMachine
    {
        #region Private 私有成员

        private readonly Action resumeAction; // awaiter 完成后调用的线程切换入口。
        private readonly Action moveNextAction; // 目标执行器上真正执行状态机的回调。
        private MTaskNode node; // Runner 所属任务节点。
        private TStateMachine stateMachine; // 池化保存的状态机实例。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取复用的状态机续体。
        /// </summary>
        public Action Continuation => resumeAction;

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建 Runner 并缓存两个稳定委托。
        /// </summary>
        private MTaskStateMachineRunner()
        {
            resumeAction = Resume;
            moveNextAction = MoveNext;
        }

        /// <summary>
        /// 从当前状态机类型的池中获取 Runner。
        /// </summary>
        /// <param name="node">Runner 所属任务节点。</param>
        /// <param name="stateMachine">需要保存的状态机。</param>
        /// <returns>初始化后的 Runner。</returns>
        internal static MTaskStateMachineRunner<TStateMachine> Rent(MTaskNode node, ref TStateMachine stateMachine)
        {
            if (!MTaskObjectPool<MTaskStateMachineRunner<TStateMachine>>.TryRent(out MTaskStateMachineRunner<TStateMachine> runner))
            {
                runner = new MTaskStateMachineRunner<TStateMachine>();
            }

            runner.node = node;
            runner.stateMachine = stateMachine;
            return runner;
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 清理状态机引用并归还对应泛型池。
        /// </summary>
        public void Return()
        {
            node = null;
            stateMachine = default;
            MTaskObjectPool<MTaskStateMachineRunner<TStateMachine>>.Return(this);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将状态机恢复派发到任务当前捕获的执行器。
        /// </summary>
        private void Resume()
        {
            IMTaskExecutor executor = node.Executor ?? MTaskExecutors.Inline;
            if (executor.IsCurrentThread)
            {
                MoveNext();
                return;
            }

            executor.Post(moveNextAction);
        }

        /// <summary>
        /// 在任务上下文中执行下一段状态机。
        /// </summary>
        private void MoveNext()
        {
            MTaskExecutionContext context = MTaskRuntime.EnterNode(node);
            try
            {
                stateMachine.MoveNext();
            }
            finally
            {
                MTaskRuntime.ExitNode(context);
            }
        }

        #endregion
    }

    /// <summary>
    /// 池化异步方法结果源的公共实现基础。
    /// </summary>
    internal abstract class MTaskPromiseBase : MTaskNode
    {
        #region Private 私有成员

        private readonly object completionGate = new object(); // 保护结果状态和单一续体。
        private Action continuation; // 唯一消费方的完成续体。
        private IMTaskExecutor continuationExecutor; // 消费方捕获的恢复执行器。
        private Exception exception; // 失败或取消原因。
        private IMTaskStateMachineRunner runner; // 首次挂起后保存状态机的 Runner。
        private MTaskStatus status; // 当前结果源状态。
        private bool continuationRegistered; // 是否已经注册消费方。
        private bool consumed; // 是否已经完成结果消费。
        private short version; // 对象池复用版本。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 获取当前对象池版本。
        /// </summary>
        internal short Version => version;

        /// <summary>
        /// 初始化池化结果源。
        /// </summary>
        internal void InitializePromise()
        {
            unchecked
            {
                version++;
                if (version == 0)
                {
                    version = 1;
                }
            }

            continuation = null;
            continuationExecutor = null;
            exception = null;
            runner = null;
            status = MTaskStatus.Pending;
            continuationRegistered = false;
            consumed = false;
            InitializeNode();
        }

        /// <summary>
        /// 在当前节点上下文中首次执行状态机。
        /// </summary>
        /// <typeparam name="TStateMachine">状态机类型。</typeparam>
        /// <param name="stateMachine">状态机实例。</param>
        internal void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            if (IsCancellationRequested)
            {
                SetPromiseException(Domain.CancellationException);
                return;
            }

            MTaskExecutionContext context = MTaskRuntime.EnterNode(this);
            try
            {
                stateMachine.MoveNext();
            }
            finally
            {
                MTaskRuntime.ExitNode(context);
            }
        }

        /// <summary>
        /// 获取并缓存当前状态机的稳定续体委托。
        /// </summary>
        /// <typeparam name="TStateMachine">状态机类型。</typeparam>
        /// <param name="stateMachine">当前状态机。</param>
        /// <returns>awaiter 应注册的续体。</returns>
        internal Action GetStateMachineContinuation<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            if (runner == null)
            {
                runner = MTaskStateMachineRunner<TStateMachine>.Rent(this, ref stateMachine);
            }

            return runner.Continuation;
        }

        /// <summary>
        /// 记录异步方法的失败或取消并等待子任务退场。
        /// </summary>
        /// <param name="value">状态机抛出的异常。</param>
        internal void SetPromiseException(Exception value)
        {
            exception = value ?? throw new ArgumentNullException(nameof(value));
            CompleteBody();
        }

        /// <summary>
        /// 获取当前版本任务状态。
        /// </summary>
        /// <param name="token">调用方持有的版本。</param>
        /// <returns>任务状态。</returns>
        internal MTaskStatus GetPromiseStatus(short token)
        {
            ValidateToken(token);
            lock (completionGate)
            {
                return status;
            }
        }

        /// <summary>
        /// 注册唯一消费方的完成续体。
        /// </summary>
        /// <param name="value">任务完成后执行的续体。</param>
        /// <param name="token">调用方持有的版本。</param>
        internal void RegisterContinuation(Action value, short token)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            ValidateToken(token);
            bool schedule;
            lock (completionGate)
            {
                if (continuationRegistered || consumed)
                {
                    throw new InvalidOperationException("MTask 默认只允许注册一个等待者；多方等待请使用 Share()。");
                }

                continuationRegistered = true;
                continuation = value;
                continuationExecutor = MTaskRuntime.CurrentExecutor ?? MTaskExecutors.Inline;
                schedule = status != MTaskStatus.Pending;
            }

            if (schedule)
            {
                ScheduleContinuation();
            }
        }

        /// <summary>
        /// 完成单次消费并取得需要传播的异常。
        /// </summary>
        /// <param name="token">调用方持有的版本。</param>
        /// <returns>失败或取消异常；成功时为 null。</returns>
        internal Exception Consume(short token)
        {
            ValidateToken(token);
            Exception result;
            lock (completionGate)
            {
                if (status == MTaskStatus.Pending)
                {
                    throw new InvalidOperationException("MTask 尚未完成。");
                }

                if (consumed)
                {
                    throw new InvalidOperationException("MTask 已经被消费；多方等待请使用 Share()。");
                }

                consumed = true;
                result = exception;
            }

            MarkObserved();
            return result;
        }

        /// <summary>
        /// 归还状态机 Runner 并清理节点公共状态。
        /// </summary>
        internal void ResetPromise()
        {
            runner?.Return();
            runner = null;
            continuation = null;
            continuationExecutor = null;
            exception = null;
            ResetNode();
        }

        /// <summary>
        /// 根据异步方法结果确定最终状态并派发续体。
        /// </summary>
        protected override void CompleteSource()
        {
            bool schedule;
            lock (completionGate)
            {
                exception ??= GetStructuralException();
                status = exception == null
                    ? MTaskStatus.Succeeded
                    : exception is OperationCanceledException
                        ? MTaskStatus.Canceled
                        : MTaskStatus.Faulted;
                schedule = continuationRegistered;
            }

            if (schedule)
            {
                ScheduleContinuation();
            }
        }

        /// <summary>
        /// 获取当前 Promise 自身或未观察子任务的失败。
        /// </summary>
        /// <returns>需要向结构化父任务传播的异常。</returns>
        protected override Exception GetNodeException()
        {
            return exception ?? GetStructuralException();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 校验调用方版本是否仍对应当前池对象。
        /// </summary>
        /// <param name="token">调用方持有的版本。</param>
        private void ValidateToken(short token)
        {
            if (token != version)
            {
                throw new InvalidOperationException("MTask 句柄已失效，底层结果源已经被对象池复用。");
            }
        }

        /// <summary>
        /// 将消费方续体派发到它注册时捕获的执行器。
        /// </summary>
        private void ScheduleContinuation()
        {
            Action callback;
            IMTaskExecutor executor;
            lock (completionGate)
            {
                callback = continuation;
                executor = continuationExecutor ?? MTaskExecutors.Inline;
            }

            if (callback == null)
            {
                return;
            }

            if (executor.IsCurrentThread)
            {
                callback();
            }
            else
            {
                executor.Post(callback);
            }
        }

        #endregion
    }

    /// <summary>
    /// 无返回值 async MTask 的池化结果源。
    /// </summary>
    internal sealed class MTaskPromise : MTaskPromiseBase, IMTaskSource
    {
        #region Private 私有成员

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 获取并初始化一个无返回值 Promise。
        /// </summary>
        /// <returns>可用于新异步方法的 Promise。</returns>
        internal static MTaskPromise Rent()
        {
            if (!MTaskObjectPool<MTaskPromise>.TryRent(out MTaskPromise promise))
            {
                promise = new MTaskPromise();
            }

            promise.InitializePromise();
            return promise;
        }

        /// <summary>
        /// 标记异步方法主体成功结束。
        /// </summary>
        internal void SetResult()
        {
            CompleteBody();
        }

        /// <summary>
        /// 标记异步方法主体异常结束。
        /// </summary>
        /// <param name="exception">状态机异常。</param>
        internal void SetException(Exception exception)
        {
            SetPromiseException(exception);
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 获取任务状态。
        /// </summary>
        /// <param name="token">结果源版本。</param>
        /// <returns>当前任务状态。</returns>
        public MTaskStatus GetStatus(short token)
        {
            return GetPromiseStatus(token);
        }

        /// <summary>
        /// 注册任务完成续体。
        /// </summary>
        /// <param name="continuation">完成续体。</param>
        /// <param name="token">结果源版本。</param>
        public void OnCompleted(Action continuation, short token)
        {
            RegisterContinuation(continuation, token);
        }

        /// <summary>
        /// 消费任务结果并归还 Promise。
        /// </summary>
        /// <param name="token">结果源版本。</param>
        public void GetResult(short token)
        {
            Exception exception = Consume(token);
            ResetPromise();
            MTaskObjectPool<MTaskPromise>.Return(this);
            if (exception != null)
            {
                throw exception;
            }
        }

        #endregion
    }

    /// <summary>
    /// 带返回值 async MTask 的池化结果源。
    /// </summary>
    /// <typeparam name="T">异步方法返回值类型。</typeparam>
    internal sealed class MTaskPromise<T> : MTaskPromiseBase, IMTaskSource<T>
    {
        #region Private 私有成员

        private T result; // 异步方法返回值。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 获取并初始化一个带返回值 Promise。
        /// </summary>
        /// <returns>可用于新异步方法的 Promise。</returns>
        internal static MTaskPromise<T> Rent()
        {
            if (!MTaskObjectPool<MTaskPromise<T>>.TryRent(out MTaskPromise<T> promise))
            {
                promise = new MTaskPromise<T>();
            }

            promise.result = default;
            promise.InitializePromise();
            return promise;
        }

        /// <summary>
        /// 标记异步方法主体成功结束并保存返回值。
        /// </summary>
        /// <param name="value">异步方法返回值。</param>
        internal void SetResult(T value)
        {
            result = value;
            CompleteBody();
        }

        /// <summary>
        /// 标记异步方法主体异常结束。
        /// </summary>
        /// <param name="exception">状态机异常。</param>
        internal void SetException(Exception exception)
        {
            SetPromiseException(exception);
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 获取任务状态。
        /// </summary>
        /// <param name="token">结果源版本。</param>
        /// <returns>当前任务状态。</returns>
        public MTaskStatus GetStatus(short token)
        {
            return GetPromiseStatus(token);
        }

        /// <summary>
        /// 注册任务完成续体。
        /// </summary>
        /// <param name="continuation">完成续体。</param>
        /// <param name="token">结果源版本。</param>
        public void OnCompleted(Action continuation, short token)
        {
            RegisterContinuation(continuation, token);
        }

        /// <summary>
        /// 消费任务结果并归还 Promise。
        /// </summary>
        /// <param name="token">结果源版本。</param>
        /// <returns>异步方法返回值。</returns>
        public T GetResult(short token)
        {
            Exception exception = Consume(token);
            T value = result;
            result = default;
            ResetPromise();
            MTaskObjectPool<MTaskPromise<T>>.Return(this);
            if (exception != null)
            {
                throw exception;
            }

            return value;
        }

        #endregion
    }

    /// <summary>
    /// MTask.Delay 使用的可池化结果源。
    /// </summary>
    internal sealed class MTaskDelaySource : IMTaskSource
    {
        #region Private 私有成员

        private readonly Action completeAction; // 计时器到期回调。
        private readonly Action cancelAction; // 当前节点取消回调。
        private readonly object gate = new object(); // 保护完成状态和续体。
        private Action continuation; // 等待延迟完成的续体。
        private IMTaskExecutor continuationExecutor; // 注册续体时捕获的执行器。
        private IMTaskScheduledHandle scheduledHandle; // 执行器延迟任务句柄。
        private MTaskNode waitingNode; // 发起延迟的任务节点。
        private Exception exception; // 取消原因。
        private MTaskStatus status; // 当前延迟状态。
        private bool registered; // 是否已经注册等待者。
        private bool consumed; // 是否已经消费。
        private short version; // 结果源复用版本。

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 创建延迟源并缓存稳定回调委托。
        /// </summary>
        private MTaskDelaySource()
        {
            completeAction = Complete;
            cancelAction = Cancel;
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建一个由当前执行器驱动的延迟 MTask。
        /// </summary>
        /// <param name="delay">延迟时间。</param>
        /// <returns>延迟任务。</returns>
        internal static MTask Create(TimeSpan delay)
        {
            if (!MTaskObjectPool<MTaskDelaySource>.TryRent(out MTaskDelaySource source))
            {
                source = new MTaskDelaySource();
            }

            unchecked
            {
                source.version++;
                if (source.version == 0)
                {
                    source.version = 1;
                }
            }

            source.continuation = null;
            source.continuationExecutor = null;
            source.scheduledHandle = null;
            source.waitingNode = MTaskRuntime.CurrentNode;
            source.exception = null;
            source.status = MTaskStatus.Pending;
            source.registered = false;
            source.consumed = false;

            IMTaskExecutor executor = MTaskRuntime.CurrentExecutor ?? MTaskExecutors.Inline;
            source.scheduledHandle = executor.Schedule(source.completeAction, delay);
            source.waitingNode?.SetCancellationContinuation(source.cancelAction);
            return new MTask(source, source.version);
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 获取延迟任务状态。
        /// </summary>
        /// <param name="token">结果源版本。</param>
        /// <returns>当前延迟状态。</returns>
        public MTaskStatus GetStatus(short token)
        {
            ValidateToken(token);
            lock (gate)
            {
                return status;
            }
        }

        /// <summary>
        /// 注册延迟完成续体。
        /// </summary>
        /// <param name="value">完成续体。</param>
        /// <param name="token">结果源版本。</param>
        public void OnCompleted(Action value, short token)
        {
            ValidateToken(token);
            bool schedule;
            lock (gate)
            {
                if (registered || consumed)
                {
                    throw new InvalidOperationException("MTask.Delay 只能被等待一次。");
                }

                registered = true;
                continuation = value;
                continuationExecutor = MTaskRuntime.CurrentExecutor ?? MTaskExecutors.Inline;
                schedule = status != MTaskStatus.Pending;
            }

            if (schedule)
            {
                ScheduleContinuation();
            }
        }

        /// <summary>
        /// 消费延迟任务结果并归还对象池。
        /// </summary>
        /// <param name="token">结果源版本。</param>
        public void GetResult(short token)
        {
            ValidateToken(token);
            Exception value;
            lock (gate)
            {
                if (status == MTaskStatus.Pending || consumed)
                {
                    throw new InvalidOperationException("MTask.Delay 尚未完成或已经被消费。");
                }

                consumed = true;
                value = exception;
            }

            waitingNode?.ClearCancellationContinuation(cancelAction);
            continuation = null;
            continuationExecutor = null;
            scheduledHandle = null;
            waitingNode = null;
            exception = null;
            MTaskObjectPool<MTaskDelaySource>.Return(this);
            if (value != null)
            {
                throw value;
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将延迟任务标记为成功完成。
        /// </summary>
        private void Complete()
        {
            CompleteCore(null);
        }

        /// <summary>
        /// 取消延迟任务并停止底层计时器。
        /// </summary>
        private void Cancel()
        {
            scheduledHandle?.Cancel();
            CompleteCore(waitingNode?.Domain.CancellationException ?? MTaskRuntime.ApplicationDomain.CancellationException);
        }

        /// <summary>
        /// 以成功或取消状态完成延迟任务。
        /// </summary>
        /// <param name="value">取消异常；成功时为 null。</param>
        private void CompleteCore(Exception value)
        {
            bool schedule;
            lock (gate)
            {
                if (status != MTaskStatus.Pending)
                {
                    return;
                }

                exception = value;
                status = value == null ? MTaskStatus.Succeeded : MTaskStatus.Canceled;
                schedule = registered;
            }

            waitingNode?.ClearCancellationContinuation(cancelAction);
            if (schedule)
            {
                ScheduleContinuation();
            }
        }

        /// <summary>
        /// 将延迟续体派发到注册时捕获的执行器。
        /// </summary>
        private void ScheduleContinuation()
        {
            Action callback;
            IMTaskExecutor executor;
            lock (gate)
            {
                callback = continuation;
                executor = continuationExecutor ?? MTaskExecutors.Inline;
            }

            if (callback == null)
            {
                return;
            }

            if (executor.IsCurrentThread)
            {
                callback();
            }
            else
            {
                executor.Post(callback);
            }
        }

        /// <summary>
        /// 校验延迟结果源版本。
        /// </summary>
        /// <param name="token">调用方持有的版本。</param>
        private void ValidateToken(short token)
        {
            if (token != version)
            {
                throw new InvalidOperationException("MTask.Delay 句柄已经失效。");
            }
        }

        #endregion
    }

    /// <summary>
    /// 监督一个被 Forget 的无返回值 MTask。
    /// </summary>
    internal sealed class MTaskForgetObserver
    {
        #region Private 私有成员

        private readonly Action continuation; // 任务完成后的稳定回调。
        private MTask task; // 当前观察的任务。
        private string ownerName; // 任务 Owner 诊断名称。

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 创建观察器并缓存完成回调。
        /// </summary>
        private MTaskForgetObserver()
        {
            continuation = Complete;
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 开始监督一个无返回值 MTask。
        /// </summary>
        /// <param name="value">不再由业务 await 的任务。</param>
        internal static void Observe(MTask value)
        {
            if (!MTaskObjectPool<MTaskForgetObserver>.TryRent(out MTaskForgetObserver observer))
            {
                observer = new MTaskForgetObserver();
            }

            observer.task = value;
            observer.ownerName = MTaskRuntime.CurrentOwner?.GetType().FullName ?? MTaskRuntime.CurrentNode?.Owner?.GetType().FullName ?? "Application";
            MTaskAwaiter awaiter = value.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                observer.Complete();
            }
            else
            {
                awaiter.UnsafeOnCompleted(observer.continuation);
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 消费后台任务结果并上报未处理异常。
        /// </summary>
        private void Complete()
        {
            try
            {
                task.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                MTaskSupervisor.Report(exception, ownerName);
            }
            finally
            {
                task = default;
                ownerName = null;
                MTaskObjectPool<MTaskForgetObserver>.Return(this);
            }
        }

        #endregion
    }

    /// <summary>
    /// 监督一个被 Forget 的带返回值 MTask。
    /// </summary>
    /// <typeparam name="T">任务返回值类型。</typeparam>
    internal sealed class MTaskForgetObserver<T>
    {
        #region Private 私有成员

        private readonly Action continuation; // 任务完成后的稳定回调。
        private MTask<T> task; // 当前观察的任务。
        private string ownerName; // 任务 Owner 诊断名称。

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 创建观察器并缓存完成回调。
        /// </summary>
        private MTaskForgetObserver()
        {
            continuation = Complete;
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 开始监督一个带返回值 MTask。
        /// </summary>
        /// <param name="value">不再由业务 await 的任务。</param>
        internal static void Observe(MTask<T> value)
        {
            if (!MTaskObjectPool<MTaskForgetObserver<T>>.TryRent(out MTaskForgetObserver<T> observer))
            {
                observer = new MTaskForgetObserver<T>();
            }

            observer.task = value;
            observer.ownerName = MTaskRuntime.CurrentOwner?.GetType().FullName ?? MTaskRuntime.CurrentNode?.Owner?.GetType().FullName ?? "Application";
            MTaskAwaiter<T> awaiter = value.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                observer.Complete();
            }
            else
            {
                awaiter.UnsafeOnCompleted(observer.continuation);
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 消费后台任务结果并上报未处理异常。
        /// </summary>
        private void Complete()
        {
            try
            {
                task.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                MTaskSupervisor.Report(exception, ownerName);
            }
            finally
            {
                task = default;
                ownerName = null;
                MTaskObjectPool<MTaskForgetObserver<T>>.Return(this);
            }
        }

        #endregion
    }
}
