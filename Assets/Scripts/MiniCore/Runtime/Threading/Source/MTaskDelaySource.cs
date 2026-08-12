using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
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
}
