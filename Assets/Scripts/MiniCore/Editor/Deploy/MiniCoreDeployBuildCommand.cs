using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace MiniCore.EditorTools.Deploy
{
    /// <summary>
    /// 根据独立桌面应用请求构建一个显式平台和显式启动场景。
    /// </summary>
    public static class MiniCoreDeployBuildCommand
    {
        #region Private 私有成员

        private const string RequestArgument = "-minicoreDeployRequest"; // 构建请求文件参数。
        private const string ResultArgument = "-minicoreDeployResult"; // 构建结果文件参数。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 执行单目标 HybridCLR、YooAsset 和 Unity Player 构建并写出结构化结果。
        /// </summary>
        public static void Execute()
        {
            string resultPath = GetArgument(ResultArgument);
            var response = new MiniCoreDeployBuildResponse();
            try
            {
                MiniCoreDeployBuildRequest request = LoadRequest(GetArgument(RequestArgument));
                ValidateRequest(request);
                string output = BuildSingleTarget(request, request.Targets[0]);
                response.Succeeded = true;
                response.Message = $"构建目标 {request.Targets[0]} 成功。";
                response.Outputs = new[] { output };
            }
            catch (Exception exception)
            {
                response.Succeeded = false;
                response.Message = exception.Message;
                response.Errors = new[] { exception.ToString() };
            }
            finally
            {
                WriteResponse(resultPath, response);
            }

            if (!response.Succeeded)
            {
                throw new InvalidOperationException(response.Message);
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 根据目标切换 Server 子目标、生成热更新包并构建 Player。
        /// </summary>
        /// <param name="request">构建请求。</param>
        /// <param name="targetName">目标名称。</param>
        /// <returns>构建输出路径。</returns>
        private static string BuildSingleTarget(MiniCoreDeployBuildRequest request, string targetName)
        {
            ResolveTarget(targetName, request, out BuildTarget target, out BuildTargetGroup group, out StandaloneBuildSubtarget subtarget, out string scene, out string locationPath);
            string targetRoot = Path.Combine(request.OutputPath, targetName);
            if (Directory.Exists(targetRoot))
            {
                Directory.Delete(targetRoot, true);
            }

            if (!BuildPipeline.IsBuildTargetSupported(group, target))
            {
                throw new InvalidOperationException($"当前 Unity 安装缺少构建模块：{targetName} ({target})。");
            }

            if (!request.ContentOnly && !File.Exists(scene))
            {
                throw new FileNotFoundException($"构建场景不存在：{scene}。", scene);
            }

            if (target == BuildTarget.Android)
            {
                EditorUserBuildSettings.buildAppBundle = request.AndroidAppBundle;
            }

            EditorUserBuildSettings.standaloneBuildSubtarget = subtarget;
            bool completeGeneration = string.Equals(request.Operation, "FirstInstall", StringComparison.OrdinalIgnoreCase)
                || string.Equals(request.Operation, "FullRelease", StringComparison.OrdinalIgnoreCase)
                || string.Equals(request.Operation, "MaintenanceRelease", StringComparison.OrdinalIgnoreCase);
            if (completeGeneration)
            {
                HybridClrYooAssetBuildCommand.GenerateAllAndBuildDefaultPackage();
            }
            else
            {
                HybridClrYooAssetBuildCommand.CompileActiveTargetAndBuildDefaultPackage();
            }

            if (request.ContentOnly)
            {
                return CopyContentOnlyOutput(request, targetName);
            }

            string directory = Directory.Exists(locationPath) ? locationPath : Path.GetDirectoryName(locationPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new BuildPlayerOptions
            {
                scenes = new[] { scene },
                target = target,
                targetGroup = group,
                subtarget = (int)subtarget,
                locationPathName = locationPath,
                options = BuildOptions.CompressWithLz4HC
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Unity Player 构建失败：{targetName}，结果 {report.summary.result}，错误 {report.summary.totalErrors}，警告 {report.summary.totalWarnings}。");
            }

            return report.summary.outputPath;
        }

        /// <summary>
        /// 将当前平台生成的 HotUpdate 与 YooAsset 首包内容复制到不可变目标目录。
        /// </summary>
        /// <param name="request">构建请求。</param>
        /// <param name="targetName">平台目标名称。</param>
        /// <returns>资源内容输出根目录。</returns>
        private static string CopyContentOnlyOutput(MiniCoreDeployBuildRequest request, string targetName)
        {
            string source = Path.GetFullPath("Assets/StreamingAssets/yoo/DefaultPackage");
            if (!Directory.Exists(source))
            {
                throw new DirectoryNotFoundException($"YooAsset 构建未生成首包目录：{source}。");
            }

            string targetRoot = Path.Combine(request.OutputPath, targetName);
            if (Directory.Exists(targetRoot))
            {
                Directory.Delete(targetRoot, true);
            }

            string target = Path.Combine(targetRoot, "StreamingAssets", "yoo", "DefaultPackage");
            CopyDirectory(source, target);
            return targetRoot;
        }

        /// <summary>
        /// 递归复制一个确定的构建目录并保留相对结构。
        /// </summary>
        /// <param name="source">源目录。</param>
        /// <param name="target">目标目录。</param>
        private static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);
            string[] files = Directory.GetFiles(source, "*", SearchOption.TopDirectoryOnly);
            for (int index = 0; index < files.Length; index++)
            {
                File.Copy(files[index], Path.Combine(target, Path.GetFileName(files[index])), true);
            }

            string[] directories = Directory.GetDirectories(source, "*", SearchOption.TopDirectoryOnly);
            for (int index = 0; index < directories.Length; index++)
            {
                CopyDirectory(directories[index], Path.Combine(target, Path.GetFileName(directories[index])));
            }
        }

        /// <summary>
        /// 将工具目标名称解析为 Unity 平台、子目标、场景与固定输出路径。
        /// </summary>
        /// <param name="targetName">工具目标名称。</param>
        /// <param name="request">构建请求。</param>
        /// <param name="target">Unity 构建目标。</param>
        /// <param name="group">Unity 构建目标组。</param>
        /// <param name="subtarget">Standalone 子目标。</param>
        /// <param name="scene">启动场景。</param>
        /// <param name="locationPath">输出路径。</param>
        private static void ResolveTarget(
            string targetName,
            MiniCoreDeployBuildRequest request,
            out BuildTarget target,
            out BuildTargetGroup group,
            out StandaloneBuildSubtarget subtarget,
            out string scene,
            out string locationPath)
        {
            subtarget = StandaloneBuildSubtarget.Player;
            scene = request.ClientScenePath;
            string targetRoot = Path.Combine(request.OutputPath, targetName);
            switch (targetName)
            {
                case "ServerLinuxX64":
                    target = BuildTarget.StandaloneLinux64;
                    group = BuildTargetGroup.Standalone;
                    subtarget = StandaloneBuildSubtarget.Server;
                    scene = request.ServerScenePath;
                    locationPath = Path.Combine(targetRoot, "MiniCoreServer.x86_64");
                    return;
                case "ServerWindowsX64":
                    target = BuildTarget.StandaloneWindows64;
                    group = BuildTargetGroup.Standalone;
                    subtarget = StandaloneBuildSubtarget.Server;
                    scene = request.ServerScenePath;
                    locationPath = Path.Combine(targetRoot, "MiniCoreServer.exe");
                    return;
                case "ClientWindowsX64":
                    target = BuildTarget.StandaloneWindows64;
                    group = BuildTargetGroup.Standalone;
                    locationPath = Path.Combine(targetRoot, "MiniCoreClient.exe");
                    return;
                case "ClientMacOS":
                    target = BuildTarget.StandaloneOSX;
                    group = BuildTargetGroup.Standalone;
                    locationPath = Path.Combine(targetRoot, "MiniCoreClient.app");
                    return;
                case "ClientAndroid":
                    target = BuildTarget.Android;
                    group = BuildTargetGroup.Android;
                    locationPath = Path.Combine(targetRoot, request.AndroidAppBundle ? "MiniCoreClient.aab" : "MiniCoreClient.apk");
                    return;
                case "ClientWebGL":
                    target = BuildTarget.WebGL;
                    group = BuildTargetGroup.WebGL;
                    locationPath = targetRoot;
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetName), targetName, "未知 Unity 构建目标。");
            }
        }

        /// <summary>
        /// 从 JSON 文件加载构建请求。
        /// </summary>
        /// <param name="path">请求文件路径。</param>
        /// <returns>构建请求。</returns>
        private static MiniCoreDeployBuildRequest LoadRequest(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("MiniCore Deploy 构建请求不存在。", path);
            }

            return JsonConvert.DeserializeObject<MiniCoreDeployBuildRequest>(File.ReadAllText(path))
                ?? throw new InvalidDataException("MiniCore Deploy 构建请求不是有效 JSON 对象。");
        }

        /// <summary>
        /// 校验单目标构建请求的版本、路径和场景。
        /// </summary>
        /// <param name="request">构建请求。</param>
        private static void ValidateRequest(MiniCoreDeployBuildRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ReleaseVersion)
                || string.IsNullOrWhiteSpace(request.OutputPath)
                || string.IsNullOrWhiteSpace(request.ClientScenePath)
                || string.IsNullOrWhiteSpace(request.ServerScenePath)
                || request.Targets == null
                || request.Targets.Length != 1)
            {
                throw new InvalidDataException("MiniCore Deploy 每次 Unity 调用必须包含版本、输出、两种场景和唯一构建目标。");
            }

            Directory.CreateDirectory(request.OutputPath);
        }

        /// <summary>
        /// 从 Unity 命令行读取指定参数的后一个值。
        /// </summary>
        /// <param name="name">参数名。</param>
        /// <returns>参数值。</returns>
        private static string GetArgument(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], name, StringComparison.Ordinal))
                {
                    return Path.GetFullPath(arguments[index + 1]);
                }
            }

            throw new ArgumentException($"Unity 命令行缺少参数：{name}。");
        }

        /// <summary>
        /// 原子写出机器可读的构建结果。
        /// </summary>
        /// <param name="path">结果路径。</param>
        /// <param name="response">构建结果。</param>
        private static void WriteResponse(string path, MiniCoreDeployBuildResponse response)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, JsonConvert.SerializeObject(response, Formatting.Indented));
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(temporaryPath, path);
        }

        #endregion
    }
}
