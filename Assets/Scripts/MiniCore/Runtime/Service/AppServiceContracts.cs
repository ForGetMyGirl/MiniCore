using System;
using System.Threading;
using System.Threading.Tasks;
using MiniCore.Model;

namespace MiniCore.Service
{
    /// <summary>
    /// 标记由项目启动配置选择并在应用生命周期内常驻的服务契约。
    /// 外部调用方只能通过该类接口从 Global 获取服务，不能直接依赖具体实现。
    /// </summary>
    public interface IAppService
    {
    }

    /// <summary>
    /// 标记可由业务在运行期间按需启用的公共模块契约。
    /// 模块不会自动进入项目启动配置，调用方应通过接口获取其实例。
    /// </summary>
    public interface IAppModule
    {
    }

    /// <summary>
    /// 为需要异步准备资源、配置或平台对象的应用服务提供初始化契约。
    /// 生成的启动代码会在依赖服务完成后调用该方法。
    /// </summary>
    public interface IAsyncAppService
    {
        /// <summary>
        /// 异步初始化服务。
        /// </summary>
        /// <param name="token">应用启动取消令牌。</param>
        /// <returns>初始化完成任务。</returns>
        Task InitializeAsync(CancellationToken token = default);
    }

    /// <summary>
    /// 系统级应用服务的组件基类。
    /// 服务仍使用 AComponent 的 owner 引用计数与 Tick 生命周期，但对外必须以 IAppService 接口暴露。
    /// </summary>
    public abstract class AAppService : AComponent
    {
    }

    /// <summary>
    /// 公共应用模块的组件基类。
    /// 模块由业务按需创建，可由接口隐藏具体实现。
    /// </summary>
    public abstract class AAppModule : AComponent
    {
    }

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
