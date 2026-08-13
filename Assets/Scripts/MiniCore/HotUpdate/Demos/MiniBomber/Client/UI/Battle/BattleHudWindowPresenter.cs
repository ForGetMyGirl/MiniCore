using System;
using System.Text;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using MiniCore.Threading;
using MiniCore.UI;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 战斗 HUD Presenter。
    /// </summary>
    public sealed class BattleHudWindowPresenter : AUIWindowPresenter<BattleHudWindowView>
    {
        #region Private 私有成员

        private readonly StringBuilder builder = new StringBuilder(256); // 排行格式化缓存。
        private BattleClientComponent battle; // 战斗状态组件。
        private INetworkService network; // HUD 网络往返延迟来源。
        private int lastPerformanceFrameCount; // 上一次性能采样时的累计渲染帧数。
        private double lastPerformanceSampleTime; // 上一次性能采样的未缩放时间。
        private bool performanceRefreshActive; // HUD 性能刷新任务是否仍然有效。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 绑定战斗快照和即时事件。
        /// </summary>
        protected override void OnBind()
        {
            battle = Global.Get<BattleClientComponent>(this);
            network = Global.GetService<INetworkService>(this);
            battle.SnapshotChanged += RenderSnapshot;
            battle.EventsChanged += RenderEvents;
            Bindings.Add(() => battle.SnapshotChanged -= RenderSnapshot);
            Bindings.Add(() => battle.EventsChanged -= RenderEvents);
            bool mobile = Application.platform == RuntimePlatform.Android;
            View.MobileControlRoot?.SetActive(mobile);
            View.DesktopHintRoot?.SetActive(!mobile);
            performanceRefreshActive = View.PerformanceText != null;
            lastPerformanceFrameCount = Time.frameCount;
            lastPerformanceSampleTime = Global.Time.UnscaledTime;
            if (performanceRefreshActive)
            {
                RefreshPerformanceLoopAsync().Forget();
            }

            RenderSnapshot();
        }

        /// <summary>
        /// 清空战斗引用和格式化缓存。
        /// </summary>
        protected override void OnDispose()
        {
            performanceRefreshActive = false;
            network = null;
            battle = null;
            builder.Clear();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 渲染剩余时间和服务器权威实时得分。
        /// </summary>
        private void RenderSnapshot()
        {
            MiniBomberBattleSnapshot snapshot = battle.Snapshot;
            if (snapshot == null)
            {
                return;
            }

            int seconds = Mathf.Max(0, snapshot.RemainingMilliseconds / 1000);
            View.RemainingTimeText.text = $"{seconds / 60:00}:{seconds % 60:00}";
            builder.Clear();
            for (int index = 0; index < snapshot.Players.Count; index++)
            {
                MiniBomberBattlePlayerDto player = snapshot.Players[index];
                builder.Append(player.PlayerName).Append("  ").Append(player.Score).AppendLine();
            }

            View.RankingText.text = builder.ToString();
        }

        /// <summary>
        /// 渲染最近一条服务器击杀事件。
        /// </summary>
        private void RenderEvents()
        {
            for (int index = battle.RecentEvents.Count - 1; index >= 0; index--)
            {
                MiniBomberBattleEventDto item = battle.RecentEvents[index];
                if (item.Type == MiniBomberBattleEventType.MiniBomberEventPlayerKilled)
                {
                    View.KillFeedText.text = item.ActorPlayerId == item.TargetPlayerId || item.ActorPlayerId == 0
                        ? $"{item.TargetName} 玩家被炸飞了"
                        : $"{item.ActorName} 玩家击杀了 {item.TargetName}";
                    ClearKillFeedAsync(View.KillFeedText.text).Forget();
                    return;
                }
            }
        }

        /// <summary>
        /// 两点五秒后清除仍未被新消息替换的击杀提示。
        /// </summary>
        /// <param name="expected">安排清除时的提示文本。</param>
        /// <returns>延迟清理完成任务。</returns>
        private async MTask ClearKillFeedAsync(string expected)
        {
            await MTask.Delay(2500);
            if (string.Equals(View.KillFeedText.text, expected, StringComparison.Ordinal))
            {
                View.KillFeedText.text = string.Empty;
            }
        }

        /// <summary>
        /// 每半秒计算平均渲染帧率并刷新跨传输统一的应用层心跳往返延迟。
        /// </summary>
        /// <returns>窗口释放或任务域取消后结束的刷新任务。</returns>
        private async MTask RefreshPerformanceLoopAsync()
        {
            while (performanceRefreshActive)
            {
                await MTask.Delay(500);
                if (!performanceRefreshActive)
                {
                    return;
                }

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
                View.PerformanceText.text = hasRtt
                    ? $"FPS: {framesPerSecond:F2}\nRTT: {rttMilliseconds} ms"
                    : $"FPS: {framesPerSecond:F2}\nRTT: --";
            }
        }

        #endregion
    }
}
