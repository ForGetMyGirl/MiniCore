using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{

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
}
