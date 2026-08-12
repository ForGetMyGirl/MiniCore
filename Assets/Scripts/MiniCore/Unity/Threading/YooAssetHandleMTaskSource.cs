using System;
using System.Collections.Concurrent;
using MiniCore.Threading;
using YooAsset;

namespace MiniCore.Unity
{

    /// <summary>
    /// YooAsset 资源句柄的池化适配源。
    /// </summary>
    internal sealed class YooAssetHandleMTaskSource : YooMTaskSourceBase
    {
        #region Private 私有成员

        private static readonly ConcurrentStack<YooAssetHandleMTaskSource> Pool = new ConcurrentStack<YooAssetHandleMTaskSource>(); // 资源句柄适配池。
        private readonly Action<AssetHandle> completedAction; // 资源句柄完成回调。
        private AssetHandle handle; // 当前等待的资源句柄。

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 创建资源句柄适配源并缓存事件委托。
        /// </summary>
        private YooAssetHandleMTaskSource()
        {
            completedAction = OnCompleted;
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建资源句柄等待任务。
        /// </summary>
        /// <param name="value">待等待句柄。</param>
        /// <returns>对应的 MTask。</returns>
        internal static MTask Create(AssetHandle value)
        {
            if (!Pool.TryPop(out YooAssetHandleMTaskSource source))
            {
                source = new YooAssetHandleMTaskSource();
            }

            source.handle = value;
            source.Initialize();
            value.Completed += source.completedAction;
            return new MTask(source, source.Version);
        }

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 解除资源句柄完成事件。
        /// </summary>
        protected override void Unsubscribe()
        {
            if (handle != null)
            {
                handle.Completed -= completedAction;
            }
        }

        /// <summary>
        /// 清理资源句柄引用并归还池。
        /// </summary>
        protected override void ReturnToPool()
        {
            handle = null;
            Pool.Push(this);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 响应资源句柄完成。
        /// </summary>
        /// <param name="value">已完成句柄。</param>
        private void OnCompleted(AssetHandle value)
        {
            Unsubscribe();
            Complete();
        }

        #endregion
    }

}
