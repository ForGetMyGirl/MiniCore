using MiniCore.Threading;
using MiniCore.Core;
using MiniCore.Model;
using UnityEngine;
using UnityEngine.U2D;

namespace MiniCore.Service
{
    /// <summary>
    /// 项目资产加载与实例化服务。
    /// 服务通过资源服务和场景绑定服务完成资源实例化与 UI 层级放置。
    /// </summary>
    [AppService(
        "资产管理",
        typeof(IAssetService),
        Description = "整合资源加载与场景绑定，管理项目资产预加载和实例化。",
        RequiresServices = new[] { typeof(IResourceService), typeof(ISceneBindingService) })]
    public sealed class AssetService : AAppService, IAssetService
    {
        #region Private 私有成员

        private ISceneBindingService sceneBindings; // 场景绑定服务。
        private IResourceService resourceService; // YooAsset 资源服务。
        private readonly System.Collections.Generic.Dictionary<string, Object> preloadAssets = new System.Collections.Generic.Dictionary<string, Object>(); // 已预加载资源缓存。

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 获取资产服务所需的资源与场景绑定服务。
        /// </summary>
        public override void Awake()
        {
            sceneBindings = Global.GetService<ISceneBindingService>(this);
            resourceService = Global.GetService<IResourceService>(this);
        }

        /// <summary>
        /// 释放当前服务持有的全局服务引用与预加载缓存。
        /// </summary>
        protected override void OnDispose()
        {
            preloadAssets.Clear();
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 异步实例化对象。
        /// </summary>
        /// <param name="key">资源地址或资源键。</param>
        /// <param name="parent">实例化对象的父节点。</param>
        /// <returns>实例化完成的游戏对象。</returns>
        public MTask<GameObject> InstantiateAsync(string key, Transform parent = null)
        {
            return ResourceService.InstantiateAsync(key, parent);
        }

        /// <summary>
        /// 异步实例化已经预加载的对象。
        /// </summary>
        /// <param name="key">预加载资源键。</param>
        /// <param name="parent">实例化对象的父节点。</param>
        /// <returns>资源已预加载时返回实例化对象；否则返回 null。</returns>
        public MTask<GameObject> InstantiatePreloadAssetAsync(string key, Transform parent)
        {
            if (preloadAssets.TryGetValue(key, out Object _))
            {
                return ResourceService.InstantiateAsync(key, parent);
            }

            return MTask.FromResult<GameObject>(null);
        }

        /// <summary>
        /// 异步预加载资源。
        /// </summary>
        /// <typeparam name="T">资源对象类型。</typeparam>
        /// <param name="key">资源地址或资源键。</param>
        /// <returns>预加载完成的资源对象。</returns>
        public async MTask<T> PreloadAssetAsync<T>(string key) where T : Object
        {
            T asset = await ResourceService.PreloadAssetAsync<T>(key);
            preloadAssets[key] = asset;
            return asset;
        }

        /// <summary>
        /// 异步实例化顶层 UI。
        /// </summary>
        /// <param name="key">UI 资源地址或资源键。</param>
        /// <returns>实例化完成的游戏对象。</returns>
        public MTask<GameObject> InstantiateTopUIAsync(string key)
        {
            return ResourceService.InstantiateAsync(key, SceneBindings.TopCanvas);
        }


        /// <summary>
        /// 异步实例化主 UI。
        /// </summary>
        /// <param name="key">UI 资源地址或资源键。</param>
        /// <returns>实例化完成的游戏对象。</returns>
        public MTask<GameObject> InstantiateMainUIAsync(string key)
        {
            return ResourceService.InstantiateAsync(key, SceneBindings.MainCanvas);
        }

        /// <summary>
        /// 异步实例化底层 UI。
        /// </summary>
        /// <param name="key">UI 资源地址或资源键。</param>
        /// <returns>实例化完成的游戏对象。</returns>
        public MTask<GameObject> InstantiateBottomUIAsync(string key)
        {
            return ResourceService.InstantiateAsync(key, SceneBindings.BottomCanvas);
        }

        /// <summary>
        /// 异步加载图集。
        /// </summary>
        /// <param name="key">图集资源地址或资源键。</param>
        /// <returns>加载完成的图集。</returns>
        public MTask<SpriteAtlas> LoadSpriteAtlasAsync(string key)
        {
            return ResourceService.LoadAssetAsync<SpriteAtlas>(key);
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <typeparam name="T">资源对象类型。</typeparam>
        /// <param name="key">资源地址或资源键。</param>
        /// <returns>加载完成的资源对象。</returns>
        public MTask<T> LoadAssetAsync<T>(string key) where T : Object
        {
            return ResourceService.LoadAssetAsync<T>(key);
        }

        /// <summary>
        /// 销毁指定游戏对象。
        /// </summary>
        /// <param name="gameObject">要销毁的游戏对象。</param>
        /// <returns>销毁请求提交成功时返回 true。</returns>
        public bool ReleaseGameObject(GameObject gameObject)
        {
            return ResourceService.ReleaseInstance(gameObject);
        }

        /// <summary>
        /// 释放指定预加载资源。
        /// </summary>
        /// <param name="key">要释放的资源地址或资源键。</param>
        /// <returns>存在并释放资源时返回 true。</returns>
        public bool ReleaseAsset(string key)
        {
            preloadAssets.Remove(key);
            return ResourceService.ReleaseAsset(key);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 获取已初始化的场景绑定服务。
        /// </summary>
        /// <returns>场景绑定服务。</returns>
        private ISceneBindingService SceneBindings => sceneBindings ?? throw new System.InvalidOperationException("场景绑定服务尚未初始化。");

        /// <summary>
        /// 获取已初始化的资源服务。
        /// </summary>
        /// <returns>资源服务。</returns>
        private IResourceService ResourceService => resourceService ?? throw new System.InvalidOperationException("资源服务尚未初始化。");

        #endregion
    }

}
