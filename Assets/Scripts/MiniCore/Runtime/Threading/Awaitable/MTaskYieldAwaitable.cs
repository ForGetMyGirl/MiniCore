using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// MTask.Yield 返回的无分配等待对象。
    /// </summary>
    public readonly struct MTaskYieldAwaitable
    {
        #region Private 私有成员

        private readonly IMTaskExecutor executor; // 当前任务的续体执行器。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建让出等待对象。
        /// </summary>
        /// <param name="executor">续体执行器。</param>
        internal MTaskYieldAwaitable(IMTaskExecutor executor)
        {
            this.executor = executor;
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取让出操作 awaiter。
        /// </summary>
        /// <returns>让出操作 awaiter。</returns>
        public MTaskYieldAwaiter GetAwaiter()
        {
            return new MTaskYieldAwaiter(executor);
        }

        #endregion
    }
}
