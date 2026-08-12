using MiniCore.Threading;
using System;
using System.Threading;

namespace MiniCore.Model
{

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
}
