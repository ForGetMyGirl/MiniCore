using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 战斗 HUD View，按时间、排名、事件和性能分区刷新。
    /// </summary>
    public sealed class BattleHudWindowView : MiniBomberWindowViewBase
    {
        #region Private 私有成员

        private readonly StringBuilder rankingBuilder = new StringBuilder(256); // 排名显示缓存。

        #endregion

        #region UnityProperty Unity 引用属性

        [SerializeField] private TMP_Text RemainingTimeText; // 剩余时间文本。
        [SerializeField] private TMP_Text RankingText; // 实时排行文本。
        [SerializeField] private TMP_Text KillFeedText; // 最近击杀提示文本。
        [SerializeField] private TMP_Text PerformanceText; // 帧率与网络延迟文本。
        [SerializeField] private GameObject MobileControlRoot; // 移动平台操作根节点。
        [SerializeField] private GameObject DesktopHintRoot; // 桌面平台键位提示根节点。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 判断当前是否配置性能诊断文本。
        /// </summary>
        public bool HasPerformanceOutput => PerformanceText != null;

        /// <summary>
        /// 根据运行平台切换操作提示。
        /// </summary>
        /// <param name="mobile">是否使用移动平台操作界面。</param>
        public void SetPlatformMode(bool mobile)
        {
            if (MobileControlRoot != null) MobileControlRoot.SetActive(mobile);
            if (DesktopHintRoot != null) DesktopHintRoot.SetActive(!mobile);
        }

        /// <summary>
        /// 刷新剩余比赛时间，相同最终文本不重复赋值。
        /// </summary>
        /// <param name="remainingMilliseconds">剩余毫秒数。</param>
        public void RefreshTime(int remainingMilliseconds)
        {
            if (RemainingTimeText == null) return;
            int seconds = Mathf.Max(0, remainingMilliseconds / 1000);
            string value = $"{seconds / 60:00}:{seconds % 60:00}";
            if (!string.Equals(RemainingTimeText.text, value, StringComparison.Ordinal)) RemainingTimeText.text = value;
        }

        /// <summary>
        /// 刷新实时排名列表。
        /// </summary>
        /// <param name="items">排名显示条目。</param>
        public void RefreshRanking(IReadOnlyList<BattleRankingItemViewData> items)
        {
            if (RankingText == null) return;
            rankingBuilder.Clear();
            for (int index = 0; index < items.Count; index++)
            {
                BattleRankingItemViewData item = items[index];
                rankingBuilder.Append(item.PlayerName).Append("  ").Append(item.Score).AppendLine();
            }

            string value = rankingBuilder.ToString();
            if (!string.Equals(RankingText.text, value, StringComparison.Ordinal)) RankingText.text = value;
        }

        /// <summary>
        /// 显示最新击杀提示。
        /// </summary>
        /// <param name="message">击杀提示。</param>
        public void ShowKillFeed(string message)
        {
            if (KillFeedText != null) KillFeedText.text = message ?? string.Empty;
        }

        /// <summary>
        /// 仅在提示仍与预期相同时清空击杀提示。
        /// </summary>
        /// <param name="expected">安排清理时的提示内容。</param>
        public void ClearKillFeed(string expected)
        {
            if (KillFeedText != null && string.Equals(KillFeedText.text, expected, StringComparison.Ordinal))
            {
                KillFeedText.text = string.Empty;
            }
        }

        /// <summary>
        /// 刷新性能与网络往返延迟文本。
        /// </summary>
        /// <param name="framesPerSecond">平均帧率。</param>
        /// <param name="hasRtt">是否存在往返延迟样本。</param>
        /// <param name="rttMilliseconds">往返延迟毫秒数。</param>
        public void RefreshPerformance(double framesPerSecond, bool hasRtt, int rttMilliseconds)
        {
            if (PerformanceText == null) return;
            string value = hasRtt
                ? $"FPS: {framesPerSecond:F2}\nRTT: {rttMilliseconds} ms"
                : $"FPS: {framesPerSecond:F2}\nRTT: --";
            if (!string.Equals(PerformanceText.text, value, StringComparison.Ordinal)) PerformanceText.text = value;
        }

        #endregion
    }
}
