using System;
using System.Collections.Generic;

namespace MiniCore.UI
{
    /// <summary>
    /// 保存业务生成的强类型窗口定义，不扫描或引用业务程序集。
    /// </summary>
    public sealed class UIWindowRegistry
    {
        #region Private 私有成员

        private static UIWindowRegistry project = new UIWindowRegistry(); // 当前业务启动周期使用的项目注册表。
        private readonly Dictionary<Type, UIWindowDefinition> byRoute = new Dictionary<Type, UIWindowDefinition>(); // 路由类型映射。
        private readonly Dictionary<UIWindowId, UIWindowDefinition> byId = new Dictionary<UIWindowId, UIWindowDefinition>(); // 稳定身份映射。
        private readonly Dictionary<string, UIWindowDefinition> byName = new Dictionary<string, UIWindowDefinition>(StringComparer.Ordinal); // 稳定路由名称映射。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取当前业务启动周期使用的项目注册表。
        /// </summary>
        public static UIWindowRegistry Project => project;

        /// <summary>
        /// 获取已经登记的窗口数量。
        /// </summary>
        public int Count => byRoute.Count;

        /// <summary>
        /// 为新的业务启动周期创建空注册表。
        /// </summary>
        public static void ResetProject()
        {
            project = new UIWindowRegistry();
        }

        /// <summary>
        /// 登记一项由业务代码生成器创建的窗口定义。
        /// </summary>
        /// <param name="definition">待登记的不可变定义。</param>
        public void Register(UIWindowDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (definition.Id.IsEmpty)
            {
                throw new InvalidOperationException($"窗口 {definition.RouteName} 的 WindowId 为空。");
            }

            if (byRoute.ContainsKey(definition.RouteType) || byId.ContainsKey(definition.Id) || byName.ContainsKey(definition.RouteName))
            {
                throw new InvalidOperationException($"窗口路由或 WindowId 重复：{definition.RouteName}。");
            }

            byRoute.Add(definition.RouteType, definition);
            byId.Add(definition.Id, definition);
            byName.Add(definition.RouteName, definition);
        }

        /// <summary>
        /// 获取指定强类型路由的窗口定义。
        /// </summary>
        /// <typeparam name="TRoute">业务生成的窗口路由。</typeparam>
        /// <returns>对应窗口定义。</returns>
        public UIWindowDefinition Get<TRoute>() where TRoute : IUIWindowRoute
        {
            if (!byRoute.TryGetValue(typeof(TRoute), out UIWindowDefinition definition))
            {
                throw new InvalidOperationException($"窗口路由未注册或已过期：{typeof(TRoute).FullName}。");
            }

            return definition;
        }

        /// <summary>
        /// 按稳定身份查询窗口定义。
        /// </summary>
        /// <param name="id">窗口稳定身份。</param>
        /// <param name="definition">查询到的窗口定义。</param>
        /// <returns>存在定义时返回 true。</returns>
        public bool TryGet(UIWindowId id, out UIWindowDefinition definition)
        {
            return byId.TryGetValue(id, out definition);
        }

        /// <summary>
        /// 按稳定路由名称获取窗口定义。
        /// </summary>
        /// <param name="routeName">窗口 Authoring 中的 RouteName。</param>
        /// <returns>已注册窗口定义。</returns>
        public UIWindowDefinition Get(string routeName)
        {
            if (string.IsNullOrWhiteSpace(routeName) || !byName.TryGetValue(routeName, out UIWindowDefinition definition))
            {
                throw new InvalidOperationException($"窗口路由名称未注册或已过期：{routeName ?? "<null>"}。");
            }

            return definition;
        }

        #endregion
    }
}
