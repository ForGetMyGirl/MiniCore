using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{

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
