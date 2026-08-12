using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MiniCore.Threading
{

    /// <summary>
    /// MTaskWorkQueue 使用的可复用链表节点。
    /// </summary>
    internal sealed class MTaskWorkItem
    {
        #region Private 私有成员

        private Action continuation; // 当前节点携带的续体。

        #endregion

        #region Internal 内部成员

        internal MTaskWorkItem Next; // 队列中的下一个节点。

        /// <summary>
        /// 从共享池中获取工作节点并绑定续体。
        /// </summary>
        /// <param name="value">待执行续体。</param>
        /// <returns>初始化后的工作节点。</returns>
        internal static MTaskWorkItem Rent(Action value)
        {
            if (!MTaskObjectPool<MTaskWorkItem>.TryRent(out MTaskWorkItem item))
            {
                item = new MTaskWorkItem();
            }

            item.continuation = value;
            item.Next = null;
            return item;
        }

        /// <summary>
        /// 取出续体并清除节点对业务对象的引用。
        /// </summary>
        /// <returns>节点中的续体。</returns>
        internal Action TakeContinuation()
        {
            Action value = continuation;
            continuation = null;
            return value;
        }

        #endregion
    }
}
