using System;
using System.Collections.Generic;
using System.Threading;
using Google.Protobuf;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 按房间身份稳定取模的默认分配策略。
    /// </summary>
    public sealed class MiniBomberModuloRoomAssignmentStrategy : IMiniBomberRoomAssignmentStrategy
    {
        /// <summary>
        /// 使用无符号房间身份取模选择 Worker。
        /// </summary>
        /// <param name="roomId">稳定房间身份。</param>
        /// <param name="workerCount">当前固定 Worker 数量。</param>
        /// <returns>稳定 Worker 下标。</returns>
        public int SelectWorker(long roomId, int workerCount)
        {
            if (workerCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(workerCount));
            }

            return (int)((ulong)roomId % (uint)workerCount);
        }
    }
}
