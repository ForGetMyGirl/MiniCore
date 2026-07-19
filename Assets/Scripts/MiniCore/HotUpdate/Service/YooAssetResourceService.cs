using System.Collections.Generic;
using MiniCore.Threading;
using MiniCore.Model;
using MiniCore.Unity;
using UnityEngine;
using YooAsset;

namespace MiniCore.Service
{
    /// <summary>
    /// 基于 YooAsset 的资源加载服务。
    /// 服务创建后绑定一个资源包，并缓存主动预加载的资源句柄。
    /// </summary>
    [AppService("YooAsset 资源", typeof(IResourceService), Description = "基于 YooAsset 加载、预加载、实例化和释放资源。", InitArgsType = typeof(YooAssetResourceServiceInitArgs))]
    public sealed class YooAssetResourceService : AAppService, IResourceService
    {
        #region Private 私有成员

        private ResourcePackage package; // 当前组件绑定的 YooAsset 资源包。
        private readonly Dictionary<string, AssetHandle> loadedAssets = new Dictionary<string, AssetHandle>(); // 已预加载资源的句柄缓存。

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 同步实例化已加载的资源对象。
        /// 资源未预加载时会先完成对应资源的异步加载。
        /// </summary>
        /// <param name="key">资源地址或资源键。</param>
        /// <param name="parent">实例化对象的父节点。</param>
        /// <returns>实例化完成的游戏对象。</returns>
        public async MTask<GameObject> InstantiateAsync(string key, Transform parent = null)
        {
            if (!loadedAssets.ContainsKey(key))
            {
                await PreloadAssetAsync<GameObject>(key);
            }

            return loadedAssets[key].InstantiateSync(parent);
        }

        /// <summary>
        /// 异步加载指定类型的资源。
        /// 此方法不将句柄加入预加载缓存，由调用方自行决定后续生命周期。
        /// </summary>
        /// <typeparam name="T">资源对象类型。</typeparam>
        /// <param name="key">资源地址或资源键。</param>
        /// <returns>加载完成的资源对象。</returns>
        public async MTask<T> LoadAssetAsync<T>(string key) where T : Object
        {
            AssetHandle handle = package.LoadAssetAsync<T>(key);
            await handle.ToMTask();
            return handle.AssetObject as T;
        }

        /// <summary>
        /// 异步预加载资源并缓存资源句柄。
        /// 对同一键重复调用会直接返回缓存的资源对象。
        /// </summary>
        /// <typeparam name="T">资源对象类型。</typeparam>
        /// <param name="key">资源地址或资源键。</param>
        /// <returns>预加载完成的资源对象。</returns>
        public async MTask<T> PreloadAssetAsync<T>(string key) where T : Object
        {
            if (!loadedAssets.TryGetValue(key, out AssetHandle handle))
            {
                handle = package.LoadAssetAsync<T>(key);
                await handle.ToMTask();
                loadedAssets.Add(key, handle);
            }

            return handle.AssetObject as T;
        }

        /// <summary>
        /// 释放指定预加载资源的句柄。
        /// </summary>
        /// <param name="key">要释放的资源地址或资源键。</param>
        /// <returns>存在并释放资源句柄时返回 true；否则返回 false。</returns>
        public bool ReleaseAsset(string key)
        {
            if (!loadedAssets.TryGetValue(key, out AssetHandle handle))
            {
                return false;
            }

            handle.Release();
            loadedAssets.Remove(key);
            return true;
        }

        /// <summary>
        /// 销毁一个已实例化的游戏对象。
        /// </summary>
        /// <param name="obj">要销毁的游戏对象。</param>
        /// <returns>销毁请求提交成功时返回 true。</returns>
        public bool ReleaseInstance(GameObject obj)
        {
            Object.Destroy(obj);
            return true;
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 释放服务缓存的预加载资源句柄。
        /// 该方法由租约归零或 Global 销毁触发，避免资源服务卸载后残留句柄。
        /// </summary>
        protected override void OnDispose()
        {
            foreach (AssetHandle handle in loadedAssets.Values)
            {
                handle.Release();
            }

            loadedAssets.Clear();
        }

        /// <summary>
        /// 使用资源包参数初始化 YooAsset 资源服务。
        /// </summary>
        /// <param name="args">包含资源包名称的初始化参数。</param>
        public override void Awake(ComponentInitArgs args)
        {
            if (args is not YooAssetResourceServiceInitArgs resourceArgs)
            {
                throw new System.ArgumentException("YooAsset 资源服务初始化参数类型不正确。", nameof(args));
            }

            if (string.IsNullOrWhiteSpace(resourceArgs.PackageName))
            {
                throw new System.ArgumentException("YooAsset 资源包名称不能为空。", nameof(args));
            }

            package = YooAssets.GetPackage(resourceArgs.PackageName);
            if (package == null)
            {
                throw new System.InvalidOperationException($"未找到 YooAsset 资源包：{resourceArgs.PackageName}。");
            }
        }

        #endregion
    }
}
