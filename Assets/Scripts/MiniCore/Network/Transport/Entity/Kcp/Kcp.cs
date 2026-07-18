using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets.Kcp;

namespace MiniCore.Model
{
    /// <summary>
    /// KCP 输出分片时调用的 UDP 数据报发送回调。
    /// </summary>
    public delegate void KcpOutput(byte[] buffer, int size);

    /// <summary>
    /// 对底层 KCP Core 的兼容封装，提供字节数组 API。
    /// </summary>
    public sealed class Kcp
    {
        /// <summary>
        /// KCP 固定包头长度。
        /// </summary>
        public const int IKCP_OVERHEAD = KcpConst.IKCP_OVERHEAD;

        private readonly CoreKcp core;
        private readonly uint startTick;
        private readonly DateTimeOffset startTime;

        /// <summary>
        /// KCP 是否因重传超过 dead link 阈值而失效。
        /// </summary>
        public bool IsDead => core.IsDead;

        /// <summary>
        /// 使用 conv 和底层输出回调创建 KCP 实例。
        /// </summary>
        /// <param name="conv">执行该方法所需的 conv 参数。</param>
        /// <param name="output">执行该方法所需的 output 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public Kcp(uint conv, KcpOutput output)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            startTick = unchecked((uint)Environment.TickCount);
            startTime = DateTimeOffset.UtcNow;
            core = new CoreKcp(conv, new OutputAdapter(output));
        }

        /// <summary>
        /// 设置无延迟、刷新间隔、快速重传和拥塞控制参数。
        /// </summary>
        /// <param name="nodelay">执行该方法所需的 nodelay 参数。</param>
        /// <param name="interval">执行该方法所需的 interval 参数。</param>
        /// <param name="resend">执行该方法所需的 resend 参数。</param>
        /// <param name="nc">执行该方法所需的 nc 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public int NoDelay(int nodelay, int interval, int resend, int nc)
        {
            return core.NoDelay(nodelay, interval, resend, nc);
        }

        /// <summary>
        /// 设置发送和接收窗口大小。
        /// </summary>
        /// <param name="sndwnd">执行该方法所需的 sndwnd 参数。</param>
        /// <param name="rcvwnd">执行该方法所需的 rcvwnd 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public int WndSize(int sndwnd, int rcvwnd)
        {
            return core.WndSize(sndwnd, rcvwnd);
        }

        /// <summary>
        /// 设置最大传输单元。
        /// </summary>
        /// <param name="mtu">执行该方法所需的 mtu 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public int SetMtu(int mtu)
        {
            return core.SetMtu(mtu);
        }

        /// <summary>
        /// 设置最小重传超时。
        /// </summary>
        /// <param name="minrto">执行该方法所需的 minrto 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public int SetMinRto(int minrto)
        {
            core.SetMinRto(minrto);
            return 0;
        }

        /// <summary>
        /// 设置判定链路失效的最大重传次数。
        /// </summary>
        /// <param name="deadlink">执行该方法所需的 deadlink 参数。</param>
        public void SetDeadLink(int deadlink)
        {
            core.SetDeadLink(deadlink);
        }

        /// <summary>
        /// 设置快速重传参数。
        /// </summary>
        /// <param name="fastresend">执行该方法所需的 fastresend 参数。</param>
        public void SetFastResend(int fastresend)
        {
            core.SetFastResend(fastresend);
        }

        /// <summary>
        /// 设置快速确认次数上限。
        /// </summary>
        /// <param name="fastack">执行该方法所需的 fastack 参数。</param>
        public void SetFastAck(int fastack)
        {
            core.SetFastAck(fastack);
        }

        /// <summary>
        /// 启用或关闭 KCP 流模式。
        /// </summary>
        /// <param name="enable">执行该方法所需的 enable 参数。</param>
        public void SetStreamMode(bool enable)
        {
            core.SetStreamMode(enable);
        }

        /// <summary>
        /// 向 KCP 发送待可靠传输的业务数据。
        /// </summary>
        /// <param name="buffer">执行该方法所需的 buffer 参数。</param>
        /// <param name="offset">执行该方法所需的 offset 参数。</param>
        /// <param name="len">执行该方法所需的 len 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public int Send(byte[] buffer, int offset, int len)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            return core.Send(new ReadOnlySpan<byte>(buffer, offset, len));
        }

        /// <summary>
        /// 将下一条已重组业务包复制到目标缓冲区。
        /// </summary>
        /// <param name="buffer">执行该方法所需的 buffer 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public int Receive(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            return core.Recv(buffer);
        }

        /// <summary>
        /// 获取下一条已重组业务包的长度。
        /// </summary>
        /// <returns>执行处理后的结果。</returns>
        public int PeekSize()
        {
            return core.PeekSize();
        }

        /// <summary>
        /// 获取 KCP 计算出的平滑 RTT（毫秒）。
        /// </summary>
        /// <returns>执行处理后的结果。</returns>
        public int GetSmoothedRttMs()
        {
            return core.GetSmoothedRttMs();
        }

        /// <summary>
        /// 输入一个收到的 KCP 数据报。
        /// </summary>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <param name="offset">执行该方法所需的 offset 参数。</param>
        /// <param name="size">执行该方法所需的 size 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public int Input(byte[] data, int offset, int size)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return core.Input(new ReadOnlySpan<byte>(data, offset, size));
        }

        /// <summary>
        /// 推进 KCP 定时器并执行重传、确认等逻辑。
        /// </summary>
        /// <param name="current">执行该方法所需的 current 参数。</param>
        public void Update(uint current)
        {
            core.Update(ToTime(current));
        }

        /// <summary>
        /// 计算下一次应调用 Update 的时间点。
        /// </summary>
        /// <param name="current">执行该方法所需的 current 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public uint Check(uint current)
        {
            return FromTime(core.Check(ToTime(current)));
        }

        /// <summary>
        /// 从 KCP 数据报头读取 conv。
        /// </summary>
        /// <param name="buffer">执行该方法所需的 buffer 参数。</param>
        /// <param name="offset">执行该方法所需的 offset 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public static uint PeekConv(byte[] buffer, int offset)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0 || buffer.Length - offset < sizeof(uint))
            {
                return 0;
            }

            var span = new ReadOnlySpan<byte>(buffer, offset, sizeof(uint));
            return KcpConst.IsLittleEndian
                ? BinaryPrimitives.ReadUInt32LittleEndian(span)
                : BinaryPrimitives.ReadUInt32BigEndian(span);
        }

        /// <summary>
        /// 执行 ToTime 相关处理。
        /// </summary>
        /// <param name="tick">执行该方法所需的 tick 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private DateTimeOffset ToTime(uint tick)
        {
            uint delta = unchecked(tick - startTick);
            return startTime.AddMilliseconds(delta);
        }

        /// <summary>
        /// 执行 FromTime 相关处理。
        /// </summary>
        /// <param name="time">执行该方法所需的 time 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private uint FromTime(DateTimeOffset time)
        {
            var delta = time - startTime;
            if (delta <= TimeSpan.Zero)
            {
                return startTick;
            }
            return unchecked(startTick + (uint)delta.TotalMilliseconds);
        }

        private sealed class OutputAdapter : IKcpCallback
        {
            private readonly KcpOutput output;

            /// <summary>
            /// 执行 OutputAdapter 相关处理。
            /// </summary>
            /// <param name="output">执行该方法所需的 output 参数。</param>
            /// <returns>执行处理后的结果。</returns>
            public OutputAdapter(KcpOutput output)
            {
                this.output = output;
            }

            /// <summary>
            /// 执行 Output 相关处理。
            /// </summary>
            /// <param name="buffer">执行该方法所需的 buffer 参数。</param>
            /// <param name="avalidLength">执行该方法所需的 avalidLength 参数。</param>
            public void Output(IMemoryOwner<byte> buffer, int avalidLength)
            {
                try
                {
                    if (buffer == null || avalidLength <= 0)
                    {
                        return;
                    }

                    var payload = buffer.Memory.Span.Slice(0, avalidLength).ToArray();
                    output(payload, avalidLength);
                }
                finally
                {
                    buffer?.Dispose();
                }
            }
        }

        private sealed class CoreKcp : SimpleSegManager.Kcp
        {
            /// <summary>
            /// 执行 CoreKcp 相关处理。
            /// </summary>
            /// <param name="conv">执行该方法所需的 conv 参数。</param>
            /// <param name="callback">执行该方法所需的 callback 参数。</param>
            /// <returns>执行处理后的结果。</returns>
            public CoreKcp(uint conv, IKcpCallback callback)
                : base(conv, callback)
            {
            }

            /// <summary>
            /// 网络模块公开成员 IsDead 的说明。
            /// </summary>
            public bool IsDead => state == -1;

            /// <summary>
            /// 执行 SetMinRto 相关处理。
            /// </summary>
            /// <param name="minrto">执行该方法所需的 minrto 参数。</param>
            public void SetMinRto(int minrto)
            {
                rx_minrto = (uint)minrto;
            }

            /// <summary>
            /// 执行 SetDeadLink 相关处理。
            /// </summary>
            /// <param name="deadlink">执行该方法所需的 deadlink 参数。</param>
            public void SetDeadLink(int deadlink)
            {
                dead_link = (uint)deadlink;
            }

            /// <summary>
            /// 执行 SetFastResend 相关处理。
            /// </summary>
            /// <param name="fastresend">执行该方法所需的 fastresend 参数。</param>
            public void SetFastResend(int fastresend)
            {
                this.fastresend = fastresend;
            }

            /// <summary>
            /// 执行 SetFastAck 相关处理。
            /// </summary>
            /// <param name="fastack">执行该方法所需的 fastack 参数。</param>
            public void SetFastAck(int fastack)
            {
                fastlimit = fastack;
            }

            /// <summary>
            /// 执行 SetStreamMode 相关处理。
            /// </summary>
            /// <param name="enable">执行该方法所需的 enable 参数。</param>
            public void SetStreamMode(bool enable)
            {
                stream = enable ? 1 : 0;
            }

            /// <summary>
            /// 执行 GetSmoothedRttMs 相关处理。
            /// </summary>
            /// <returns>执行处理后的结果。</returns>
            public int GetSmoothedRttMs()
            {
                return (int)rx_srtt;
            }
        }
    }
}
