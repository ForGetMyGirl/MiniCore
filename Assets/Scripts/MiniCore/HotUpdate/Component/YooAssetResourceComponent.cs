using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MiniCore.Model;
using UnityEngine;
using YooAsset;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// 基于 YooAsset 的资源加载组件。
    /// 组件创建后绑定一个资源包，并缓存主动预加载的资源句柄。
    /// </summary>
    public class YooAssetResourceComponent : AComponent<YooAssetResourceComponentInitArgs>, IResourcesComponent
    {
        #region Private 私有成员

        private ResourcePackage package; // 当前组件绑定的 YooAsset 资源包。
        private readonly Dictionary<string, AssetHandle> loadedAssets = new Dictionary<string, AssetHandle>(); // 已预加载资源的句柄缓存。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 同步实例化已加载的资源对象。
        /// 资源未预加载时会先完成对应资源的异步加载。
        /// </summary>
        /// <param name="key">资源地址或资源键。</param>
        /// <param name="parent">实例化对象的父节点。</param>
        /// <returns>实例化完成的游戏对象。</returns>
        public async UniTask<GameObject> InstantiateAsync(string key, Transform parent = null)
        {
            if (!loadedAssets.ContainsKey(key))
            {
                await PreloadAssetsAsync<GameObject>(key);
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
        public async UniTask<T> LoadAssetAsync<T>(string key) where T : Object
        {
            AssetHandle handle = package.LoadAssetAsync<T>(key);
            await handle.Task;
            return handle.AssetObject as T;
        }

        /// <summary>
        /// 异步预加载资源并缓存资源句柄。
        /// 对同一键重复调用会直接返回缓存的资源对象。
        /// </summary>
        /// <typeparam name="T">资源对象类型。</typeparam>
        /// <param name="key">资源地址或资源键。</param>
        /// <returns>预加载完成的资源对象。</returns>
        public async UniTask<T> PreloadAssetsAsync<T>(string key) where T : Object
        {
            if (!loadedAssets.TryGetValue(key, out AssetHandle handle))
            {
                handle = package.LoadAssetAsync<T>(key);
                await handle.Task;
                loadedAssets.Add(key, handle);
            }

            return handle.AssetObject as T;
        }

        /// <summary>
        /// 释放指定预加载资源的句柄。
        /// </summary>
        /// <param name="key">要释放的资源地址或资源键。</param>
        /// <returns>存在并释放资源句柄时返回 true；否则返回 false。</returns>
        public bool ReleaseAssetAsync(string key)
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
        /// 释放组件缓存的预加载资源句柄。
        /// 该方法由租约归零或 Global 销毁触发，避免资源组件卸载后残留句柄。
        /// </summary>
        public override void Dispose()
        {
            foreach (AssetHandle handle in loadedAssets.Values)
            {
                handle.Release();
            }

            loadedAssets.Clear();
            base.Dispose();
        }

        /// <summary>
        /// 使用资源包参数初始化 YooAsset 资源组件。
        /// </summary>
        /// <param name="args">包含资源包名称的初始化参数。</param>
        protected override void Awake(YooAssetResourceComponentInitArgs args)
        {
            package = YooAssets.GetPackage(args.PackageName);
        }

        #endregion
    }
}
