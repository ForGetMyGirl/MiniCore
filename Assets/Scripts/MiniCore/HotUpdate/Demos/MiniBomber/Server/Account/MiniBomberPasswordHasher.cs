using System;
using System.Security.Cryptography;
using System.Text;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 开发阶段使用的随机盐 SHA-256 密码摘要工具。
    /// 该实现只用于内网 Demo，不提供旧格式兼容或生产级慢哈希防护。
    /// </summary>
    public static class MiniBomberPasswordHasher
    {
        #region Public 公共成员

        /// <summary>
        /// 创建密码摘要使用的密码学随机盐。
        /// </summary>
        /// <returns>固定长度随机盐。</returns>
        public static byte[] CreateSalt()
        {
            var salt = new byte[SaltByteCount];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create())
            {
                random.GetBytes(salt);
            }

            return salt;
        }

        /// <summary>
        /// 使用随机盐和 UTF-8 密码计算 SHA-256 摘要。
        /// </summary>
        /// <param name="password">不会写入日志或存档的原始密码。</param>
        /// <param name="salt">密码学随机盐。</param>
        /// <returns>固定长度密码摘要。</returns>
        public static byte[] Hash(string password, byte[] salt)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            if (salt == null || salt.Length == 0)
            {
                throw new ArgumentException("密码盐不能为空。", nameof(salt));
            }

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            var source = new byte[salt.Length + passwordBytes.Length];
            Buffer.BlockCopy(salt, 0, source, 0, salt.Length);
            Buffer.BlockCopy(passwordBytes, 0, source, salt.Length, passwordBytes.Length);
            using SHA256 sha256 = SHA256.Create();
            byte[] digest = sha256.ComputeHash(source);
            CryptographicOperations.ZeroMemory(passwordBytes);
            CryptographicOperations.ZeroMemory(source);
            return digest;
        }

        /// <summary>
        /// 解析当前 Demo 存档字段并以固定时间方式验证密码。
        /// </summary>
        /// <param name="password">原始密码。</param>
        /// <param name="saltBase64">Base64 随机盐。</param>
        /// <param name="digestBase64">Base64 期望摘要。</param>
        /// <returns>字段合法且密码一致时返回 true。</returns>
        public static bool Verify(string password, string saltBase64, string digestBase64)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(saltBase64) || string.IsNullOrEmpty(digestBase64))
            {
                return false;
            }

            try
            {
                byte[] salt = Convert.FromBase64String(saltBase64);
                byte[] expected = Convert.FromBase64String(digestBase64);
                byte[] actual = Hash(password, salt);
                bool verified = FixedTimeEquals(expected, actual);
                CryptographicOperations.ZeroMemory(actual);
                return verified;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        #endregion

        #region Private 私有成员

        private const int SaltByteCount = 16; // Demo 账号使用的随机盐长度。

        /// <summary>
        /// 以与内容无关的循环比较两个摘要。
        /// </summary>
        /// <param name="left">期望摘要。</param>
        /// <param name="right">实际摘要。</param>
        /// <returns>长度和所有字节均一致时返回 true。</returns>
        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            int difference = 0;
            for (int index = 0; index < left.Length; index++)
            {
                difference |= left[index] ^ right[index];
            }

            return difference == 0;
        }

        #endregion
    }
}
