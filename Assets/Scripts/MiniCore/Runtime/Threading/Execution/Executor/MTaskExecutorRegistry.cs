using System.Collections.Generic;

namespace MiniCore.Threading
{
    /// <summary>
    /// 统一登记模块创建的有生命周期执行器，并在应用退出时提供兜底释放。
    /// </summary>
    internal static class MTaskExecutorRegistry
    {
        #region Private 私有成员

        private static readonly object Gate = new object(); // 保护执行器登记集合。
        private static readonly HashSet<IMTaskOwnedExecutor> Executors = new HashSet<IMTaskOwnedExecutor>(); // 当前尚未释放的模块执行器。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 登记一个刚创建的模块执行器。
        /// </summary>
        /// <param name="executor">需要纳入退出监管的执行器。</param>
        internal static void Register(IMTaskOwnedExecutor executor)
        {
            lock (Gate)
            {
                Executors.Add(executor);
            }
        }

        /// <summary>
        /// 移除已经完成正常释放的模块执行器。
        /// </summary>
        /// <param name="executor">已经结束生命周期的执行器。</param>
        internal static void Unregister(IMTaskOwnedExecutor executor)
        {
            lock (Gate)
            {
                Executors.Remove(executor);
            }
        }

        /// <summary>
        /// 依次释放所有仍登记的模块执行器，供应用退出兜底使用。
        /// </summary>
        internal static void DisposeAll()
        {
            while (true)
            {
                IMTaskOwnedExecutor executor;
                lock (Gate)
                {
                    using HashSet<IMTaskOwnedExecutor>.Enumerator enumerator = Executors.GetEnumerator();
                    if (!enumerator.MoveNext())
                    {
                        return;
                    }

                    executor = enumerator.Current;
                    Executors.Remove(executor);
                }

                executor.Dispose();
            }
        }

        #endregion
    }
}
