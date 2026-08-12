using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniCore.UI
{

    /// <summary>
    /// 保存编辑器生成的强类型窗口定义，不执行运行时程序集扫描。
    /// </summary>
    public static partial class UIWindowRegistry
    {
        #region Private 私有成员

        private static readonly Dictionary<Type, UIWindowDefinition> ByRoute = new Dictionary<Type, UIWindowDefinition>(); // 路由类型映射。
        private static readonly Dictionary<UIWindowId, UIWindowDefinition> ById = new Dictionary<UIWindowId, UIWindowDefinition>(); // 稳定身份映射。
        private static readonly Dictionary<string, UIWindowDefinition> ByName = new Dictionary<string, UIWindowDefinition>(StringComparer.Ordinal); // 稳定路由名称映射。
        private static bool initialized; // 生成注册表是否已经装载。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 装载生成定义；重复调用不会重复登记。
        /// </summary>
        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            ByRoute.Clear();
            ById.Clear();
            ByName.Clear();
            RegisterGenerated();
            initialized = true;
        }

        /// <summary>
        /// 获取指定强类型路由的窗口定义。
        /// </summary>
        /// <typeparam name="TRoute">编辑器生成的路由。</typeparam>
        /// <returns>对应窗口定义。</returns>
        public static UIWindowDefinition Get<TRoute>() where TRoute : IUIWindowRoute
        {
            Initialize();
            if (!ByRoute.TryGetValue(typeof(TRoute), out UIWindowDefinition definition))
            {
                throw new InvalidOperationException($"窗口路由未生成或已过期：{typeof(TRoute).FullName}。");
            }

            return definition;
        }

        /// <summary>
        /// 按稳定身份查询窗口定义。
        /// </summary>
        /// <param name="id">窗口稳定身份。</param>
        /// <param name="definition">查询到的窗口定义。</param>
        /// <returns>存在定义时返回 true。</returns>
        public static bool TryGet(UIWindowId id, out UIWindowDefinition definition)
        {
            Initialize();
            return ById.TryGetValue(id, out definition);
        }

        /// <summary>
        /// 按稳定路由名称获取窗口定义。
        /// </summary>
        /// <param name="routeName">窗口 Authoring 中的 RouteName。</param>
        /// <returns>已生成窗口定义。</returns>
        public static UIWindowDefinition Get(string routeName)
        {
            Initialize();
            if (string.IsNullOrWhiteSpace(routeName) || !ByName.TryGetValue(routeName, out UIWindowDefinition definition))
            {
                throw new InvalidOperationException($"窗口路由名称未生成或已过期：{routeName ?? "<null>"}。");
            }

            return definition;
        }

        /// <summary>
        /// 清空当前注册表，供编辑器测试和 Domain Reload 使用。
        /// </summary>
        public static void Reset()
        {
            ByRoute.Clear();
            ById.Clear();
            ByName.Clear();
            initialized = false;
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 登记一项由代码生成器创建的窗口定义。
        /// </summary>
        /// <param name="definition">待登记的不可变定义。</param>
        internal static void Register(UIWindowDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (definition.Id.IsEmpty)
            {
                throw new InvalidOperationException($"窗口 {definition.RouteName} 的 WindowId 为空。");
            }

            if (ByRoute.ContainsKey(definition.RouteType) || ById.ContainsKey(definition.Id) || ByName.ContainsKey(definition.RouteName))
            {
                throw new InvalidOperationException($"窗口路由或 WindowId 重复：{definition.RouteName}。");
            }

            ByRoute.Add(definition.RouteType, definition);
            ById.Add(definition.Id, definition);
            ByName.Add(definition.RouteName, definition);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 由 UIWindowRegistry.Generated.cs 提供实际窗口登记代码。
        /// </summary>
        static partial void RegisterGenerated();

        #endregion
    }
}
