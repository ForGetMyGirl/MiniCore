using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{

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
            MTaskExecutorRegistry.DisposeAll();
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
}
