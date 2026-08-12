using System;
using System.IO;
using MiniCore.Model;
using UnityEngine;

namespace MiniCore.Service
{
    /// <summary>
    /// 本地持久化路径服务的启动参数。
    /// 默认使用与旧版本兼容的 MiniCore 相对目录；项目可在启动配置中覆盖它。
    /// </summary>
    public sealed class StoragePathServiceInitArgs : ComponentInitArgs
    {
        /// <summary>
        /// 获取或设置相对于 Application.persistentDataPath 的项目数据目录。
        /// 默认值为 MiniCore；不允许为空、绝对路径、当前目录或上级目录片段。
        /// </summary>
        public string RelativePath { get; set; } = "MiniCore";
    }
}
