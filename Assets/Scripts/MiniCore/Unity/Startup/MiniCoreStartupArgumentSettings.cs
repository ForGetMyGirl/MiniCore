using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniCore.Unity
{

    /// <summary>
    /// 启动初始化参数中单个可编辑成员的保存值。
    /// 值以字符串保存，由编辑器按成员类型校验并生成对应的 C# 字面量。
    /// </summary>
    [Serializable]
    public sealed class MiniCoreStartupArgumentSettings
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置 Args 成员名称。
        /// </summary>
        public string MemberName;

        /// <summary>
        /// 获取或设置是否保留 Args 类中的代码默认值。
        /// 启用时生成器不会为该成员生成初始化器赋值。
        /// </summary>
        public bool UseCodeDefault = true;

        /// <summary>
        /// 获取或设置编辑器保存的文本值。
        /// </summary>
        public string Value;

        #endregion
    }
}
