using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiniCore.UI
{
    /// <summary>
    /// 标记经过框架校验的窗口内部特殊 Canvas。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed partial class UISubCanvas : MonoBehaviour
    {
        #region Private 私有成员

        [SerializeField] private int relativeOrder; // 相对窗口所属层 Canvas 的排序偏移。
        [SerializeField] private bool receivesRaycasts = true; // 是否允许当前子 Canvas 接收射线。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取相对排序偏移。
        /// </summary>
        public int RelativeOrder => relativeOrder;

        /// <summary>
        /// 获取当前子 Canvas 是否接收射线。
        /// </summary>
        public bool ReceivesRaycasts => receivesRaycasts;

        #endregion
    }
}
