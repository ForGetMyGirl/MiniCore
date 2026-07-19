using System;
using MiniCore.Model;
using MiniCore.Service;

namespace MiniCore.Core
{
    /// <summary>
    /// 全局组件静态门面，业务代码通过此类型直接获取、持有和释放组件。
    /// </summary>
    public static class Global
    {
        #region Private 私有成员

        private static GlobalRuntime runtime; // 当前进程唯一的组件运行时。
        private static ITimeProvider timeProvider; // 当前运行时使用的时间来源。
        private static GlobalServiceRegistry serviceRegistry; // 当前启动目标选择的服务接口映射。
        private static GlobalModuleRegistry moduleRegistry; // 当前热更新程序集注册的公共模块工厂。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取当前运行时使用的时间来源。
        /// </summary>
        public static ITimeProvider Time => timeProvider ?? SystemTimeProvider.Shared;

        /// <summary>
        /// 初始化全局组件运行时；重复调用保持已有实例不变。
        /// </summary>
        /// <param name="provider">可选的时间来源。</param>
        public static void Initialize(ITimeProvider provider = null)
        {
            if (runtime != null)
            {
                return;
            }

            timeProvider = provider ?? SystemTimeProvider.Shared;
            runtime = new GlobalRuntime();
        }

        /// <summary>
        /// 获取已存在组件并增加 owner 引用。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="owner">本次持有组件的 owner。</param>
        /// <returns>已激活组件。</returns>
        public static T Get<T>(object owner) where T : AComponent, new()
        {
            ThrowIfDirectAppServiceAccess<T>();
            return EnsureRuntime().Get<T>(owner);
        }

        /// <summary>
        /// 获取指定分组中已存在的组件并增加 owner 引用。
        /// </summary>
        /// <typeparam name="T">组件具体类型。</typeparam>
        /// <param name="owner">本次持有组件的 owner。</param>
        /// <param name="groupId">组件所属分组。</param>
        /// <returns>已激活的分组组件。</returns>
        public static T Get<T>(object owner, ComponentGroupId groupId) where T : AComponent, new()
        {
            ThrowIfDirectAppServiceAccess<T>();
            return EnsureRuntime().Get<T>(owner, groupId);
        }

        /// <summary>
        /// 获取或创建无参组件并增加 owner 引用。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="owner">本次持有组件的 owner。</param>
        /// <returns>已激活组件。</returns>
        public static T GetOrAdd<T>(object owner) where T : AComponent, new()
        {
            ThrowIfDirectAppServiceAccess<T>();
            return EnsureRuntime().GetOrAdd<T>(owner);
        }

        /// <summary>
        /// 获取或创建带参数组件并增加 owner 引用。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="owner">本次持有组件的 owner。</param>
        /// <param name="args">首次创建参数。</param>
        /// <returns>已激活组件。</returns>
        public static T GetOrAdd<T>(object owner, ComponentInitArgs args) where T : AComponent, new()
        {
            ThrowIfDirectAppServiceAccess<T>();
            return EnsureRuntime().GetOrAdd<T>(owner, args);
        }

        /// <summary>
        /// 获取或创建指定分组中的无参组件并增加 owner 引用。
        /// </summary>
        /// <typeparam name="T">组件具体类型。</typeparam>
        /// <param name="owner">本次持有组件的 owner。</param>
        /// <param name="groupId">组件所属分组。</param>
        /// <returns>已激活的分组组件。</returns>
        public static T GetOrAdd<T>(object owner, ComponentGroupId groupId) where T : AComponent, new()
        {
            ThrowIfDirectAppServiceAccess<T>();
            return EnsureRuntime().GetOrAdd<T>(owner, groupId);
        }

        /// <summary>
        /// 获取或创建指定分组中的带参数组件并增加 owner 引用。
        /// </summary>
        /// <typeparam name="T">组件具体类型。</typeparam>
        /// <param name="owner">本次持有组件的 owner。</param>
        /// <param name="groupId">组件所属分组。</param>
        /// <param name="args">仅首次创建时使用的初始化参数。</param>
        /// <returns>已激活的分组组件。</returns>
        public static T GetOrAdd<T>(object owner, ComponentGroupId groupId, ComponentInitArgs args) where T : AComponent, new()
        {
            ThrowIfDirectAppServiceAccess<T>();
            return EnsureRuntime().GetOrAdd<T>(owner, groupId, args);
        }

        /// <summary>
        /// 获取或创建常驻组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <returns>常驻组件。</returns>
        public static T Pin<T>() where T : AComponent, new()
        {
            ThrowIfDirectAppServiceAccess<T>();
            return EnsureRuntime().Pin<T>();
        }

        /// <summary>
        /// 获取或创建带参数的常驻组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="args">首次创建参数。</param>
        /// <returns>常驻组件。</returns>
        public static T Pin<T>(ComponentInitArgs args) where T : AComponent, new()
        {
            ThrowIfDirectAppServiceAccess<T>();
            return EnsureRuntime().Pin<T>(args);
        }

        /// <summary>
        /// 注册并启动一个项目选中的应用服务实现。
        /// 此方法只应由自动生成的启动代码调用。
        /// </summary>
        /// <typeparam name="TService">对外暴露的服务接口。</typeparam>
        /// <typeparam name="TImplementation">实际常驻服务组件类型。</typeparam>
        /// <param name="args">服务首次启动时使用的初始化参数；无参服务传入 null。</param>
        public static TImplementation RegisterAppService<TService, TImplementation>(ComponentInitArgs args = null)
            where TImplementation : AAppService, TService, new()
        {
            BindAppService<TService, TImplementation>();
            if (args == null)
            {
                return runtime.Pin<TImplementation>();
            }

            return runtime.Pin<TImplementation>(args);
        }

        /// <summary>
        /// 为已启动的应用服务追加一个对外接口映射。
        /// 仅供自动生成的启动代码在同一服务实现多个接口时调用。
        /// </summary>
        /// <typeparam name="TService">新增对外服务接口。</typeparam>
        /// <typeparam name="TImplementation">已经或即将启动的服务实现。</typeparam>
        public static void BindAppService<TService, TImplementation>()
            where TImplementation : AAppService, TService, new()
        {
            EnsureRuntime();
            serviceRegistry ??= new GlobalServiceRegistry();
            serviceRegistry.Register<TService, TImplementation>(owner => runtime.Get<TImplementation>(owner));
        }

        /// <summary>
        /// 通过服务接口获取当前启动目标选中的系统服务。
        /// </summary>
        /// <typeparam name="TService">服务接口类型。</typeparam>
        /// <param name="owner">本次持有服务的 owner。</param>
        /// <returns>当前目标选中的服务实现。</returns>
        public static TService GetService<TService>(object owner)
        {
            EnsureRuntime();
            if (serviceRegistry == null)
            {
                throw new InvalidOperationException($"尚未注册应用服务接口：{typeof(TService).FullName}。");
            }

            return serviceRegistry.Get<TService>(owner);
        }

        /// <summary>
        /// 尝试通过接口获取一个可选应用服务。
        /// 未在当前 Client/Server 服务配置中选择的服务会返回 false。
        /// </summary>
        /// <typeparam name="TService">服务接口类型。</typeparam>
        /// <param name="owner">本次持有服务的 owner。</param>
        /// <param name="service">已绑定服务实现。</param>
        /// <returns>服务存在时返回 true。</returns>
        public static bool TryGetService<TService>(object owner, out TService service)
        {
            EnsureRuntime();
            if (serviceRegistry == null)
            {
                service = default;
                return false;
            }

            return serviceRegistry.TryGet(owner, out service);
        }

        /// <summary>
        /// 注册一个可按需创建的公共应用模块。
        /// 此方法由自动生成的模块注册表调用，业务代码不应直接依赖具体模块实现。
        /// </summary>
        /// <typeparam name="TModule">模块对外接口。</typeparam>
        /// <typeparam name="TImplementation">具体模块组件类型。</typeparam>
        /// <param name="key">多实现时使用的稳定选择键。</param>
        public static void RegisterAppModule<TModule, TImplementation>(string key = null)
            where TImplementation : AAppModule, TModule, new()
        {
            EnsureRuntime();
            moduleRegistry ??= new GlobalModuleRegistry();
            moduleRegistry.Register<TModule>(key, (owner, args) => args == null
                ? runtime.GetOrAdd<TImplementation>(owner)
                : runtime.GetOrAdd<TImplementation>(owner, args));
        }

        /// <summary>
        /// 获取或创建当前项目注册的单实现应用模块。
        /// </summary>
        /// <typeparam name="TModule">模块对外接口。</typeparam>
        /// <param name="owner">本次持有模块的 owner。</param>
        /// <param name="args">首次创建时使用的初始化参数。</param>
        /// <returns>模块接口实现。</returns>
        public static TModule GetOrAddModule<TModule>(object owner, ComponentInitArgs args = null)
        {
            return GetOrAddModule<TModule>(null, owner, args);
        }

        /// <summary>
        /// 获取或创建指定 Key 的应用模块。
        /// </summary>
        /// <typeparam name="TModule">模块对外接口。</typeparam>
        /// <param name="key">多实现时使用的稳定选择键。</param>
        /// <param name="owner">本次持有模块的 owner。</param>
        /// <param name="args">首次创建时使用的初始化参数。</param>
        /// <returns>模块接口实现。</returns>
        public static TModule GetOrAddModule<TModule>(string key, object owner, ComponentInitArgs args = null)
        {
            EnsureRuntime();
            if (moduleRegistry == null)
            {
                throw new InvalidOperationException($"尚未注册应用模块接口：{typeof(TModule).FullName}。");
            }

            return moduleRegistry.GetOrAdd<TModule>(key, owner, args);
        }

        /// <summary>
        /// 解除组件的根 owner 常驻引用。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        public static void Unpin<T>() where T : AComponent, new()
        {
            EnsureRuntime().Unpin<T>();
        }

        /// <summary>
        /// 释放 owner 对指定组件的一份引用。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="owner">持有组件的 owner。</param>
        public static void Remove<T>(object owner) where T : AComponent, new()
        {
            EnsureRuntime().Remove<T>(owner);
        }

        /// <summary>
        /// 释放 owner 持有的全部组件。
        /// </summary>
        /// <param name="owner">持有组件的 owner。</param>
        public static void ReleaseAll(object owner)
        {
            EnsureRuntime().ReleaseAll(owner);
        }

        /// <summary>
        /// 忽略引用计数并立即移除组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        public static void ForceRemove<T>() where T : AComponent, new()
        {
            EnsureRuntime().ForceRemove<T>();
        }

        /// <summary>
        /// 创建一个用于批量释放组件引用的作用域。
        /// </summary>
        /// <param name="name">诊断用作用域名称。</param>
        /// <returns>新建作用域。</returns>
        public static GlobalScope CreateScope(string name)
        {
            EnsureRuntime();
            return new GlobalScope(name);
        }

        /// <summary>
        /// 创建一个可承载同类型多实例组件的业务分组。
        /// </summary>
        /// <param name="name">用于诊断的分组名称。</param>
        /// <param name="businessId">非零的业务唯一标识。</param>
        /// <returns>新建组件分组。</returns>
        public static ComponentGroup CreateGroup(string name, long businessId)
        {
            EnsureRuntime();
            return new ComponentGroup(name, new ComponentGroupId(businessId));
        }

        /// <summary>
        /// 由 ComponentGroup 调用，强制销毁指定非默认组内的所有组件。
        /// </summary>
        /// <param name="groupId">待销毁分组身份。</param>
        /// <param name="groupName">用于诊断的分组名称。</param>
        internal static void DestroyGroup(ComponentGroupId groupId, string groupName)
        {
            EnsureRuntime().DestroyGroup(groupId, groupName);
        }

        /// <summary>
        /// 调度所有激活的全局组件。
        /// </summary>
        public static void Tick()
        {
            runtime?.Tick();
        }

        /// <summary>
        /// 强制关闭全局组件运行时。
        /// </summary>
        public static void Shutdown()
        {
            if (runtime == null)
            {
                return;
            }

            runtime.Dispose();
            runtime = null;
            timeProvider = null;
            serviceRegistry?.Clear();
            serviceRegistry = null;
            moduleRegistry?.Clear();
            moduleRegistry = null;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 获取已初始化的运行时，首次访问时使用系统时间自动初始化。
        /// </summary>
        /// <returns>可用的组件运行时。</returns>
        private static GlobalRuntime EnsureRuntime()
        {
            if (runtime == null)
            {
                Initialize();
            }

            return runtime;
        }

        /// <summary>
        /// 在开发环境中阻止业务绕过服务接口直接使用启动服务实现。
        /// </summary>
        /// <typeparam name="T">调用方尝试获取的组件类型。</typeparam>
        private static void ThrowIfDirectAppServiceAccess<T>() where T : AComponent
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (serviceRegistry != null && serviceRegistry.IsManagedImplementation(typeof(T)))
            {
                throw new InvalidOperationException($"应用服务 {typeof(T).FullName} 必须通过 Global.GetService<TService>(owner) 获取。");
            }
#endif
        }

        #endregion
    }
}
