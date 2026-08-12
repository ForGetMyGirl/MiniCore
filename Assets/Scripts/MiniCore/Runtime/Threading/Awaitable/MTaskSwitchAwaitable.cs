using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// MTask.SwitchTo 返回的无分配等待对象。
    /// </summary>
    public readonly struct MTaskSwitchAwaitable
    {
        #region Private 私有成员

        private readonly IMTaskExecutor executor; // 目标执行器。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建执行器切换等待对象。
        /// </summary>
        /// <param name="executor">目标执行器。</param>
        internal MTaskSwitchAwaitable(IMTaskExecutor executor)
        {
            this.executor = executor;
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取执行器切换 awaiter。
        /// </summary>
        /// <returns>执行器切换 awaiter。</returns>
        public MTaskSwitchAwaiter GetAwaiter()
        {
            return new MTaskSwitchAwaiter(executor);
        }

        #endregion
    }
}
