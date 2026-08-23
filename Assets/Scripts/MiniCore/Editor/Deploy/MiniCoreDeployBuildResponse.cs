namespace MiniCore.EditorTools.Deploy
{
    /// <summary>
    /// 表示 Unity BatchMode 写给桌面应用的单目标构建结果。
    /// </summary>
    public sealed class MiniCoreDeployBuildResponse
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置目标是否完整成功。
        /// </summary>
        public bool Succeeded { get; set; }

        /// <summary>
        /// 获取或设置结果摘要。
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置成功输出路径。
        /// </summary>
        public string[] Outputs { get; set; } = System.Array.Empty<string>();

        /// <summary>
        /// 获取或设置失败原因。
        /// </summary>
        public string[] Errors { get; set; } = System.Array.Empty<string>();

        #endregion
    }
}
