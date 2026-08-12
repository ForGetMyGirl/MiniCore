using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 大厅房间列表界面的被动 View。
    /// </summary>
    public sealed class LobbyWindowView : MiniBomberWindowViewBase
    {
        #region UnityProperty Unity 引用属性

        /// <summary>
        /// 当前玩家姓名。
        /// </summary>
        public TMP_Text PlayerNameText;
        /// <summary>
        /// 服务器在线人数。
        /// </summary>
        public TMP_Text OnlineCountText;
        /// <summary>
        /// 房间列表文本；首版无需额外行组件即可验证同步。
        /// </summary>
        public TMP_Text RoomListText;
        /// <summary>
        /// 房间身份输入。
        /// </summary>
        public TMP_InputField JoinRoomIdInput;
        /// <summary>
        /// 刷新按钮。
        /// </summary>
        public Button RefreshButton;
        /// <summary>
        /// 创建房间按钮。
        /// </summary>
        public Button CreateButton;
        /// <summary>
        /// 加入房间按钮。
        /// </summary>
        public Button JoinButton;
        /// <summary>
        /// 注销按钮。
        /// </summary>
        public Button LogoutButton;
        /// <summary>
        /// 响应提示。
        /// </summary>
        public TMP_Text PromptText;

        #endregion
    }
}
