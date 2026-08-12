using System;
using MiniCore.Model;
using MiniCore.Threading;

namespace MiniCore.Service
{

    /// <summary>
    /// 标记可由项目启动配置选择的系统级服务实现。
    /// 每个声明的服务契约在同一运行目标中只能选择一个实现。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class AppServiceAttribute : Attribute
    {
        #region Public 公共成员

        /// <summary>
        /// 获取启动配置窗口显示的服务名称。
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// 获取或设置服务面向开发者的具体职责说明。
        /// 该说明会显示在启动配置窗口的服务配置和能力目录中。
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 获取服务对外提供的接口契约。
        /// </summary>
        public Type[] ServiceTypes { get; }

        /// <summary>
        /// 获取或设置当前服务依赖的其他服务接口。
        /// </summary>
        public Type[] RequiresServices { get; set; }

        /// <summary>
        /// 获取或设置服务使用的启动参数类型。
        /// 参数类型必须继承 ComponentInitArgs；未设置时服务使用无参初始化。
        /// </summary>
        public Type InitArgsType { get; set; }

        /// <summary>
        /// 获取或设置服务是否在 BatchMode 中参与启动装配。
        /// 默认为 true；依赖图形设备或交互界面的客户端服务可显式关闭。
        /// </summary>
        public bool RunInBatchMode { get; set; } = true;

        /// <summary>
        /// 创建应用服务标记。
        /// </summary>
        /// <param name="displayName">编辑器中显示的服务名称。</param>
        /// <param name="serviceTypes">服务提供的接口契约。</param>
        public AppServiceAttribute(string displayName, params Type[] serviceTypes)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("应用服务显示名称不能为空。", nameof(displayName));
            }

            if (serviceTypes == null || serviceTypes.Length == 0)
            {
                throw new ArgumentException("应用服务必须声明至少一个服务接口。", nameof(serviceTypes));
            }

            DisplayName = displayName;
            ServiceTypes = serviceTypes;
        }

        #endregion
    }
}
