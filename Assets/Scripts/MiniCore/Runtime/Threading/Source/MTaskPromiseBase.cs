using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
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
}
