using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniCore.UI
{

    /// <summary>
    /// 在锚点布局之后限制内容的设计坐标尺寸。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed partial class UIContentConstraint : MonoBehaviour
    {
        #region UnityProperty Unity 引用属性

        [SerializeField] private Vector2 minimumSize; // 允许的最小设计尺寸。
        [SerializeField] private Vector2 maximumSize; // 允许的最大设计尺寸，零表示不限制。

        #endregion

        #region Private 私有成员

        private RectTransform rectTransform; // 当前布局节点。
        private Vector2 lastParentSize = new Vector2(-1f, -1f); // 上次父节点尺寸。

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 父节点尺寸改变后重新应用限制。
        /// </summary>
        private void OnRectTransformDimensionsChange()
        {
            RectTransform current = rectTransform ??= GetComponent<RectTransform>();
            RectTransform parent = current.parent as RectTransform;
            if (parent == null || parent.rect.size == lastParentSize)
            {
                return;
            }

            lastParentSize = parent.rect.size;
            Vector2 size = lastParentSize;
            size.x = Mathf.Max(minimumSize.x, maximumSize.x > 0f ? Mathf.Min(size.x, maximumSize.x) : size.x);
            size.y = Mathf.Max(minimumSize.y, maximumSize.y > 0f ? Mathf.Min(size.y, maximumSize.y) : size.y);
            current.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            current.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        }

        #endregion
    }
}
