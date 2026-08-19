using MiniCore.Core;
using MiniCore.Threading;
using MiniCore.UI;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 比赛结果 Presenter，投影最终结果 Model 并协调关闭意图。
    /// </summary>
    public sealed class MatchResultWindowPresenter : AUIWindowPresenter<MatchResultWindowView>
    {
        #region Private 私有成员

        private readonly MatchResultWindowViewData viewData = new MatchResultWindowViewData(); // 复用比赛结果显示数据。
        private BattleClientComponent battle; // 战斗状态组件。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 获取战斗依赖、绑定关闭意图并投影最终成绩。
        /// </summary>
        protected override void OnBind()
        {
            battle = Global.Get<BattleClientComponent>(this);
            View.BindActions(Bindings, Close);
            Render();
        }

        /// <summary>
        /// 清空战斗引用和复用成绩列表。
        /// </summary>
        protected override void OnDispose()
        {
            battle = null;
            viewData.MutableEntries.Clear();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将最终比赛结果 Model 投影为窗口专用显示数据。
        /// </summary>
        private void Render()
        {
            MiniBomberMatchResultModel result = battle.Model.Result;
            viewData.ReturnCountdownSeconds = 0;
            if (result != null)
            {
                viewData.ReturnCountdownSeconds = result.ReturnToRoomMilliseconds / 1000;
                while (viewData.MutableEntries.Count < result.Entries.Count)
                {
                    viewData.MutableEntries.Add(new MatchResultEntryViewData());
                }

                for (int index = 0; index < result.Entries.Count; index++)
                {
                    MiniBomberMatchResultEntryModel source = result.Entries[index];
                    MatchResultEntryViewData item = viewData.MutableEntries[index];
                    item.Rank = source.Rank;
                    item.PlayerName = source.PlayerName;
                    item.Score = source.Score;
                    item.Kills = source.Kills;
                    item.Deaths = source.Deaths;
                }

                if (viewData.MutableEntries.Count > result.Entries.Count)
                {
                    viewData.MutableEntries.RemoveRange(result.Entries.Count, viewData.MutableEntries.Count - result.Entries.Count);
                }
            }
            else
            {
                viewData.MutableEntries.Clear();
            }

            View.Refresh(viewData);
        }

        /// <summary>
        /// 关闭比赛结果窗口。
        /// </summary>
        private void Close()
        {
            Context.Service.CloseAsync(Context.Handle).Forget();
        }

        #endregion
    }
}
