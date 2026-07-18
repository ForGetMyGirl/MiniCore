using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniCore.Model
{
    /// <summary>
    /// UI画布层级
    /// </summary>
    public enum UICanvasLayer
    {
        /// <summary>
        /// 背景层
        /// </summary>
        Background,
        /// <summary>
        /// 普通层
        /// </summary>
        Normal,
        /// <summary>
        /// 弹窗层
        /// </summary>
        Popup,
        /// <summary>
        /// 顶层
        /// </summary>
        Top,
        /// <summary>
        /// 提示层
        /// </summary>
        Tips,
        /// <summary>
        /// 系统提示层
        /// </summary>
        System,
        /// <summary>
        /// 引导层
        /// </summary>
        Guide
    }
}
