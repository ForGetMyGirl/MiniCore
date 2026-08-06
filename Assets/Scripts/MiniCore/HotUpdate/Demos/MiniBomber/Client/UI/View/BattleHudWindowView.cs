using TMPro;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>战斗时间、排行、击杀提示和平台操作界面的被动 View。</summary>
    public sealed class BattleHudWindowView : MiniBomberWindowViewBase
    {
        #region UnityProperty Unity 引用属性

        /// <summary>剩余时间文本。</summary>
        public TMP_Text RemainingTimeText;
        /// <summary>实时排行文本。</summary>
        public TMP_Text RankingText;
        /// <summary>最近击杀提示文本。</summary>
        public TMP_Text KillFeedText;
        /// <summary>客户端帧率与网络往返延迟文本。</summary>
        public TMP_Text PerformanceText;
        /// <summary>Android 摇杆和炸弹按钮根节点。</summary>
        public GameObject MobileControlRoot;
        /// <summary>Windows 键位提示根节点。</summary>
        public GameObject DesktopHintRoot;

        #endregion
    }
}
