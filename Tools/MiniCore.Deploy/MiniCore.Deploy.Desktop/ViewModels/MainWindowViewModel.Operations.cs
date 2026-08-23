using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Desktop.ViewModels;

/// <summary>
/// 提供发布方式的中文选项与详细影响说明。
/// </summary>
public sealed partial class MainWindowViewModel
{
    #region Private 私有成员

    private static readonly DeploymentOperationOptionViewModel[] OperationOptions =
    {
        new(DeploymentOperation.FirstInstall, "首次完整发布", "目标环境尚未安装 MiniCore 时使用，上传完整制品、创建服务并按顺序启动。", "会创建远程目录和系统服务；启动 Coordinator、可选 Auth/DB 与业务 DS。"),
        new(DeploymentOperation.FullRelease, "全量版本更新", "已有环境升级到新的完整 ReleaseVersion，按实例执行 Drain、切换与健康检查。", "优先滚动更新；更新最后实例或 Coordinator 时要求人工确认。"),
        new(DeploymentOperation.BusinessRelease, "业务内容更新", "只更新兼容的 HotUpdate 程序集与 YooAsset 内容，不改固定控制协议。", "适合业务逻辑和资源变化；必须保持控制协议兼容。"),
        new(DeploymentOperation.MaintenanceRelease, "维护窗口更新", "控制协议不兼容或环境没有冗余时，在明确维护窗口内完成全停更新。", "会造成实际停服，所有关键停止步骤都需要人工确认。"),
        new(DeploymentOperation.ScaleOut, "横向扩容", "复用当前 ReleaseVersion 的已有制品，在目标主机新增一个服务实例。", "不重启现有实例；必须选择目标实例并确保端口与 Instance ID 唯一。"),
        new(DeploymentOperation.ConfigurationUpdate, "配置更新", "只修改实例外部配置，并滚动重启受到影响的实例。", "不重新构建制品；配置哈希变化的实例才会处理。"),
        new(DeploymentOperation.Repair, "单实例修复", "对比远程制品、配置和服务定义，只修复指定实例的不一致项。", "只影响所选实例；不会扩大到其他正常实例。"),
        new(DeploymentOperation.Rollback, "回滚到历史版本", "切换到上一份完整 ReleaseManifest，并按兼容性选择滚动或维护窗口。", "协议不兼容时必须停服确认；不会自动回滚数据库结构。"),
        new(DeploymentOperation.RemoveInstance, "安全下线实例", "先 Drain 指定实例，确认无活动任务后停止并注销系统服务。", "保留远程配置和日志，不自动删除业务数据。")
    };

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 获取全部中文发布方式选项。
    /// </summary>
    public IReadOnlyList<DeploymentOperationOptionViewModel> DeploymentOperationOptions => OperationOptions;

    /// <summary>
    /// 获取或设置当前中文发布方式选项。
    /// </summary>
    public DeploymentOperationOptionViewModel SelectedDeploymentOperationOption
    {
        get => FindOperationOption(profile.Operation);
        set
        {
            if (value != null)
            {
                Operation = value.Operation;
            }
        }
    }

    /// <summary>
    /// 获取当前发布方式的用途说明。
    /// </summary>
    public string SelectedOperationDescription => FindOperationOption(profile.Operation).Description;

    /// <summary>
    /// 获取当前发布方式的影响范围说明。
    /// </summary>
    public string SelectedOperationImpact => FindOperationOption(profile.Operation).Impact;

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 查找指定枚举对应的中文发布方式选项。
    /// </summary>
    /// <param name="operation">发布操作枚举。</param>
    /// <returns>匹配的显示选项。</returns>
    private static DeploymentOperationOptionViewModel FindOperationOption(DeploymentOperation operation)
    {
        for (int index = 0; index < OperationOptions.Length; index++)
        {
            if (OperationOptions[index].Operation == operation)
            {
                return OperationOptions[index];
            }
        }

        return OperationOptions[0];
    }

    #endregion
}
