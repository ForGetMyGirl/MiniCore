using MiniCore.Threading;
using UnityEngine;
using UnityEngine.U2D;

namespace MiniCore.Service
{
    /// <summary>
    /// 提供 YooAsset 资源加载、预加载、实例化与释放能力的服务契约。
    /// </summary>
    public interface IResourceService : IAppService
    {
        /// <summary>
        /// 异步加载指定类型的资源。
        /// </summary>
        /// <typeparam name="T">资源对象类型。</typeparam>
        /// <param name="key">资源地址或资源键。</param>
        /// <returns>加载完成的资源对象。</returns>
        MTask<T> LoadAssetAsync<T>(string key) where T : Object;

        /// <summary>
        /// 异步实例化资源对象。
        /// </summary>
        /// <param name="key">资源地址或资源键。</param>
        /// <param name="parent">实例化对象的父节点。</param>
        /// <returns>实例化完成的游戏对象。</returns>
        MTask<GameObject> InstantiateAsync(string key, Transform parent = null);

        /// <summary>
        /// 异步预加载资源并缓存句柄。
        /// </summary>
        /// <typeparam name="T">资源对象类型。</typeparam>
        /// <param name="key">资源地址或资源键。</param>
        /// <returns>预加载完成的资源对象。</returns>
        MTask<T> PreloadAssetAsync<T>(string key) where T : Object;

        /// <summary>
        /// 释放指定预加载资源的句柄。
        /// </summary>
        /// <param name="key">要释放的资源地址或资源键。</param>
        /// <returns>存在并释放资源句柄时返回 true。</returns>
        bool ReleaseAsset(string key);

        /// <summary>
        /// 销毁一个由资源服务实例化的游戏对象。
        /// </summary>
        /// <param name="instance">要销毁的游戏对象。</param>
        /// <returns>销毁请求提交成功时返回 true。</returns>
        bool ReleaseInstance(GameObject instance);
    }

    /// <summary>
    /// 提供项目通用资产加载、实例化与释放能力的服务契约。
    /// </summary>
    public interface IAssetService : IAppService
    {
        /// <summary>
        /// 异步实例化对象。
        /// </summary>
        /// <param name="key">资源地址或资源键。</param>
        /// <param name="parent">实例化对象的父节点。</param>
        /// <returns>实例化完成的游戏对象。</returns>
        MTask<GameObject> InstantiateAsync(string key, Transform parent = null);

        /// <summary>
        /// 异步实例化已经预加载的对象。
        /// </summary>
        /// <param name="key">预加载资源键。</param>
        /// <param name="parent">实例化对象的父节点。</param>
        /// <returns>资源已预加载时返回实例化对象；否则返回 null。</returns>
        MTask<GameObject> InstantiatePreloadAssetAsync(string key, Transform parent);

        /// <summary>
        /// 异步预加载资源。
        /// </summary>
        /// <typeparam name="T">资源对象类型。</typeparam>
        /// <param name="key">资源地址或资源键。</param>
        /// <returns>预加载完成的资源对象。</returns>
        MTask<T> PreloadAssetAsync<T>(string key) where T : Object;

        /// <summary>
        /// 异步加载图集。
        /// </summary>
        /// <param name="key">图集资源地址或资源键。</param>
        /// <returns>加载完成的图集。</returns>
        MTask<SpriteAtlas> LoadSpriteAtlasAsync(string key);

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <typeparam name="T">资源对象类型。</typeparam>
        /// <param name="key">资源地址或资源键。</param>
        /// <returns>加载完成的资源对象。</returns>
        MTask<T> LoadAssetAsync<T>(string key) where T : Object;

        /// <summary>
        /// 销毁指定游戏对象。
        /// </summary>
        /// <param name="gameObject">要销毁的游戏对象。</param>
        /// <returns>销毁请求提交成功时返回 true。</returns>
        bool ReleaseGameObject(GameObject gameObject);

        /// <summary>
        /// 释放指定预加载资源。
        /// </summary>
        /// <param name="key">要释放的资源地址或资源键。</param>
        /// <returns>存在并释放资源时返回 true。</returns>
        bool ReleaseAsset(string key);
    }

}
