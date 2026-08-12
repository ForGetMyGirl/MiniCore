using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniCore.UI
{

    /// <summary>
    /// 响应式断点与布局节点的序列化映射。
    /// </summary>
    [Serializable]
    public struct UIResponsiveVariant
    {
        #region Public 公共成员

        /// <summary>
        /// Profile 中的断点名称。
        /// </summary>
        public string Breakpoint;

        /// <summary>
        /// 命中断点时启用的布局节点。
        /// </summary>
        public GameObject Root;

        #endregion
    }
}
