using System;

namespace MiniCore.Model
{
    /// <summary>
    /// 标记可由 MiniCore 项目启动配置发现的全局组件。
    /// 只有显式添加此特性的 AComponent 才会出现在编辑器列表中，避免把临时组件或运行期组件误加入启动流程。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class MiniCoreStartupModuleAttribute : Attribute
    {
        #region Public 公共成员

        /// <summary>
        /// 获取组件在项目启动配置窗口中显示的名称。
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// 获取或设置当前组件所依赖的其他启动组件类型。
        /// 生成器会先初始化依赖，再初始化当前组件；组件自身仍应通过 Global.Get 管理实际持有关系。
        /// </summary>
        public Type[] DependsOn { get; set; }

        /// <summary>
        /// 创建启动模块标记。
        /// </summary>
        /// <param name="displayName">编辑器中显示的模块名称。</param>
        public MiniCoreStartupModuleAttribute(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("启动模块显示名称不能为空。", nameof(displayName));
            }

            DisplayName = displayName;
        }

        #endregion
    }
}
