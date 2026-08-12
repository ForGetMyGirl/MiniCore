using System;
using System.Collections.Generic;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 服务器账号数据库序列化根对象。
    /// </summary>
    [Serializable]
    public sealed class MiniBomberAccountDatabase
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置下一个可分配玩家身份。
        /// </summary>
        public long NextPlayerId { get; set; } = 1;

        /// <summary>
        /// 获取或设置全部已注册账号。
        /// </summary>
        public List<MiniBomberAccountRecord> Accounts { get; set; } = new List<MiniBomberAccountRecord>();

        #endregion
    }
}
