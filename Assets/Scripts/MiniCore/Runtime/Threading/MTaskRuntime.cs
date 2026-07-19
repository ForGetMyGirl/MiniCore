using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// MTask 生命周期持有者，由组件、模块和 Unity 对象实现。
    /// </summary>
    public interface IMTaskOwner
    {
        /// <summary>
        /// 获取当前对象的任务生命周期域。
        /// </summary>
        /// <returns>当前对象使用的任务域。</returns>
        MTaskDomain GetMTaskDomain();
    }

    /// <summary>
    /// 标记需要由编译后处理器注入 MTask 生命周期的类型。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class MTaskOwnerAttribute : Attribute
    {
    }

    /// <summary>
    /// MTask 的协作式取消异常；同一任务域复用一个实例以避免取消风暴产生大量垃圾。
    /// </summary>
    public sealed class MTaskCanceledException : OperationCanceledException
    {
        #region Internal 内部成员

        /// <summary>
        /// 创建指定任务域使用的取消异常。
        /// </summary>
        /// <param name="domainName">任务域诊断名称。</param>
        internal MTaskCanceledException(string domainName)
            : base($"MTask 任务域已取消：{domainName}")
        {
        }

        #endregion
    }

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

    /// <summary>
    /// MTask 未观察异常的统一监督器。
    /// </summary>
    public static class MTaskSupervisor
    {
        #region Private 私有成员

        private static readonly object OrphanGate = new object(); // 保护已上报的孤立任务类型集合。
        private static readonly HashSet<Type> ReportedOrphanTypes = new HashSet<Type>(); // 每种结果源类型只警告一次，避免开发环境刷屏和稳态分配。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 当 Forget 任务出现未处理异常时触发。
        /// </summary>
        public static event Action<Exception, string> UnhandledException;

        /// <summary>
        /// 当任务无法找到父节点或 Owner 而挂到应用根域时触发。
        /// </summary>
        public static event Action<string> OrphanedTask;

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 上报一个未观察的后台任务异常。
        /// </summary>
        /// <param name="exception">任务抛出的异常。</param>
        /// <param name="ownerName">所属 Owner 的诊断名称。</param>
        internal static void Report(Exception exception, string ownerName)
        {
            Action<Exception, string> handler = UnhandledException;
            if (handler != null)
            {
                handler(exception, ownerName);
                return;
            }

            Console.Error.WriteLine($"[MTask] 未处理异常 owner:{ownerName}\n{exception}");
        }

        /// <summary>
        /// 上报一个无父节点且无 Owner 的应用根任务。
        /// </summary>
        /// <param name="taskType">任务结果源类型。</param>
        internal static void ReportOrphan(Type taskType)
        {
            lock (OrphanGate)
            {
                if (!ReportedOrphanTypes.Add(taskType))
                {
                    return;
                }
            }

            string taskName = taskType.FullName;
            Action<string> handler = OrphanedTask;
            if (handler != null)
            {
                handler(taskName);
                return;
            }

            Console.Error.WriteLine($"[MTask] 任务未找到父节点或 Owner，已挂到应用根域：{taskName}");
        }

        #endregion
    }

    /// <summary>
    /// MTask 运行时上下文，负责父子节点、Owner 和执行器传播。
    /// </summary>
    public static class MTaskRuntime
    {
        #region Private 私有成员

        [ThreadStatic]
        private static MTaskNode currentNode; // 当前正在执行的任务节点。

        [ThreadStatic]
        private static IMTaskExecutor currentExecutor; // 当前续体执行器。

        [ThreadStatic]
        private static IMTaskOwner currentOwner; // 当前同步入口所属 Owner。

        private static readonly ConditionalWeakTable<object, MTaskExternalOwner> ExternalOwners = new ConditionalWeakTable<object, MTaskExternalOwner>(); // 仅标记特性的普通对象 Owner 域。
        private static MTaskDomain applicationDomain; // 无显式 Owner 时使用的应用根域。
        private static int fastShutdownRequested; // 是否已进入不等待任务退场的快速退出阶段。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取当前线程正在执行的 MTask 执行器。
        /// </summary>
        public static IMTaskExecutor CurrentExecutor => currentExecutor;

        /// <summary>
        /// 获取运行时是否已进入快速退出阶段。
        /// </summary>
        public static bool IsFastShutdown => Volatile.Read(ref fastShutdownRequested) != 0;

        /// <summary>
        /// 获取应用根任务域。
        /// </summary>
        public static MTaskDomain ApplicationDomain
        {
            get
            {
                if (applicationDomain == null)
                {
                    applicationDomain = new MTaskDomain("Application", currentExecutor ?? MTaskExecutors.Inline);
                    if (IsFastShutdown)
                    {
                        applicationDomain.Dispose();
                    }
                }

                return applicationDomain;
            }
        }

        /// <summary>
        /// 使用主执行器初始化应用根任务域。
        /// </summary>
        /// <param name="executor">应用默认执行器。</param>
        public static void Initialize(IMTaskExecutor executor)
        {
            if (executor == null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            Volatile.Write(ref fastShutdownRequested, 0);
            currentExecutor = executor;
            applicationDomain?.Dispose();
            applicationDomain = new MTaskDomain("Application", executor);
        }

        /// <summary>
        /// 进入快速退出阶段，取消应用根任务并拒绝后续根任务继续运行。
        /// </summary>
        /// <remarks>
        /// 此方法不等待 finally、线程或外部 I/O；宿主应在进程退出或停止 Play Mode 前调用。
        /// </remarks>
        public static void BeginFastShutdown()
        {
            if (Interlocked.Exchange(ref fastShutdownRequested, 1) != 0)
            {
                return;
            }

            applicationDomain?.Dispose();
        }

        /// <summary>
        /// 取消应用根域并清理当前线程上下文。
        /// </summary>
        public static void Shutdown()
        {
            BeginFastShutdown();
            currentNode = null;
            currentOwner = null;
            currentExecutor = null;
        }

        /// <summary>
        /// 请求取消无显式 Owner 的应用根任务，供宿主在抽干执行器前调用。
        /// </summary>
        public static void CancelApplicationTasks()
        {
            applicationDomain?.Cancel();
        }

        /// <summary>
        /// 在同步入口执行期间临时绑定一个任务 Owner。
        /// </summary>
        /// <param name="owner">同步入口所属 Owner；可以是 IMTaskOwner 或只标记 MTaskOwnerAttribute 的对象。</param>
        /// <returns>离开入口时需要释放的上下文令牌。</returns>
        public static MTaskOwnerContext EnterOwner(object owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            IMTaskOwner previous = currentOwner;
            currentOwner = owner as IMTaskOwner ?? ExternalOwners.GetValue(owner, CreateExternalOwner);
            return new MTaskOwnerContext(previous);
        }

        /// <summary>
        /// 取消并移除仅由 MTaskOwnerAttribute 绑定的外部 Owner 任务域。
        /// </summary>
        /// <param name="owner">即将销毁的 Owner 对象。</param>
        public static void DisposeOwner(object owner)
        {
            if (owner == null || owner is IMTaskOwner)
            {
                return;
            }

            if (ExternalOwners.TryGetValue(owner, out MTaskExternalOwner externalOwner))
            {
                ExternalOwners.Remove(owner);
                externalOwner.Dispose();
            }
        }

        /// <summary>
        /// 为当前任务节点注册一个原生等待操作的取消回调。
        /// </summary>
        /// <param name="continuation">当前节点取消时执行的回调。</param>
        /// <returns>解除本次取消回调的注册句柄。</returns>
        public static MTaskCancellationRegistration RegisterCancellation(Action continuation)
        {
            MTaskNode node = currentNode;
            node?.SetCancellationContinuation(continuation);
            return new MTaskCancellationRegistration(node, continuation);
        }

        /// <summary>
        /// 获取当前节点或应用根域复用的取消异常。
        /// </summary>
        /// <returns>当前结构化任务使用的取消异常。</returns>
        public static MTaskCanceledException GetCancellationException()
        {
            return currentNode?.Domain.CancellationException ?? ApplicationDomain.CancellationException;
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 获取当前结构化父任务节点。
        /// </summary>
        internal static MTaskNode CurrentNode => currentNode;

        /// <summary>
        /// 获取当前同步入口 Owner。
        /// </summary>
        internal static IMTaskOwner CurrentOwner => currentOwner;

        /// <summary>
        /// 获取新建根任务是否必须立即取消。
        /// </summary>
        internal static bool ShouldCancelNewRoot => IsFastShutdown;

        /// <summary>
        /// 进入指定任务节点的状态机执行上下文。
        /// </summary>
        /// <param name="node">即将执行的任务节点。</param>
        /// <returns>离开状态机时需要恢复的上下文。</returns>
        internal static MTaskExecutionContext EnterNode(MTaskNode node)
        {
            MTaskExecutionContext context = new MTaskExecutionContext(currentNode, currentExecutor, currentOwner);
            currentNode = node;
            currentExecutor = node.Executor;
            currentOwner = node.Owner;
            return context;
        }

        /// <summary>
        /// 恢复进入任务节点前的运行时上下文。
        /// </summary>
        /// <param name="context">之前保存的上下文。</param>
        internal static void ExitNode(MTaskExecutionContext context)
        {
            currentNode = context.Node;
            currentExecutor = context.Executor;
            currentOwner = context.Owner;
        }

        /// <summary>
        /// 修改当前节点后续执行使用的执行器。
        /// </summary>
        /// <param name="executor">新的续体执行器。</param>
        internal static void SwitchCurrentExecutor(IMTaskExecutor executor)
        {
            if (currentNode != null)
            {
                currentNode.Executor = executor;
            }

            currentExecutor = executor;
        }

        /// <summary>
        /// 恢复同步入口之前的 Owner。
        /// </summary>
        /// <param name="owner">之前保存的 Owner。</param>
        internal static void RestoreOwner(IMTaskOwner owner)
        {
            currentOwner = owner;
        }

        /// <summary>
        /// 为首次进入的特性 Owner 创建弱关联任务域适配器。
        /// </summary>
        /// <param name="owner">只标记特性的 Owner 对象。</param>
        /// <returns>对应的内部 Owner 适配器。</returns>
        private static MTaskExternalOwner CreateExternalOwner(object owner)
        {
            MTaskExternalOwner externalOwner = new MTaskExternalOwner(owner.GetType().FullName, currentExecutor ?? MTaskExecutors.Unity);
            if (IsFastShutdown)
            {
                externalOwner.Dispose();
            }

            return externalOwner;
        }

        #endregion
    }

    /// <summary>
    /// 为只标记 MTaskOwnerAttribute 的对象提供弱关联任务域。
    /// </summary>
    internal sealed class MTaskExternalOwner : IMTaskOwner, IDisposable
    {
        #region Private 私有成员

        private readonly MTaskDomain domain; // 与外部对象生命周期绑定的任务域。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 使用对象类型名称和当前执行器创建外部 Owner。
        /// </summary>
        /// <param name="name">Owner 诊断名称。</param>
        /// <param name="executor">Owner 默认执行器。</param>
        internal MTaskExternalOwner(string name, IMTaskExecutor executor)
        {
            domain = new MTaskDomain(name, executor);
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 获取外部 Owner 的任务生命周期域。
        /// </summary>
        /// <returns>外部 Owner 任务域。</returns>
        public MTaskDomain GetMTaskDomain()
        {
            return domain;
        }

        /// <summary>
        /// 取消外部 Owner 名下全部任务。
        /// </summary>
        public void Dispose()
        {
            domain.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// 原生异步适配器注册到当前 MTask 节点的取消回调句柄。
    /// </summary>
    public readonly struct MTaskCancellationRegistration : IDisposable
    {
        #region Private 私有成员

        private readonly MTaskNode node; // 注册取消回调的任务节点。
        private readonly Action continuation; // 已注册的取消回调。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建取消回调注册句柄。
        /// </summary>
        /// <param name="node">当前任务节点。</param>
        /// <param name="continuation">取消回调。</param>
        internal MTaskCancellationRegistration(MTaskNode node, Action continuation)
        {
            this.node = node;
            this.continuation = continuation;
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 从任务节点解除本次取消回调。
        /// </summary>
        public void Dispose()
        {
            node?.ClearCancellationContinuation(continuation);
        }

        #endregion
    }

    /// <summary>
    /// 同步 Owner 入口的栈式恢复令牌。
    /// </summary>
    public readonly struct MTaskOwnerContext : IDisposable
    {
        #region Private 私有成员

        private readonly IMTaskOwner previous; // 进入当前 Owner 前的上下文。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建 Owner 上下文恢复令牌。
        /// </summary>
        /// <param name="previous">进入前的 Owner。</param>
        internal MTaskOwnerContext(IMTaskOwner previous)
        {
            this.previous = previous;
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 恢复进入 Owner 之前的同步上下文。
        /// </summary>
        public void Dispose()
        {
            MTaskRuntime.RestoreOwner(previous);
        }

        #endregion
    }

    /// <summary>
    /// 任务状态机执行期间保存的线程上下文。
    /// </summary>
    internal readonly struct MTaskExecutionContext
    {
        #region Internal 内部成员

        internal readonly MTaskNode Node; // 进入前的任务节点。
        internal readonly IMTaskExecutor Executor; // 进入前的执行器。
        internal readonly IMTaskOwner Owner; // 进入前的 Owner。

        /// <summary>
        /// 保存当前线程的 MTask 上下文。
        /// </summary>
        /// <param name="node">进入前的任务节点。</param>
        /// <param name="executor">进入前的执行器。</param>
        /// <param name="owner">进入前的 Owner。</param>
        internal MTaskExecutionContext(MTaskNode node, IMTaskExecutor executor, IMTaskOwner owner)
        {
            Node = node;
            Executor = executor;
            Owner = owner;
        }

        #endregion
    }
}
