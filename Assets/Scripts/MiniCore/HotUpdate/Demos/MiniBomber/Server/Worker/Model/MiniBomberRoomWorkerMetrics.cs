using System;
using System.Collections.Generic;
using System.Threading;
using Google.Protobuf;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 单个 RoomWorker 的只读诊断快照。
    /// </summary>
    public readonly struct MiniBomberRoomWorkerMetrics
    {
        #region Public 公共成员

        /// <summary>
        /// Worker 下标。
        /// </summary>
        public int WorkerIndex { get; }
        /// <summary>
        /// 独占线程托管标识。
        /// </summary>
        public int ThreadId { get; }
        /// <summary>
        /// 当前归属比赛数量。
        /// </summary>
        public int ActiveMatchCount { get; }
        /// <summary>
        /// 当前输入队列深度。
        /// </summary>
        public int InputQueueDepth { get; }
        /// <summary>
        /// 当前输出队列深度。
        /// </summary>
        public int OutputQueueDepth { get; }
        /// <summary>
        /// 因输入队列满而拒绝的命令数。
        /// </summary>
        public long RejectedInputCount { get; }
        /// <summary>
        /// 因输出背压而跳过的逻辑步数。
        /// </summary>
        public long OutputBackpressureCount { get; }
        /// <summary>
        /// 已经完成的比赛逻辑步数。
        /// </summary>
        public long ProcessedTickCount { get; }

        /// <summary>
        /// 创建 RoomWorker 诊断快照。
        /// </summary>
        /// <param name="workerIndex">Worker 下标。</param>
        /// <param name="threadId">独占线程标识。</param>
        /// <param name="activeMatchCount">归属比赛数量。</param>
        /// <param name="inputQueueDepth">输入队列深度。</param>
        /// <param name="outputQueueDepth">输出队列深度。</param>
        /// <param name="rejectedInputCount">拒绝输入数量。</param>
        /// <param name="outputBackpressureCount">输出背压数量。</param>
        /// <param name="processedTickCount">已完成逻辑步数量。</param>
        public MiniBomberRoomWorkerMetrics(int workerIndex, int threadId, int activeMatchCount, int inputQueueDepth, int outputQueueDepth, long rejectedInputCount, long outputBackpressureCount, long processedTickCount)
        {
            WorkerIndex = workerIndex;
            ThreadId = threadId;
            ActiveMatchCount = activeMatchCount;
            InputQueueDepth = inputQueueDepth;
            OutputQueueDepth = outputQueueDepth;
            RejectedInputCount = rejectedInputCount;
            OutputBackpressureCount = outputBackpressureCount;
            ProcessedTickCount = processedTickCount;
        }

        #endregion
    }
}
