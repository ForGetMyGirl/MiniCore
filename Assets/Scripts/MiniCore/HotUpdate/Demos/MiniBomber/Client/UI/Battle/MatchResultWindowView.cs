using System;
using System.Text;
using MiniCore.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 比赛结果窗口 View，负责最终排名与返回提示表现。
    /// </summary>
    public sealed class MatchResultWindowView : MiniBomberWindowViewBase
    {
        #region Private 私有成员

        private readonly StringBuilder resultBuilder = new StringBuilder(256); // 成绩列表显示缓存。

        #endregion

        #region UnityProperty Unity 引用属性

        [SerializeField] private TMP_Text ResultsText; // 最终成绩列表。
        [SerializeField] private TMP_Text ReturnCountdownText; // 返回房间倒计时。
        [SerializeField] private Button CloseButton; // 关闭按钮。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 绑定关闭比赛结果窗口意图。
        /// </summary>
        /// <param name="bindings">窗口周期绑定集合。</param>
        /// <param name="close">关闭意图。</param>
        public void BindActions(UIBindingSet bindings, Action close)
        {
            if (close != null) bindings.Add(CloseButton, close.Invoke);
        }

        /// <summary>
        /// 使用比赛结果专用显示数据刷新界面。
        /// </summary>
        /// <param name="data">比赛结果显示数据。</param>
        public void Refresh(MatchResultWindowViewData data)
        {
            resultBuilder.Clear();
            if (data != null)
            {
                for (int index = 0; index < data.Entries.Count; index++)
                {
                    MatchResultEntryViewData item = data.Entries[index];
                    resultBuilder.Append(item.Rank).Append(". ").Append(item.PlayerName)
                        .Append("  得分:").Append(item.Score)
                        .Append("  击杀:").Append(item.Kills)
                        .Append("  死亡:").Append(item.Deaths).AppendLine();
                }
            }

            if (ResultsText != null) ResultsText.text = resultBuilder.ToString();
            if (ReturnCountdownText != null)
            {
                ReturnCountdownText.text = data != null ? $"{data.ReturnCountdownSeconds} 秒后返回房间" : string.Empty;
            }
        }

        #endregion
    }
}
