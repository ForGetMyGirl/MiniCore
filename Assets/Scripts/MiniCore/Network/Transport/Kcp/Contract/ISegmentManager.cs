namespace System.Net.Sockets.Kcp
{
    /// <summary>
    /// 管理 KCP 分片分配与释放的接口。
    /// </summary>
    public interface ISegmentManager<Segment> where Segment : IKcpSegment
    {
        #region Public 公共成员

        /// <summary>
        /// 分配一个可容纳指定负载的分片。
        /// </summary>
        /// <param name="appendDateSize">需要附加的负载字节数。</param>
        /// <returns>新分配或从池中取得的分片。</returns>
        Segment Alloc(int appendDateSize);

        /// <summary>
        /// 释放不再使用的分片。
        /// </summary>
        /// <param name="seg">要释放的分片。</param>
        void Free(Segment seg);

        #endregion
    }
}
