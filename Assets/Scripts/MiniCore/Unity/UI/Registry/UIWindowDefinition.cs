using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniCore.UI
{
    /// <summary>
    /// 由业务注册代码创建并供 UI Runtime 直接执行的不可变窗口定义。
    /// </summary>
    public sealed class UIWindowDefinition
    {
        #region Public 公共成员

        /// <summary>
        /// 获取稳定窗口身份。
        /// </summary>
        public UIWindowId Id { get; }

        /// <summary>
        /// 获取生成的路由类型。
        /// </summary>
        public Type RouteType { get; }

        /// <summary>
        /// 获取诊断名称。
        /// </summary>
        public string RouteName { get; }

        /// <summary>
        /// 获取 YooAsset 资源地址。
        /// </summary>
        public string Address { get; }

        /// <summary>
        /// 获取窗口渲染空间。
        /// </summary>
        public UIRenderSpace RenderSpace { get; }

        /// <summary>
        /// 获取窗口逻辑显示层。
        /// </summary>
        public UILayer Layer { get; }

        /// <summary>
        /// 获取窗口实例策略。
        /// </summary>
        public UIInstancePolicy InstancePolicy { get; }

        /// <summary>
        /// 获取重复打开策略。
        /// </summary>
        public UIDuplicateOpenPolicy DuplicateOpenPolicy { get; }

        /// <summary>
        /// 获取 View 缓存策略。
        /// </summary>
        public UICachePolicy CachePolicy { get; }

        /// <summary>
        /// 获取安全区域策略。
        /// </summary>
        public UISafeAreaPolicy SafeAreaPolicy { get; }

        /// <summary>
        /// 获取是否为模态窗口。
        /// </summary>
        public bool Modal { get; }

        /// <summary>
        /// 获取是否允许点击遮罩关闭。
        /// </summary>
        public bool CloseOnMaskClick { get; }

        /// <summary>
        /// 获取 View 最大缓存数量。
        /// </summary>
        public int MaxCacheCount { get; }

        /// <summary>
        /// 获取 Screen 窗口所属导航组。
        /// </summary>
        public string NavigationGroup { get; }

        /// <summary>
        /// 获取生成的 View 解析委托。
        /// </summary>
        public Func<GameObject, AUIWindowView> ResolveView { get; }

        /// <summary>
        /// 获取生成的逻辑直接构造委托。
        /// </summary>
        public Func<IUIWindowLogic> CreateLogic { get; }

        /// <summary>
        /// 创建一份不可变窗口定义。
        /// </summary>
        /// <param name="id">稳定窗口身份。</param>
        /// <param name="routeType">生成的路由类型。</param>
        /// <param name="routeName">稳定路由名称。</param>
        /// <param name="address">YooAsset 地址。</param>
        /// <param name="renderSpace">渲染空间。</param>
        /// <param name="layer">逻辑层。</param>
        /// <param name="instancePolicy">实例策略。</param>
        /// <param name="duplicateOpenPolicy">重复打开策略。</param>
        /// <param name="cachePolicy">缓存策略。</param>
        /// <param name="safeAreaPolicy">安全区域策略。</param>
        /// <param name="modal">是否模态。</param>
        /// <param name="closeOnMaskClick">是否点击遮罩关闭。</param>
        /// <param name="maxCacheCount">最大缓存数。</param>
        /// <param name="navigationGroup">导航组。</param>
        /// <param name="resolveView">View 直接解析委托。</param>
        /// <param name="createLogic">逻辑直接构造委托。</param>
        public UIWindowDefinition(
            UIWindowId id,
            Type routeType,
            string routeName,
            string address,
            UIRenderSpace renderSpace,
            UILayer layer,
            UIInstancePolicy instancePolicy,
            UIDuplicateOpenPolicy duplicateOpenPolicy,
            UICachePolicy cachePolicy,
            UISafeAreaPolicy safeAreaPolicy,
            bool modal,
            bool closeOnMaskClick,
            int maxCacheCount,
            string navigationGroup,
            Func<GameObject, AUIWindowView> resolveView,
            Func<IUIWindowLogic> createLogic)
        {
            Id = id;
            RouteType = routeType ?? throw new ArgumentNullException(nameof(routeType));
            RouteName = routeName ?? throw new ArgumentNullException(nameof(routeName));
            Address = address ?? throw new ArgumentNullException(nameof(address));
            RenderSpace = renderSpace;
            Layer = layer;
            InstancePolicy = instancePolicy;
            DuplicateOpenPolicy = duplicateOpenPolicy;
            CachePolicy = cachePolicy;
            SafeAreaPolicy = safeAreaPolicy;
            Modal = modal;
            CloseOnMaskClick = closeOnMaskClick;
            MaxCacheCount = Mathf.Max(0, maxCacheCount);
            NavigationGroup = navigationGroup ?? string.Empty;
            ResolveView = resolveView ?? throw new ArgumentNullException(nameof(resolveView));
            CreateLogic = createLogic ?? throw new ArgumentNullException(nameof(createLogic));
        }

        #endregion
    }
}
