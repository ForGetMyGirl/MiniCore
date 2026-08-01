using MiniCore.Threading;
using System;
using System.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// 传输层接口，负责底层传输实现（如 TCP/WebSocket 等）。
    /// </summary>
    public interface INetworkTransport : IDisposable
    {
        /// <summary>
        /// 传输层是否保持连接或可用状态。
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 连接到指定远端地址。
        /// </summary>
        MTask ConnectAsync(string host, int port);

        /// <summary>
        /// 发送一个完整的业务数据包。
        /// </summary>
        MTask SendAsync(ArraySegment<byte> data);

        /// <summary>
        /// 主动断开传输层连接。
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 接收到完整业务数据包时触发。
        /// </summary>
        event Func<ReadOnlyMemory<byte>, MTask> OnDataReceived;
        /// <summary>
        /// 传输层断开时触发。
        /// </summary>
        event Action OnDisconnected;
    }

    /// <summary>
    /// 表示底层传输在完整长度帧读取完成与收包回调派发完成两个边界上的诊断计数。
    /// 仅在显式启用诊断时累积，用于定位数据停留在 TCP 读循环之前还是应用入站队列之前。
    /// </summary>
    public readonly struct NetworkTransportReceiveSnapshot
    {
        #region Public 公共成员

        /// <summary>
        /// 获取当前统计周期内已从底层传输完整读取长度头与业务正文的数据包数量。
        /// </summary>
        public long FramedPacketCount { get; }

        /// <summary>
        /// 获取当前统计周期内已完成传输层收包回调派发的数据包数量。
        /// </summary>
        public long DispatchedPacketCount { get; }

        /// <summary>
        /// 获取当前统计周期内底层 Socket 接收操作完成的次数。
        /// 一个完整长度帧通常至少需要读取长度头和正文两次。
        /// </summary>
        public long ReceiveOperationCount { get; }

        /// <summary>
        /// 获取一次底层 Socket 接收操作从发起到完成的平均等待时间，单位为毫秒。
        /// </summary>
        public double AverageReceiveOperationMilliseconds { get; }

        /// <summary>
        /// 获取一次底层 Socket 接收操作从发起到完成的最大等待时间，单位为毫秒。
        /// </summary>
        public double MaxReceiveOperationMilliseconds { get; }

        /// <summary>
        /// 使用完整帧读取数和回调派发数创建不可变诊断快照。
        /// </summary>
        /// <param name="framedPacketCount">已完成完整长度帧读取的数据包数量。</param>
        /// <param name="dispatchedPacketCount">已完成收包回调派发的数据包数量。</param>
        /// <param name="receiveOperationCount">底层 Socket 接收操作完成次数。</param>
        /// <param name="averageReceiveOperationMilliseconds">底层 Socket 接收操作平均等待时间。</param>
        /// <param name="maxReceiveOperationMilliseconds">底层 Socket 接收操作最大等待时间。</param>
        public NetworkTransportReceiveSnapshot(
            long framedPacketCount,
            long dispatchedPacketCount,
            long receiveOperationCount,
            double averageReceiveOperationMilliseconds,
            double maxReceiveOperationMilliseconds)
        {
            FramedPacketCount = framedPacketCount;
            DispatchedPacketCount = dispatchedPacketCount;
            ReceiveOperationCount = receiveOperationCount;
            AverageReceiveOperationMilliseconds = averageReceiveOperationMilliseconds;
            MaxReceiveOperationMilliseconds = maxReceiveOperationMilliseconds;
        }

        #endregion
    }

    /// <summary>
    /// 表示底层传输 Socket 发送操作的诊断计数与等待时间。
    /// 仅在显式启用诊断时累积，用于区分发送泵排队和操作系统 Socket 写入背压。
    /// </summary>
    public readonly struct NetworkTransportSendSnapshot
    {
        #region Public 公共成员

        /// <summary>
        /// 获取当前统计周期内底层 Socket 发送操作完成的次数。
        /// 一个发送批次在部分写入时可能对应多次 Socket 发送操作。
        /// </summary>
        public long SendOperationCount { get; }

        /// <summary>
        /// 获取一次底层 Socket 发送操作从发起到完成的平均等待时间，单位为毫秒。
        /// </summary>
        public double AverageSendOperationMilliseconds { get; }

        /// <summary>
        /// 获取一次底层 Socket 发送操作从发起到完成的最大等待时间，单位为毫秒。
        /// </summary>
        public double MaxSendOperationMilliseconds { get; }

        /// <summary>
        /// 使用底层 Socket 发送操作计数和等待时间创建不可变诊断快照。
        /// </summary>
        /// <param name="sendOperationCount">底层 Socket 发送操作完成次数。</param>
        /// <param name="averageSendOperationMilliseconds">底层 Socket 发送操作平均等待时间。</param>
        /// <param name="maxSendOperationMilliseconds">底层 Socket 发送操作最大等待时间。</param>
        public NetworkTransportSendSnapshot(
            long sendOperationCount,
            double averageSendOperationMilliseconds,
            double maxSendOperationMilliseconds)
        {
            SendOperationCount = sendOperationCount;
            AverageSendOperationMilliseconds = averageSendOperationMilliseconds;
            MaxSendOperationMilliseconds = maxSendOperationMilliseconds;
        }

        #endregion
    }

    /// <summary>
    /// 允许发送器直接写入已包含长度前缀的连续 TCP 帧字节。
    /// 仅供同会话、已保持顺序的出站发送器批量合并普通数据包使用；调用方不得混入未加长度前缀的业务正文。
    /// </summary>
    internal interface IFramedBatchNetworkTransport
    {
        /// <summary>
        /// 将一个或多个已完成长度前缀封装的连续 TCP 帧写入底层传输。
        /// </summary>
        /// <param name="frames">按发送顺序连续排列的完整长度帧字节。</param>
        /// <returns>全部帧字节写入完成或发生异常时完成的任务。</returns>
        MTask SendFramedBatchAsync(ArraySegment<byte> frames);
    }

    /// <summary>
    /// 允许会话发送器写入一个已封装多个逻辑业务包的 UDP 数据报。
    /// 仅供 TrySend 数据队列使用；调用方必须确保该数据报不超过当前路径的安全 MTU 预算。
    /// </summary>
    internal interface IDatagramBatchNetworkTransport
    {
        /// <summary>
        /// 将一个完整 UDP 批量数据报写入底层传输。
        /// </summary>
        /// <param name="datagram">已包含批量协议头和多个业务包的完整 UDP 数据报。</param>
        /// <returns>数据报被底层 Socket 接受或发生异常时完成的任务。</returns>
        MTask SendDatagramBatchAsync(ArraySegment<byte> datagram);
    }

    /// <summary>
    /// 允许逻辑会话按需启用底层传输收发边界诊断。
    /// 仅供压测与故障定位区分 Socket 收发、完整帧读取与收包回调，不参与正常收发控制。
    /// </summary>
    internal interface ITransportDiagnosticsNetworkTransport
    {
        /// <summary>
        /// 启用或关闭收发边界诊断，并清空上一统计周期的数据。
        /// </summary>
        /// <param name="enabled">为 true 时记录 Socket 收发、完整帧读取与回调完成数量。</param>
        void SetTransportDiagnosticsEnabled(bool enabled);

        /// <summary>
        /// 获取当前统计周期的收包边界快照。
        /// </summary>
        /// <returns>不转移缓冲区所有权的只读统计快照。</returns>
        NetworkTransportReceiveSnapshot CaptureReceiveDiagnostics();

        /// <summary>
        /// 获取当前统计周期的底层 Socket 发送操作快照。
        /// </summary>
        /// <returns>不转移缓冲区所有权的只读统计快照。</returns>
        NetworkTransportSendSnapshot CaptureSendDiagnostics();
    }
}
