using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using MiniCore.Core;
using MiniCore.Eventing;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using MiniCore.Threading;
using MiniCore.Unity;
using Unity.Profiling;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace MiniCore.HotUpdate
{

    /// <summary>
    /// 表示一条可导出的网络基准运行结果。
    /// </summary>
    [Serializable]
    public sealed class NetworkBenchmarkRunResult
    {
        #region Public 公共成员

        /// <summary>
        /// 传输名称。
        /// </summary>
        public string Transport;
        /// <summary>
        /// 压测场景名称。
        /// </summary>
        public string Scenario;
        /// <summary>
        /// 正常消息目标速率或 RPC 并发度。
        /// </summary>
        public int TargetRateOrConcurrency;
        /// <summary>
        /// 同一场景的重复运行序号。
        /// </summary>
        public int Repeat;
        /// <summary>
        /// 样本总持续时间，单位为毫秒。
        /// </summary>
        public double DurationMilliseconds;
        /// <summary>
        /// 已发起发送或请求的消息数量。
        /// </summary>
        public int SentCount;
        /// <summary>
        /// 普通消息场景尝试提交到出站队列的总数量。
        /// </summary>
        public int OfferedCount;
        /// <summary>
        /// 普通消息场景被有界出站队列拒绝的总数量。
        /// </summary>
        public int RejectedCount;
        /// <summary>
        /// 已收到业务回调或 RPC 响应的消息数量。
        /// </summary>
        public int ReceivedCount;
        /// <summary>
        /// 发送、响应或处理失败数量。
        /// </summary>
        public int FailureCount;
        /// <summary>
        /// 发送后未收到成功或失败结果的消息数量。
        /// </summary>
        public int DroppedCount;
        /// <summary>
        /// 样本期间观察到的服务端逻辑会话断开次数。
        /// </summary>
        public int DisconnectCount;
        /// <summary>
        /// 按已接收消息计算的每秒吞吐量。
        /// </summary>
        public double ThroughputPerSecond;
        /// <summary>
        /// 参与延迟百分位计算的样本数量。
        /// </summary>
        public int LatencySampleCount;
        /// <summary>
        /// P50 端到端延迟，单位为毫秒。
        /// </summary>
        public double P50Milliseconds;
        /// <summary>
        /// P95 端到端延迟，单位为毫秒。
        /// </summary>
        public double P95Milliseconds;
        /// <summary>
        /// P99 端到端延迟，单位为毫秒。
        /// </summary>
        public double P99Milliseconds;
        /// <summary>
        /// 最大端到端延迟，单位为毫秒。
        /// </summary>
        public double MaxLatencyMilliseconds;
        /// <summary>
        /// 收包队列积压包数量峰值。
        /// </summary>
        public long PeakQueuePacketCount;
        /// <summary>
        /// 收包队列积压字节数峰值。
        /// </summary>
        public long PeakQueueByteCount;
        /// <summary>
        /// 当前样本由主线程处理的收包总数。
        /// </summary>
        public long QueueProcessedPacketCount;
        /// <summary>
        /// 当前样本入站固定队列因容量不足拒绝的数据包总数。
        /// RPC 样本出现该值时，不能作为可靠性通过结果。
        /// </summary>
        public long QueueRejectedPacketCount;
        /// <summary>
        /// 单包主线程处理最大耗时，单位为毫秒。
        /// </summary>
        public double MaxPacketProcessMilliseconds;
        /// <summary>
        /// 主线程处理单个收包的 P50 耗时，单位为毫秒。
        /// </summary>
        public double IncomingPacketProcessP50Milliseconds;
        /// <summary>
        /// 主线程处理单个收包的 P95 耗时，单位为毫秒。
        /// </summary>
        public double IncomingPacketProcessP95Milliseconds;
        /// <summary>
        /// 主线程处理单个收包的 P99 耗时，单位为毫秒。
        /// </summary>
        public double IncomingPacketProcessP99Milliseconds;
        /// <summary>
        /// 入站队列等待耗时的有效样本数量；包含本机客户端与服务端两个方向。
        /// </summary>
        public long IncomingQueueWaitSampleCount;
        /// <summary>
        /// 网络线程入队到主线程开始处理的平均等待时间，单位为毫秒；包含本机客户端与服务端两个方向。
        /// </summary>
        public double IncomingQueueWaitAverageMilliseconds;
        /// <summary>
        /// 网络线程入队到主线程开始处理的最大等待时间，单位为毫秒；包含本机客户端与服务端两个方向。
        /// </summary>
        public double IncomingQueueWaitMaxMilliseconds;
        /// <summary>
        /// 网络线程入队到主线程开始处理的 P50 等待时间，单位为毫秒。
        /// </summary>
        public double IncomingQueueWaitP50Milliseconds;
        /// <summary>
        /// 网络线程入队到主线程开始处理的 P95 等待时间，单位为毫秒。
        /// </summary>
        public double IncomingQueueWaitP95Milliseconds;
        /// <summary>
        /// 网络线程入队到主线程开始处理的 P99 等待时间，单位为毫秒。
        /// </summary>
        public double IncomingQueueWaitP99Milliseconds;
        /// <summary>
        /// 本机服务端 TCP 已完成完整长度帧读取的包数量；非 TCP 传输为零。
        /// </summary>
        public long ServerTransportFramedPacketCount;
        /// <summary>
        /// 本机服务端 TCP 已完成收包回调派发的包数量；非 TCP 传输为零。
        /// </summary>
        public long ServerTransportDispatchedPacketCount;
        /// <summary>
        /// 本机服务端 TCP 底层 Socket 接收操作完成次数；一个完整长度帧通常至少对应包头和正文两次接收操作。
        /// 非 TCP 传输为零。
        /// </summary>
        public long ServerTransportReceiveOperationCount;
        /// <summary>
        /// 本机服务端 TCP 单次底层 Socket 接收操作平均等待时间，单位为毫秒；非 TCP 传输为零。
        /// </summary>
        public double ServerTransportReceiveOperationAverageMilliseconds;
        /// <summary>
        /// 本机服务端 TCP 单次底层 Socket 接收操作最大等待时间，单位为毫秒；非 TCP 传输为零。
        /// </summary>
        public double ServerTransportReceiveOperationMaxMilliseconds;
        /// <summary>
        /// 样本期间压测订阅者观察到的全部业务事件数量。
        /// </summary>
        public int NormalEventObservedCount;
        /// <summary>
        /// 样本期间被识别为 MCBENCH-N 普通消息的业务事件数量。
        /// </summary>
        public int NormalEventRecognizedCount;
        /// <summary>
        /// 样本期间未包含 MCBENCH-N 固定序号的业务事件数量。
        /// </summary>
        public int NormalEventUnrecognizedCount;
        /// <summary>
        /// 样本期间序号未登记或超过已发送范围的普通消息事件数量。
        /// </summary>
        public int NormalEventOutOfRangeCount;
        /// <summary>
        /// 样本期间已被记入接收结果的重复普通消息事件数量。
        /// </summary>
        public int NormalEventDuplicateCount;
        /// <summary>
        /// 样本期间序号已登记但发送时间尚未写入的普通消息事件数量。
        /// </summary>
        public int NormalEventMissingTimestampCount;
        /// <summary>
        /// 客户端会话出站分段耗时的有效样本数量。
        /// </summary>
        public long ClientOutboundTimingSampleCount;
        /// <summary>
        /// 客户端实际调用底层传输写入的次数；TCP 普通消息批量时小于已发送包数量。
        /// </summary>
        public long ClientTransportWriteCount;
        /// <summary>
        /// 客户端底层 Socket 发送操作完成次数；一个传输写入在部分写入时可能对应多次 Socket 操作。
        /// </summary>
        public long ClientSocketSendOperationCount;
        /// <summary>
        /// 客户端单次底层 Socket 发送操作平均等待时间，单位为毫秒。
        /// </summary>
        public double ClientSocketSendOperationAverageMilliseconds;
        /// <summary>
        /// 客户端单次底层 Socket 发送操作最大等待时间，单位为毫秒。
        /// </summary>
        public double ClientSocketSendOperationMaxMilliseconds;
        /// <summary>
        /// 客户端包进入出站队列到开始调用传输发送的平均等待时间，单位为毫秒。
        /// </summary>
        public double ClientOutboundQueueWaitAverageMilliseconds;
        /// <summary>
        /// 客户端包进入出站队列到开始调用传输发送的最大等待时间，单位为毫秒。
        /// </summary>
        public double ClientOutboundQueueWaitMaxMilliseconds;
        /// <summary>
        /// 客户端调用底层传输发送到完成的平均等待时间，单位为毫秒。
        /// </summary>
        public double ClientTransportSendAverageMilliseconds;
        /// <summary>
        /// 客户端调用底层传输发送到完成的最大等待时间，单位为毫秒。
        /// </summary>
        public double ClientTransportSendMaxMilliseconds;
        /// <summary>
        /// 本机服务端全部会话出站分段耗时的有效样本数量。
        /// </summary>
        public long ServerOutboundTimingSampleCount;
        /// <summary>
        /// 本机服务端包进入出站队列到开始调用传输发送的平均等待时间，单位为毫秒。
        /// </summary>
        public double ServerOutboundQueueWaitAverageMilliseconds;
        /// <summary>
        /// 本机服务端包进入出站队列到开始调用传输发送的最大等待时间，单位为毫秒。
        /// </summary>
        public double ServerOutboundQueueWaitMaxMilliseconds;
        /// <summary>
        /// 本机服务端调用底层传输发送到完成的平均等待时间，单位为毫秒。
        /// </summary>
        public double ServerTransportSendAverageMilliseconds;
        /// <summary>
        /// 本机服务端调用底层传输发送到完成的最大等待时间，单位为毫秒。
        /// </summary>
        public double ServerTransportSendMaxMilliseconds;
        /// <summary>
        /// 样本期间 GC Allocated In Frame 的最大字节数。
        /// </summary>
        public long MaxGcAllocatedBytesPerFrame;
        /// <summary>
        /// 主线程停顿场景的人为停顿时长；其他场景为零。
        /// </summary>
        public int HitchMilliseconds;
        /// <summary>
        /// 主线程停顿后等待消息与队列恢复的耗时；其他场景为零。
        /// </summary>
        public double QueueRecoveryMilliseconds;

        #endregion
    }
}
