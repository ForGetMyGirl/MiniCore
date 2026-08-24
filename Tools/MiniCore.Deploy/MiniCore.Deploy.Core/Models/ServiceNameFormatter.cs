using System.Text;

namespace MiniCore.Deploy.Core.Models;

/// <summary>
/// 将稳定实例标识转换为 systemd 与 Windows 服务共同使用的规范名称。
/// </summary>
public static class ServiceNameFormatter
{
    #region Public 公共成员

    /// <summary>
    /// 将实例标识规范化为只含小写字母、数字、短横线和点的服务名。
    /// </summary>
    /// <param name="instanceId">环境内稳定实例标识。</param>
    /// <returns>带 minicore 前缀的规范服务名。</returns>
    public static string Format(string instanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        var builder = new StringBuilder(instanceId.Length + 9);
        builder.Append("minicore-");
        for (int index = 0; index < instanceId.Length; index++)
        {
            char character = char.ToLowerInvariant(instanceId[index]);
            builder.Append(char.IsLetterOrDigit(character) || character is '-' or '.' ? character : '-');
        }

        return builder.ToString();
    }

    #endregion
}
