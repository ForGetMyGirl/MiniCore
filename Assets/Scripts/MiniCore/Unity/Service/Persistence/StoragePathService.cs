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

    /// <summary>
    /// 统一管理本地存档与运行数据的根目录。
    /// 项目可在启动配置中指定 persistentDataPath 下的相对目录，避免多个产品共享固定框架目录名称。
    /// </summary>
    [AppService("本地存储路径", typeof(IStoragePathService), Description = "在 persistentDataPath 下为存档和本地运行数据提供开发者可配置的相对根目录。", InitArgsType = typeof(StoragePathServiceInitArgs))]
    public sealed class StoragePathService : AAppService, IStoragePathService
    {
        #region Private 私有成员

        private string rootPath; // 当前项目本地持久化数据根目录。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取当前项目解析后的本地持久化根目录。
        /// </summary>
        public string RootPath => rootPath;

        /// <summary>
        /// 获取并确保指定用途的一级子目录存在。
        /// </summary>
        /// <param name="directoryName">不包含路径分隔符的用途目录名称。</param>
        /// <returns>已创建或已存在的子目录绝对路径。</returns>
        public string GetDirectory(string directoryName)
        {
            if (string.IsNullOrWhiteSpace(directoryName) || directoryName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || directoryName.IndexOf(Path.DirectorySeparatorChar) >= 0 || directoryName.IndexOf(Path.AltDirectorySeparatorChar) >= 0 || directoryName == "." || directoryName == "..")
            {
                throw new ArgumentException("本地存储子目录名称无效。", nameof(directoryName));
            }

            string directoryPath = Path.Combine(rootPath, directoryName);
            Directory.CreateDirectory(directoryPath);
            return directoryPath;
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 使用当前产品默认的持久化目录初始化服务。
        /// </summary>
        public override void Awake()
        {
            Initialize("MiniCore");
        }

        /// <summary>
        /// 使用启动配置指定的根目录初始化服务。
        /// </summary>
        /// <param name="args">本地存储路径服务启动参数。</param>
        public override void Awake(ComponentInitArgs args)
        {
            if (!(args is StoragePathServiceInitArgs storageArgs))
            {
                throw new ArgumentException("本地存储路径服务必须使用 StoragePathServiceInitArgs 初始化。", nameof(args));
            }

            Initialize(storageArgs.RelativePath);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 解析并创建当前服务的持久化根目录。
        /// </summary>
        /// <param name="relativePath">启动配置中相对于 persistentDataPath 的项目目录。</param>
        private void Initialize(string relativePath)
        {
            string persistentRoot = Path.GetFullPath(Application.persistentDataPath);
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException("本地存储相对路径不能为空。", nameof(relativePath));
            }

            string configuredPath = relativePath.Trim();
            if (Path.IsPathRooted(configuredPath))
            {
                throw new ArgumentException("本地存储相对路径不能使用绝对路径。", nameof(relativePath));
            }

            string[] segments = configuredPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                throw new ArgumentException("本地存储相对路径不能为空白目录。", nameof(relativePath));
            }

            foreach (string segment in segments)
            {
                if (segment == "." || segment == ".." || segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    throw new ArgumentException("本地存储相对路径包含无效目录片段。", nameof(relativePath));
                }
            }

            rootPath = Path.GetFullPath(Path.Combine(persistentRoot, Path.Combine(segments)));
            string persistentPrefix = persistentRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? persistentRoot
                : persistentRoot + Path.DirectorySeparatorChar;
            if (!rootPath.StartsWith(persistentPrefix, StringComparison.Ordinal))
            {
                throw new ArgumentException("本地存储相对路径不能离开 persistentDataPath。", nameof(relativePath));
            }

            Directory.CreateDirectory(rootPath);
        }

        #endregion
    }
}
