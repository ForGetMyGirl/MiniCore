using System;
using System.Collections.Concurrent;
using MiniCore.Threading;
using YooAsset;

namespace MiniCore.Unity
{

    /// <summary>
    /// YooAsset 通用异步操作的池化 MTask 适配源。
    /// </summary>
    internal sealed class YooOperationMTaskSource : YooMTaskSourceBase
    {
        #region Private 私有成员

        private static readonly ConcurrentStack<YooOperationMTaskSource> Pool = new ConcurrentStack<YooOperationMTaskSource>(); // 通用操作适配池。
        private readonly Action<AsyncOperationBase> completedAction; // YooAsset 完成事件回调。
        private AsyncOperationBase operation; // 当前等待的通用操作。

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 创建适配源并缓存事件委托。
        /// </summary>
        private YooOperationMTaskSource()
        {
            completedAction = OnCompleted;
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建通用 YooAsset 操作等待任务。
        /// </summary>
        /// <param name="value">待等待操作。</param>
        /// <returns>对应的 MTask。</returns>
        internal static MTask Create(AsyncOperationBase value)
        {
            if (!Pool.TryPop(out YooOperationMTaskSource source))
            {
                source = new YooOperationMTaskSource();
            }

            source.operation = value;
            source.Initialize();
            value.Completed += source.completedAction;
            return new MTask(source, source.Version);
        }

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 解除通用操作完成事件。
        /// </summary>
        protected override void Unsubscribe()
        {
            if (operation != null)
            {
                operation.Completed -= completedAction;
            }
        }

        /// <summary>
        /// 清理通用操作引用并归还池。
        /// </summary>
        protected override void ReturnToPool()
        {
            operation = null;
            Pool.Push(this);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 响应 YooAsset 通用操作完成。
        /// </summary>
        /// <param name="value">已完成操作。</param>
        private void OnCompleted(AsyncOperationBase value)
        {
            Unsubscribe();
            Complete();
        }

        #endregion
    }

}
