using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniCore.Unity
{

    /// <summary>
    /// 单个 AppService 实现在项目中的启用状态及启动参数。
    /// </summary>
    [Serializable]
    public sealed class MiniCoreAppServiceSettings
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置具体 AppService 实现程序集限定名。
        /// </summary>
        public string AssemblyQualifiedTypeName;

        /// <summary>
        /// 获取或设置是否在项目启动时注册当前服务实现。
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// 获取或设置当前服务实现的初始化参数覆盖值。
        /// </summary>
        public List<MiniCoreStartupArgumentSettings> Arguments = new List<MiniCoreStartupArgumentSettings>();

        #endregion
    }
}
