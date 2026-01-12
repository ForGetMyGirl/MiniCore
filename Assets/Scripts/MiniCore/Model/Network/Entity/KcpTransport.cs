using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace MiniCore.Model
{
    public class KcpTransportConfig
    {
        public int Mtu = 1400;
        public int SendWindow = 128;
        public int ReceiveWindow = 128;
        public int NoDelay = 1;
        public int Interval = 10;
        public int Resend = 2;
        public int NoCongestion = 1;
        public int MinRto = 30;
        public int FastResend = 2;
        public int FastAck = 1;
        public int DeadLink = 20;
        public bool Stream = false;
    }

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
        private bool disconnected;

        public bool IsConnected => socket != null;

        public event Func<ReadOnlyMemory<byte>, UniTask> OnDataReceived;
        public event Action OnDisconnected;

        public KcpTransport(uint conv, KcpTransportConfig config = null)
        {
            this.conv = conv;
            this.config = config ?? new KcpTransportConfig();
        }

        public UniTask ConnectAsync(string host, int port, CancellationToken token = default)
        {
            Disconnect();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect(host, port);
            disconnected = false;

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

        private async UniTask ReceiveLoopAsync(CancellationToken token)
        {
            byte[] buffer = ByteBufferPool.Shared.Rent(MaxDatagramSize);
            try
            {
                while (!token.IsCancellationRequested && IsConnected)
                {
                    int received = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None, token);
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
            catch (Exception ex)
            {
                EventCenter.Broadcast(GameEvent.LogWarning, $"KcpTransport receive loop error: {ex.Message}");
            }
            finally
            {
                ByteBufferPool.Shared.Return(buffer);
                Disconnect();
            }
        }

        private async UniTask UpdateLoopAsync(CancellationToken token)
        {
            try
            {
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
                    await UniTask.Delay(delay, cancellationToken: token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                EventCenter.Broadcast(GameEvent.LogWarning, $"KcpTransport update loop error: {ex.Message}");
            }
        }

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
            catch (Exception ex)
            {
                EventCenter.Broadcast(GameEvent.LogWarning, $"KcpTransport output error: {ex.Message}");
            }
        }

        private async UniTask InvokeDataReceivedAsync(ReadOnlyMemory<byte> data)
        {
            var handler = OnDataReceived;
            if (handler == null)
            {
                return;
            }

            foreach (var del in handler.GetInvocationList())
            {
                var callback = (Func<ReadOnlyMemory<byte>, UniTask>)del;
                await callback(data);
            }
        }

        public void Disconnect()
        {
            if (disconnected)
            {
                return;
            }

            disconnected = true;

            try
            {
                receiveCts?.Cancel();
                updateCts?.Cancel();
            }
            catch { }

            if (socket != null)
            {
                try
                {
                    socket.Close();
                }
                catch { }
                socket = null;
            }

            OnDisconnected?.Invoke();
        }

        public void Dispose()
        {
            Disconnect();
        }

        private static uint CurrentMS()
        {
            return unchecked((uint)Environment.TickCount);
        }

        private static int TimeDiff(uint later, uint earlier)
        {
            return (int)(later - earlier);
        }

        private readonly struct ReceivedPacket
        {
            public readonly byte[] Buffer;
            public readonly int Length;

            public ReceivedPacket(byte[] buffer, int length)
            {
                Buffer = buffer;
                Length = length;
            }
        }
    }
}
