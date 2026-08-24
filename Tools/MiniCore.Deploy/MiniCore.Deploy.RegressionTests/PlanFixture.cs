using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.RegressionTests;

/// <summary>
/// 为计划校验回归检查准备完全本地化的临时发布配置。
/// </summary>
internal sealed class PlanFixture : IDisposable
{
    #region Private 私有成员

    private readonly string rootPath; // 当前回归场景独占的临时目录。

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 获取待验证的发布配置。
    /// </summary>
    public DeploymentProfile Profile { get; }

    /// <summary>
    /// 创建客户端端点计划场景。
    /// </summary>
    /// <param name="outerAdvertisedUrl">Coordinator 客户端公布地址。</param>
    /// <param name="enforcePublicEndpointSafety">是否启用生产公网安全校验。</param>
    public PlanFixture(string outerAdvertisedUrl, bool enforcePublicEndpointSafety)
    {
        rootPath = Path.Combine(Path.GetTempPath(), "minicore-deploy-regression-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootPath);
        string unityPath = Path.Combine(rootPath, "Unity");
        string privateKeyPath = Path.Combine(rootPath, "deploy.key");
        string scenePath = Path.Combine(rootPath, "Server.unity");
        File.WriteAllText(unityPath, string.Empty);
        File.WriteAllText(privateKeyPath, string.Empty);
        File.WriteAllText(scenePath, string.Empty);

        Profile = new DeploymentProfile
        {
            Project = new ProjectDefinition
            {
                UnityExecutablePath = unityPath,
                ProjectPath = rootPath,
                OutputPath = Path.Combine(rootPath, "output"),
                ServerScenePath = "Server.unity"
            },
            Environment = new EnvironmentDefinition
            {
                EnvironmentId = "regression",
                ReleaseVersion = "1.0.0",
                EnforcePublicEndpointSafety = enforcePublicEndpointSafety
            }
        };
        Profile.Project.BuildTargets.Add(BuildTargetKind.ServerLinuxX64);
        Profile.Project.PublishTargets.Add(BuildTargetKind.ServerLinuxX64);
        Profile.Environment.Hosts.Add(new HostDefinition
        {
            HostId = "host-1",
            Address = "203.0.113.10",
            PrivateAddress = "10.0.0.10",
            SshPort = 22,
            UserName = "deploy",
            PrivateKeyPath = privateKeyPath,
            HostKeyFingerprint = "SHA256:regression",
            OperatingSystem = HostOperatingSystem.Linux,
            DeploymentRoot = "/opt/minicore"
        });
        Profile.Environment.Instances.Add(new InstanceDefinition
        {
            InstanceId = "coordinator-1",
            HostId = "host-1",
            Component = ComponentKind.Coordinator,
            InnerPort = 7000,
            OuterPort = 7001,
            ManagementPort = 7099,
            OuterAdvertisedUrl = outerAdvertisedUrl,
            Roles = new List<string> { "Coordinator" }
        });
    }

    /// <summary>
    /// 删除当前场景创建的独占临时目录。
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(rootPath))
        {
            Directory.Delete(rootPath, true);
        }
    }

    #endregion
}
