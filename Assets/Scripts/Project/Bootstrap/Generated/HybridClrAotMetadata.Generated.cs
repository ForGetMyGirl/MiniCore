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
        /// YooAsset 中热更新程序集的固定加载地址。
        /// </summary>
        public const string HotUpdateDllAddress = "HotUpdate";

        /// <summary>
        /// 当前构建目标需要在加载热更新程序集前补充的 AOT 元数据地址。
        /// </summary>
        public static IReadOnlyList<string> AotMetadataAddresses => _aotMetadataAddresses;

        #endregion

        #region Private 私有成员

        private static readonly string[] _aotMetadataAddresses =
        {
            "Google.Protobuf.dll",
            "MiniCore.Network.dll",
            "MiniCore.Runtime.dll",
            "MiniCore.Unity.dll",
            "Newtonsoft.Json.dll",
            "System.Core.dll",
            "UnityEngine.CoreModule.dll",
            "YooAsset.dll",
            "mscorlib.dll",
        };

        #endregion
    }
}
