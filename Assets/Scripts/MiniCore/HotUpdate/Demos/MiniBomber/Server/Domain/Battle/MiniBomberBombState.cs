using System;
using System.Collections.Generic;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 服务端权威炸弹状态。
    /// </summary>
    public sealed class MiniBomberBombState
    {
        #region Public 公共成员

        public long BombId { get; internal set; }
        public long OwnerPlayerId { get; internal set; }
        public int CellX { get; internal set; }
        public int CellZ { get; internal set; }
        public int Range { get; internal set; }
        public long ExplodeTick { get; internal set; }
        public bool OwnerCanPass { get; internal set; }

        #endregion
    }
}
