using System;
using System.Collections.Generic;

namespace MiniCore.Core
{
    /// <summary>
    /// 保存 AppService 接口到当前项目选中实现的受控映射。
    /// 映射由生成启动代码注册，业务侧只能通过 Global.GetService 获取接口。
    /// </summary>
    internal sealed class GlobalServiceRegistry
    {
        #region Private 私有成员

        private readonly Dictionary<Type, Func<object, object>> getters = new Dictionary<Type, Func<object, object>>(); // 服务接口到具体组件获取器的映射。
        private readonly HashSet<Type> implementationTypes = new HashSet<Type>(); // 受控服务实现类型，用于阻止业务直接访问。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 注册一个服务接口的具体获取器。
        /// </summary>
        /// <typeparam name="TService">服务接口类型。</typeparam>
        /// <param name="getter">以调用 owner 获取具体服务的委托。</param>
        internal void Register<TService, TImplementation>(Func<object, TService> getter)
        {
            if (getter == null)
            {
                throw new ArgumentNullException(nameof(getter));
            }

            Type serviceType = typeof(TService);
            if (!serviceType.IsInterface)
            {
                throw new ArgumentException($"服务类型必须是接口：{serviceType.FullName}", nameof(TService));
            }

            getters[serviceType] = owner => getter(owner);
            implementationTypes.Add(typeof(TImplementation));
        }

        /// <summary>
        /// 获取已注册的服务接口实现。
        /// </summary>
        /// <typeparam name="TService">服务接口类型。</typeparam>
        /// <param name="owner">本次持有服务的 owner。</param>
        /// <returns>当前目标选中的服务实现。</returns>
        internal TService Get<TService>(object owner)
        {
            if (!getters.TryGetValue(typeof(TService), out Func<object, object> getter))
            {
                throw new InvalidOperationException($"未注册应用服务接口：{typeof(TService).FullName}。请在项目启动服务配置中选择实现。");
            }

            return (TService)getter(owner);
        }

        /// <summary>
        /// 尝试获取可选服务；未绑定时不会抛出异常，也不会产生组件引用。
        /// </summary>
        /// <typeparam name="TService">服务接口类型。</typeparam>
        /// <param name="owner">本次持有服务的 owner。</param>
        /// <param name="service">已绑定服务实现。</param>
        /// <returns>存在绑定时返回 true。</returns>
        internal bool TryGet<TService>(object owner, out TService service)
        {
            if (getters.TryGetValue(typeof(TService), out Func<object, object> getter))
            {
                service = (TService)getter(owner);
                return true;
            }

            service = default;
            return false;
        }

        /// <summary>
        /// 判断类型是否为启动配置管理的具体服务实现。
        /// </summary>
        /// <param name="type">待判断的组件具体类型。</param>
        /// <returns>属于受控 AppService 时返回 true。</returns>
        internal bool IsManagedImplementation(Type type)
        {
            return implementationTypes.Contains(type);
        }

        /// <summary>
        /// 清空全部服务接口映射。
        /// </summary>
        internal void Clear()
        {
            getters.Clear();
            implementationTypes.Clear();
        }

        #endregion
    }
}
