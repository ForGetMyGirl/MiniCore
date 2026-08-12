using System;
using System.Collections.Generic;
using System.Threading;
using Google.Protobuf;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 房间到固定 Worker 的稳定分配策略扩展点。
    /// </summary>
    public interface IMiniBomberRoomAssignmentStrategy
    {
        /// <summary>
        /// 为房间选择固定 Worker 下标。
        /// </summary>
        /// <param name="roomId">稳定房间身份。</param>
        /// <param name="workerCount">当前固定 Worker 数量。</param>
        /// <returns>零到 Worker 数量减一之间的下标。</returns>
        int SelectWorker(long roomId, int workerCount);
    }
}
