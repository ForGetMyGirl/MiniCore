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
}
