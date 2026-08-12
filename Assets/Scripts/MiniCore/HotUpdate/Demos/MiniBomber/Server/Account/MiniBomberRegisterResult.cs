using System;
using System.Collections.Generic;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// MiniBomber 注册操作结果。
    /// </summary>
    public readonly struct MiniBomberRegisterResult
    {
        #region Public 公共成员

        /// <summary>
        /// 获取注册业务错误码。
        /// </summary>
        public int Code { get; }

        /// <summary>
        /// 获取面向用户的注册结果消息。
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 获取成功创建的账号记录。
        /// </summary>
        public MiniBomberAccountRecord Account { get; }

        /// <summary>
        /// 获取注册操作是否成功。
        /// </summary>
        public bool Succeeded => Code == MiniBomberErrorCode.Success;

        /// <summary>
        /// 创建注册结果。
        /// </summary>
        /// <param name="code">业务错误码。</param>
        /// <param name="message">面向用户的结果消息。</param>
        /// <param name="account">成功创建的账号。</param>
        public MiniBomberRegisterResult(int code, string message, MiniBomberAccountRecord account)
        {
            Code = code;
            Message = message;
            Account = account;
        }

        #endregion
    }
}
