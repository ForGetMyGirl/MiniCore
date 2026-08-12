using System;
using System.Collections.Generic;
using System.Threading;
using Google.Protobuf;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// Worker 独占的单局状态。
    /// </summary>
    internal sealed class MiniBomberWorkerMatchState
    {
        #region Internal 内部成员

        internal long RoomId { get; }
        internal MiniBomberBattleSimulation Simulation { get; }
        internal bool IsStarted { get; set; }
        internal bool ResultCreated { get; set; }
        internal long LastPublishedTick { get; set; }
        internal long LastEventId { get; set; }

        /// <summary>
        /// 创建 Worker 独占比赛状态。
        /// </summary>
        /// <param name="roomId">房间身份。</param>
        /// <param name="simulation">权威模拟。</param>
        internal MiniBomberWorkerMatchState(long roomId, MiniBomberBattleSimulation simulation)
        {
            RoomId = roomId;
            Simulation = simulation;
        }

        #endregion
    }
}
