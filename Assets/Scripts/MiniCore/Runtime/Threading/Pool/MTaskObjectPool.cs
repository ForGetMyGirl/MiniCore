using System;
using System.Collections.Generic;
using System.Threading;

namespace MiniCore.Threading
{

    /// <summary>
    /// 为每个具体类型提供有容量上限的无闭包共享对象池。
    /// </summary>
    /// <typeparam name="T">池化引用类型。</typeparam>
    internal static class MTaskObjectPool<T> where T : class
    {
        #region Private 私有成员

        private static readonly object Gate = new object(); // 保护数组栈，避免 ConcurrentStack 回池节点分配。
        private static readonly Stack<T> Pool = new Stack<T>(16); // 当前具体类型的共享池。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 尝试从当前类型池中获取对象。
        /// </summary>
        /// <param name="value">成功获取的对象。</param>
        /// <returns>命中对象池时返回 true。</returns>
        internal static bool TryRent(out T value)
        {
            lock (Gate)
            {
                if (Pool.Count > 0)
                {
                    value = Pool.Pop();
                    MTaskDiagnostics.OnPoolHit();
                    return true;
                }
            }

            value = null;
            MTaskDiagnostics.OnPoolExpansion();
            return false;
        }

        /// <summary>
        /// 在容量限制内将对象归还当前类型池。
        /// </summary>
        /// <param name="value">已清理的对象。</param>
        internal static void Return(T value)
        {
            lock (Gate)
            {
                if (Pool.Count < MTaskDiagnostics.MaxRetainedPerType)
                {
                    Pool.Push(value);
                    return;
                }
            }

            MTaskDiagnostics.OnPoolRecycleFailure();
        }

        #endregion
    }
}
