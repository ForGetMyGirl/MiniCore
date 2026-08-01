using MiniCore.Threading;
using System;
using System.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// 将 UDP 服务端会话包装为统一网络传输接口。
    /// </summary>
    public sealed class UdpServerTransport : INetworkTransport, IDatagramBatchNetworkTransport
    {
        #region Private 私有成员

        private readonly IServerSession session; // 被包装的 UDP 服务端会话。
        private bool closed; // 传输层关闭状态。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 使用指定服务端会话创建 UDP 传输包装器。
        /// </summary>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public UdpServerTransport(IServerSession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            session.OnDisconnected += HandleSessionDisconnected;
        }

        /// <summary>
        /// 传输包装器是否仍可用。
        /// </summary>
        public bool IsConnected => !closed;

        /// <summary>
        /// 服务端收到数据报并转发时触发。
        /// </summary>
        public event Func<ReadOnlyMemory<byte>, MTask> OnDataReceived;
        /// <summary>
        /// 服务端会话或传输断开时触发。
        /// </summary>
        public event Action OnDisconnected;

        /// <summary>
        /// 服务端传输不支持主动连接。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public MTask ConnectAsync(string host, int port)
        {
            throw new InvalidOperationException("Server-side transport does not support ConnectAsync.");
        }

        /// <summary>
        /// 通过关联的服务端会话发送数据报。
        /// </summary>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public MTask SendAsync(ArraySegment<byte> data)
        {
            return session.SendAsync(data);
        }

        /// <summary>
        /// 关闭传输包装器并通知断开事件。
        /// </summary>
        public void Disconnect()
        {
            if (closed)
            {
                return;
            }

            closed = true;
            OnDisconnected?.Invoke();
        }

        /// <summary>
        /// 释放传输包装器资源。
        /// </summary>
        public void Dispose()
        {
            Disconnect();
        }

        /// <summary>
        /// 将服务端接收的数据报派发给传输订阅者。
        /// </summary>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public MTask PushReceivedAsync(ReadOnlyMemory<byte> data)
        {
            return DispatchReceivedDatagramAsync(data);
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 将已封装多个业务包的完整 UDP 数据报发送到当前服务端会话对应的远端。
        /// </summary>
        /// <param name="datagram">由会话发送器构造的完整 UDP 批量数据报。</param>
        /// <returns>底层服务端 UDP Socket 接受数据报或发生异常时完成的任务。</returns>
        MTask IDatagramBatchNetworkTransport.SendDatagramBatchAsync(ArraySegment<byte> datagram)
        {
            return SendAsync(datagram);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将普通 UDP 单包或已验证批量数据报中的每个逻辑业务包派发给服务端网络会话。
        /// 批量格式非法时不向上层投递任何部分数据。
        /// </summary>
        /// <param name="datagram">当前远端发送的完整 UDP 数据报。</param>
        /// <returns>全部逻辑业务包完成服务端传输回调派发或非法数据报被丢弃时完成的任务。</returns>
        private async MTask DispatchReceivedDatagramAsync(ReadOnlyMemory<byte> datagram)
        {
            if (!UdpBatchDatagramCodec.TryValidateBatchDatagram(datagram, out bool isBatchDatagram, out int packetCount))
            {
                return;
            }

            if (!isBatchDatagram)
            {
                await TransportEventDispatcher.DispatchAsync(OnDataReceived, datagram);
                return;
            }

            int offset = UdpBatchDatagramCodec.HeaderByteCount;
            for (int index = 0; index < packetCount; index++)
            {
                UdpBatchDatagramCodec.TryReadPacket(datagram, ref offset, out ReadOnlyMemory<byte> packet);
                await TransportEventDispatcher.DispatchAsync(OnDataReceived, packet);
            }
        }

        /// <summary>
        /// 执行 HandleSessionDisconnected 相关处理。
        /// </summary>
        private void HandleSessionDisconnected()
        {
            if (closed)
            {
                return;
            }

            closed = true;
            OnDisconnected?.Invoke();
        }

        #endregion
    }
}
