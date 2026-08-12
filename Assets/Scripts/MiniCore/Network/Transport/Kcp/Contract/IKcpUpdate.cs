using System;

namespace System.Net.Sockets.Kcp
{
    /// <summary>
    /// KCP 定时更新接口。
    /// </summary>
    public interface IKcpUpdate
    {
        #region Public 公共成员

        /// <summary>
        /// 以指定时间推进 KCP 协议状态。
        /// </summary>
        /// <param name="time">当前协议时间。</param>
        void Update(in DateTimeOffset time);

        #endregion
    }
}
