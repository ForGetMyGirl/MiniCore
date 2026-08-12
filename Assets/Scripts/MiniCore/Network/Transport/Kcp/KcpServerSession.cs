using MiniCore.Threading;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// 服务端按 conv 和远端地址维护的 KCP 逻辑会话。
    /// </summary>
    public class KcpServerSession : IServerSession
    {
        private readonly Socket socket; // 所属 KCP 服务端的 UDP 套接字。
        private readonly Kcp kcp; // 此会话独占的 KCP 协议实例。
        private readonly KcpServerConfig config; // KCP 会话配置。
        private readonly object kcpLock = new object(); // KCP 状态读写同步锁。
        private bool closed; // 会话关闭状态。
        private uint lastRecvMs; // 最近一次接收 KCP 数据的时间戳。

        /// <summary>
        /// KCP 会话标识 conv。
        /// </summary>
        public uint Conv { get; }
        /// <summary>
        /// 会话对应的远端网络终结点。
        /// </summary>
        public EndPoint RemoteEndPoint { get; }
        /// <summary>
        /// 由 conv 和远端终结点构成的会话标识。
        /// </summary>
        public string SessionId => $"{Conv}:{RemoteEndPoint}";
        /// <summary>
        /// 会话是否尚未关闭。
        /// </summary>
        public bool IsConnected => !closed;
        /// <summary>
        /// KCP 是否已因超过 dead link 阈值失效。
        /// </summary>
        public bool IsDead => kcp.IsDead;

        /// <summary>
        /// 会话关闭时触发。
        /// </summary>
        public event Action OnDisconnected;

        /// <summary>
        /// 使用指定 conv、远端地址和服务端套接字创建 KCP 会话。
        /// </summary>
        /// <param name="conv">执行该方法所需的 conv 参数。</param>
        /// <param name="remoteEndPoint">执行该方法所需的 remoteEndPoint 参数。</param>
        /// <param name="socket">执行该方法所需的 socket 参数。</param>
        /// <param name="config">执行该方法所需的 config 参数。</param>
        /// <returns>执行处理后的结果。</returns>
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

        /// <summary>
        /// 将完整业务包交给 KCP 分片并发送。
        /// </summary>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public MTask SendAsync(ArraySegment<byte> data)
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

            return MTask.CompletedTask;
        }

        /// <summary>
        /// 输入一个 UDP 承载的 KCP 数据报。
        /// </summary>
        /// <param name="buffer">执行该方法所需的 buffer 参数。</param>
        /// <param name="size">执行该方法所需的 size 参数。</param>
        /// <returns>执行处理后的结果。</returns>
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

        /// <summary>
        /// 尝试取出一条已完成重组的业务包。
        /// </summary>
        /// <param name="packet">执行该方法所需的 packet 参数。</param>
        /// <returns>执行处理后的结果。</returns>
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

        /// <summary>
        /// 推进 KCP 定时器并处理重传。
        /// </summary>
        /// <param name="now">执行该方法所需的 now 参数。</param>
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

        /// <summary>
        /// 判断会话是否已超过指定的接收空闲时长。
        /// </summary>
        /// <param name="now">执行该方法所需的 now 参数。</param>
        /// <param name="timeoutMs">执行该方法所需的 timeoutMs 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public bool IsTimedOut(uint now, int timeoutMs)
        {
            if (timeoutMs <= 0)
            {
                return false;
            }
            int diff = (int)(now - lastRecvMs);
            return diff > timeoutMs;
        }

        /// <summary>
        /// 关闭会话并通知断开事件。
        /// </summary>
        public void Close()
        {
            if (closed)
            {
                return;
            }
            closed = true;
            OnDisconnected?.Invoke();
        }

        /// <summary>
        /// 释放会话资源。
        /// </summary>
        public void Dispose()
        {
            Close();
        }

        /// <summary>
        /// 执行 KcpOutput 相关处理。
        /// </summary>
        /// <param name="buffer">执行该方法所需的 buffer 参数。</param>
        /// <param name="size">执行该方法所需的 size 参数。</param>
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
                LogSwitch.Warning($"KcpServerSession output error: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 CurrentMS 相关处理。
        /// </summary>
        /// <returns>执行处理后的结果。</returns>
        private static uint CurrentMS()
        {
            return unchecked((uint)Environment.TickCount);
        }
    }
}
