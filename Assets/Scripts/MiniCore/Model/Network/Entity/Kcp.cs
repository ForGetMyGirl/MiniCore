using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets.Kcp;

namespace MiniCore.Model
{
    public delegate void KcpOutput(byte[] buffer, int size);

    public sealed class Kcp
    {
        public const int IKCP_OVERHEAD = KcpConst.IKCP_OVERHEAD;

        private readonly CoreKcp core;
        private readonly uint startTick;
        private readonly DateTimeOffset startTime;

        public bool IsDead => core.IsDead;

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

        public int NoDelay(int nodelay, int interval, int resend, int nc)
        {
            return core.NoDelay(nodelay, interval, resend, nc);
        }

        public int WndSize(int sndwnd, int rcvwnd)
        {
            return core.WndSize(sndwnd, rcvwnd);
        }

        public int SetMtu(int mtu)
        {
            return core.SetMtu(mtu);
        }

        public int SetMinRto(int minrto)
        {
            core.SetMinRto(minrto);
            return 0;
        }

        public void SetDeadLink(int deadlink)
        {
            core.SetDeadLink(deadlink);
        }

        public void SetFastResend(int fastresend)
        {
            core.SetFastResend(fastresend);
        }

        public void SetFastAck(int fastack)
        {
            core.SetFastAck(fastack);
        }

        public void SetStreamMode(bool enable)
        {
            core.SetStreamMode(enable);
        }

        public int Send(byte[] buffer, int offset, int len)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            return core.Send(new ReadOnlySpan<byte>(buffer, offset, len));
        }

        public int Receive(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            return core.Recv(buffer);
        }

        public int PeekSize()
        {
            return core.PeekSize();
        }

        public int GetSmoothedRttMs()
        {
            return core.GetSmoothedRttMs();
        }

        public int Input(byte[] data, int offset, int size)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return core.Input(new ReadOnlySpan<byte>(data, offset, size));
        }

        public void Update(uint current)
        {
            core.Update(ToTime(current));
        }

        public uint Check(uint current)
        {
            return FromTime(core.Check(ToTime(current)));
        }

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

        private DateTimeOffset ToTime(uint tick)
        {
            uint delta = unchecked(tick - startTick);
            return startTime.AddMilliseconds(delta);
        }

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

            public OutputAdapter(KcpOutput output)
            {
                this.output = output;
            }

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
            public CoreKcp(uint conv, IKcpCallback callback)
                : base(conv, callback)
            {
            }

            public bool IsDead => state == -1;

            public void SetMinRto(int minrto)
            {
                rx_minrto = (uint)minrto;
            }

            public void SetDeadLink(int deadlink)
            {
                dead_link = (uint)deadlink;
            }

            public void SetFastResend(int fastresend)
            {
                this.fastresend = fastresend;
            }

            public void SetFastAck(int fastack)
            {
                fastlimit = fastack;
            }

            public void SetStreamMode(bool enable)
            {
                stream = enable ? 1 : 0;
            }

            public int GetSmoothedRttMs()
            {
                return (int)rx_srtt;
            }
        }
    }
}
