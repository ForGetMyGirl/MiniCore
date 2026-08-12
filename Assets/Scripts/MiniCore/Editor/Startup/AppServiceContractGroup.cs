using System;
using System.Collections.Generic;
using MiniCore.Unity;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 启动配置窗口中按服务接口组织的 Provider 分组。
    /// </summary>
    internal sealed class AppServiceContractGroup
    {
        #region Internal 内部成员

        internal Type Contract { get; }
        internal List<MiniCoreStartupCodeGenerator.AppServiceInfo> Providers { get; }
        internal List<MiniCoreStartupCodeGenerator.AppServiceInfo> EnabledProviders { get; }
        internal bool HasConflict => EnabledProviders.Count > 1;
        internal MiniCoreStartupCodeGenerator.AppServiceInfo SelectedProvider => EnabledProviders.Count == 1 ? EnabledProviders[0] : null;

        /// <summary>
        /// 创建一个接口级 Provider 分组。
        /// </summary>
        /// <param name="contract">当前分组对应的服务接口。</param>
        /// <param name="providers">实现该接口的全部 Provider。</param>
        /// <param name="enabledProviders">当前配置中已启用的 Provider。</param>
        internal AppServiceContractGroup(
            Type contract,
            List<MiniCoreStartupCodeGenerator.AppServiceInfo> providers,
            List<MiniCoreStartupCodeGenerator.AppServiceInfo> enabledProviders)
        {
            Contract = contract;
            Providers = providers;
            EnabledProviders = enabledProviders;
        }

        #endregion
    }
}
