using MiniCore.Threading;
using MiniCore.Core;
using MiniCore.Model;
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
    /// 提供场景中约定 Canvas 与对象池根节点绑定的服务契约。
    /// </summary>
    public interface ISceneBindingService : IAppService
    {
        /// <summary>
        /// 获取预加载对象池根节点。
        /// </summary>
        Transform PreloadPool { get; }

        /// <summary>
        /// 获取主 UI 画布节点。
        /// </summary>
        Transform MainCanvas { get; }

        /// <summary>
        /// 获取弹窗 UI 画布节点。
        /// </summary>
        Transform PopupWindowCanvas { get; }

        /// <summary>
        /// 获取顶层 UI 画布节点。
        /// </summary>
        Transform TopCanvas { get; }

        /// <summary>
        /// 获取可复用对象池根节点。
        /// </summary>
        Transform UsefulPoolObjects { get; }

        /// <summary>
        /// 获取错误码 UI 画布节点。
        /// </summary>
        Transform ErrorCodeCanvas { get; }

        /// <summary>
        /// 获取底层 UI 画布节点。
        /// </summary>
        Transform BottomCanvas { get; }
    }

    /// <summary>
    /// 提供项目资产加载、实例化与 UI 层级放置能力的服务契约。
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
        /// 异步实例化顶层 UI。
        /// </summary>
        /// <param name="key">UI 资源地址或资源键。</param>
        /// <returns>实例化完成的游戏对象。</returns>
        MTask<GameObject> InstantiateTopUIAsync(string key);

        /// <summary>
        /// 异步实例化主 UI。
        /// </summary>
        /// <param name="key">UI 资源地址或资源键。</param>
        /// <returns>实例化完成的游戏对象。</returns>
        MTask<GameObject> InstantiateMainUIAsync(string key);

        /// <summary>
        /// 异步实例化底层 UI。
        /// </summary>
        /// <param name="key">UI 资源地址或资源键。</param>
        /// <returns>实例化完成的游戏对象。</returns>
        MTask<GameObject> InstantiateBottomUIAsync(string key);

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

    /// <summary>
    /// 提供 UI View、Presenter 创建、缓存与关闭能力的服务契约。
    /// </summary>
    public interface IUIService : IAppService
    {
        /// <summary>
        /// 预加载指定窗口实例到缓存池。
        /// </summary>
        /// <typeparam name="TView">窗口 View 类型。</typeparam>
        /// <typeparam name="TPresenter">窗口 Presenter 类型。</typeparam>
        /// <param name="assetPath">窗口资源地址。</param>
        /// <param name="layer">窗口显示层级。</param>
        /// <param name="count">预加载数量。</param>
        /// <returns>预加载完成任务。</returns>
        MTask PreloadAsync<TView, TPresenter>(string assetPath, UICanvasLayer layer, int count = 1)
            where TView : AUIBase
            where TPresenter : IPresenter, new();

        /// <summary>
        /// 打开窗口并返回其 View 与 Presenter。
        /// </summary>
        /// <typeparam name="TView">窗口 View 类型。</typeparam>
        /// <typeparam name="TPresenter">窗口 Presenter 类型。</typeparam>
        /// <param name="assetPath">窗口资源地址。</param>
        /// <param name="layer">窗口显示层级。</param>
        /// <returns>打开后的 View 与 Presenter。</returns>
        MTask<(TView View, TPresenter Presenter)> OpenAsync<TView, TPresenter>(string assetPath, UICanvasLayer layer)
            where TView : AUIBase
            where TPresenter : IPresenter, new();

        /// <summary>
        /// 关闭窗口，并按需放回缓存池。
        /// </summary>
        /// <typeparam name="TView">窗口 View 类型。</typeparam>
        /// <param name="view">要关闭的窗口 View。</param>
        /// <param name="cache">是否放入缓存池。</param>
        /// <returns>关闭完成任务。</returns>
        MTask CloseAsync<TView>(TView view, bool cache = true) where TView : AUIBase;
    }
}
