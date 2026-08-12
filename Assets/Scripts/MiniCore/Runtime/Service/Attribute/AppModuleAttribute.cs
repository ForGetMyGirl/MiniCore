using System;
using MiniCore.Model;
using MiniCore.Threading;

namespace MiniCore.Service
{

    /// <summary>
    /// 标记可由运行期模块注册表按接口创建的公共模块实现。
    /// 同一接口存在多个实现时必须使用唯一 Key 进行选择。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class AppModuleAttribute : Attribute
    {
        #region Public 公共成员

        /// <summary>
        /// 获取模块对外提供的接口契约。
        /// </summary>
        public Type ModuleType { get; }

        /// <summary>
        /// 获取模块实现的稳定选择键。
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// 获取或设置模块面向开发者的具体职责说明。
        /// 该说明会显示在启动配置窗口的项目能力目录中。
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 创建应用模块标记。
        /// </summary>
        /// <param name="moduleType">模块对外接口契约。</param>
        /// <param name="key">多实现时用于选择实现的稳定键。</param>
        public AppModuleAttribute(Type moduleType, string key = null)
        {
            ModuleType = moduleType ?? throw new ArgumentNullException(nameof(moduleType));
            Key = key ?? string.Empty;
        }

        #endregion
    }
}
