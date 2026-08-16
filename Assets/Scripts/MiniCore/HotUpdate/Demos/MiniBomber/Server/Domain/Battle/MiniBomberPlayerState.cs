using System;
using System.Collections.Generic;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 服务端权威玩家状态。
    /// </summary>
    public sealed class MiniBomberPlayerState
    {
        #region Public 公共成员

        public long PlayerId { get; internal set; }
        public string PlayerName { get; internal set; }
        public int PositionXMillimeters { get; internal set; }
        public int PositionZMillimeters { get; internal set; }
        public int FacingX { get; internal set; }
        public int FacingZ { get; internal set; }
        public int MoveX { get; internal set; }
        public int MoveZ { get; internal set; }
        public long MovementRemainderX { get; internal set; }
        public long MovementRemainderZ { get; internal set; }
        public long LastInputSequence { get; internal set; }
        public long LastInputServerTick { get; internal set; }
        public bool IsAlive { get; internal set; }
        public bool IsOnline { get; internal set; }
        public long RespawnTick { get; internal set; }
        public long InvulnerableUntilTick { get; internal set; }
        public int Score { get; internal set; }
        public int Kills { get; internal set; }
        public int Deaths { get; internal set; }
        public int BombCapacity { get; internal set; }
        public int BombRange { get; internal set; }
        public int SpawnIndex { get; internal set; }
        public bool PendingBombPlacement { get; internal set; }

        #endregion
    }
}
