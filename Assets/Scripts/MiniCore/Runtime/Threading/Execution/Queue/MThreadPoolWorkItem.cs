#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MiniCore.Threading
{

    /// <summary>
    /// CLR 线程池执行器使用的可复用工作项。
    /// </summary>
    internal sealed class MThreadPoolWorkItem
    {
        #region Private 私有成员

        private Action continuation; // 当前工作项持有的续体。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 从共享池中获取工作项。
        /// </summary>
        /// <param name="value">需要在线程池执行的续体。</param>
        /// <returns>已绑定续体的工作项。</returns>
        internal static MThreadPoolWorkItem Rent(Action value)
        {
            if (!MTaskObjectPool<MThreadPoolWorkItem>.TryRent(out MThreadPoolWorkItem item))
            {
                item = new MThreadPoolWorkItem();
            }

            item.continuation = value;
            return item;
        }

        /// <summary>
        /// 调用当前工作项绑定的续体。
        /// </summary>
        internal void Invoke()
        {
            continuation?.Invoke();
        }

        /// <summary>
        /// 清理对业务续体的引用并回收到共享池。
        /// </summary>
        internal void Return()
        {
            continuation = null;
            MTaskObjectPool<MThreadPoolWorkItem>.Return(this);
        }

        #endregion
    }
}
#endif
