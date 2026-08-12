using System;
using System.Collections.Generic;
using MiniCore.Protocol.Generated;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// Dedicated Server 持有的一局战斗运行状态。
    /// </summary>
    public sealed class MiniBomberServerMatch
    {
        #region Public 公共成员

        /// <summary>
        /// 比赛身份。
        /// </summary>
        public long MatchId { get; set; }

        /// <summary>
        /// 所属房间身份。
        /// </summary>
        public long RoomId { get; set; }

        /// <summary>
        /// 稳定归属的 RoomWorker 下标。
        /// </summary>
        public int WorkerIndex { get; set; }

        /// <summary>
        /// 倒计时结束并开始模拟的单调时间秒数。
        /// </summary>
        public double StartTime { get; set; }

        /// <summary>
        /// 是否已经向 RoomWorker 投递开始命令。
        /// </summary>
        public bool IsStarted { get; set; }

        /// <summary>
        /// 是否已经广播最终成绩。
        /// </summary>
        public bool ResultBroadcasted { get; set; }

        /// <summary>
        /// 成绩展示结束并返回房间的单调时间秒数。
        /// </summary>
        public double ReturnToRoomTime { get; set; }

        #endregion
    }
}
