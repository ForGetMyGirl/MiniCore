using Cysharp.Threading.Tasks;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MiniCore.Model
{
    public class KcpServerSession
    {
        private readonly Socket socket;
        private readonly Kcp kcp;
        private readonly KcpServerConfig config;
        private readonly object kcpLock = new object();
        private bool closed;
        private uint lastRecvMs;

        public uint Conv { get; }
        public EndPoint RemoteEndPoint { get; }
        public string SessionId => $"{Conv}:{RemoteEndPoint}";
        public bool IsDead => kcp.IsDead;

        public event Action OnDisconnected;

        public KcpServerSession(uint conv, EndPoint remoteEndPoint, Socket socket, KcpServerConfig config)
        {
            Conv = conv;
            RemoteEndPoint = remoteEndPoint;
            this.socket = socket;
            this.config = config;
            lastRecvMs = CurrentMS();

            kcp = new Kcp(conv, KcpOutput);
            kcp.SetMtu(config.Mtu);
            kcp.WndSize(config.SendWindow, config.ReceiveWindow);
            kcp.NoDelay(config.NoDelay, config.Interval, config.Resend, config.NoCongestion);
            kcp.SetMinRto(config.MinRto);
            kcp.SetFastResend(config.FastResend);
            kcp.SetFastAck(config.FastAck);
            kcp.SetDeadLink(config.DeadLink);
            kcp.SetStreamMode(config.Stream);
        }

        public UniTask SendAsync(ArraySegment<byte> data, CancellationToken token = default)
        {
            if (closed)
            {
                throw new InvalidOperationException("KcpServerSession is closed; cannot send data.");
            }
            if (data.Array == null)
            {
                throw new ArgumentException("ArraySegment has no backing array.", nameof(data));
            }

            lock (kcpLock)
            {
                kcp.Send(data.Array, data.Offset, data.Count);
                kcp.Update(CurrentMS());
            }

            return UniTask.CompletedTask;
        }

        public bool Input(byte[] buffer, int size)
        {
            if (closed)
            {
                return false;
            }

            lock (kcpLock)
            {
                kcp.Input(buffer, 0, size);
                kcp.Update(CurrentMS());
                lastRecvMs = CurrentMS();
            }
            return true;
        }

        public bool TryReceive(out byte[] packet)
        {
            packet = null;
            if (closed)
            {
                return false;
            }

            lock (kcpLock)
            {
                int size = kcp.PeekSize();
                if (size <= 0)
                {
                    return false;
                }
                packet = new byte[size];
                int n = kcp.Receive(packet);
                if (n < 0)
                {
                    packet = null;
                    return false;
                }
                return true;
            }
        }

        public void Update(uint now)
        {
            if (closed)
            {
                return;
            }
            lock (kcpLock)
            {
                kcp.Update(now);
            }
        }

        public bool IsTimedOut(uint now, int timeoutMs)
        {
            if (timeoutMs <= 0)
            {
                return false;
            }
            int diff = (int)(now - lastRecvMs);
            return diff > timeoutMs;
        }

        public void Close()
        {
            if (closed)
            {
                return;
            }
            closed = true;
            OnDisconnected?.Invoke();
        }

        private void KcpOutput(byte[] buffer, int size)
        {
            if (socket == null || size <= 0 || closed)
            {
                return;
            }

            try
            {
                byte[] payload = ByteBufferPool.Shared.Rent(size);
                try
                {
                    Buffer.BlockCopy(buffer, 0, payload, 0, size);
                    socket.SendTo(payload, 0, size, SocketFlags.None, RemoteEndPoint);
                }
                finally
                {
                    ByteBufferPool.Shared.Return(payload);
                }
            }
            catch (Exception ex)
            {
                EventCenter.Broadcast(GameEvent.LogWarning, $"KcpServerSession output error: {ex.Message}");
            }
        }

        private static uint CurrentMS()
        {
            return unchecked((uint)Environment.TickCount);
        }
    }
}
