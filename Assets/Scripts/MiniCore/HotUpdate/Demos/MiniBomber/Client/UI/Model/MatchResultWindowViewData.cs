using System.Collections.Generic;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 比赛结果窗口一次完整刷新的专用显示数据。
    /// </summary>
    public sealed class MatchResultWindowViewData
    {
        #region Private 私有成员

        private readonly List<MatchResultEntryViewData> entries = new List<MatchResultEntryViewData>(4); // 复用成绩显示条目。

        #endregion

        #region Public 公共成员

        /// <summary>获取自动返回房间的秒数。</summary>
        public int ReturnCountdownSeconds { get; internal set; }
        /// <summary>获取最终成绩显示列表。</summary>
        public IReadOnlyList<MatchResultEntryViewData> Entries => entries;

        #endregion

        #region Internal 内部成员

        /// <summary>获取仅供 Presenter 投影复用的成绩集合。</summary>
        internal List<MatchResultEntryViewData> MutableEntries => entries;

        #endregion
    }
}
