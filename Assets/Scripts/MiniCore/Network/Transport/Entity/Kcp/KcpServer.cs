using MiniCore.Threading;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;

namespace MiniCore.Model
{
    /// <summary>
    /// KCP 服务端监听和会话参数配置。
    /// </summary>
    public class KcpServerConfig
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
        /// KCP 刷新间隔（毫秒）。
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
        /// <summary>
        /// 服务端判定会话空闲超时的时长（毫秒）。
        /// </summary>
        public int SessionTimeoutMs = 30000;
    }

    /// <summary>
    /// 基于 UDP 的 KCP 服务端，按 conv 和远端地址维护会话。
    /// </summary>
    public sealed class KcpServer
    {
        private const int MaxDatagramSize = 65507;

        private readonly KcpServerConfig config;
        private readonly Dictionary<string, KcpServerSession> sessions = new Dictionary<string, KcpServerSession>();
        private readonly object sessionLock = new object();

        private Socket socket;
        private CancellationTokenSource receiveCts;
        private CancellationTokenSource updateCts;
        private bool running;
        private long lastConnectionResetLogTicks;

        /// <summary>
        /// 创建新的 KCP 服务端会话时触发。
        /// </summary>
        public event Action<IServerSession> OnSessionCreated;
        /// <summary>
        /// KCP 服务端会话关闭时触发。
        /// </summary>
        public event Action<IServerSession> OnSessionClosed;
        /// <summary>
        /// 接收到 KCP 重组后的业务包时触发。
        /// </summary>
        public event Func<IServerSession, ReadOnlyMemory<byte>, MTask> OnDataReceived;

        /// <summary>
        /// 使用指定配置创建 KCP 服务端。
        /// </summary>
        /// <param name="config">执行该方法所需的 config 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public KcpServer(KcpServerConfig config = null)
        {
            this.config = config ?? new KcpServerConfig();
        }

        /// <summary>
        /// 绑定地址并启动 KCP 收包和更新循环。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public MTask StartAsync(string host, int port)
        {
            CancellationToken token = MTaskExternal.GetCancellationToken();
            if (running)
            {
                throw new InvalidOperationException("KcpServer already running.");
            }

            running = true;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            TryDisableUdpConnReset(socket);
            socket.Bind(new IPEndPoint(ParseAddress(host), port));

            receiveCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            updateCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            ReceiveLoopAsync(receiveCts.Token).Forget();
            UpdateLoopAsync(updateCts.Token).Forget();
            return MTask.CompletedTask;
        }

        /// <summary>
        /// 停止 KCP 服务端并关闭其全部会话。
        /// </summary>
        public void Stop()
        {
            if (!running)
            {
                return;
            }

            running = false;

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

            List<KcpServerSession> toClose;
            lock (sessionLock)
            {
                toClose = new List<KcpServerSession>(sessions.Values);
                sessions.Clear();
            }

            foreach (var session in toClose)
            {
                session.Close();
                OnSessionClosed?.Invoke(session);
            }
        }

        /// <summary>
        /// 关闭指定 KCP 服务端会话。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        public void CloseSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return;
            }

            KcpServerSession session;
            lock (sessionLock)
            {
                if (!sessions.TryGetValue(sessionId, out session))
                {
                    return;
                }
                sessions.Remove(sessionId);
            }

            session.Close();
            OnSessionClosed?.Invoke(session);
        }

        /// <summary>
        /// 执行 ReceiveLoopAsync 相关处理。
        /// </summary>
        /// <returns>执行处理后的结果。</returns>
        private async MTask ReceiveLoopAsync(CancellationToken token)
        {
            byte[] buffer = ByteBufferPool.Shared.Rent(MaxDatagramSize);
            try
            {
                await MTask.SwitchTo(MTaskExecutors.Network);
                EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                while (!token.IsCancellationRequested && running)
                {
                    SocketReceiveFromResult result;
                    try
                    {
                        result = await socket.ReceiveFromAsync(
                            new ArraySegment<byte>(buffer),
                            SocketFlags.None,
                            remote).ConfigureAwait(false);
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (SocketException ex)
                    {
                        if (ex.SocketErrorCode == SocketError.ConnectionReset)
                        {
                            // Ignore/reset-throttle noisy UDP connreset notifications on Windows.
                            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            if (now - lastConnectionResetLogTicks > 2000)
                            {
                                lastConnectionResetLogTicks = now;
                                LogSwitch.Warning($"KcpServer receive socket warning: {ex.SocketErrorCode}");
                            }
                            continue;
                        }

                        LogSwitch.Warning($"KcpServer receive socket error: {ex.SocketErrorCode}");
                        continue;
                    }
                    catch (Exception ex)
                    {
                        LogSwitch.Warning($"KcpServer receive error: {ex.Message}");
                        continue;
                    }

                    int received = result.ReceivedBytes;
                    if (received <= 0)
                    {
                        continue;
                    }

                    uint conv = Kcp.PeekConv(buffer, 0);
                    if (conv == 0)
                    {
                        continue;
                    }

                    var session = GetOrCreateSession(conv, result.RemoteEndPoint);
                    if (!session.Input(buffer, received))
                    {
                        continue;
                    }

                    while (session.TryReceive(out var packet))
                    {
                        try
                        {
                            await InvokeDataReceivedAsync(session, new ReadOnlyMemory<byte>(packet, 0, packet.Length));
                        }
                        catch (Exception ex)
                        {
                            LogSwitch.Error($"Server message handling error: {ex.Message}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                LogSwitch.Warning($"KcpServer receive loop error: {ex.Message}");
            }
            finally
            {
                ByteBufferPool.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// 执行 UpdateLoopAsync 相关处理。
        /// </summary>
        /// <returns>执行处理后的结果。</returns>
        private async MTask UpdateLoopAsync(CancellationToken token)
        {
            try
            {
                await MTask.SwitchTo(MTaskExecutors.Network);
                while (!token.IsCancellationRequested && running)
                {
                    uint now = CurrentMS();
                    List<KcpServerSession> snapshot;
                    lock (sessionLock)
                    {
                        snapshot = new List<KcpServerSession>(sessions.Values);
                    }

                    foreach (var session in snapshot)
                    {
                        session.Update(now);
                        if (session.IsDead || session.IsTimedOut(now, config.SessionTimeoutMs))
                        {
                            CloseSession(session, session.IsTimedOut(now, config.SessionTimeoutMs));
                        }
                    }

                    await MTask.Delay(config.Interval);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                LogSwitch.Warning($"KcpServer update loop error: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 CloseSession 相关处理。
        /// </summary>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        /// <param name="timeout">执行该方法所需的 timeout 参数。</param>
        private void CloseSession(KcpServerSession session, bool timeout)
        {
            bool removed;
            lock (sessionLock)
            {
                removed = sessions.Remove(session.SessionId);
            }

            if (!removed)
            {
                return;
            }

            session.Close();
            if (timeout)
            {
                LogSwitch.Warning($"Server heartbeat timeout, kick session:{session.SessionId}");
            }
            else
            {
                LogSwitch.Warning($"Server session disconnected: {session.SessionId}");
            }
            OnSessionClosed?.Invoke(session);
        }

        /// <summary>
        /// 执行 GetOrCreateSession 相关处理。
        /// </summary>
        /// <param name="conv">执行该方法所需的 conv 参数。</param>
        /// <param name="remote">执行该方法所需的 remote 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private KcpServerSession GetOrCreateSession(uint conv, EndPoint remote)
        {
            string sessionId = $"{conv}:{remote}";
            KcpServerSession session;
            bool created = false;

            lock (sessionLock)
            {
                if (!sessions.TryGetValue(sessionId, out session))
                {
                    session = new KcpServerSession(conv, remote, socket, config);
                    sessions.Add(sessionId, session);
                    created = true;
                }
            }

            if (created)
            {
                OnSessionCreated?.Invoke(session);
            }

            return session;
        }

        /// <summary>
        /// 执行 InvokeDataReceivedAsync 相关处理。
        /// </summary>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private async MTask InvokeDataReceivedAsync(IServerSession session, ReadOnlyMemory<byte> data)
        {
            await TransportEventDispatcher.DispatchAsync(OnDataReceived, session, data);
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
        /// 执行 ParseAddress 相关处理。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private static IPAddress ParseAddress(string host)
        {
            if (string.IsNullOrWhiteSpace(host) || host == "0.0.0.0")
            {
                return IPAddress.Any;
            }
            if (IPAddress.TryParse(host, out var address))
            {
                return address;
            }
            return IPAddress.Any;
        }

        /// <summary>
        /// 执行 TryDisableUdpConnReset 相关处理。
        /// </summary>
        /// <param name="udpSocket">执行该方法所需的 udpSocket 参数。</param>
        private static void TryDisableUdpConnReset(Socket udpSocket)
        {
            if (udpSocket == null)
            {
                return;
            }

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    const int SIO_UDP_CONNRESET = -1744830452;
                    udpSocket.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
                }
            }
            catch
            {
                // Ignore platform/socket capability differences.
            }
        }
    }
}
