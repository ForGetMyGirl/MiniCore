namespace MiniCore.Deploy.Infrastructure.Build;

/// <summary>
/// 表示 Unity 构建入口写出的机器可读结果。
/// </summary>
public sealed class UnityBuildResponse
{
    #region Public 公共成员

    /// <summary>
    /// 获取或设置是否全部成功。
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// 获取或设置结果摘要。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置成功输出路径。
    /// </summary>
    public string[] Outputs { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 获取或设置按目标记录的失败原因。
    /// </summary>
    public string[] Errors { get; set; } = Array.Empty<string>();

    #endregion
}
