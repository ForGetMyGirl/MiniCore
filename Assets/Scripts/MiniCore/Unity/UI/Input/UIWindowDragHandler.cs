using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiniCore.UI
{

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
}
