using System;
using System.Collections.Generic;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 服务器持久化的单个 MiniBomber 账号记录。
    /// </summary>
    [Serializable]
    public sealed class MiniBomberAccountRecord
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置稳定玩家身份。
        /// </summary>
        public long PlayerId { get; set; }

        /// <summary>
        /// 获取或设置登录账号。
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// 获取或设置游戏内唯一玩家名。
        /// </summary>
        public string PlayerName { get; set; }

        /// <summary>
        /// 获取或设置 Base64 随机盐。
        /// </summary>
        public string PasswordSalt { get; set; }

        /// <summary>
        /// 获取或设置 Base64 SHA-256 密码摘要。
        /// </summary>
        public string PasswordHash { get; set; }

        #endregion
    }
}
