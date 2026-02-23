using System;

namespace System.Net.Sockets.Kcp
{
    public sealed class SimpleSegManager : ISegmentManager<KcpSegment>
    {
        public static SimpleSegManager Default { get; } = new SimpleSegManager();

        public KcpSegment Alloc(int appendDateSize)
        {
            return KcpSegment.AllocHGlobal(appendDateSize);
        }

        public void Free(KcpSegment seg)
        {
            KcpSegment.FreeHGlobal(seg);
        }

        public class Kcp : Kcp<KcpSegment>
        {
            public Kcp(uint conv_, IKcpCallback callback, IRentable rentable = null)
                : base(conv_, callback, rentable)
            {
                SegmentManager = Default;
            }
        }
    }
}