using TMPro;
using UnityEngine.UI;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>房间成员和房主设置界面的被动 View。</summary>
    public sealed class RoomWindowView : MiniBomberWindowViewBase
    {
        #region UnityProperty Unity 引用属性

        /// <summary>房间身份和名称。</summary>
        public TMP_Text RoomTitleText;
        /// <summary>成员状态列表文本。</summary>
        public TMP_Text MemberListText;
        /// <summary>房间名输入，仅房主可编辑。</summary>
        public TMP_InputField RoomNameInput;
        /// <summary>局时长下拉框，仅房主可编辑。</summary>
        public TMP_Dropdown DurationDropdown;
        /// <summary>应用房间设置按钮。</summary>
        public Button ApplySettingsButton;
        /// <summary>准备或取消准备按钮。</summary>
        public Button ReadyButton;
        /// <summary>房主开始比赛按钮。</summary>
        public Button StartButton;
        /// <summary>离开房间按钮。</summary>
        public Button LeaveButton;
        /// <summary>响应提示。</summary>
        public TMP_Text PromptText;

        #endregion
    }
}
