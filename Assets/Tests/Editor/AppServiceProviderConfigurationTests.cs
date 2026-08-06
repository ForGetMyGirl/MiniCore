using System;
using System.Collections.Generic;
using MiniCore.EditorTools;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Unity;
using NUnit.Framework;
using UnityEngine;

namespace MiniCore.EditorTests
{
    /// <summary>
    /// 验证启动配置按接口单选 Provider 时的同步、冲突和参数保留行为。
    /// </summary>
    public sealed class AppServiceProviderConfigurationTests
    {
        #region Public 公共成员

        /// <summary>
        /// 验证同一接口的多个启用实现会被识别为冲突，重新选择后只保留一个实现。
        /// </summary>
        [Test]
        public void BuildGroups_ConflictCanBeResolvedBySingleSelection()
        {
            List<MiniCoreStartupCodeGenerator.AppServiceInfo> services = CreateServices();
            MiniCoreStartupSettings settings = CreateSettings(services);
            try
            {
                FindSettings(settings, typeof(PrimaryProvider)).Enabled = true;
                FindSettings(settings, typeof(AlternativePrimaryProvider)).Enabled = true;

                AppServiceContractGroup conflict = FindGroup(AppServiceProviderConfiguration.BuildGroups(services, settings), typeof(IPrimaryService));
                Assert.IsTrue(conflict.HasConflict);

                MiniCoreStartupCodeGenerator.AppServiceInfo selected = FindProvider(services, typeof(AlternativePrimaryProvider));
                AppServiceProviderConfiguration.SelectProvider(settings, services, typeof(IPrimaryService), selected);
                AppServiceContractGroup resolved = FindGroup(AppServiceProviderConfiguration.BuildGroups(services, settings), typeof(IPrimaryService));

                Assert.IsFalse(resolved.HasConflict);
                Assert.AreEqual(typeof(AlternativePrimaryProvider), resolved.SelectedProvider.Type);
                Assert.IsFalse(FindSettings(settings, typeof(PrimaryProvider)).Enabled);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        /// <summary>
        /// 验证多接口 Provider 在任一接口分组中选择或关闭时会作为一个整体同步。
        /// </summary>
        [Test]
        public void SelectProvider_MultiContractProviderSynchronizesAllGroups()
        {
            List<MiniCoreStartupCodeGenerator.AppServiceInfo> services = CreateServices();
            MiniCoreStartupSettings settings = CreateSettings(services);
            try
            {
                MiniCoreStartupCodeGenerator.AppServiceInfo multiProvider = FindProvider(services, typeof(MultiContractProvider));
                AppServiceProviderConfiguration.SelectProvider(settings, services, typeof(IPrimaryService), multiProvider);
                List<AppServiceContractGroup> selectedGroups = AppServiceProviderConfiguration.BuildGroups(services, settings);

                Assert.AreEqual(typeof(MultiContractProvider), FindGroup(selectedGroups, typeof(IPrimaryService)).SelectedProvider.Type);
                Assert.AreEqual(typeof(MultiContractProvider), FindGroup(selectedGroups, typeof(ISecondaryService)).SelectedProvider.Type);
                Assert.IsFalse(FindSettings(settings, typeof(PrimaryProvider)).Enabled);

                AppServiceProviderConfiguration.SelectProvider(settings, services, typeof(ISecondaryService), null);
                List<AppServiceContractGroup> disabledGroups = AppServiceProviderConfiguration.BuildGroups(services, settings);
                Assert.IsNull(FindGroup(disabledGroups, typeof(IPrimaryService)).SelectedProvider);
                Assert.IsNull(FindGroup(disabledGroups, typeof(ISecondaryService)).SelectedProvider);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        /// <summary>
        /// 验证切换 Provider 只改变启用状态，不会丢失各实现独立保存的 Args。
        /// </summary>
        [Test]
        public void SelectProvider_PreservesArgumentsForInactiveImplementations()
        {
            List<MiniCoreStartupCodeGenerator.AppServiceInfo> services = CreateServices();
            MiniCoreStartupSettings settings = CreateSettings(services);
            try
            {
                MiniCoreAppServiceSettings primarySettings = FindSettings(settings, typeof(PrimaryProvider));
                primarySettings.Arguments.Add(new MiniCoreStartupArgumentSettings
                {
                    MemberName = nameof(TestProviderArgs.Endpoint),
                    UseCodeDefault = false,
                    Value = "saved-endpoint"
                });

                AppServiceProviderConfiguration.SelectProvider(
                    settings,
                    services,
                    typeof(IPrimaryService),
                    FindProvider(services, typeof(PrimaryProvider)));
                AppServiceProviderConfiguration.SelectProvider(
                    settings,
                    services,
                    typeof(IPrimaryService),
                    FindProvider(services, typeof(AlternativePrimaryProvider)));

                Assert.IsFalse(primarySettings.Enabled);
                Assert.AreEqual("saved-endpoint", primarySettings.Arguments[0].Value);
                Assert.IsFalse(primarySettings.Arguments[0].UseCodeDefault);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        /// <summary>
        /// 验证依赖诊断只报告尚未选择唯一 Provider 的接口，且不会自动启用依赖。
        /// </summary>
        [Test]
        public void GetMissingDependencies_ReportsWithoutAutoSelecting()
        {
            List<MiniCoreStartupCodeGenerator.AppServiceInfo> services = CreateServices();
            MiniCoreStartupSettings settings = CreateSettings(services);
            try
            {
                MiniCoreStartupCodeGenerator.AppServiceInfo dependentProvider = FindProvider(services, typeof(DependentProvider));
                AppServiceProviderConfiguration.SelectProvider(settings, services, typeof(IDependentService), dependentProvider);
                List<AppServiceContractGroup> groups = AppServiceProviderConfiguration.BuildGroups(services, settings);

                CollectionAssert.AreEqual(
                    new[] { typeof(ISecondaryService) },
                    AppServiceProviderConfiguration.GetMissingDependencies(dependentProvider, groups));
                Assert.IsNull(FindGroup(groups, typeof(ISecondaryService)).SelectedProvider);

                MiniCoreStartupCodeGenerator.AppServiceInfo multiProvider = FindProvider(services, typeof(MultiContractProvider));
                AppServiceProviderConfiguration.SelectProvider(settings, services, typeof(ISecondaryService), multiProvider);
                groups = AppServiceProviderConfiguration.BuildGroups(services, settings);
                Assert.IsEmpty(AppServiceProviderConfiguration.GetMissingDependencies(dependentProvider, groups));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 创建覆盖单接口、多接口与依赖关系的测试服务描述。
        /// </summary>
        /// <returns>测试服务描述集合。</returns>
        private static List<MiniCoreStartupCodeGenerator.AppServiceInfo> CreateServices()
        {
            return new List<MiniCoreStartupCodeGenerator.AppServiceInfo>
            {
                CreateProvider<PrimaryProvider>("主实现", typeof(TestProviderArgs), typeof(IPrimaryService)),
                CreateProvider<AlternativePrimaryProvider>("备选实现", null, typeof(IPrimaryService)),
                CreateProvider<MultiContractProvider>("多接口实现", null, typeof(IPrimaryService), typeof(ISecondaryService)),
                CreateProvider<DependentProvider>("依赖实现", null, new[] { typeof(ISecondaryService) }, typeof(IDependentService))
            };
        }

        /// <summary>
        /// 创建一个测试 Provider 的编辑器描述。
        /// </summary>
        /// <typeparam name="TProvider">具体测试 Provider 类型。</typeparam>
        /// <param name="displayName">显示名称。</param>
        /// <param name="argsType">可选参数类型。</param>
        /// <param name="contracts">Provider 声明的服务接口。</param>
        /// <returns>测试 Provider 描述。</returns>
        private static MiniCoreStartupCodeGenerator.AppServiceInfo CreateProvider<TProvider>(
            string displayName,
            Type argsType,
            params Type[] contracts)
            where TProvider : AAppService
        {
            return CreateProvider<TProvider>(displayName, argsType, Array.Empty<Type>(), contracts);
        }

        /// <summary>
        /// 创建一个包含依赖声明的测试 Provider 编辑器描述。
        /// </summary>
        /// <typeparam name="TProvider">具体测试 Provider 类型。</typeparam>
        /// <param name="displayName">显示名称。</param>
        /// <param name="argsType">可选参数类型。</param>
        /// <param name="dependencies">依赖接口。</param>
        /// <param name="contracts">Provider 声明的服务接口。</param>
        /// <returns>测试 Provider 描述。</returns>
        private static MiniCoreStartupCodeGenerator.AppServiceInfo CreateProvider<TProvider>(
            string displayName,
            Type argsType,
            Type[] dependencies,
            params Type[] contracts)
            where TProvider : AAppService
        {
            var attribute = new AppServiceAttribute(displayName, contracts)
            {
                RequiresServices = dependencies,
                InitArgsType = argsType
            };
            return new MiniCoreStartupCodeGenerator.AppServiceInfo(typeof(TProvider), attribute, argsType);
        }

        /// <summary>
        /// 为全部测试 Provider 创建按具体实现保存的启动配置。
        /// </summary>
        /// <param name="services">测试服务描述。</param>
        /// <returns>临时启动配置资源。</returns>
        private static MiniCoreStartupSettings CreateSettings(List<MiniCoreStartupCodeGenerator.AppServiceInfo> services)
        {
            MiniCoreStartupSettings settings = ScriptableObject.CreateInstance<MiniCoreStartupSettings>();
            for (int index = 0; index < services.Count; index++)
            {
                settings.Services.Add(new MiniCoreAppServiceSettings
                {
                    AssemblyQualifiedTypeName = services[index].Type.AssemblyQualifiedName
                });
            }

            return settings;
        }

        /// <summary>
        /// 查找测试配置中的具体 Provider 设置。
        /// </summary>
        /// <param name="settings">测试启动配置。</param>
        /// <param name="providerType">Provider 类型。</param>
        /// <returns>对应设置。</returns>
        private static MiniCoreAppServiceSettings FindSettings(MiniCoreStartupSettings settings, Type providerType)
        {
            MiniCoreAppServiceSettings result = AppServiceProviderConfiguration.FindSettings(settings, providerType);
            Assert.IsNotNull(result, providerType.FullName);
            return result;
        }

        /// <summary>
        /// 查找测试服务集合中的具体 Provider 描述。
        /// </summary>
        /// <param name="services">测试服务描述。</param>
        /// <param name="providerType">Provider 类型。</param>
        /// <returns>对应 Provider 描述。</returns>
        private static MiniCoreStartupCodeGenerator.AppServiceInfo FindProvider(
            List<MiniCoreStartupCodeGenerator.AppServiceInfo> services,
            Type providerType)
        {
            for (int index = 0; index < services.Count; index++)
            {
                if (services[index].Type == providerType)
                {
                    return services[index];
                }
            }

            Assert.Fail($"未找到测试 Provider：{providerType.FullName}");
            return null;
        }

        /// <summary>
        /// 查找指定接口的测试分组。
        /// </summary>
        /// <param name="groups">测试分组集合。</param>
        /// <param name="contract">服务接口。</param>
        /// <returns>对应分组。</returns>
        private static AppServiceContractGroup FindGroup(List<AppServiceContractGroup> groups, Type contract)
        {
            for (int index = 0; index < groups.Count; index++)
            {
                if (groups[index].Contract == contract)
                {
                    return groups[index];
                }
            }

            Assert.Fail($"未找到测试接口分组：{contract.FullName}");
            return null;
        }

        /// <summary>
        /// 测试用主要服务接口。
        /// </summary>
        private interface IPrimaryService : IAppService
        {
        }

        /// <summary>
        /// 测试用次要服务接口。
        /// </summary>
        private interface ISecondaryService : IAppService
        {
        }

        /// <summary>
        /// 测试用依赖方服务接口。
        /// </summary>
        private interface IDependentService : IAppService
        {
        }

        /// <summary>
        /// 测试用主要 Provider。
        /// </summary>
        private sealed class PrimaryProvider : AAppService, IPrimaryService
        {
        }

        /// <summary>
        /// 测试用备选 Provider。
        /// </summary>
        private sealed class AlternativePrimaryProvider : AAppService, IPrimaryService
        {
        }

        /// <summary>
        /// 测试用多接口 Provider。
        /// </summary>
        private sealed class MultiContractProvider : AAppService, IPrimaryService, ISecondaryService
        {
        }

        /// <summary>
        /// 测试用依赖方 Provider。
        /// </summary>
        private sealed class DependentProvider : AAppService, IDependentService
        {
        }

        /// <summary>
        /// 测试用 Provider 参数。
        /// </summary>
        private sealed class TestProviderArgs : ComponentInitArgs
        {
            #region Public 公共成员

            /// <summary>
            /// 获取或设置测试端点。
            /// </summary>
            public string Endpoint { get; set; } = "default";

            #endregion
        }

        #endregion
    }
}
