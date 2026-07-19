using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiniCore.Threading
{
    /// <summary>
    /// 由业务或外部回调手动完成的单次 MTask 结果源。
    /// </summary>
    /// <typeparam name="T">任务返回值类型。</typeparam>
    public sealed class MTaskCompletionSource<T> : IMTaskSource<T>
    {
        #region Private 私有成员

        private readonly object gate = new object(); // 保护完成状态和唯一续体。
        private readonly Action cancelAction; // 等待方结构化节点取消回调。
        private Action continuation; // 等待结果的唯一续体。
        private IMTaskExecutor continuationExecutor; // 等待方捕获的执行器。
        private Exception exception; // 失败或取消原因。
        private T result; // 成功返回值。
        private MTaskStatus status; // 当前结果状态。
        private bool registered; // 是否已经注册等待者。
        private bool consumed; // 是否已经消费结果。
        private MTaskNode waitingNode; // 等待当前完成源的结构化节点。
        private MTaskCancellationRegistration cancellationRegistration; // 等待方节点取消注册。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取当前手动结果源对应的任务。
        /// </summary>
        public MTask<T> Task => new MTask<T>(this, 1);

        /// <summary>
        /// 创建一个等待手动完成的结果源。
        /// </summary>
        public MTaskCompletionSource()
        {
            cancelAction = CancelFromNode;
            status = MTaskStatus.Pending;
        }

        /// <summary>
        /// 尝试以成功结果完成任务。
        /// </summary>
        /// <param name="value">任务返回值。</param>
        /// <returns>本次调用完成任务时返回 true。</returns>
        public bool TrySetResult(T value)
        {
            result = value;
            return TryComplete(MTaskStatus.Succeeded, null);
        }

        /// <summary>
        /// 尝试以异常完成任务。
        /// </summary>
        /// <param name="value">任务失败异常。</param>
        /// <returns>本次调用完成任务时返回 true。</returns>
        public bool TrySetException(Exception value)
        {
            return TryComplete(MTaskStatus.Faulted, value ?? throw new ArgumentNullException(nameof(value)));
        }

        /// <summary>
        /// 尝试以当前 MTask 域的取消状态完成任务。
        /// </summary>
        /// <returns>本次调用完成任务时返回 true。</returns>
        public bool TrySetCanceled()
        {
            MTaskNode node = MTaskRuntime.CurrentNode;
            Exception value = node?.Domain.CancellationException ?? MTaskRuntime.ApplicationDomain.CancellationException;
            return TryComplete(MTaskStatus.Canceled, value);
        }

        /// <summary>
        /// 获取任务当前状态。
        /// </summary>
        /// <param name="token">固定版本标识。</param>
        /// <returns>当前结果状态。</returns>
        public MTaskStatus GetStatus(short token)
        {
            ValidateToken(token);
            lock (gate)
            {
                return status;
            }
        }

        /// <summary>
        /// 注册唯一等待者的完成续体。
        /// </summary>
        /// <param name="value">完成续体。</param>
        /// <param name="token">固定版本标识。</param>
        public void OnCompleted(Action value, short token)
        {
            ValidateToken(token);
            bool schedule;
            lock (gate)
            {
                if (registered || consumed)
                {
                    throw new InvalidOperationException("MTaskCompletionSource 默认只支持一个等待者。");
                }

                registered = true;
                waitingNode = MTaskRuntime.CurrentNode;
                continuation = value ?? throw new ArgumentNullException(nameof(value));
                continuationExecutor = MTaskRuntime.CurrentExecutor ?? MTaskExecutors.Inline;
                schedule = status != MTaskStatus.Pending;
            }

            if (!schedule)
            {
                cancellationRegistration = MTaskRuntime.RegisterCancellation(cancelAction);
            }

            if (schedule)
            {
                ScheduleContinuation();
            }
        }

        /// <summary>
        /// 消费任务结果并传播失败或取消。
        /// </summary>
        /// <param name="token">固定版本标识。</param>
        /// <returns>手动设置的返回值。</returns>
        public T GetResult(short token)
        {
            ValidateToken(token);
            Exception value;
            T currentResult;
            lock (gate)
            {
                if (status == MTaskStatus.Pending || consumed)
                {
                    throw new InvalidOperationException("MTaskCompletionSource 尚未完成或已经被消费。");
                }

                consumed = true;
                value = exception;
                currentResult = result;
                continuation = null;
                continuationExecutor = null;
            }

            cancellationRegistration.Dispose();
            waitingNode = null;

            if (value != null)
            {
                throw value;
            }

            return currentResult;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 尝试原子地完成结果源并派发等待者。
        /// </summary>
        /// <param name="targetStatus">最终任务状态。</param>
        /// <param name="value">失败或取消原因。</param>
        /// <returns>本次调用改变状态时返回 true。</returns>
        private bool TryComplete(MTaskStatus targetStatus, Exception value)
        {
            bool schedule;
            lock (gate)
            {
                if (status != MTaskStatus.Pending)
                {
                    return false;
                }

                exception = value;
                status = targetStatus;
                schedule = registered;
            }

            cancellationRegistration.Dispose();

            if (schedule)
            {
                ScheduleContinuation();
            }

            return true;
        }

        /// <summary>
        /// 将等待者派发到注册时捕获的执行器。
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
        /// 校验手动结果源固定版本。
        /// </summary>
        /// <param name="token">调用方版本。</param>
        private static void ValidateToken(short token)
        {
            if (token != 1)
            {
                throw new InvalidOperationException("MTaskCompletionSource 版本无效。");
            }
        }

        /// <summary>
        /// 响应等待方结构化节点取消。
        /// </summary>
        private void CancelFromNode()
        {
            TryComplete(
                MTaskStatus.Canceled,
                waitingNode?.Domain.CancellationException ?? MTaskRuntime.GetCancellationException());
        }

        #endregion
    }

    /// <summary>
    /// MTask 常用组合操作。
    /// </summary>
    public readonly partial struct MTask
    {
        #region Public 公共成员

        /// <summary>
        /// 等待两个已经启动的任务全部完成。
        /// </summary>
        /// <param name="first">第一个任务。</param>
        /// <param name="second">第二个任务。</param>
        /// <returns>两个任务均完成后的任务。</returns>
        public static async MTask WhenAll(MTask first, MTask second)
        {
            await first;
            await second;
        }

        /// <summary>
        /// 等待任务数组中的所有任务完成。
        /// </summary>
        /// <param name="tasks">已经启动的任务数组。</param>
        /// <returns>全部任务完成后的任务。</returns>
        public static async MTask WhenAll(params MTask[] tasks)
        {
            if (tasks == null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }

            for (int i = 0; i < tasks.Length; i++)
            {
                await tasks[i];
            }
        }

        /// <summary>
        /// 等待两个任务中的任意一个完成，并持续监督另一个任务直至结束。
        /// </summary>
        /// <param name="first">第一个任务。</param>
        /// <param name="second">第二个任务。</param>
        /// <returns>首先完成的任务索引，0 表示 first，1 表示 second。</returns>
        public static MTask<int> WhenAny(MTask first, MTask second)
        {
            MTaskCompletionSource<int> completion = new MTaskCompletionSource<int>();
            ObserveWhenAny(first, 0, completion).Forget();
            ObserveWhenAny(second, 1, completion).Forget();
            return completion.Task;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 消费 WhenAny 的一个候选任务并尝试提交胜出索引。
        /// </summary>
        /// <param name="task">候选任务。</param>
        /// <param name="index">候选索引。</param>
        /// <param name="completion">共享完成源。</param>
        /// <returns>候选任务监督过程。</returns>
        private static async MTask ObserveWhenAny(MTask task, int index, MTaskCompletionSource<int> completion)
        {
            try
            {
                await task;
                completion.TrySetResult(index);
            }
            catch (OperationCanceledException)
            {
                completion.TrySetCanceled();
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
        }

        #endregion
    }

    /// <summary>
    /// System Task 等外部异步 API 到 MTask 的集中适配入口。
    /// </summary>
    public static class MTaskExternal
    {
        #region Public 公共成员

        /// <summary>
        /// 等待外部无返回值 Task，并恢复到调用方捕获的 MTask 执行器。
        /// </summary>
        /// <param name="task">外部 Task。</param>
        /// <returns>对应的 MTask。</returns>
        public static async MTask Await(Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            await task.ConfigureAwait(false);
        }

        /// <summary>
        /// 等待外部带返回值 Task，并恢复到调用方捕获的 MTask 执行器。
        /// </summary>
        /// <typeparam name="T">外部 Task 返回值类型。</typeparam>
        /// <param name="task">外部 Task。</param>
        /// <returns>携带外部结果的 MTask。</returns>
        public static async MTask<T> Await<T>(Task<T> task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            return await task.ConfigureAwait(false);
        }

        /// <summary>
        /// 获取当前 MTask 节点的延迟创建 CancellationToken，供外部 API 边界使用。
        /// </summary>
        /// <returns>随当前结构化节点取消的 Token；没有节点时返回默认值。</returns>
        public static CancellationToken GetCancellationToken()
        {
            return MTaskRuntime.CurrentNode?.GetExternalCancellationToken() ?? default;
        }

        #endregion
    }

}
