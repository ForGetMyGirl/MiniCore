using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{

    /// <summary>
    /// 持有一组结构化 MTask 根节点，并负责统一取消与退场通知。
    /// </summary>
    public sealed class MTaskDomain : IDisposable
    {
        #region Private 私有成员

        private readonly object gate = new object(); // 保护根节点集合和退场回调。
        private readonly HashSet<MTaskNode> roots = new HashSet<MTaskNode>(); // 当前仍存活的根任务。
        private Action drained; // 域内任务全部结束后的回调。
        private int cancellationRequested; // 是否已经请求取消。
        private int disposed; // 是否已经释放。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取任务域的诊断名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 获取任务域首选的最终化执行器。
        /// </summary>
        public IMTaskExecutor Executor { get; }

        /// <summary>
        /// 获取任务域是否已经收到取消请求。
        /// </summary>
        public bool IsCancellationRequested => Volatile.Read(ref cancellationRequested) != 0;

        /// <summary>
        /// 获取任务域是否已经释放。
        /// </summary>
        public bool IsDisposed => Volatile.Read(ref disposed) != 0;

        /// <summary>
        /// 获取域级复用的取消异常。
        /// </summary>
        public MTaskCanceledException CancellationException { get; }

        /// <summary>
        /// 创建任务生命周期域。
        /// </summary>
        /// <param name="name">用于日志和诊断的名称。</param>
        /// <param name="executor">域最终化时使用的执行器。</param>
        public MTaskDomain(string name, IMTaskExecutor executor = null)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Unnamed" : name;
            Executor = executor ?? MTaskRuntime.CurrentExecutor ?? MTaskExecutors.Inline;
            CancellationException = new MTaskCanceledException(Name);
        }

        /// <summary>
        /// 请求取消域内所有根任务及其后代。
        /// </summary>
        public void Cancel()
        {
            if (Interlocked.Exchange(ref cancellationRequested, 1) != 0)
            {
                return;
            }

            lock (gate)
            {
                while (true)
                {
                    MTaskNode root = null;
                    foreach (MTaskNode candidate in roots)
                    {
                        if (!candidate.HasNodeCancellationRequest)
                        {
                            root = candidate;
                            break;
                        }
                    }

                    if (root == null)
                    {
                        break;
                    }

                    root.Cancel();
                }
            }

            InvokeDrainedIfReady();
        }

        /// <summary>
        /// 在域内任务全部结束后执行一次回调。
        /// </summary>
        /// <param name="continuation">退场完成后的处理。</param>
        public void OnDrained(Action continuation)
        {
            if (continuation == null)
            {
                throw new ArgumentNullException(nameof(continuation));
            }

            bool invokeNow;
            lock (gate)
            {
                invokeNow = roots.Count == 0;
                if (!invokeNow)
                {
                    drained += continuation;
                }
            }

            if (invokeNow)
            {
                Post(continuation);
            }
        }

        /// <summary>
        /// 取消任务域并阻止后续根任务进入。
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
            {
                return;
            }

            Cancel();
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 向任务域注册一个根任务。
        /// </summary>
        /// <param name="node">要注册的任务节点。</param>
        internal void AddRoot(MTaskNode node)
        {
            bool cancel;
            lock (gate)
            {
                cancel = IsCancellationRequested || IsDisposed;
                if (!cancel)
                {
                    roots.Add(node);
                }
            }

            if (cancel)
            {
                node.Cancel();
            }
        }

        /// <summary>
        /// 从任务域移除已经完成的根任务。
        /// </summary>
        /// <param name="node">已经结束的根任务。</param>
        internal void RemoveRoot(MTaskNode node)
        {
            Action callback = null;
            lock (gate)
            {
                roots.Remove(node);
                if (roots.Count == 0)
                {
                    callback = drained;
                    drained = null;
                }
            }

            if (callback != null)
            {
                Post(callback);
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 在任务域已经为空时触发退场回调。
        /// </summary>
        private void InvokeDrainedIfReady()
        {
            Action callback = null;
            lock (gate)
            {
                if (roots.Count == 0)
                {
                    callback = drained;
                    drained = null;
                }
            }

            if (callback != null)
            {
                Post(callback);
            }
        }

        /// <summary>
        /// 在任务域执行器上派发回调。
        /// </summary>
        /// <param name="continuation">要执行的回调。</param>
        private void Post(Action continuation)
        {
            if (Executor.IsCurrentThread)
            {
                continuation();
                return;
            }

            Executor.Post(continuation);
        }

        #endregion
    }
}
