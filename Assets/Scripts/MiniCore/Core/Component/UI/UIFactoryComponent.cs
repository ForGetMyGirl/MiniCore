using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using MiniCore.Model;
using UnityEngine;

namespace MiniCore.Core
{
    public class UIFactoryComponent : AComponent
    {
        private Dictionary<Type, Type> uiBaseAndPresenterMapping;
        private readonly Dictionary<string, Stack<AUIBase>> cachedWindows = new Dictionary<string, Stack<AUIBase>>();
        private readonly Dictionary<AUIBase, IPresenter> activePresenters = new Dictionary<AUIBase, IPresenter>();
        private AssetsComponent assetsComponent;
        private TagsComponent tagsComponent;

        public override void Awake()
        {
            uiBaseAndPresenterMapping = new Dictionary<Type, Type>();
            LoadUIBaseAndPresenter();
            assetsComponent = Global.Com.Get<AssetsComponent>();
            tagsComponent = Global.Com.Get<TagsComponent>();
        }

        /// <summary>
        /// 预加载到缓冲池但不显示。
        /// </summary>
        public async UniTask PreloadAsync<TView, TPresenter>(string assetPath, UICanvasLayer layer, int count = 1)
            where TView : AUIBase
            where TPresenter : IPresenter, new()
        {
            for (int i = 0; i < count; i++)
            {
                var view = await CreateWindowInstanceAsync<TView>(assetPath, layer, setActive: false);
                CacheInstance(assetPath, view);
            }
        }

        /// <summary>
        /// 打开（或从缓冲池取出）窗口，返回 View/Presenter。
        /// </summary>
        public async UniTask<(TView, TPresenter)> OpenAsync<TView, TPresenter>(string assetPath, UICanvasLayer layer)
            where TView : AUIBase
            where TPresenter : IPresenter, new()
        {
            AUIBase viewBase = TryGetFromCache(assetPath);
            if (viewBase == null)
            {
                viewBase = await CreateWindowInstanceAsync<TView>(assetPath, layer, setActive: true);
            }
            else
            {
                AttachToLayer(viewBase.transform, layer);
                viewBase.gameObject.SetActive(true);
            }

            var view = viewBase as TView;
            var presenter = new TPresenter();
            presenter.BindView(view);
            activePresenters[view] = presenter;

            await view.OpenAsync();
            return (view, presenter);
        }

        /// <summary>
        /// 关闭窗口并放入缓冲池。
        /// </summary>
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
                view.transform.SetParent(tagsComponent.PreloadPool, false);
                CacheInstance(view.gameObject.name, view);
            }
            else
            {
                UnityEngine.Object.Destroy(view.gameObject);
            }
        }

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
                    EventCenter.Broadcast(GameEvent.LogError, $"UIBase:{curType.FullName} 缺少 UIWindowAttribute 属性，请确认。");
                }
            }
        }

        private Transform GetParentByLayer(UICanvasLayer layer)
        {
            switch (layer)
            {
                case UICanvasLayer.Background:
                    return tagsComponent.BottomCanvas;
                case UICanvasLayer.Normal:
                    return tagsComponent.MainCanvas;
                case UICanvasLayer.Popup:
                    return tagsComponent.PopupWindowCanvas;
                case UICanvasLayer.Top:
                    return tagsComponent.TopCanvas;
                case UICanvasLayer.Tips:
                    return tagsComponent.TopCanvas;
                case UICanvasLayer.System:
                    return tagsComponent.ErrorCodeCanvas != null ? tagsComponent.ErrorCodeCanvas : tagsComponent.TopCanvas;
                case UICanvasLayer.Guide:
                    return tagsComponent.TopCanvas;
                default:
                    return tagsComponent.MainCanvas;
            }
        }

        private async UniTask<AUIBase> CreateWindowInstanceAsync<TView>(string assetPath, UICanvasLayer layer, bool setActive)
            where TView : AUIBase
        {
            Transform parent = GetParentByLayer(layer);
            var go = await assetsComponent.InstantiateAsync(assetPath, parent);
            go.name = assetPath;
            go.SetActive(setActive);
            var view = go.GetComponent<TView>();
            if (view == null)
            {
                throw new InvalidOperationException($"View component {typeof(TView).FullName} not found on instantiated UI {assetPath}");
            }
            return view;
        }

        private void AttachToLayer(Transform target, UICanvasLayer layer)
        {
            var parent = GetParentByLayer(layer);
            target.SetParent(parent, false);
        }

        private AUIBase TryGetFromCache(string key)
        {
            if (cachedWindows.TryGetValue(key, out var stack) && stack.Count > 0)
            {
                return stack.Pop();
            }
            return null;
        }

        private void CacheInstance(string key, AUIBase view)
        {
            if (!cachedWindows.TryGetValue(key, out var stack))
            {
                stack = new Stack<AUIBase>();
                cachedWindows[key] = stack;
            }
            stack.Push(view);
        }
    }

}
