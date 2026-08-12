using System;
using System.Collections.Concurrent;
using MiniCore.Threading;
using YooAsset;

namespace MiniCore.Unity
{

    /// <summary>
    /// YooAsset 场景句柄的池化适配源。
    /// </summary>
    internal sealed class YooSceneHandleMTaskSource : YooMTaskSourceBase
    {
        #region Private 私有成员

        private static readonly ConcurrentStack<YooSceneHandleMTaskSource> Pool = new ConcurrentStack<YooSceneHandleMTaskSource>(); // 场景句柄适配池。
        private readonly Action<SceneHandle> completedAction; // 场景句柄完成回调。
        private SceneHandle handle; // 当前等待的场景句柄。

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 创建场景句柄适配源并缓存事件委托。
        /// </summary>
        private YooSceneHandleMTaskSource()
        {
            completedAction = OnCompleted;
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建场景句柄等待任务。
        /// </summary>
        /// <param name="value">待等待句柄。</param>
        /// <returns>对应的 MTask。</returns>
        internal static MTask Create(SceneHandle value)
        {
            if (!Pool.TryPop(out YooSceneHandleMTaskSource source))
            {
                source = new YooSceneHandleMTaskSource();
            }

            source.handle = value;
            source.Initialize();
            value.Completed += source.completedAction;
            return new MTask(source, source.Version);
        }

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 解除场景句柄完成事件。
        /// </summary>
        protected override void Unsubscribe()
        {
            if (handle != null)
            {
                handle.Completed -= completedAction;
            }
        }

        /// <summary>
        /// 清理场景句柄引用并归还池。
        /// </summary>
        protected override void ReturnToPool()
        {
            handle = null;
            Pool.Push(this);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 响应场景句柄完成。
        /// </summary>
        /// <param name="value">已完成句柄。</param>
        private void OnCompleted(SceneHandle value)
        {
            Unsubscribe();
            Complete();
        }

        #endregion
    }

}
