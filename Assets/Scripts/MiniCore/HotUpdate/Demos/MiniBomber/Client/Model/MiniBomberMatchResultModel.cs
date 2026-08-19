using System.Collections.Generic;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 客户端最终比赛成绩数据。
    /// </summary>
    public sealed class MiniBomberMatchResultModel
    {
        #region Private 私有成员

        private readonly List<MiniBomberMatchResultEntryModel> entries = new List<MiniBomberMatchResultEntryModel>(4); // 服务器权威排名。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取所属房间标识。
        /// </summary>
        public long RoomId { get; internal set; }

        /// <summary>
        /// 获取所属比赛标识。
        /// </summary>
        public long MatchId { get; internal set; }

        /// <summary>
        /// 获取自动返回房间的毫秒数。
        /// </summary>
        public int ReturnToRoomMilliseconds { get; internal set; }

        /// <summary>
        /// 获取只读成绩列表。
        /// </summary>
        public IReadOnlyList<MiniBomberMatchResultEntryModel> Entries => entries;

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 获取仅供战斗组件归并的成绩集合。
        /// </summary>
        internal List<MiniBomberMatchResultEntryModel> MutableEntries => entries;

        #endregion
    }
}
