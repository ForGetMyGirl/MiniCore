using MiniCore.Threading;
using System;
using System.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// 封装底层传输实现的逻辑网络会话。
    /// </summary>
    public class NetworkSession : IClientSession
    {
        private int disposed; // 防止底层传输重复释放的标志。
        private readonly NetworkOutboundQueue outboundQueue; // 会话独占的固定容量出站发送器。

        /// <summary>
        /// 逻辑会话的唯一标识。
        /// </summary>
        public string SessionId { get; }
        /// <summary>
        /// 会话持有的底层网络传输。
        /// </summary>
        public INetworkTransport Transport { get; }
        /// <summary>
        /// 底层传输当前是否可用。
        /// </summary>
        public bool IsConnected => Transport.IsConnected;

        /// <summary>
        /// 获取该会话数据与可靠出站队列的当前诊断快照。
        /// 该方法不修改队列状态，供压测与运行时拥堵观测使用。
        /// </summary>
        /// <returns>当前会话两条出站队列的占用和累计拒绝次数。</returns>
        public NetworkOutboundQueueSnapshot GetOutboundQueueSnapshot()
        {
            return outboundQueue.CaptureSnapshot();
        }

        /// <summary>
        /// 启用或关闭当前会话的出站分段耗时诊断，并清空上一周期诊断数据。
        /// </summary>
        /// <param name="enabled">为 true 时记录队列等待和底层传输发送等待；仅建议由压测启用。</param>
        public void SetOutboundTimingMetricsEnabled(bool enabled)
        {
            outboundQueue.SetTimingMetricsEnabled(enabled);
        }

        /// <summary>
        /// 清空当前会话的出站分段耗时诊断，不影响已排队数据与拒绝计数。
        /// </summary>
        public void ResetOutboundTimingMetrics()
        {
            outboundQueue.ResetTimingMetrics();
        }

        /// <summary>
        /// 使用给定标识和传输实现创建逻辑会话。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="transport">执行该方法所需的 transport 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public NetworkSession(string sessionId, INetworkTransport transport)
        {
            SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            Transport = transport ?? throw new ArgumentNullException(nameof(transport));
            outboundQueue = new NetworkOutboundQueue(transport);
            Transport.OnDisconnected += HandleTransportDisconnected;
        }

        /// <summary>
        /// 通过底层传输发送一个完整业务数据包。
        /// </summary>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public MTask SendAsync(ArraySegment<byte> data)
        {
            if (data.Array == null)
            {
                throw new ArgumentException("ArraySegment 没有可发送的底层数组。", nameof(data));
            }

            byte[] buffer = ByteBufferPool.Shared.Rent(data.Count);
            Buffer.BlockCopy(data.Array, data.Offset, buffer, 0, data.Count);
            return outboundQueue.EnqueueReliableAsync(buffer, data.Count, true);
        }

        /// <summary>
        /// 将已由调用方封装并转交所有权的完整业务包放入可靠出站队列。
        /// </summary>
        /// <param name="buffer">由会话发送器负责归还的完整业务包数组。</param>
        /// <param name="length">数组中有效业务包长度。</param>
        /// <returns>底层传输完成写入或失败时完成的任务。</returns>
        internal MTask SendOwnedAsync(byte[] buffer, int length)
        {
            return outboundQueue.EnqueueReliableAsync(buffer, length, true);
        }

        /// <summary>
        /// 尝试将已封装的高频普通业务包放入非等待数据队列。
        /// </summary>
        /// <param name="buffer">由会话发送器在所有结果路径归还的完整业务包数组。</param>
        /// <param name="length">数组中有效业务包长度。</param>
        /// <returns>本次尝试的队列与连接状态。</returns>
        internal NetworkSendResult TrySendOwned(byte[] buffer, int length)
        {
            return outboundQueue.TryEnqueueData(buffer, length, true);
        }

        /// <summary>
        /// 主动断开底层传输。
        /// </summary>
        public void Close()
        {
            Transport.Disconnect();
        }

        /// <summary>
        /// 底层传输断开时触发。
        /// </summary>
        public event Action OnDisconnected
        {
            add => Transport.OnDisconnected += value;
            remove => Transport.OnDisconnected -= value;
        }

        /// <summary>
        /// 释放底层传输资源。
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
            {
                return;
            }

            outboundQueue.Dispose();
            Transport.OnDisconnected -= HandleTransportDisconnected;
            Transport.Dispose();
        }

        /// <summary>
        /// 在底层传输主动或被动断开时立即失败待发可靠包并归还所有出站缓冲。
        /// </summary>
        private void HandleTransportDisconnected()
        {
            outboundQueue.Dispose();
        }
    }
}
