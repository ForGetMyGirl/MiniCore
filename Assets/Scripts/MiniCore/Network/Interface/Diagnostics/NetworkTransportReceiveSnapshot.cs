using MiniCore.Threading;
using System;
using System.Threading;

namespace MiniCore.Model
{

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
}
