using System.Collections.Generic;

namespace MiniCore.Bootstrap
{
    /// <summary>
    /// 由 HybridCLR 与 YooAsset 发布准备流程生成的 AOT 元数据地址表。
    /// </summary>
    public static class HybridClrAotMetadata
    {
        #region Public 公共成员

        /// <summary>
        /// 包含最终启动入口的程序集名称。
        /// </summary>
        public const string StartupAssemblyName = "MiniCore.HotUpdate.Client";

        /// <summary>
        /// Bootstrap 反射调用的启动类型完整名称。
        /// </summary>
        public const string StartupTypeName = "MiniCore.HotUpdate.MiniCoreStartup";

        /// <summary>
        /// Bootstrap 反射调用的启动静态方法名称。
        /// </summary>
        public const string StartupMethodName = "StartAsync";

        /// <summary>
        /// YooAsset 中按依赖顺序加载的热更新程序集独立地址。
        /// </summary>
        public static IReadOnlyList<string> HotUpdateAssemblyAddresses => _hotUpdateAssemblyAddresses;

        /// <summary>
        /// 当前构建目标需要在加载热更新程序集前补充的 AOT 元数据地址。
        /// </summary>
        public static IReadOnlyList<string> AotMetadataAddresses => _aotMetadataAddresses;

        #endregion

        #region Private 私有成员

        private static readonly string[] _hotUpdateAssemblyAddresses =
        {
            "MiniCore.Protocol.Common.dll",
            "MiniCore.Protocol.Outer.dll",
            "MiniCore.HotUpdate.Shared.dll",
            "MiniCore.HotUpdate.Client.dll",
        };

        private static readonly string[] _aotMetadataAddresses =
        {
            "Google.Protobuf.dll",
            "MiniCore.Network.dll",
            "MiniCore.Runtime.dll",
            "MiniCore.Serialization.dll",
            "MiniCore.Unity.dll",
            "Newtonsoft.Json.dll",
            "System.Core.dll",
            "Unity.InputSystem.dll",
            "UnityEngine.CoreModule.dll",
            "mscorlib.dll",
        };

        #endregion
    }
}
