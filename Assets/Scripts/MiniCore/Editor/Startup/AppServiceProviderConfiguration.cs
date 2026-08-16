using System;
using System.Collections.Generic;
using MiniCore.Service;
using MiniCore.Unity;

namespace MiniCore.EditorTools
{

    /// <summary>
    /// 将按具体实现持久化的启动设置投影为按接口单选的编辑模型。
    /// </summary>
    internal static class AppServiceProviderConfiguration
    {
        #region Internal 内部成员

        /// <summary>
        /// 按服务接口构建稳定排序的 Provider 分组。
        /// </summary>
        /// <param name="services">项目中发现的全部服务实现。</param>
        /// <param name="settings">当前项目启动设置。</param>
        /// <param name="runtimeTarget">当前配置界面允许选择的运行目标。</param>
        /// <returns>按接口完整类型名排序的分组。</returns>
        internal static List<AppServiceContractGroup> BuildGroups(
            List<MiniCoreStartupCodeGenerator.AppServiceInfo> services,
            MiniCoreStartupSettings settings,
            AppServiceRuntimeTargets runtimeTarget = AppServiceRuntimeTargets.All)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var providersByContract = new Dictionary<Type, List<MiniCoreStartupCodeGenerator.AppServiceInfo>>();
            for (int serviceIndex = 0; serviceIndex < services.Count; serviceIndex++)
            {
                MiniCoreStartupCodeGenerator.AppServiceInfo provider = services[serviceIndex];
                if ((provider.Attribute.RuntimeTargets & runtimeTarget) == 0)
                {
                    continue;
                }

                Type[] contracts = provider.Attribute.ServiceTypes;
                for (int contractIndex = 0; contractIndex < contracts.Length; contractIndex++)
                {
                    Type contract = contracts[contractIndex];
                    if (!providersByContract.TryGetValue(contract, out List<MiniCoreStartupCodeGenerator.AppServiceInfo> providers))
                    {
                        providers = new List<MiniCoreStartupCodeGenerator.AppServiceInfo>();
                        providersByContract.Add(contract, providers);
                    }

                    providers.Add(provider);
                }
            }

            var contractsSorted = new List<Type>(providersByContract.Keys);
            contractsSorted.Sort(CompareTypes);
            var result = new List<AppServiceContractGroup>(contractsSorted.Count);
            for (int contractIndex = 0; contractIndex < contractsSorted.Count; contractIndex++)
            {
                Type contract = contractsSorted[contractIndex];
                List<MiniCoreStartupCodeGenerator.AppServiceInfo> providers = providersByContract[contract];
                providers.Sort(CompareProviders);
                var enabledProviders = new List<MiniCoreStartupCodeGenerator.AppServiceInfo>();
                for (int providerIndex = 0; providerIndex < providers.Count; providerIndex++)
                {
                    MiniCoreStartupCodeGenerator.AppServiceInfo provider = providers[providerIndex];
                    MiniCoreAppServiceSettings providerSettings = FindSettings(settings, provider.Type);
                    if (providerSettings?.Enabled == true)
                    {
                        enabledProviders.Add(provider);
                    }
                }

                result.Add(new AppServiceContractGroup(contract, providers, enabledProviders));
            }

            return result;
        }

        /// <summary>
        /// 为一个服务接口选择 Provider，并同步该实现声明的其他接口。
        /// 选择实现时会关闭与其任一接口冲突的实现；选择空项时会关闭当前接口下的全部实现。
        /// </summary>
        /// <param name="settings">待修改的启动配置。</param>
        /// <param name="services">项目中发现的全部服务实现。</param>
        /// <param name="contract">正在编辑的服务接口。</param>
        /// <param name="selectedProvider">新 Provider；为 null 时表示不启用。</param>
        internal static void SelectProvider(
            MiniCoreStartupSettings settings,
            List<MiniCoreStartupCodeGenerator.AppServiceInfo> services,
            Type contract,
            MiniCoreStartupCodeGenerator.AppServiceInfo selectedProvider)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (contract == null)
            {
                throw new ArgumentNullException(nameof(contract));
            }

            if (selectedProvider != null && !ProvidesContract(selectedProvider, contract))
            {
                throw new InvalidOperationException($"Provider {selectedProvider.Type.FullName} 未实现服务接口 {contract.FullName}。");
            }

            for (int serviceIndex = 0; serviceIndex < services.Count; serviceIndex++)
            {
                MiniCoreStartupCodeGenerator.AppServiceInfo provider = services[serviceIndex];
                MiniCoreAppServiceSettings providerSettings = FindSettings(settings, provider.Type);
                if (providerSettings == null)
                {
                    continue;
                }

                if (selectedProvider == null)
                {
                    if (ProvidesContract(provider, contract))
                    {
                        providerSettings.Enabled = false;
                    }

                    continue;
                }

                if (provider.Type == selectedProvider.Type)
                {
                    providerSettings.Enabled = true;
                }
                else if (SharesAnyContract(provider, selectedProvider))
                {
                    providerSettings.Enabled = false;
                }
            }
        }

        /// <summary>
        /// 查找当前 Provider 尚未配置的依赖接口。
        /// </summary>
        /// <param name="provider">待检查的 Provider。</param>
        /// <param name="groups">当前接口分组及其选择状态。</param>
        /// <returns>按完整类型名排序的缺失依赖。</returns>
        internal static List<Type> GetMissingDependencies(
            MiniCoreStartupCodeGenerator.AppServiceInfo provider,
            List<AppServiceContractGroup> groups)
        {
            var missing = new List<Type>();
            Type[] dependencies = provider.Attribute.RequiresServices ?? Array.Empty<Type>();
            for (int dependencyIndex = 0; dependencyIndex < dependencies.Length; dependencyIndex++)
            {
                Type dependency = dependencies[dependencyIndex];
                AppServiceContractGroup group = FindGroup(groups, dependency);
                if (group == null || group.SelectedProvider == null)
                {
                    missing.Add(dependency);
                }
            }

            missing.Sort(CompareTypes);
            return missing;
        }

        /// <summary>
        /// 获取指定具体服务实现的持久化设置。
        /// </summary>
        /// <param name="settings">项目启动配置。</param>
        /// <param name="providerType">具体 Provider 类型。</param>
        /// <returns>对应配置；未同步时返回 null。</returns>
        internal static MiniCoreAppServiceSettings FindSettings(MiniCoreStartupSettings settings, Type providerType)
        {
            for (int index = 0; index < settings.Services.Count; index++)
            {
                MiniCoreAppServiceSettings service = settings.Services[index];
                if (service != null && string.Equals(service.AssemblyQualifiedTypeName, providerType.AssemblyQualifiedName, StringComparison.Ordinal))
                {
                    return service;
                }
            }

            return null;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 判断 Provider 是否声明指定服务接口。
        /// </summary>
        /// <param name="provider">待检查 Provider。</param>
        /// <param name="contract">目标服务接口。</param>
        /// <returns>声明该接口时返回 true。</returns>
        private static bool ProvidesContract(MiniCoreStartupCodeGenerator.AppServiceInfo provider, Type contract)
        {
            Type[] contracts = provider.Attribute.ServiceTypes;
            for (int index = 0; index < contracts.Length; index++)
            {
                if (contracts[index] == contract)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断两个 Provider 是否声明至少一个相同服务接口。
        /// </summary>
        /// <param name="left">第一个 Provider。</param>
        /// <param name="right">第二个 Provider。</param>
        /// <returns>存在共同接口时返回 true。</returns>
        private static bool SharesAnyContract(
            MiniCoreStartupCodeGenerator.AppServiceInfo left,
            MiniCoreStartupCodeGenerator.AppServiceInfo right)
        {
            Type[] contracts = left.Attribute.ServiceTypes;
            for (int index = 0; index < contracts.Length; index++)
            {
                if (ProvidesContract(right, contracts[index]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 在分组集合中查找指定服务接口。
        /// </summary>
        /// <param name="groups">待查询分组。</param>
        /// <param name="contract">目标接口。</param>
        /// <returns>对应分组；不存在时返回 null。</returns>
        private static AppServiceContractGroup FindGroup(List<AppServiceContractGroup> groups, Type contract)
        {
            for (int index = 0; index < groups.Count; index++)
            {
                if (groups[index].Contract == contract)
                {
                    return groups[index];
                }
            }

            return null;
        }

        /// <summary>
        /// 按服务显示名称和类型名比较 Provider。
        /// </summary>
        /// <param name="left">左侧 Provider。</param>
        /// <param name="right">右侧 Provider。</param>
        /// <returns>稳定排序比较结果。</returns>
        private static int CompareProviders(
            MiniCoreStartupCodeGenerator.AppServiceInfo left,
            MiniCoreStartupCodeGenerator.AppServiceInfo right)
        {
            int displayNameResult = string.CompareOrdinal(left.Attribute.DisplayName, right.Attribute.DisplayName);
            return displayNameResult != 0 ? displayNameResult : string.CompareOrdinal(left.Type.FullName, right.Type.FullName);
        }

        /// <summary>
        /// 按完整类型名比较服务接口。
        /// </summary>
        /// <param name="left">左侧接口。</param>
        /// <param name="right">右侧接口。</param>
        /// <returns>稳定排序比较结果。</returns>
        private static int CompareTypes(Type left, Type right)
        {
            return string.CompareOrdinal(left.FullName, right.FullName);
        }

        #endregion
    }
}
