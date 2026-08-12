using MiniCore.Threading;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MiniCore.Core;

namespace MiniCore.Model
{
    /// <summary>
    /// 基于无连接数据报的客户端 UDP 传输实现。
    /// </summary>
    public class UdpTransport : INetworkTransport, IDatagramBatchNetworkTransport
    {
        #region Private 私有成员

        private const int MaxDatagramSize = 65507; // UDP 数据报理论最大长度。
        private static readonly TimeSpan DefaultInitTimeout = TimeSpan.FromSeconds(3); // 解析远端地址的默认超时。

        private Socket socket; // UDP 客户端套接字。
        private EndPoint remoteEndPoint; // 发送数据报的远端终结点。
        private CancellationTokenSource receiveCts; // 接收循环取消令牌源。
        private int disconnected; // 断开状态的原子标志。
        private readonly IMTaskExecutor networkExecutor; // 接收循环使用的执行器。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 创建使用指定执行器的 UDP 客户端传输。
        /// </summary>
        /// <param name="executor">接收循环使用的执行器；为空时按当前环境选择默认执行器。</param>
        public UdpTransport(IMTaskExecutor executor = null)
        {
            networkExecutor = NetworkExecutorResolver.Resolve(executor);
        }

        /// <summary>
        /// UDP 套接字是否已建立。
        /// </summary>
        public bool IsConnected => socket != null;

        /// <summary>
        /// 接收到一个完整 UDP 数据报时触发。
        /// </summary>
        public event Func<ReadOnlyMemory<byte>, MTask> OnDataReceived;
        /// <summary>
        /// 传输关闭时触发。
        /// </summary>
        public event Action OnDisconnected;

        /// <summary>
        /// 解析远端地址、创建套接字并启动接收循环。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public async MTask ConnectAsync(string host, int port)
        {
            CancellationToken token = MTaskExternal.GetCancellationToken();
            Disconnect();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            MTask resolveTask = ResolveRemoteEndPointAsync(host, port);
            int winner = await MTask.WhenAny(
                resolveTask,
                MTask.Delay(DefaultInitTimeout));

            if (winner != 0)
            {
                TryCloseSocket();
                throw new TimeoutException($"UDP init timeout to {host}:{port}");
            }

            disconnected = 0;
            receiveCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            ReceiveLoopAsync(receiveCts.Token).Forget();
        }

        /// <summary>
        /// 向远端发送一个完整 UDP 数据报。
        /// </summary>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public async MTask SendAsync(ArraySegment<byte> data)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("UDP is not connected; cannot send data.");
            }

            if (remoteEndPoint == null)
            {
                throw new InvalidOperationException("UDP remote endpoint is not initialized.");
            }

            await socket.SendToAsync(data, SocketFlags.None, remoteEndPoint).ConfigureAwait(false);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 执行 ReceiveLoopAsync 相关处理。
        /// </summary>
        /// <returns>执行处理后的结果。</returns>
        private async MTask ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                await MTask.SwitchTo(networkExecutor);
                while (!token.IsCancellationRequested && IsConnected)
                {
                    byte[] buffer = ByteBufferPool.Shared.Rent(MaxDatagramSize);
                    try
                    {
                        EndPoint from = new IPEndPoint(IPAddress.Any, 0);
                        SocketReceiveFromResult result = await socket.ReceiveFromAsync(
                            new ArraySegment<byte>(buffer),
                            SocketFlags.None,
                            from).ConfigureAwait(false);

                        int received = result.ReceivedBytes;
                        if (received <= 0)
                        {
                            break;
                        }

                        if (!IsExpectedRemote(result.RemoteEndPoint))
                        {
                            continue;
                        }

                        await DispatchReceivedDatagramAsync(new ReadOnlyMemory<byte>(buffer, 0, received));
                    }
                    finally
                    {
                        ByteBufferPool.Shared.Return(buffer);
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
                    LogSwitch.Warning($"UdpTransport receive loop error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                if (!IsActiveDisconnect())
                {
                    LogSwitch.Warning($"UdpTransport receive loop error: {ex.Message}");
                }
            }
            finally
            {
                Disconnect();
            }
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 取消接收循环、关闭套接字并通知断开事件。
        /// </summary>
        public void Disconnect()
        {
            if (Interlocked.Exchange(ref disconnected, 1) != 0)
            {
                return;
            }

            var currentCts = receiveCts;
            receiveCts = null;
            try
            {
                currentCts?.Cancel();
            }
            catch
            {
            }
            finally
            {
                currentCts?.Dispose();
            }

            TryCloseSocket();

            var handler = OnDisconnected;
            OnDisconnected = null;
            handler?.Invoke();
        }

        /// <summary>
        /// 释放 UDP 传输资源。
        /// </summary>
        public void Dispose()
        {
            Disconnect();
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 将已封装多个业务包的完整 UDP 数据报写入远端。
        /// 此实现与普通单包写入共用同一 Socket，确保数据报边界不被拆分。
        /// </summary>
        /// <param name="datagram">由会话发送器构造的完整 UDP 批量数据报。</param>
        /// <returns>底层 UDP Socket 接受数据报或发生异常时完成的任务。</returns>
        MTask IDatagramBatchNetworkTransport.SendDatagramBatchAsync(ArraySegment<byte> datagram)
        {
            return SendAsync(datagram);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 执行 TryCloseSocket 相关处理。
        /// </summary>
        private void TryCloseSocket()
        {
            if (socket == null)
            {
                return;
            }

            try
            {
                socket.Close();
            }
            catch
            {
            }

            socket = null;
            remoteEndPoint = null;
        }

        /// <summary>
        /// 根据数据报格式将普通 UDP 单包或批量数据报依次派发给上层。
        /// 批量格式非法时丢弃整条数据报，避免向业务层投递部分不可信消息。
        /// </summary>
        /// <param name="datagram">从远端收到的完整 UDP 数据报。</param>
        /// <returns>全部逻辑业务包完成上层派发或非法数据报被丢弃时完成的任务。</returns>
        private async MTask DispatchReceivedDatagramAsync(ReadOnlyMemory<byte> datagram)
        {
            if (!UdpBatchDatagramCodec.TryValidateBatchDatagram(datagram, out bool isBatchDatagram, out int packetCount))
            {
                return;
            }

            if (!isBatchDatagram)
            {
                await InvokeDataReceivedAsync(datagram);
                return;
            }

            int offset = UdpBatchDatagramCodec.HeaderByteCount;
            for (int index = 0; index < packetCount; index++)
            {
                UdpBatchDatagramCodec.TryReadPacket(datagram, ref offset, out ReadOnlyMemory<byte> packet);
                await InvokeDataReceivedAsync(packet);
            }
        }

        /// <summary>
        /// 执行 InvokeDataReceivedAsync 相关处理。
        /// </summary>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private async MTask InvokeDataReceivedAsync(ReadOnlyMemory<byte> data)
        {
            await TransportEventDispatcher.DispatchAsync(OnDataReceived, data);
        }

        /// <summary>
        /// 执行 ResolveRemoteEndPointAsync 相关处理。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private async MTask ResolveRemoteEndPointAsync(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentException("Host is empty.", nameof(host));
            }

            if (IPAddress.TryParse(host, out var ip))
            {
                remoteEndPoint = new IPEndPoint(ip, port);
                return;
            }

            IPAddress[] addresses = await Dns.GetHostAddressesAsync(host);
            foreach (var address in addresses)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    remoteEndPoint = new IPEndPoint(address, port);
                    return;
                }
            }

            throw new SocketException((int)SocketError.AddressFamilyNotSupported);
        }

        /// <summary>
        /// 执行 IsExpectedRemote 相关处理。
        /// </summary>
        /// <param name="remote">执行该方法所需的 remote 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private bool IsExpectedRemote(EndPoint remote)
        {
            if (remoteEndPoint == null)
            {
                return true;
            }

            if (!(remoteEndPoint is IPEndPoint expected) || !(remote is IPEndPoint actual))
            {
                return Equals(remoteEndPoint, remote);
            }

            if (expected.Port != actual.Port)
            {
                return false;
            }

            IPAddress expectedIp = expected.Address.MapToIPv4();
            IPAddress actualIp = actual.Address.MapToIPv4();
            return expectedIp.Equals(actualIp);
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

        #endregion

    }
}
