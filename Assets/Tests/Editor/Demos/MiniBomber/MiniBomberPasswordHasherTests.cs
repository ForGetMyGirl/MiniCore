using System;
using MiniCore.Demo.MiniBomber;
using NUnit.Framework;

namespace MiniCore.Tests.Editor.Demos.MiniBomber
{
    /// <summary>
    /// MiniBomber 开发阶段随机盐密码摘要回归测试。
    /// </summary>
    public sealed class MiniBomberPasswordHasherTests
    {
        #region Public 公共成员

        /// <summary>
        /// 验证新账号使用随机盐 SHA-256 且能够通过正确密码。
        /// </summary>
        [Test]
        public void SaltedSha256_CorrectPassword_Verifies()
        {
            byte[] salt = MiniBomberPasswordHasher.CreateSalt();
            string digest = Convert.ToBase64String(MiniBomberPasswordHasher.Hash("demo-password", salt));

            Assert.That(MiniBomberPasswordHasher.Verify("demo-password", Convert.ToBase64String(salt), digest), Is.True);
        }

        /// <summary>
        /// 验证错误密码无法通过当前摘要的固定时间比较。
        /// </summary>
        [Test]
        public void SaltedSha256_WrongPassword_IsRejected()
        {
            byte[] salt = MiniBomberPasswordHasher.CreateSalt();
            string digest = Convert.ToBase64String(MiniBomberPasswordHasher.Hash("correct-password", salt));

            Assert.That(MiniBomberPasswordHasher.Verify("wrong-password", Convert.ToBase64String(salt), digest), Is.False);
        }

        /// <summary>
        /// 验证损坏或非当前格式的存档字段不会导致登录异常。
        /// </summary>
        [Test]
        public void MalformedStoredDigest_IsRejected()
        {
            Assert.That(MiniBomberPasswordHasher.Verify("demo-password", "not-base64", "also-not-base64"), Is.False);
        }

        #endregion
    }
}
