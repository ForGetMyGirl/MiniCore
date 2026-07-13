using System;

namespace System.Net.Sockets.Kcp
{
    /// <summary>
    /// 使用非托管内存分配 KCP 分片的默认管理器。
    /// </summary>
    public sealed class SimpleSegManager : ISegmentManager<KcpSegment>
    {
        /// <summary>
        /// 默认分片管理器实例。
        /// </summary>
        public static SimpleSegManager Default { get; } = new SimpleSegManager();

        /// <summary>
        /// 分配指定负载长度的 KCP 分片。
        /// </summary>
        /// <param name="appendDateSize">执行该方法所需的 appendDateSize 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public KcpSegment Alloc(int appendDateSize)
        {
            return KcpSegment.AllocHGlobal(appendDateSize);
        }

        /// <summary>
        /// 释放 KCP 分片的非托管内存。
        /// </summary>
        /// <param name="seg">执行该方法所需的 seg 参数。</param>
        public void Free(KcpSegment seg)
        {
            KcpSegment.FreeHGlobal(seg);
        }

        /// <summary>
        /// 使用默认分片管理器的具体 KCP Core 类型。
        /// </summary>
        public class Kcp : Kcp<KcpSegment>
        {
            /// <summary>
            /// 使用 conv、输出回调和可选缓冲池创建 KCP Core。
            /// </summary>
            /// <param name="conv_">执行该方法所需的 conv_ 参数。</param>
            /// <param name="callback">执行该方法所需的 callback 参数。</param>
            /// <param name="rentable">执行该方法所需的 rentable 参数。</param>
            /// <returns>执行处理后的结果。</returns>
            public Kcp(uint conv_, IKcpCallback callback, IRentable rentable = null)
                : base(conv_, callback, rentable)
            {
                SegmentManager = Default;
            }
        }
    }
}
