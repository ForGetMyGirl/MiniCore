using System;
using MiniCore.Threading;
using YooAsset;

namespace MiniCore.Unity
{
    /// <summary>
    /// YooAsset 原生异步操作到 MTask 的零 TaskCompletionSource 适配。
    /// </summary>
    public static class YooAssetMTaskExtensions
    {
        #region Public 公共成员

        /// <summary>
        /// 将 YooAsset 通用异步操作转换为 MTask。
        /// </summary>
        /// <param name="operation">YooAsset 异步操作。</param>
        /// <returns>操作完成或当前 MTask 取消时结束的任务。</returns>
        public static MTask ToMTask(this AsyncOperationBase operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return operation.IsDone ? MTask.CompletedTask : YooOperationMTaskSource.Create(operation);
        }

        /// <summary>
        /// 将 YooAsset 资源句柄转换为 MTask。
        /// </summary>
        /// <param name="handle">资源加载句柄。</param>
        /// <returns>句柄完成或当前 MTask 取消时结束的任务。</returns>
        public static MTask ToMTask(this AssetHandle handle)
        {
            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            return handle.IsDone ? MTask.CompletedTask : YooAssetHandleMTaskSource.Create(handle);
        }

        /// <summary>
        /// 将 YooAsset 场景句柄转换为 MTask。
        /// </summary>
        /// <param name="handle">场景加载句柄。</param>
        /// <returns>句柄完成或当前 MTask 取消时结束的任务。</returns>
        public static MTask ToMTask(this SceneHandle handle)
        {
            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            return handle.IsDone ? MTask.CompletedTask : YooSceneHandleMTaskSource.Create(handle);
        }

        #endregion
    }

}
