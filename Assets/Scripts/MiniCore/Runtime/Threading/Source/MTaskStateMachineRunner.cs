using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
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
}
