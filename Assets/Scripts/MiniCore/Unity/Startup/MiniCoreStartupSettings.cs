using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniCore.Unity
{
    /// <summary>
    /// MiniCore 项目启动配置资源。
    /// 编辑器读取该资源生成 HotUpdate 启动代码；运行时不反射或读取此配置，因此不会增加启动热路径开销。
    /// </summary>
    public sealed class MiniCoreStartupSettings : ScriptableObject
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置已发现启动模块的配置列表。
        /// 每项通过组件的程序集限定名稳定关联到对应类型。
        /// </summary>
        public List<MiniCoreStartupModuleSettings> Modules = new List<MiniCoreStartupModuleSettings>();

        /// <summary>
        /// 获取或设置按具体 AppService 实现保存的项目启用状态。
        /// 生成器负责校验同一服务接口最多启用一个实现。
        /// </summary>
        public List<MiniCoreAppServiceSettings> Services = new List<MiniCoreAppServiceSettings>();

        #endregion
    }
}
