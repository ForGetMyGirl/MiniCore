using System;
using System.Collections.Generic;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 客户端提交给权威模拟的量化输入。
    /// </summary>
    public readonly struct MiniBomberBattleInput
    {
        #region Public 公共成员

        public long Sequence { get; }
        public int MoveX { get; }
        public int MoveZ { get; }
        public bool PlaceBomb { get; }

        /// <summary>
        /// 创建量化战斗输入。
        /// </summary>
        /// <param name="sequence">单玩家递增输入序号。</param>
        /// <param name="moveX">范围为负一千到一千的横向输入。</param>
        /// <param name="moveZ">范围为负一千到一千的纵向输入。</param>
        /// <param name="placeBomb">本帧是否触发放置炸弹。</param>
        public MiniBomberBattleInput(long sequence, int moveX, int moveZ, bool placeBomb)
        {
            Sequence = sequence;
            MoveX = moveX;
            MoveZ = moveZ;
            PlaceBomb = placeBomb;
        }

        #endregion
    }
}
