using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 在 WebGL 构建前校验浏览器适配层、原生插件隔离和无线程约束。
    /// </summary>
    public sealed class BrowserBuildValidator : IPreprocessBuildWithReport
    {
        #region Private 私有成员

        private const string BrowserAssemblyPath = "Assets/Scripts/MiniCore/Platform/Browser/MiniCore.Platform.Browser.asmdef";
        private const string BrowserSourceDirectory = "Assets/Scripts/MiniCore/Platform/Browser";
        private const string WebSocketLibraryPath = "Assets/Plugins/MiniCore/Browser/MiniCoreWebSocket.jslib";
        private const string StorageLibraryPath = "Assets/Plugins/MiniCore/Browser/MiniCoreStorage.jslib";
        private const string NativeWebSocketPluginMetaPath = "Assets/Plugins/MiniCore/WebSocketSharp/websocket-sharp.dll.meta";
        private static readonly string[] ForbiddenBrowserTokens =
        {
            "System.Net.Sockets",
            "new Thread(",
            "new System.Threading.Thread(",
            "ThreadPool.",
            "Task.Run(",
            "WaitHandle",
            "System.Threading.Timer",
            "System.Timers.Timer"
        }; // 浏览器专属代码不得直接使用的线程或套接字 API。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 在协议与 HybridCLR 完整性校验之后检查浏览器目标约束。
        /// </summary>
        public int callbackOrder => -90;

        /// <summary>
        /// WebGL 构建缺少平台适配层或包含禁用 API 时阻止继续构建。
        /// </summary>
        /// <param name="report">当前 Unity 构建报告。</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL)
            {
                return;
            }

            if (!Validate(out string error))
            {
                throw new BuildFailedException(error);
            }
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 校验浏览器程序集、JavaScript 适配绑定、插件隔离和专属源码约束。
        /// </summary>
        /// <param name="error">校验失败原因。</param>
        /// <returns>浏览器构建边界完整时返回 true。</returns>
        internal static bool Validate(out string error)
        {
            if (!File.Exists(BrowserAssemblyPath)
                || !File.Exists(WebSocketLibraryPath)
                || !File.Exists(StorageLibraryPath))
            {
                error = "WebGL 构建缺少 MiniCore 浏览器程序集、WebSocket 客户端适配器或 IndexedDB 存储后端。";
                return false;
            }

            string assemblyDefinition = File.ReadAllText(BrowserAssemblyPath);
            if (assemblyDefinition.IndexOf("\"includePlatforms\": [\"WebGL\"]", StringComparison.Ordinal) < 0)
            {
                error = $"浏览器程序集必须只包含 WebGL 平台：{BrowserAssemblyPath}";
                return false;
            }

            if (!File.Exists(NativeWebSocketPluginMetaPath))
            {
                error = $"缺少原生 WebSocket 插件导入配置：{NativeWebSocketPluginMetaPath}";
                return false;
            }

            string nativePluginMeta = File.ReadAllText(NativeWebSocketPluginMetaPath);
            if (nativePluginMeta.IndexOf("WebGL: WebGL", StringComparison.Ordinal) < 0
                || nativePluginMeta.IndexOf("enabled: 0", nativePluginMeta.IndexOf("WebGL: WebGL", StringComparison.Ordinal), StringComparison.Ordinal) < 0)
            {
                error = "websocket-sharp 必须在 WebGL 平台禁用，由浏览器 JavaScript 客户端适配器替代。";
                return false;
            }

            string[] sourceFiles = Directory.GetFiles(BrowserSourceDirectory, "*.cs", SearchOption.AllDirectories);
            for (int fileIndex = 0; fileIndex < sourceFiles.Length; fileIndex++)
            {
                string source = File.ReadAllText(sourceFiles[fileIndex]);
                if (source.IndexOf("#if UNITY_WEBGL && !UNITY_EDITOR", StringComparison.Ordinal) < 0)
                {
                    error = $"浏览器专属源码缺少 WebGL 编译边界：{sourceFiles[fileIndex]}";
                    return false;
                }

                for (int tokenIndex = 0; tokenIndex < ForbiddenBrowserTokens.Length; tokenIndex++)
                {
                    if (source.IndexOf(ForbiddenBrowserTokens[tokenIndex], StringComparison.Ordinal) >= 0)
                    {
                        error = $"浏览器专属源码引用了禁用 API {ForbiddenBrowserTokens[tokenIndex]}：{sourceFiles[fileIndex]}";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        #endregion
    }
}
