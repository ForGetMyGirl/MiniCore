using System;
using MiniCore.Model;
using MiniCore.Threading;
using MiniCore.Unity;
using UnityEngine.SceneManagement;
using YooAsset;

namespace MiniCore.Service
{

    /// <summary>
    /// 基于 YooAsset 的单场景切换服务，遵循 YooAsset 的 Single 场景句柄自动释放语义。
    /// </summary>
    [AppService("YooAsset 场景", typeof(ISceneService), Description = "以 Single 模式切换业务场景并管理 SceneHandle 生命周期。", InitArgsType = typeof(YooAssetSceneServiceInitArgs))]
    public sealed class YooAssetSceneService : AAppService, ISceneService
    {
        #region Private 私有成员

        private ResourcePackage package; // 当前绑定的 YooAsset 资源包。
        private SceneHandle currentHandle; // 当前业务场景句柄。
        private SceneHandle loadingHandle; // 正在加载的场景句柄。

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 获取当前场景加载进度，空闲时为一。
        /// </summary>
        public float Progress => loadingHandle == null ? 1f : loadingHandle.Progress;

        /// <summary>
        /// 场景加载进度变化事件。
        /// </summary>
        public event Action<float> ProgressChanged;

        /// <summary>
        /// 以 Single 模式加载并激活业务场景；旧场景句柄由 YooAsset 在场景卸载回调中自动释放。
        /// </summary>
        /// <param name="address">场景 YooAsset 地址。</param>
        /// <returns>场景加载完成任务。</returns>
        public async MTask LoadSingleAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("场景地址不能为空。", nameof(address));
            }

            if (loadingHandle != null)
            {
                throw new InvalidOperationException("已有场景正在加载，不能并发切换业务场景。");
            }

            SceneHandle handle = package.LoadSceneAsync(address.Trim(), LoadSceneMode.Single, LocalPhysicsMode.None, false);
            loadingHandle = handle;
            try
            {
                ProgressChanged?.Invoke(0f);
                while (!handle.IsDone)
                {
                    ProgressChanged?.Invoke(handle.Progress);
                    await MTask.Yield();
                }

                await handle.ToMTask();
                currentHandle = handle;
                ProgressChanged?.Invoke(1f);
            }
            catch
            {
                if (handle.IsValid)
                {
                    handle.Release();
                }

                throw;
            }
            finally
            {
                if (ReferenceEquals(loadingHandle, handle))
                {
                    loadingHandle = null;
                }
            }
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 绑定启动配置指定的 YooAsset 资源包。
        /// </summary>
        /// <param name="args">场景服务初始化参数。</param>
        public override void Awake(ComponentInitArgs args)
        {
            if (!(args is YooAssetSceneServiceInitArgs sceneArgs) || string.IsNullOrWhiteSpace(sceneArgs.PackageName))
            {
                throw new ArgumentException("YooAsset 场景服务初始化参数不正确。", nameof(args));
            }

            package = YooAssets.GetPackage(sceneArgs.PackageName);
            if (package == null)
            {
                throw new InvalidOperationException($"未找到 YooAsset 资源包：{sceneArgs.PackageName}。");
            }
        }

        /// <summary>
        /// 释放当前场景句柄引用和资源包引用。
        /// </summary>
        protected override void OnDispose()
        {
            SceneHandle loading = loadingHandle;
            SceneHandle current = currentHandle;
            if (loading != null && loading.IsValid)
            {
                loading.Release();
            }

            if (current != null && !ReferenceEquals(current, loading) && current.IsValid)
            {
                current.Release();
            }

            loadingHandle = null;
            currentHandle = null;
            package = null;
            ProgressChanged = null;
        }

        #endregion
    }
}
