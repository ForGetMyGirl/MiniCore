using TMPro;
using UnityEngine.UI;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 创建房间弹窗的被动 View。
    /// </summary>
    public sealed class CreateRoomPopupView : MiniBomberWindowViewBase
    {
        #region UnityProperty Unity 引用属性

        /// <summary>
        /// 房间名输入。
        /// </summary>
        public TMP_InputField RoomNameInput;
        /// <summary>
        /// 二、五、十分钟下拉框。
        /// </summary>
        public TMP_Dropdown DurationDropdown;
        /// <summary>
        /// 创建按钮。
        /// </summary>
        public Button SubmitButton;
        /// <summary>
        /// 取消按钮。
        /// </summary>
        public Button CancelButton;
        /// <summary>
        /// 响应提示。
        /// </summary>
        public TMP_Text PromptText;

        #endregion
    }
}
