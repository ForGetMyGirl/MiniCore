using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniCore.Unity
{

    /// <summary>
    /// 单个启动模块在项目中的启用状态及初始化参数。
    /// </summary>
    [Serializable]
    public sealed class MiniCoreStartupModuleSettings
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置组件的程序集限定类型名称。
        /// </summary>
        public string AssemblyQualifiedTypeName;

        /// <summary>
        /// 获取或设置是否在项目启动时生成该模块的 Pin 代码。
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// 获取或设置组件初始化参数的字段或属性覆盖值。
        /// 未覆盖的成员保持 Args 类型在代码中声明的默认值。
        /// </summary>
        public List<MiniCoreStartupArgumentSettings> Arguments = new List<MiniCoreStartupArgumentSettings>();

        #endregion
    }
}
