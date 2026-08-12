using UnityEditor;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 保存项目级 Proto 代码生成设置。
    /// </summary>
    [FilePath("ProjectSettings/MiniCoreProtocolSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class MiniCoreProtocolSettings : ScriptableSingleton<MiniCoreProtocolSettings>
    {
        #region Private 私有成员

        private const string DefaultOutputDirectory = "Assets/Scripts/MiniCore/Protocol/Generated"; // 默认项目协议输出目录。
        [UnityEngine.SerializeField] private string projectOutputDirectory = DefaultOutputDirectory; // 当前项目协议输出目录。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 获取项目业务 Proto 的统一 C# 输出目录。
        /// </summary>
        internal string ProjectOutputDirectory => string.IsNullOrWhiteSpace(projectOutputDirectory)
            ? DefaultOutputDirectory
            : projectOutputDirectory.Replace('\\', '/').TrimEnd('/');

        /// <summary>
        /// 更新并持久化项目业务 Proto 的统一输出目录。
        /// </summary>
        /// <param name="value">Assets 下的目标目录。</param>
        internal void SetProjectOutputDirectory(string value)
        {
            projectOutputDirectory = value.Replace('\\', '/').TrimEnd('/');
            Save(true);
        }

        #endregion
    }
}
