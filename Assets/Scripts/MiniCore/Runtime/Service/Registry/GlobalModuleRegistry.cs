using System;
using System.Collections.Generic;
using MiniCore.Model;

namespace MiniCore.Core
{
    /// <summary>
    /// 保存 AppModule 接口到无反射创建委托的注册表。
    /// 注册代码由编辑器生成，业务调用方只按接口与可选 Key 获取模块。
    /// </summary>
    internal sealed class GlobalModuleRegistry
    {
        #region Private 私有成员

        private readonly Dictionary<Type, Dictionary<string, Func<object, ComponentInitArgs, object>>> factories = new Dictionary<Type, Dictionary<string, Func<object, ComponentInitArgs, object>>>(); // 接口到按 Key 创建模块的委托映射。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 注册一个应用模块实现。
        /// </summary>
        /// <typeparam name="TModule">对外模块接口。</typeparam>
        /// <param name="key">多实现时使用的稳定选择键。</param>
        /// <param name="factory">创建或获取具体模块的无反射委托。</param>
        internal void Register<TModule>(string key, Func<object, ComponentInitArgs, TModule> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            Type moduleType = typeof(TModule);
            if (!moduleType.IsInterface)
            {
                throw new ArgumentException($"模块类型必须是接口：{moduleType.FullName}", nameof(TModule));
            }

            if (!factories.TryGetValue(moduleType, out Dictionary<string, Func<object, ComponentInitArgs, object>> keyedFactories))
            {
                keyedFactories = new Dictionary<string, Func<object, ComponentInitArgs, object>>(StringComparer.Ordinal);
                factories.Add(moduleType, keyedFactories);
            }

            string normalizedKey = key ?? string.Empty;
            if (keyedFactories.ContainsKey(normalizedKey))
            {
                throw new InvalidOperationException($"应用模块接口 {moduleType.FullName} 存在重复 Key：{normalizedKey}。");
            }

            keyedFactories[normalizedKey] = (owner, args) => factory(owner, args);
        }

        /// <summary>
        /// 获取或创建指定接口的应用模块。
        /// </summary>
        /// <typeparam name="TModule">对外模块接口。</typeparam>
        /// <param name="key">多实现时使用的稳定选择键；单实现可传空。</param>
        /// <param name="owner">本次持有模块的 owner。</param>
        /// <param name="args">首次创建时使用的初始化参数。</param>
        /// <returns>当前选择的模块实现。</returns>
        internal TModule GetOrAdd<TModule>(string key, object owner, ComponentInitArgs args)
        {
            if (!factories.TryGetValue(typeof(TModule), out Dictionary<string, Func<object, ComponentInitArgs, object>> keyedFactories))
            {
                throw new InvalidOperationException($"未注册应用模块接口：{typeof(TModule).FullName}。");
            }

            Func<object, ComponentInitArgs, object> factory;
            string normalizedKey = key ?? string.Empty;
            if (normalizedKey.Length == 0)
            {
                if (keyedFactories.Count != 1)
                {
                    throw new InvalidOperationException($"应用模块接口 {typeof(TModule).FullName} 存在多个实现，请显式传入 Module Key。");
                }

                foreach (KeyValuePair<string, Func<object, ComponentInitArgs, object>> pair in keyedFactories)
                {
                    factory = pair.Value;
                    return (TModule)factory(owner, args);
                }
            }

            if (!keyedFactories.TryGetValue(normalizedKey, out factory))
            {
                throw new InvalidOperationException($"未注册应用模块：{typeof(TModule).FullName} key:{normalizedKey}。");
            }

            return (TModule)factory(owner, args);
        }

        /// <summary>
        /// 清空全部模块工厂注册。
        /// </summary>
        internal void Clear()
        {
            factories.Clear();
        }

        #endregion
    }
}
