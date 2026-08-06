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

    /// <summary>
    /// 为可移动浮窗提供边界受限的拖拽行为。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed partial class UIWindowDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        #region UnityProperty Unity 引用属性

        [SerializeField] private RectTransform target; // 实际移动的窗口节点。
        [SerializeField] private RectTransform bounds; // 允许窗口移动的边界节点。

        #endregion

        #region Private 私有成员

        private Vector2 pointerOffset; // 开始拖拽时的局部坐标偏移。

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 记录指针与窗口锚点位置的偏移。
        /// </summary>
        /// <param name="eventData">Unity 指针事件。</param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            RectTransform moving = ResolveTarget();
            RectTransform parent = moving.parent as RectTransform;
            if (parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                pointerOffset = moving.anchoredPosition - local;
                moving.SetAsLastSibling();
            }
        }

        /// <summary>
        /// 按指针位置移动窗口并限制其中心点不离开边界。
        /// </summary>
        /// <param name="eventData">Unity 指针事件。</param>
        public void OnDrag(PointerEventData eventData)
        {
            RectTransform moving = ResolveTarget();
            RectTransform parent = moving.parent as RectTransform;
            if (parent == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                return;
            }

            Vector2 desired = local + pointerOffset;
            RectTransform limit = bounds != null ? bounds : parent;
            Rect limitRect = limit.rect;
            Vector2 half = moving.rect.size * 0.5f;
            desired.x = Mathf.Clamp(desired.x, limitRect.xMin + half.x, limitRect.xMax - half.x);
            desired.y = Mathf.Clamp(desired.y, limitRect.yMin + half.y, limitRect.yMax - half.y);
            moving.anchoredPosition = desired;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 获取编辑器指定或当前节点上的窗口 RectTransform。
        /// </summary>
        /// <returns>实际拖拽目标。</returns>
        private RectTransform ResolveTarget()
        {
            return target != null ? target : (RectTransform)transform;
        }

        #endregion
    }

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
