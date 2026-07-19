using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using MiniCore.Core;
using MiniCore.Model;
using UnityEngine;

namespace MiniCore.Service
{
    /// <summary>
    /// UI View 与 Presenter 的创建、缓存和层级管理服务。
    /// </summary>
    [AppService(
        "UI",
        typeof(IUIService),
        Description = "创建、显示、缓存和回收 UI 窗口，并绑定 Presenter。",
        RequiresServices = new[] { typeof(IAssetService), typeof(ISceneBindingService) })]
    public sealed class UIService : AAppService, IUIService
    {
        #region Private 私有成员

        private Dictionary<Type, Type> uiBaseAndPresenterMapping; // View 与 Presenter 的类型映射。
        private readonly Dictionary<string, Stack<AUIBase>> cachedWindows = new Dictionary<string, Stack<AUIBase>>(); // 按资源键缓存的窗口实例。
        private readonly Dictionary<AUIBase, IPresenter> activePresenters = new Dictionary<AUIBase, IPresenter>(); // 当前激活窗口对应的 Presenter。
        private IAssetService assetService; // 资产服务。
        private ISceneBindingService sceneBindingService; // 场景绑定服务。

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 初始化窗口类型映射并获取服务依赖。
        /// </summary>
        public override void Awake()
        {
            uiBaseAndPresenterMapping = new Dictionary<Type, Type>();
            LoadUIBaseAndPresenter();
            assetService = Global.GetService<IAssetService>(this);
            sceneBindingService = Global.GetService<ISceneBindingService>(this);
        }

        /// <summary>
        /// 释放 UI 服务持有的全局服务引用和窗口缓存。
        /// </summary>
        public override void Dispose()
        {
            uiBaseAndPresenterMapping?.Clear();
            cachedWindows.Clear();
            activePresenters.Clear();
            Global.ReleaseAll(this);
            base.Dispose();
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 预加载到缓冲池但不显示。
        /// </summary>
        /// <typeparam name="TView">窗口 View 类型。</typeparam>
        /// <typeparam name="TPresenter">窗口 Presenter 类型。</typeparam>
        /// <param name="assetPath">窗口资源地址。</param>
        /// <param name="layer">窗口显示层级。</param>
        /// <param name="count">预加载数量。</param>
        /// <returns>预加载完成任务。</returns>
        public async UniTask PreloadAsync<TView, TPresenter>(string assetPath, UICanvasLayer layer, int count = 1)
            where TView : AUIBase
            where TPresenter : IPresenter, new()
        {
            for (int i = 0; i < count; i++)
            {
                AUIBase view = await CreateWindowInstanceAsync<TView>(assetPath, layer, false);
                CacheInstance(assetPath, view);
            }
        }

        /// <summary>
        /// 打开（或从缓冲池取出）窗口，返回 View/Presenter。
        /// </summary>
        /// <typeparam name="TView">窗口 View 类型。</typeparam>
        /// <typeparam name="TPresenter">窗口 Presenter 类型。</typeparam>
        /// <param name="assetPath">窗口资源地址。</param>
        /// <param name="layer">窗口显示层级。</param>
        /// <returns>打开后的 View 与 Presenter。</returns>
        public async UniTask<(TView, TPresenter)> OpenAsync<TView, TPresenter>(string assetPath, UICanvasLayer layer)
            where TView : AUIBase
            where TPresenter : IPresenter, new()
        {
            AUIBase viewBase = TryGetFromCache(assetPath);
            if (viewBase == null)
            {
                viewBase = await CreateWindowInstanceAsync<TView>(assetPath, layer, true);
            }
            else
            {
                AttachToLayer(viewBase.transform, layer);
                viewBase.gameObject.SetActive(true);
            }

            TView view = viewBase as TView;
            TPresenter presenter = new TPresenter();
            presenter.BindView(view);
            activePresenters[view] = presenter;

            await view.OpenAsync();
            return (view, presenter);
        }

        /// <summary>
        /// 关闭窗口并放入缓冲池。
        /// </summary>
        /// <typeparam name="TView">窗口 View 类型。</typeparam>
        /// <param name="view">要关闭的窗口 View。</param>
        /// <param name="cache">是否放入缓存池。</param>
        /// <returns>关闭完成任务。</returns>
        public async UniTask CloseAsync<TView>(TView view, bool cache = true) where TView : AUIBase
        {
            if (view == null) return;

            if (activePresenters.TryGetValue(view, out var presenter))
            {
                presenter.UnbindView();
                activePresenters.Remove(view);
            }

            await view.CloseAsync();
            view.gameObject.SetActive(false);

            if (cache)
            {
                view.transform.SetParent(SceneBindingService.PreloadPool, false);
                CacheInstance(view.gameObject.name, view);
            }
            else
            {
                UnityEngine.Object.Destroy(view.gameObject);
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 扫描热更新程序集中的窗口类型并缓存 View 到 Presenter 映射。
        /// </summary>
        private void LoadUIBaseAndPresenter()
        {
            List<Type> uiwindowTypes = ReflectionUtils.GetClassTypesFromCustomAssembly(typeof(AUIBase), "HotUpdate");
            for (int i = 0; i < uiwindowTypes.Count; i++)
            {
                Type curType = uiwindowTypes[i];
                UIWindowAttribute uiWindowAttribute = (UIWindowAttribute)Attribute.GetCustomAttribute(curType, typeof(UIWindowAttribute));
                if (uiWindowAttribute != null)
                {
                    Type presenterType = uiWindowAttribute.PresenterType;
                    uiBaseAndPresenterMapping[curType] = presenterType;
                }
                else
                {
                    LogSwitch.Error($"UIBase:{curType.FullName} 缺少 UIWindowAttribute 属性，请确认。");
                }
            }
        }

        /// <summary>
        /// 根据逻辑 UI 层级获取对应的场景父节点。
        /// </summary>
        /// <param name="layer">目标 UI 层级。</param>
        /// <returns>承载窗口的场景节点。</returns>
        private Transform GetParentByLayer(UICanvasLayer layer)
        {
            switch (layer)
            {
                case UICanvasLayer.Background:
                    return SceneBindingService.BottomCanvas;
                case UICanvasLayer.Normal:
                    return SceneBindingService.MainCanvas;
                case UICanvasLayer.Popup:
                    return SceneBindingService.PopupWindowCanvas;
                case UICanvasLayer.Top:
                    return SceneBindingService.TopCanvas;
                case UICanvasLayer.Tips:
                    return SceneBindingService.TopCanvas;
                case UICanvasLayer.System:
                    return SceneBindingService.ErrorCodeCanvas != null ? SceneBindingService.ErrorCodeCanvas : SceneBindingService.TopCanvas;
                case UICanvasLayer.Guide:
                    return SceneBindingService.TopCanvas;
                default:
                    return SceneBindingService.MainCanvas;
            }
        }

        /// <summary>
        /// 实例化窗口预制体并验证其包含目标 View 组件。
        /// </summary>
        /// <typeparam name="TView">窗口 View 类型。</typeparam>
        /// <param name="assetPath">窗口资源地址。</param>
        /// <param name="layer">窗口显示层级。</param>
        /// <param name="setActive">实例化后是否立即激活。</param>
        /// <returns>实例化完成的窗口 View。</returns>
        private async UniTask<AUIBase> CreateWindowInstanceAsync<TView>(string assetPath, UICanvasLayer layer, bool setActive)
            where TView : AUIBase
        {
            Transform parent = GetParentByLayer(layer);
            GameObject go = await AssetService.InstantiateAsync(assetPath, parent);
            go.name = assetPath;
            go.SetActive(setActive);
            TView view = go.GetComponent<TView>();
            if (view == null)
            {
                throw new InvalidOperationException($"View component {typeof(TView).FullName} not found on instantiated UI {assetPath}");
            }
            return view;
        }

        /// <summary>
        /// 将目标窗口挂接到指定 UI 层级。
        /// </summary>
        /// <param name="target">要重新挂接的窗口节点。</param>
        /// <param name="layer">目标 UI 层级。</param>
        private void AttachToLayer(Transform target, UICanvasLayer layer)
        {
            Transform parent = GetParentByLayer(layer);
            target.SetParent(parent, false);
        }

        /// <summary>
        /// 从指定资源键的窗口缓存中取出一个实例。
        /// </summary>
        /// <param name="key">窗口资源键。</param>
        /// <returns>缓存命中时返回窗口实例；否则返回 null。</returns>
        private AUIBase TryGetFromCache(string key)
        {
            if (cachedWindows.TryGetValue(key, out Stack<AUIBase> stack) && stack.Count > 0)
            {
                return stack.Pop();
            }
            return null;
        }

        /// <summary>
        /// 将窗口实例缓存到指定资源键的对象池。
        /// </summary>
        /// <param name="key">窗口资源键。</param>
        /// <param name="view">要缓存的窗口实例。</param>
        private void CacheInstance(string key, AUIBase view)
        {
            if (!cachedWindows.TryGetValue(key, out Stack<AUIBase> stack))
            {
                stack = new Stack<AUIBase>();
                cachedWindows[key] = stack;
            }
            stack.Push(view);
        }

        /// <summary>
        /// 获取已初始化的资产服务。
        /// </summary>
        /// <returns>资产服务。</returns>
        private IAssetService AssetService => assetService ?? throw new InvalidOperationException("资产服务尚未初始化。");

        /// <summary>
        /// 获取已初始化的场景绑定服务。
        /// </summary>
        /// <returns>场景绑定服务。</returns>
        private ISceneBindingService SceneBindingService => sceneBindingService ?? throw new InvalidOperationException("场景绑定服务尚未初始化。");

        #endregion
    }

}
