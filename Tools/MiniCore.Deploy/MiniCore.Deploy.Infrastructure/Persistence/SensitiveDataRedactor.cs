using System.Text.RegularExpressions;

namespace MiniCore.Deploy.Infrastructure.Persistence;

/// <summary>
/// 对进入执行日志和发布历史的文本执行统一凭据脱敏。
/// </summary>
public static partial class SensitiveDataRedactor
{
    #region Public 公共成员

    /// <summary>
    /// 移除常见密码、令牌、私钥和连接字符串中的凭据值。
    /// </summary>
    /// <param name="value">可能含敏感信息的文本。</param>
    /// <returns>可以安全进入本地日志的文本。</returns>
    public static string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        string redacted = PemBlockRegex().Replace(value, "[REDACTED PRIVATE KEY]");
        redacted = JsonSecretRegex().Replace(redacted, static match => match.Groups[1].Value + "\"***\"");
        return AssignmentSecretRegex().Replace(redacted, static match => match.Groups[1].Value + "=***");
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 匹配 JSON 中的敏感属性值。
    /// </summary>
    /// <returns>已编译正则表达式。</returns>
    [GeneratedRegex("(\\\"(?:password|pwd|token|passphrase|privateKey|private_key|secret|accessKey|access_key)\\\"\\s*:\\s*)\\\"(?:\\\\.|[^\\\"])*\\\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex JsonSecretRegex();

    /// <summary>
    /// 匹配参数、连接字符串和普通键值文本中的敏感值。
    /// </summary>
    /// <returns>已编译正则表达式。</returns>
    [GeneratedRegex("((?:password|pwd|token|passphrase|private[_ -]?key|secret|access[_ -]?key)\\s*)[:=]\\s*[^;,\\s}]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AssignmentSecretRegex();

    /// <summary>
    /// 匹配完整 PEM 私钥区块。
    /// </summary>
    /// <returns>已编译正则表达式。</returns>
    [GeneratedRegex("-----BEGIN [^-]*PRIVATE KEY-----[\\s\\S]*?-----END [^-]*PRIVATE KEY-----", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PemBlockRegex();

    #endregion
}
