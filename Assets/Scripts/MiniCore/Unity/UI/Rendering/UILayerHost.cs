using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiniCore.UI
{

    /// <summary>
    /// ApplicationUIRoot 中一个固定渲染空间和排序的 Canvas 层。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(Canvas))]
    public sealed partial class UILayerHost : MonoBehaviour
    {
        #region UnityProperty Unity 引用属性

        [SerializeField] private Canvas canvas; // 当前层 Canvas。

        #endregion

        #region Private 私有成员

        [SerializeField] private UIRenderSpace renderSpace; // 当前渲染空间。
        [SerializeField] private UILayer layer; // 当前逻辑层。
        [SerializeField] private int sortingOrder; // 当前 Canvas 排序值。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取当前渲染空间。
        /// </summary>
        public UIRenderSpace RenderSpace => renderSpace;

        /// <summary>
        /// 获取当前逻辑层。
        /// </summary>
        public UILayer Layer => layer;

        /// <summary>
        /// 获取当前排序值。
        /// </summary>
        public int SortingOrder => sortingOrder;

        /// <summary>
        /// 获取窗口直接挂载的当前层 RectTransform。
        /// </summary>
        public RectTransform Root => transform as RectTransform;

        /// <summary>
        /// 为编辑器生成器配置渲染空间、逻辑层和排序。
        /// </summary>
        /// <param name="space">渲染空间。</param>
        /// <param name="layerValue">逻辑层。</param>
        /// <param name="order">Canvas 排序值。</param>
        public void Configure(UIRenderSpace space, UILayer layerValue, int order)
        {
            renderSpace = space;
            layer = layerValue;
            sortingOrder = order;
            ApplyCanvasSettings();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 在 Unity 校验阶段同步当前层排序配置。
        /// </summary>
        private void OnValidate()
        {
            ApplyCanvasSettings();
        }

        /// <summary>
        /// 惰性获取 Canvas 并应用嵌套层排序。
        /// </summary>
        private void ApplyCanvasSettings()
        {
            canvas ??= GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
        }

        #endregion
    }
}
