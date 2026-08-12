using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiniCore.UI
{

    /// <summary>
    /// 在软键盘显示时仅移动指定内容，不改变安全区域定义。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class UIKeyboardAvoidance : MonoBehaviour
    {
        #region UnityProperty Unity 引用属性

        [SerializeField] private RectTransform contentRoot; // 需要避让键盘的内容节点。
        [SerializeField, Min(0f)] private float padding = 24f; // 键盘上方额外设计坐标留白。

        #endregion

        #region Private 私有成员

        private Vector2 originalPosition; // 未显示键盘时的锚点位置。
        private bool captured; // 是否已记录原始位置。
        private float lastKeyboardHeight = -1f; // 上次已应用的键盘像素高度。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 根据平台提供的键盘可见区域刷新内容偏移。
        /// </summary>
        public void Refresh()
        {
            if (contentRoot == null)
            {
                return;
            }

            if (!captured)
            {
                originalPosition = contentRoot.anchoredPosition;
                captured = true;
            }

            Rect keyboard = TouchScreenKeyboard.area;
            if (Mathf.Approximately(lastKeyboardHeight, keyboard.height))
            {
                return;
            }

            lastKeyboardHeight = keyboard.height;
            float scale = Mathf.Max(0.0001f, contentRoot.lossyScale.y);
            float offset = keyboard.height > 0f ? keyboard.height / scale + padding : 0f;
            contentRoot.anchoredPosition = originalPosition + Vector2.up * offset;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 键盘可见区域变化由平台轮询提供，未变化时不触发布局重建。
        /// </summary>
        private void Update()
        {
            Refresh();
        }

        /// <summary>
        /// 组件失活时恢复内容原始位置。
        /// </summary>
        private void OnDisable()
        {
            if (contentRoot != null && captured)
            {
                contentRoot.anchoredPosition = originalPosition;
            }

            lastKeyboardHeight = -1f;
        }

        #endregion
    }
}
