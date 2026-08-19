using System.Collections.Generic;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;
using MiniCore.UI;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 战斗 HUD Presenter，按修订号协调各显示分区。
    /// </summary>
    public sealed class BattleHudWindowPresenter : AUIWindowPresenter<BattleHudWindowView>
    {
        #region Private 私有成员

        private readonly List<BattleRankingItemViewData> rankingItems = new List<BattleRankingItemViewData>(4); // 复用排名显示条目。
        private BattleClientComponent battle; // 战斗状态组件。
        private INetworkService network; // 网络往返延迟来源。
        private long lastRankingRevision = -1; // 已显示排名修订号。
        private long lastEventRevision = -1; // 已显示事件修订号。
        private int lastPerformanceFrameCount; // 上次性能采样累计帧数。
        private double lastPerformanceSampleTime; // 上次性能采样时间。
        private bool performanceRefreshActive; // 性能刷新循环是否有效。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 获取战斗依赖并绑定战斗 Model 分区变化事件。
        /// </summary>
        protected override void OnBind()
        {
            battle = Global.Get<BattleClientComponent>(this);
            network = Global.GetService<INetworkService>(this);
            battle.SnapshotChanged += RenderSnapshotSections;
            battle.EventsChanged += RenderEventSection;
            Bindings.Add(() => battle.SnapshotChanged -= RenderSnapshotSections);
            Bindings.Add(() => battle.EventsChanged -= RenderEventSection);
            View.SetPlatformMode(Application.platform == RuntimePlatform.Android);
            performanceRefreshActive = View.HasPerformanceOutput;
            lastPerformanceFrameCount = Time.frameCount;
            lastPerformanceSampleTime = Global.Time.UnscaledTime;
            if (performanceRefreshActive) RefreshPerformanceLoopAsync().Forget();
            RenderSnapshotSections();
            RenderEventSection();
        }

        /// <summary>
        /// 停止性能刷新并清空战斗依赖与排名缓存。
        /// </summary>
        protected override void OnDispose()
        {
            performanceRefreshActive = false;
            network = null;
            battle = null;
            rankingItems.Clear();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 刷新时间分区，并仅在排名修订变化时投影排名分区。
        /// </summary>
        private void RenderSnapshotSections()
        {
            MiniBomberBattleModel model = battle.Model;
            if (!model.HasSnapshot) return;
            View.RefreshTime(model.RemainingMilliseconds);
            if (lastRankingRevision == model.RankingRevision) return;
            lastRankingRevision = model.RankingRevision;
            while (rankingItems.Count < model.Players.Count)
            {
                rankingItems.Add(new BattleRankingItemViewData());
            }

            for (int index = 0; index < model.Players.Count; index++)
            {
                MiniBomberBattlePlayerModel player = model.Players[index];
                BattleRankingItemViewData item = rankingItems[index];
                item.PlayerName = player.PlayerName;
                item.Score = player.Score;
            }

            if (rankingItems.Count > model.Players.Count)
            {
                rankingItems.RemoveRange(model.Players.Count, rankingItems.Count - model.Players.Count);
            }

            View.RefreshRanking(rankingItems);
        }

        /// <summary>
        /// 在事件修订变化时投影最近一条击杀提示。
        /// </summary>
        private void RenderEventSection()
        {
            MiniBomberBattleModel model = battle.Model;
            if (lastEventRevision == model.EventRevision) return;
            lastEventRevision = model.EventRevision;
            for (int index = model.RecentEvents.Count - 1; index >= 0; index--)
            {
                MiniBomberBattleEventModel item = model.RecentEvents[index];
                if (item.Kind != MiniBomberBattleEventKind.PlayerKilled) continue;
                string message = item.ActorPlayerId == item.TargetPlayerId || item.ActorPlayerId == 0
                    ? $"{item.TargetName} 玩家被炸飞了"
                    : $"{item.ActorName} 玩家击杀了 {item.TargetName}";
                View.ShowKillFeed(message);
                ClearKillFeedAsync(message).Forget();
                return;
            }
        }

        /// <summary>
        /// 延迟清除仍未被新消息替换的击杀提示。
        /// </summary>
        /// <param name="expected">安排清除时的提示内容。</param>
        /// <returns>延迟清理完成任务。</returns>
        private async MTask ClearKillFeedAsync(string expected)
        {
            await MTask.Delay(2500);
            View.ClearKillFeed(expected);
        }

        /// <summary>
        /// 每半秒刷新一次帧率与应用层心跳往返延迟。
        /// </summary>
        /// <returns>窗口释放后结束的刷新任务。</returns>
        private async MTask RefreshPerformanceLoopAsync()
        {
            while (performanceRefreshActive)
            {
                await MTask.Delay(500);
                if (!performanceRefreshActive) return;
                double now = Global.Time.UnscaledTime;
                int frameCount = Time.frameCount;
                double elapsedSeconds = now - lastPerformanceSampleTime;
                double framesPerSecond = elapsedSeconds > 0d
                    ? (frameCount - lastPerformanceFrameCount) / elapsedSeconds
                    : 0d;
                lastPerformanceFrameCount = frameCount;
                lastPerformanceSampleTime = now;
                int rttMilliseconds = 0;
                bool hasRtt = network != null && network.TryGetLastPingMs(MiniBomberConstants.DefaultSessionId, out rttMilliseconds);
                View.RefreshPerformance(framesPerSecond, hasRtt, rttMilliseconds);
            }
        }

        #endregion
    }
}
