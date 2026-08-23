namespace MiniCore.EditorTools.Deploy
{
    /// <summary>
    /// 表示独立桌面应用传入 Unity BatchMode 的单目标构建请求。
    /// </summary>
    public sealed class MiniCoreDeployBuildRequest
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置统一发布版本。
        /// </summary>
        public string ReleaseVersion { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置发布操作名称。
        /// </summary>
        public string Operation { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置该版本的输出根目录。
        /// </summary>
        public string OutputPath { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置客户端启动场景。
        /// </summary>
        public string ClientScenePath { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置 Dedicated Server 启动场景。
        /// </summary>
        public string ServerScenePath { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置本轮唯一构建目标。
        /// </summary>
        public string[] Targets { get; set; } = System.Array.Empty<string>();

        /// <summary>
        /// 获取或设置 Android 是否生成 AAB；false 时生成 APK。
        /// </summary>
        public bool AndroidAppBundle { get; set; } = true;

        /// <summary>
        /// 获取或设置是否只输出 HotUpdate 与 YooAsset 内容而不构建 Player。
        /// </summary>
        public bool ContentOnly { get; set; }

        #endregion
    }
}
