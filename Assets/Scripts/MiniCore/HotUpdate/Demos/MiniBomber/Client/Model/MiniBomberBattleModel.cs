using System;
using System.Collections.Generic;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 客户端战斗复制与表现共享的长期业务数据。
    /// </summary>
    public sealed class MiniBomberBattleModel
    {
        #region Private 私有成员

        private readonly List<MiniBomberBattlePlayerModel> players = new List<MiniBomberBattlePlayerModel>(4); // 当前玩家状态。
        private readonly List<MiniBomberBattleBombModel> bombs = new List<MiniBomberBattleBombModel>(16); // 当前炸弹状态。
        private readonly List<MiniBomberBattlePickupModel> pickups = new List<MiniBomberBattlePickupModel>(16); // 当前道具状态。
        private readonly List<MiniBomberBattleEventModel> recentEvents = new List<MiniBomberBattleEventModel>(32); // 最近即时事件。
        private byte[] destroyedBreakableCells = Array.Empty<byte>(); // 被摧毁木箱位图。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 判断当前是否持有有效战斗快照。
        /// </summary>
        public bool HasSnapshot => MatchId > 0;

        /// <summary>
        /// 获取比赛标识。
        /// </summary>
        public long MatchId { get; internal set; }

        /// <summary>
        /// 获取服务器 Tick。
        /// </summary>
        public long ServerTick { get; internal set; }

        /// <summary>
        /// 获取剩余比赛毫秒数。
        /// </summary>
        public int RemainingMilliseconds { get; internal set; }

        /// <summary>
        /// 获取服务器状态修订号。
        /// </summary>
        public long Revision { get; internal set; }

        /// <summary>
        /// 获取最后连续应用的事件标识。
        /// </summary>
        public long LastEventId { get; internal set; }

        /// <summary>
        /// 获取最新快照到达客户端的单调时间。
        /// </summary>
        public double LastSnapshotReceiveTime { get; internal set; }

        /// <summary>
        /// 获取排名数据修订号。
        /// </summary>
        public long RankingRevision { get; internal set; }

        /// <summary>
        /// 获取即时事件修订号。
        /// </summary>
        public long EventRevision { get; internal set; }

        /// <summary>
        /// 获取被摧毁木箱的位图副本。
        /// </summary>
        public IReadOnlyList<byte> DestroyedBreakableCells => destroyedBreakableCells;

        /// <summary>
        /// 获取只读玩家列表。
        /// </summary>
        public IReadOnlyList<MiniBomberBattlePlayerModel> Players => players;

        /// <summary>
        /// 获取只读炸弹列表。
        /// </summary>
        public IReadOnlyList<MiniBomberBattleBombModel> Bombs => bombs;

        /// <summary>
        /// 获取只读道具列表。
        /// </summary>
        public IReadOnlyList<MiniBomberBattlePickupModel> Pickups => pickups;

        /// <summary>
        /// 获取只读近期事件列表。
        /// </summary>
        public IReadOnlyList<MiniBomberBattleEventModel> RecentEvents => recentEvents;

        /// <summary>
        /// 获取当前最终比赛成绩。
        /// </summary>
        public MiniBomberMatchResultModel Result { get; internal set; }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 获取仅供战斗组件归并的玩家集合。
        /// </summary>
        internal List<MiniBomberBattlePlayerModel> MutablePlayers => players;

        /// <summary>
        /// 获取仅供战斗组件归并的炸弹集合。
        /// </summary>
        internal List<MiniBomberBattleBombModel> MutableBombs => bombs;

        /// <summary>
        /// 获取仅供战斗组件归并的道具集合。
        /// </summary>
        internal List<MiniBomberBattlePickupModel> MutablePickups => pickups;

        /// <summary>
        /// 获取仅供战斗组件归并的近期事件集合。
        /// </summary>
        internal List<MiniBomberBattleEventModel> MutableRecentEvents => recentEvents;

        /// <summary>
        /// 获取或替换仅供战斗组件写入的木箱位图缓冲。
        /// </summary>
        internal byte[] MutableDestroyedBreakableCells
        {
            get => destroyedBreakableCells;
            set => destroyedBreakableCells = value ?? Array.Empty<byte>();
        }

        #endregion
    }
}
