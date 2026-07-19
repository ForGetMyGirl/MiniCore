using System;

namespace MiniCore.Model
{
    /// <summary>
    /// 标记需要在启动配置窗口的项目能力目录中展示的普通组件。
    /// 该标记只提供开发期的可发现性信息，不影响组件创建、生命周期或启动配置。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ComponentCatalogAttribute : Attribute
    {
        #region Public 公共成员

        /// <summary>
        /// 获取能力目录中显示的组件名称。
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// 获取或设置组件面向开发者的具体职责说明。
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 创建普通组件能力目录标记。
        /// </summary>
        /// <param name="displayName">能力目录中显示的组件名称。</param>
        public ComponentCatalogAttribute(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("组件目录显示名称不能为空。", nameof(displayName));
            }

            DisplayName = displayName;
        }

        #endregion
    }
}
