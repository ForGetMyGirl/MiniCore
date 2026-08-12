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
    /// 比赛成绩 Presenter。
    /// </summary>
    public sealed class MatchResultWindowPresenter : AUIWindowPresenter<MatchResultWindowView>
    {
        #region Private 私有成员

        private readonly StringBuilder builder = new StringBuilder(256); // 成绩格式化缓存。
        private BattleClientComponent battle; // 战斗状态组件。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 原样渲染服务器最终排名并绑定关闭按钮。
        /// </summary>
        protected override void OnBind()
        {
            battle = Global.Get<BattleClientComponent>(this);
            Bindings.Add(View.CloseButton, Close);
            MiniBomberMatchResultNotice result = battle.Result;
            builder.Clear();
            if (result != null)
            {
                for (int index = 0; index < result.Results.Count; index++)
                {
                    MiniBomberMatchResultEntryDto item = result.Results[index];
                    builder.Append(item.Rank).Append(". ").Append(item.PlayerName)
                        .Append("  得分:").Append(item.Score)
                        .Append("  击杀:").Append(item.Kills)
                        .Append("  死亡:").Append(item.Deaths).AppendLine();
                }

                View.ReturnCountdownText.text = $"{result.ReturnToRoomMilliseconds / 1000} 秒后返回房间";
            }

            View.ResultsText.text = builder.ToString();
        }

        /// <summary>
        /// 清空战斗引用。
        /// </summary>
        protected override void OnDispose()
        {
            battle = null;
            builder.Clear();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 关闭成绩弹窗。
        /// </summary>
        private void Close()
        {
            Context.Service.CloseAsync(Context.Handle).Forget();
        }

        #endregion
    }
}
