using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiniCore.UI
{
    /// <summary>
    /// 当前屏幕和安全区域使用的无分配布局指标。
    /// </summary>
    public readonly struct UIResolutionMetrics
    {
        #region Public 公共成员

        /// <summary>
        /// 获取屏幕像素尺寸。
        /// </summary>
        public Vector2 PixelSize { get; }

        /// <summary>
        /// 获取安全区域像素矩形。
        /// </summary>
        public Rect SafeArea { get; }

        /// <summary>
        /// 获取当前屏幕宽高比。
        /// </summary>
        public float AspectRatio { get; }

        /// <summary>
        /// 判断当前是否为竖屏。
        /// </summary>
        public bool Portrait { get; }

        /// <summary>
        /// 获取当前响应式断点名称。
        /// </summary>
        public string Breakpoint { get; }

        /// <summary>
        /// 创建一份不可变分辨率指标。
        /// </summary>
        /// <param name="pixelSize">屏幕像素尺寸。</param>
        /// <param name="safeArea">安全区域。</param>
        /// <param name="aspectRatio">宽高比。</param>
        /// <param name="portrait">是否竖屏。</param>
        /// <param name="breakpoint">响应式断点。</param>
        public UIResolutionMetrics(Vector2 pixelSize, Rect safeArea, float aspectRatio, bool portrait, string breakpoint)
        {
            PixelSize = pixelSize;
            SafeArea = safeArea;
            AspectRatio = aspectRatio;
            Portrait = portrait;
            Breakpoint = breakpoint;
        }

        #endregion
    }
}
