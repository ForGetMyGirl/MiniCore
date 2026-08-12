using TMPro;
using UnityEngine.UI;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 服务器最终比赛排名弹窗的被动 View。
    /// </summary>
    public sealed class MatchResultWindowView : MiniBomberWindowViewBase
    {
        #region UnityProperty Unity 引用属性

        /// <summary>
        /// 名次、玩家名、得分、击杀和死亡列表。
        /// </summary>
        public TMP_Text ResultsText;
        /// <summary>
        /// 返回房间倒计时文本。
        /// </summary>
        public TMP_Text ReturnCountdownText;
        /// <summary>
        /// 关闭结果弹窗按钮。
        /// </summary>
        public Button CloseButton;

        #endregion
    }
}
