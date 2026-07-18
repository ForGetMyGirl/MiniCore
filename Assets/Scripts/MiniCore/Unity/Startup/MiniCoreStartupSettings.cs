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

        #endregion
    }

    /// <summary>
    /// 单个启动模块在 Client 与 Server 目标上的启用状态及初始化参数。
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
        /// 获取或设置是否为客户端生成该模块的 Pin 代码。
        /// </summary>
        public bool EnableClient;

        /// <summary>
        /// 获取或设置是否为服务端生成该模块的 Pin 代码。
        /// </summary>
        public bool EnableServer;

        /// <summary>
        /// 获取或设置组件初始化参数的字段或属性覆盖值。
        /// 未覆盖的成员保持 Args 类型在代码中声明的默认值。
        /// </summary>
        public List<MiniCoreStartupArgumentSettings> Arguments = new List<MiniCoreStartupArgumentSettings>();

        #endregion
    }

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
