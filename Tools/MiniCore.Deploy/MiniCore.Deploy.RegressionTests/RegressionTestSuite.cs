using System.ComponentModel;
using MiniCore.Deploy.Core.Exceptions;
using MiniCore.Deploy.Core.Models;
using MiniCore.Deploy.Core.Planning;
using MiniCore.Deploy.Desktop.ViewModels;

namespace MiniCore.Deploy.RegressionTests;

/// <summary>
/// 覆盖主机地址继承、客户端 URL 风险和动态可选服务拓扑的回归场景。
/// </summary>
internal static class RegressionTestSuite
{
    #region Public 公共成员

    /// <summary>
    /// 按固定顺序执行全部回归检查。
    /// </summary>
    public static void RunAll()
    {
        VerifyHostAddressInheritanceAndSwitching();
        VerifyInstanceAddressOverrideSurvivesHostSwitch();
        VerifyProductionRejectsPrivateClientEndpoint();
        VerifyProductionAcceptsPublicSecureEndpoint();
        VerifyDevelopmentRequiresPrivateEndpointApproval();
        VerifyDynamicOptionalComponentAvailability();
        VerifyInstanceEditorSignalsTopologyChanges();
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 验证未覆盖实例会跟随当前所选主机的 VPC 地址。
    /// </summary>
    private static void VerifyHostAddressInheritanceAndSwitching()
    {
        var hosts = new List<HostDefinition>
        {
            new() { HostId = "host-a", PrivateAddress = "10.0.0.10" },
            new() { HostId = "host-b", PrivateAddress = "10.0.1.10" }
        };
        var instance = new InstanceDefinition { HostId = "host-a" };
        RegressionAssert.Equal(
            "10.0.0.10",
            InstanceNetworkAddressResolver.ResolveInnerAdvertisedHost(hosts, instance),
            "实例应继承第一台主机的 VPC 地址。");

        instance.HostId = "host-b";
        RegressionAssert.Equal(
            "10.0.1.10",
            InstanceNetworkAddressResolver.ResolveInnerAdvertisedHost(hosts, instance),
            "未覆盖实例切换主机后应跟随新主机 VPC 地址。");
    }

    /// <summary>
    /// 验证实例显式覆盖不会在切换主机时被静默替换。
    /// </summary>
    private static void VerifyInstanceAddressOverrideSurvivesHostSwitch()
    {
        var hosts = new List<HostDefinition>
        {
            new() { HostId = "host-a", PrivateAddress = "10.0.0.10" },
            new() { HostId = "host-b", PrivateAddress = "10.0.1.10" }
        };
        var instance = new InstanceDefinition
        {
            HostId = "host-a",
            InnerAdvertisedHost = "service.internal.example"
        };
        instance.HostId = "host-b";
        RegressionAssert.Equal(
            "service.internal.example",
            InstanceNetworkAddressResolver.ResolveInnerAdvertisedHost(hosts, instance),
            "显式覆盖必须在主机切换后保持不变。");
    }

    /// <summary>
    /// 验证生产公网安全策略拒绝私网或本机客户端 URL。
    /// </summary>
    private static void VerifyProductionRejectsPrivateClientEndpoint()
    {
        using var fixture = new PlanFixture("ws://127.0.0.1:7001/minicore", true);
        bool rejected = false;
        try
        {
            _ = new DeploymentPlanBuilder().Build(fixture.Profile);
        }
        catch (PlanValidationException)
        {
            rejected = true;
        }

        RegressionAssert.True(rejected, "生产安全策略必须拒绝回环客户端地址。");
    }

    /// <summary>
    /// 验证生产公网安全策略接受公网 WSS 绝对地址。
    /// </summary>
    private static void VerifyProductionAcceptsPublicSecureEndpoint()
    {
        using var fixture = new PlanFixture("wss://coordinator.example.com/minicore", true);
        DeploymentPlan plan = new DeploymentPlanBuilder().Build(fixture.Profile);
        RegressionAssert.True(
            plan.Steps.Count > 0 && !plan.Steps[0].RequiresApproval,
            "公网 WSS 地址不应产生客户端端点风险确认。");
    }

    /// <summary>
    /// 验证非生产方案使用私网或未加密客户端 URL 时要求人工确认。
    /// </summary>
    private static void VerifyDevelopmentRequiresPrivateEndpointApproval()
    {
        using var fixture = new PlanFixture("ws://10.0.0.20:7001/minicore", false);
        DeploymentPlan plan = new DeploymentPlanBuilder().Build(fixture.Profile);
        RegressionAssert.True(
            plan.Steps.Count > 0 && plan.Steps[0].RequiresApproval,
            "非生产私网客户端地址必须把预检标记为人工确认步骤。");
    }

    /// <summary>
    /// 验证动态新增、禁用和删除 Auth/DB 后可用性与失效选择同步。
    /// </summary>
    private static void VerifyDynamicOptionalComponentAvailability()
    {
        var instances = new List<InstanceDefinition>();
        var project = new ProjectDefinition();
        var authentication = new InstanceDefinition
        {
            Component = ComponentKind.AuthenticationServer,
            Enabled = true
        };
        var database = new InstanceDefinition
        {
            Component = ComponentKind.DatabaseServer,
            Enabled = true
        };
        instances.Add(authentication);
        instances.Add(database);
        RegressionAssert.True(
            BuildTargetTopologyPolicy.IsOptionalComponentEnabled(instances, BuildTargetKind.AuthenticationServer),
            "动态新增 Auth 后认证目标应立即可用。");
        RegressionAssert.True(
            BuildTargetTopologyPolicy.IsOptionalComponentEnabled(instances, BuildTargetKind.DatabaseServer),
            "动态新增 DB 后数据库目标应立即可用。");

        project.BuildTargets.Add(BuildTargetKind.AuthenticationServer);
        project.PublishTargets.Add(BuildTargetKind.AuthenticationServer);
        project.BuildTargets.Add(BuildTargetKind.DatabaseServer);
        project.PublishTargets.Add(BuildTargetKind.DatabaseServer);
        authentication.Enabled = false;
        BuildTargetTopologyPolicy.RemoveUnavailableOptionalTargets(project, instances);
        RegressionAssert.True(
            !project.BuildTargets.Contains(BuildTargetKind.AuthenticationServer)
            && !project.PublishTargets.Contains(BuildTargetKind.AuthenticationServer),
            "禁用最后一个 Auth 后必须清除无效构建与发布选择。");

        instances.Remove(database);
        BuildTargetTopologyPolicy.RemoveUnavailableOptionalTargets(project, instances);
        RegressionAssert.True(
            !project.BuildTargets.Contains(BuildTargetKind.DatabaseServer)
            && !project.PublishTargets.Contains(BuildTargetKind.DatabaseServer),
            "删除最后一个 DB 后必须清除无效构建与发布选择。");
    }

    /// <summary>
    /// 验证实例编辑器会为组件和启用状态变化发出确定通知。
    /// </summary>
    private static void VerifyInstanceEditorSignalsTopologyChanges()
    {
        var model = new InstanceDefinition();
        var hosts = new List<HostDefinition>();
        var editor = new InstanceEditorViewModel(model, hosts);
        bool componentChanged = false;
        bool enabledChanged = false;
        editor.PropertyChanged += OnPropertyChanged;
        editor.Component = ComponentKind.AuthenticationServer;
        editor.Enabled = false;
        editor.PropertyChanged -= OnPropertyChanged;
        RegressionAssert.True(componentChanged, "组件类型变化必须通知构建目标刷新链。");
        RegressionAssert.True(enabledChanged, "启用状态变化必须通知构建目标刷新链。");
        return;

        /// <summary>
        /// 记录本场景关心的属性变化。
        /// </summary>
        /// <param name="sender">事件来源。</param>
        /// <param name="eventArgs">属性变化参数。</param>
        void OnPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
        {
            componentChanged |= string.Equals(eventArgs.PropertyName, nameof(InstanceEditorViewModel.Component), StringComparison.Ordinal);
            enabledChanged |= string.Equals(eventArgs.PropertyName, nameof(InstanceEditorViewModel.Enabled), StringComparison.Ordinal);
        }
    }

    #endregion
}
