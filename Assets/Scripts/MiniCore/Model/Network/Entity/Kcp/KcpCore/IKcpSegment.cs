namespace System.Net.Sockets.Kcp
{
    
    
    
    
    /// <summary>
    /// KCP 固定包头字段接口。
    /// </summary>
    public interface IKcpHeader
    {
        
        
        
        uint conv { get; set; }
        
        
        
        
        
        
        
        
        
        byte cmd { get; set; }
        
        
        
        byte frg { get; set; }
        
        
        
        ushort wnd { get; set; }
        
        
        
        uint ts { get; set; }
        
        
        
        uint sn { get; set; }
        
        
        
        uint una { get; set; }
        
        
        
        uint len { get; }
    }
    /// <summary>
    /// 包含本地重传状态和负载的 KCP 分片接口。
    /// </summary>
    public interface IKcpSegment : IKcpHeader
    {
        
        
        
        uint resendts { get; set; }
        
        
        
        uint rto { get; set; }
        
        
        
        uint fastack { get; set; }
        
        
        
        uint xmit { get; set; }

        
        
        
        Span<byte> data { get; }
        
        
        
        
        
        int Encode(Span<byte> buffer);
    }

    /// <summary>
    /// 管理 KCP 分片分配与释放的接口。
    /// </summary>
    public interface ISegmentManager<Segment> where Segment : IKcpSegment
    {
        Segment Alloc(int appendDateSize);
        void Free(Segment seg);
    }

}


