using System.Text;
using MiniCore.Deploy.Core.Models;

namespace MiniCore.Deploy.Infrastructure.Remote;

/// <summary>
/// 将固定发布动作编码为 Linux Shell 或 Windows PowerShell 命令。
/// </summary>
public static class RemoteCommandBuilder
{
    #region Public 公共成员

    /// <summary>
    /// 构建目标系统的命令文本。
    /// </summary>
    /// <param name="host">目标主机。</param>
    /// <param name="linuxCommand">只包含经过引用值的 Linux 命令。</param>
    /// <param name="windowsScript">只包含经过引用值的 PowerShell 脚本。</param>
    /// <returns>可交给 SSH 执行的命令。</returns>
    public static string ForHost(HostDefinition host, string linuxCommand, string windowsScript)
    {
        ArgumentNullException.ThrowIfNull(host);
        return host.OperatingSystem == HostOperatingSystem.Linux
            ? "sh -lc " + QuoteLinux(linuxCommand)
            : EncodePowerShell(windowsScript);
    }

    /// <summary>
    /// 使用 POSIX 单引号安全引用一个路径或标识。
    /// </summary>
    /// <param name="value">待引用值。</param>
    /// <returns>Shell 安全字面量。</returns>
    public static string QuoteLinux(string value)
    {
        return "'" + value.Replace("'", "'\"'\"'", StringComparison.Ordinal) + "'";
    }

    /// <summary>
    /// 使用 PowerShell 单引号安全引用一个路径或标识。
    /// </summary>
    /// <param name="value">待引用值。</param>
    /// <returns>PowerShell 安全字面量。</returns>
    public static string QuotePowerShell(string value)
    {
        return "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
    }

    /// <summary>
    /// 将脚本编码为 PowerShell EncodedCommand，避免 SSH Shell 二次解释参数。
    /// </summary>
    /// <param name="script">PowerShell 脚本。</param>
    /// <returns>远程命令。</returns>
    public static string EncodePowerShell(string script)
    {
        byte[] bytes = Encoding.Unicode.GetBytes("$ErrorActionPreference='Stop';" + script);
        return "powershell.exe -NoLogo -NoProfile -NonInteractive -EncodedCommand " + Convert.ToBase64String(bytes);
    }

    #endregion
}
