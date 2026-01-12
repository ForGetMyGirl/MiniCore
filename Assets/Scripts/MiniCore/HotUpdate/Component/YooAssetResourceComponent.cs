using System.Collections.Generic;
using UnityEngine;
using MiniCore;
using Cysharp.Threading.Tasks;
using YooAsset;
using MiniCore.Model;
namespace MiniCore.HotUpdate
{

    public class YooAssetResourceComponent : AComponent, IResourcesComponent
    {

        private ResourcePackage package;
        private Dictionary<string, AssetHandle> _loadedAssets = new Dictionary<string, AssetHandle>();

        public override void Awake(object[] obj)
        {
            base.Awake(obj);
            _loadedAssets = new Dictionary<string, AssetHandle>();
            package = YooAssets.GetPackage((string)(obj[0]));
        }

        public async UniTask<GameObject> InstantiateAsync(string key, Transform parent = null)
        {
            if (!_loadedAssets.ContainsKey(key))
            {
                // 资源未加载，先预加载
                await PreloadAssetsAsync<GameObject>(key);
            }
            return _loadedAssets[key].InstantiateSync(parent);
        }

        public async UniTask<T> LoadAssetAsync<T>(string key) where T : Object
        {
            AssetHandle handle = package.LoadAssetAsync<T>(key);
            await handle.Task;
            return handle.AssetObject as T;
        }

        public async UniTask<T> PreloadAssetsAsync<T>(string key) where T : Object
        {
            // 预加载资源
            if (!_loadedAssets.ContainsKey(key))
            {
                AssetHandle handle = package.LoadAssetAsync<T>(key);
                await handle.Task;
                _loadedAssets.Add(key, handle);
            }
            return _loadedAssets[key].AssetObject as T;
        }

        public bool ReleaseAssetAsync(string key)
        {
            if (_loadedAssets.ContainsKey(key))
            {
                _loadedAssets[key].Release();
                _loadedAssets.Remove(key);
                return true;
            }
            return false;
        }

        public bool ReleaseInstance(GameObject obj)
        {
            Object.Destroy(obj);
            return true;
        }
    }

}
