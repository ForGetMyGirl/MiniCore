using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MiniCore.Model
{
    /// <summary>
    /// 客户端 KCP 传输参数配置。
    /// </summary>
    public class KcpTransportConfig
    {
        /// <summary>
        /// KCP 最大传输单元。
        /// </summary>
        public int Mtu = 1400;
        /// <summary>
        /// KCP 发送窗口大小。
        /// </summary>
        public int SendWindow = 128;
        /// <summary>
        /// KCP 接收窗口大小。
        /// </summary>
        public int ReceiveWindow = 128;
        /// <summary>
        /// KCP 无延迟模式开关。
        /// </summary>
        public int NoDelay = 1;
        /// <summary>
        /// KCP 内部刷新间隔（毫秒）。
        /// </summary>
        public int Interval = 10;
        /// <summary>
        /// KCP 快速重传阈值。
        /// </summary>
        public int Resend = 2;
        /// <summary>
        /// 是否禁用 KCP 拥塞控制。
        /// </summary>
        public int NoCongestion = 1;
        /// <summary>
        /// 最小重传超时（毫秒）。
        /// </summary>
        public int MinRto = 30;
        /// <summary>
        /// 快速重传触发参数。
        /// </summary>
        public int FastResend = 2;
        /// <summary>
        /// 快速确认次数上限。
        /// </summary>
        public int FastAck = 1;
        /// <summary>
        /// 判定 KCP 链路失效的重传次数。
        /// </summary>
        public int DeadLink = 20;
        /// <summary>
        /// 是否启用流模式。
        /// </summary>
        public bool Stream = false;
    }

    /// <summary>
    /// 基于 UDP 和 KCP 的可靠客户端传输实现。
    /// </summary>
    public class KcpTransport : INetworkTransport
    {
        private const int MaxDatagramSize = 65507;

        private readonly uint conv;
        private readonly KcpTransportConfig config;

        private Socket socket;
        private Kcp kcp;
        private CancellationTokenSource receiveCts;
        private CancellationTokenSource updateCts;
        private readonly object kcpLock = new object();
        private int disconnected;

        /// <summary>
        /// 底层 UDP 套接字是否已建立。
        /// </summary>
        public bool IsConnected => socket != null;

        /// <summary>
        /// 接收到 KCP 重组后的完整业务包时触发。
        /// </summary>
        public event Func<ReadOnlyMemory<byte>, UniTask> OnDataReceived;
        /// <summary>
        /// KCP 传输断开时触发。
        /// </summary>
        public event Action OnDisconnected;

        /// <summary>
        /// 使用指定会话标识和配置创建 KCP 传输。
        /// </summary>
        /// <param name="conv">执行该方法所需的 conv 参数。</param>
        /// <param name="config">执行该方法所需的 config 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public KcpTransport(uint conv, KcpTransportConfig config = null)
        {
            this.conv = conv;
            this.config = config ?? new KcpTransportConfig();
        }

        /// <summary>
        /// 初始化 UDP 套接字、KCP 状态并启动收包和更新循环。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <param name="token">执行该方法所需的 token 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public UniTask ConnectAsync(string host, int port, CancellationToken token = default)
        {
            Disconnect();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect(host, port);
            disconnected = 0;

            kcp = new Kcp(conv, KcpOutput);
            kcp.SetMtu(config.Mtu);
            kcp.WndSize(config.SendWindow, config.ReceiveWindow);
            kcp.NoDelay(config.NoDelay, config.Interval, config.Resend, config.NoCongestion);
            kcp.SetMinRto(config.MinRto);
            kcp.SetFastResend(config.FastResend);
            kcp.SetFastAck(config.FastAck);
            kcp.SetDeadLink(config.DeadLink);
            kcp.SetStreamMode(config.Stream);

            receiveCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            updateCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _ = ReceiveLoopAsync(receiveCts.Token);
            _ = UpdateLoopAsync(updateCts.Token);
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 将完整业务包交给 KCP 分片并发送。
        /// </summary>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <param name="token">执行该方法所需的 token 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public UniTask SendAsync(ArraySegment<byte> data, CancellationToken token = default)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("KCP is not connected; cannot send data.");
            }

            lock (kcpLock)
            {
                if (data.Array == null)
                {
                    throw new ArgumentException("ArraySegment has no backing array.", nameof(data));
                }
                kcp.Send(data.Array, data.Offset, data.Count);
                uint now = CurrentMS();
                kcp.Update(now);
            }
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 尝试获取 KCP 平滑往返时延。
        /// </summary>
        /// <param name="rttMs">执行该方法所需的 rttMs 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public bool TryGetSmoothedRttMs(out int rttMs)
        {
            rttMs = 0;
            lock (kcpLock)
            {
                if (kcp == null)
                {
                    return false;
                }
                rttMs = kcp.GetSmoothedRttMs();
            }
            return rttMs > 0;
        }

        /// <summary>
        /// 执行 ReceiveLoopAsync 相关处理。
        /// </summary>
        /// <param name="token">执行该方法所需的 token 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private async UniTask ReceiveLoopAsync(CancellationToken token)
        {
            byte[] buffer = ByteBufferPool.Shared.Rent(MaxDatagramSize);
            try
            {
                await UniTask.SwitchToThreadPool();
                while (!token.IsCancellationRequested && IsConnected)
                {
                    int received = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None, token).ConfigureAwait(false);
                    if (received <= 0)
                    {
                        break;
                    }

                    List<ReceivedPacket> packets = null;
                    lock (kcpLock)
                    {
                        kcp.Input(buffer, 0, received);
                        while (true)
                        {
                            int size = kcp.PeekSize();
                            if (size <= 0)
                            {
                                break;
                            }
                            byte[] data = ByteBufferPool.Shared.Rent(size);
                            int n = kcp.Receive(data);
                            if (n < 0)
                            {
                                ByteBufferPool.Shared.Return(data);
                                break;
                            }
                            if (packets == null)
                            {
                                packets = new List<ReceivedPacket>();
                            }
                            packets.Add(new ReceivedPacket(data, n));
                        }
                    }

                    if (packets != null)
                    {
                        foreach (var packet in packets)
                        {
                            await InvokeDataReceivedAsync(new ReadOnlyMemory<byte>(packet.Buffer, 0, packet.Length));
                            ByteBufferPool.Shared.Return(packet.Buffer);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException ex) when (IsExpectedSocketClosure(ex))
            {
            }
            catch (SocketException ex)
            {
                if (!IsActiveDisconnect())
                {
                    LogSwitch.Warning($"KcpTransport receive loop error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                if (!IsActiveDisconnect())
                {
                    LogSwitch.Warning($"KcpTransport receive loop error: {ex.Message}");
                }
            }
            finally
            {
                ByteBufferPool.Shared.Return(buffer);
                Disconnect();
            }
        }

        /// <summary>
        /// 执行 UpdateLoopAsync 相关处理。
        /// </summary>
        /// <param name="token">执行该方法所需的 token 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private async UniTask UpdateLoopAsync(CancellationToken token)
        {
            try
            {
                await UniTask.SwitchToThreadPool();
                while (!token.IsCancellationRequested && IsConnected)
                {
                    uint current = CurrentMS();
                    uint next;
                    lock (kcpLock)
                    {
                        kcp.Update(current);
                        next = kcp.Check(current);
                    }

                    int delay = TimeDiff(next, current);
                    if (delay < 1) delay = 1;
                    if (delay > config.Interval) delay = config.Interval;
                    await Task.Delay(delay, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException ex) when (IsExpectedSocketClosure(ex))
            {
            }
            catch (SocketException ex)
            {
                if (!IsActiveDisconnect())
                {
                    LogSwitch.Warning($"KcpTransport update loop error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                if (!IsActiveDisconnect())
                {
                    LogSwitch.Warning($"KcpTransport update loop error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 执行 KcpOutput 相关处理。
        /// </summary>
        /// <param name="buffer">执行该方法所需的 buffer 参数。</param>
        /// <param name="size">执行该方法所需的 size 参数。</param>
        private void KcpOutput(byte[] buffer, int size)
        {
            if (socket == null || size <= 0)
            {
                return;
            }
            try
            {
                byte[] payload = ByteBufferPool.Shared.Rent(size);
                try
                {
                    Buffer.BlockCopy(buffer, 0, payload, 0, size);
                    socket.Send(payload, 0, size, SocketFlags.None);
                }
                finally
                {
                    ByteBufferPool.Shared.Return(payload);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException ex) when (IsExpectedSocketClosure(ex))
            {
            }
            catch (Exception ex)
            {
                if (!IsActiveDisconnect())
                {
                    LogSwitch.Warning($"KcpTransport output error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 执行 InvokeDataReceivedAsync 相关处理。
        /// </summary>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private async UniTask InvokeDataReceivedAsync(ReadOnlyMemory<byte> data)
        {
            await TransportEventDispatcher.DispatchAsync(OnDataReceived, data);
        }

        /// <summary>
        /// 停止 KCP 循环、关闭套接字并通知断开事件。
        /// </summary>
        public void Disconnect()
        {
            if (Interlocked.Exchange(ref disconnected, 1) != 0)
            {
                return;
            }

            try
            {
                receiveCts?.Cancel();
                updateCts?.Cancel();
            }
            catch { }
            finally
            {
                receiveCts?.Dispose();
                updateCts?.Dispose();
                receiveCts = null;
                updateCts = null;
            }

            if (socket != null)
            {
                try
                {
                    socket.Close();
                }
                catch { }
                socket = null;
            }

            var handler = OnDisconnected;
            OnDisconnected = null;
            handler?.Invoke();
        }

        /// <summary>
        /// 释放 KCP 传输资源。
        /// </summary>
        public void Dispose()
        {
            Disconnect();
        }

        /// <summary>
        /// 执行 CurrentMS 相关处理。
        /// </summary>
        /// <returns>执行处理后的结果。</returns>
        private static uint CurrentMS()
        {
            return unchecked((uint)Environment.TickCount);
        }

        /// <summary>
        /// 执行 TimeDiff 相关处理。
        /// </summary>
        /// <param name="later">执行该方法所需的 later 参数。</param>
        /// <param name="earlier">执行该方法所需的 earlier 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private static int TimeDiff(uint later, uint earlier)
        {
            return (int)(later - earlier);
        }

        /// <summary>
        /// 执行 IsActiveDisconnect 相关处理。
        /// </summary>
        /// <returns>执行处理后的结果。</returns>
        private bool IsActiveDisconnect()
        {
            return Volatile.Read(ref disconnected) != 0;
        }

        /// <summary>
        /// 执行 IsExpectedSocketClosure 相关处理。
        /// </summary>
        /// <param name="ex">执行该方法所需的 ex 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private static bool IsExpectedSocketClosure(SocketException ex)
        {
            return ex.SocketErrorCode == SocketError.OperationAborted
                || ex.SocketErrorCode == SocketError.Interrupted
                || ex.SocketErrorCode == SocketError.ConnectionAborted
                || ex.SocketErrorCode == SocketError.ConnectionReset
                || ex.SocketErrorCode == SocketError.NotSocket;
        }

        private readonly struct ReceivedPacket
        {
            /// <summary>
            /// 网络模块公开成员 Buffer 的说明。
            /// </summary>
            public readonly byte[] Buffer;
            public readonly int Length;

            /// <summary>
            /// 执行 ReceivedPacket 相关处理。
            /// </summary>
            /// <param name="buffer">执行该方法所需的 buffer 参数。</param>
            /// <param name="length">执行该方法所需的 length 参数。</param>
            /// <returns>执行处理后的结果。</returns>
            public ReceivedPacket(byte[] buffer, int length)
            {
                Buffer = buffer;
                Length = length;
            }
        }
    }
}
