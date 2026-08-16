using System.Security.Cryptography;

namespace AuthenticationServer.Security;

/// <summary>
/// 使用 PBKDF2-SHA256 生成和固定时间验证账号密码摘要。
/// </summary>
public static class PasswordHashing
{
    #region Private 私有成员

    private const int Iterations = 210000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 为新密码生成随机盐和摘要。
    /// </summary>
    /// <param name="password">待派生摘要的原始密码。</param>
    /// <returns>Base64 盐与摘要。</returns>
    public static (string Salt, string Hash) Create(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return (Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    /// <summary>
    /// 固定时间验证输入密码是否与存档摘要一致。
    /// </summary>
    /// <param name="password">待验证密码。</param>
    /// <param name="saltText">Base64 盐。</param>
    /// <param name="hashText">Base64 期望摘要。</param>
    /// <returns>密码匹配时返回 true。</returns>
    public static bool Verify(string password, string saltText, string hashText)
    {
        try
        {
            byte[] salt = Convert.FromBase64String(saltText);
            byte[] expected = Convert.FromBase64String(hashText);
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    #endregion
}
