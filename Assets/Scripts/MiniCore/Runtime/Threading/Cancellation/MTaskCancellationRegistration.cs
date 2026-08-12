using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{

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
}
