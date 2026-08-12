namespace System.Net.Sockets.Kcp
{
    /// <summary>
    /// KCP 传输参数配置接口。
    /// </summary>
    public interface IKcpSetting
    {
        #region Public 公共成员

        /// <summary>
        /// 设置协议更新间隔。
        /// </summary>
        /// <param name="interval">协议更新间隔毫秒数。</param>
        /// <returns>零表示成功，负数表示参数无效。</returns>
        int Interval(int interval);

        /// <summary>
        /// 配置无延迟、更新间隔、快速重传和拥塞控制。
        /// </summary>
        /// <param name="nodelay">是否启用无延迟模式。</param>
        /// <param name="interval">协议更新间隔毫秒数。</param>
        /// <param name="resend">快速重传触发次数。</param>
        /// <param name="nc">是否关闭拥塞控制。</param>
        /// <returns>零表示配置完成。</returns>
        int NoDelay(int nodelay, int interval, int resend, int nc);

        /// <summary>
        /// 设置最大传输单元大小。
        /// </summary>
        /// <param name="mtu">最大传输单元字节数。</param>
        /// <returns>零表示成功，负数表示参数无效。</returns>
        int SetMtu(int mtu = 1400);

        /// <summary>
        /// 设置发送和接收窗口大小。
        /// </summary>
        /// <param name="sndwnd">发送窗口分片数。</param>
        /// <param name="rcvwnd">接收窗口分片数。</param>
        /// <returns>零表示配置完成。</returns>
        int WndSize(int sndwnd = 32, int rcvwnd = 128);

        #endregion
    }
}
