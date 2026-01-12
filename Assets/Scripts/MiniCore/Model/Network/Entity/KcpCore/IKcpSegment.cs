namespace System.Net.Sockets.Kcp
{
    
    
    
    
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
    public interface IKcpSegment : IKcpHeader
    {
        
        
        
        uint resendts { get; set; }
        
        
        
        uint rto { get; set; }
        
        
        
        uint fastack { get; set; }
        
        
        
        uint xmit { get; set; }

        
        
        
        Span<byte> data { get; }
        
        
        
        
        
        int Encode(Span<byte> buffer);
    }

    public interface ISegmentManager<Segment> where Segment : IKcpSegment
    {
        Segment Alloc(int appendDateSize);
        void Free(Segment seg);
    }

}



