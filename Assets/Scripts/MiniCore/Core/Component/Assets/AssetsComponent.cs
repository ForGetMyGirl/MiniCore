using Cysharp.Threading.Tasks;
using MiniCore.Model;
using System.Collections.Generic;
using UnityEngine;
/*using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;*/
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

namespace MiniCore.Core
{
    /// <summary>
    /// 资产管理器，依赖TagsComponent；
    /// 使用前必须先调用RegisterResourcesComponent(IResourcesComponent)方法注册资源加载组件
    /// </summary>
    public class AssetsComponent : AComponent
    {

        private TagsComponent tagsComponent;
        private IResourcesComponent resourcesComponent;
        private IResourcesComponent ResourcesComponent
        {
            get
            {
                if (resourcesComponent == null)
                {
                    EventCenter.Broadcast(GameEvent.LogError, "资源加载组件没有注册，请调用RegisterResourcesComponent(IResourcesComponent)方法注册资源加载组件");
                    throw new System.Exception("资源加载组件没有注册，请调用RegisterResourcesComponent(IResourcesComponent)方法注册资源加载组件");
                }
                return resourcesComponent;
            }
            set
            {
                resourcesComponent = value;
            }
        }

        public override void Awake()
        {
            tagsComponent = Global.Com.Get<TagsComponent>();
        }

        private Dictionary<string, Object> preloadAssets = new Dictionary<string, Object>();

        public void RegisterResourcesComponent(IResourcesComponent resourcesComponent)
        {
            this.ResourcesComponent = resourcesComponent;
        }

        /// <summary>
        /// 异步实例化对象
        /// </summary>
        /// <param name="key"></param>
        /// <param name="parent"></param>
        /// <param name="instantiateInWorldSpace"></param>
        /// <param name="trackHandle"></param>
        /// <returns></returns>
        public async UniTask<GameObject> InstantiateAsync(string key, Transform parent = null/*, bool instantiateInWorldSpace = false, bool trackHandle = true*/)
        {
            return await ResourcesComponent.InstantiateAsync(key, parent);
        }


        public async UniTask<GameObject> InstantiatePreloadAssetAsync(string key, Transform parent)
        {
            if (preloadAssets.TryGetValue(key, out Object value))
            {
                return await ResourcesComponent.InstantiateAsync(key, parent);
            }
            return null;
        }

        public async UniTask<T> PreloadAssetAsync<T>(string key) where T : Object
        {
            return await ResourcesComponent.PreloadAssetsAsync<T>(key);
        }

        /// <summary>
        /// 异步实例化最上层UI
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async UniTask<GameObject> InstantiateTopUIAsync(string key)
        {
            return await ResourcesComponent.InstantiateAsync(key, tagsComponent.TopCanvas);
        }


        /// <summary>
        /// 异步实例化主UI
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async UniTask<GameObject> InstantiateMainUIAsync(string key)
        {
            return await ResourcesComponent.InstantiateAsync(key, tagsComponent.MainCanvas);
        }

        /// <summary>
        /// 异步实例化到最底层UI
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async UniTask<GameObject> InstantiateBottomUIAsync(string key)
        {
            return await ResourcesComponent.InstantiateAsync(key, tagsComponent.BottomCanvas);
        }

        /// <summary>
        /// 异步加载图集
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async UniTask<SpriteAtlas> LoadSpriteAtlasAsync(string key)
        {
            return await ResourcesComponent.LoadAssetAsync<SpriteAtlas>(key);
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="key">资源地址</param>
        /// <returns></returns>
        public async UniTask<T> LoadAssetAsync<T>(string key) where T : Object
        {
            return await ResourcesComponent.LoadAssetAsync<T>(key);
        }


        public bool ReleaseGameObject(GameObject gameObject)
        {
            return ResourcesComponent.ReleaseInstance(gameObject);
        }

        public bool ReleaseAssetAsync(string key)
        {
            return ResourcesComponent.ReleaseAssetAsync(key);
        }

    }

}