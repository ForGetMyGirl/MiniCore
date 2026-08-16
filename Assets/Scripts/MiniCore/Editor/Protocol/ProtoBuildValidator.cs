using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 校验框架内部与项目业务 Proto 的生成产物、角色和注册代码。
    /// </summary>
    public static class ProtoBuildValidator
    {
        #region Private 私有成员

        private const string ProtoRoot = "Proto";
        private const string ClientSettingsGeneratedPath = "Assets/Scripts/MiniCore/Unity/Service/Persistence/Generated/ClientSettings.cs";
        private const string ControlOutputDirectory = "Assets/Scripts/MiniCore/Protocol/Control/Generated";
        private static readonly Regex MessageRegex = new Regex(@"//\[(INormalMessage|IRpcRequest|IRpcResponse)\]\s*\r?\n\s*message\s+(\w+)\s*\{(?<body>.*?)\}", RegexOptions.Singleline | RegexOptions.Compiled); // 网络角色注解匹配器。
        private static readonly Regex CodeFieldRegex = new Regex(@"\bint32\s+code\s*=\s*1\s*;", RegexOptions.Compiled); // RPC Code 固定字段匹配器。
        private static readonly Regex MsgFieldRegex = new Regex(@"\bstring\s+msg\s*=\s*2\s*;", RegexOptions.Compiled); // RPC Msg 固定字段匹配器。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 校验所有 Proto 及当前配置输出目录中的生成文件。
        /// </summary>
        /// <param name="error">校验失败原因。</param>
        /// <returns>全部生成文件有效时返回 true。</returns>
        public static bool Validate(out string error)
        {
            try
            {
                if (!File.Exists(GetFullPath(ClientSettingsGeneratedPath)))
                {
                    error = "缺少框架内部 ClientSettings PB 生成文件，请执行 MiniCore/Protocol/Generate All。";
                    return false;
                }

                string businessOutputDirectory = GetFullPath(MiniCoreProtocolSettings.instance.ProjectOutputDirectory);
                string controlOutputDirectory = GetFullPath(ControlOutputDirectory);
                if (!Directory.Exists(businessOutputDirectory) || !Directory.Exists(controlOutputDirectory))
                {
                    error = "控制面或业务 Proto 输出目录不存在，请先生成协议。";
                    return false;
                }

                string clientRegistrationPath = Path.Combine(businessOutputDirectory, "Outer", "BusinessClientProtocolRegistration.g.cs");
                string serverRegistrationPath = Path.Combine(businessOutputDirectory, "Inner", "BusinessServerProtocolRegistration.g.cs");
                if (!File.Exists(clientRegistrationPath) || !File.Exists(serverRegistrationPath))
                {
                    error = "缺少业务客户端或服务端统一协议注册入口，请重新生成协议。";
                    return false;
                }

                string clientRegistration = File.ReadAllText(clientRegistrationPath);
                string serverRegistration = File.ReadAllText(serverRegistrationPath);
                string protoRoot = GetFullPath(ProtoRoot);
                if (!ValidateProtoFiles(
                        ProtoCodeGenerator.GetControlProtoFiles(protoRoot),
                        controlOutputDirectory,
                        null,
                        null,
                        out error))
                {
                    return false;
                }

                if (!ValidateProtoFiles(
                        ProtoCodeGenerator.GetBusinessProtoFiles(protoRoot),
                        businessOutputDirectory,
                        clientRegistration,
                        serverRegistration,
                        out error))
                {
                    return false;
                }

                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = $"Proto 构建校验失败：{exception.Message}";
                return false;
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 校验一组 Proto 的 PB、角色、分协议注册和业务聚合注册产物。
        /// </summary>
        /// <param name="protoFiles">待校验 Proto 文件。</param>
        /// <param name="outputDirectory">对应生成根目录。</param>
        /// <param name="clientRegistration">业务客户端聚合注册源码；控制面传空。</param>
        /// <param name="serverRegistration">业务服务端聚合注册源码；控制面传空。</param>
        /// <param name="error">失败原因。</param>
        /// <returns>全部生成产物有效时返回 true。</returns>
        private static bool ValidateProtoFiles(
            IReadOnlyList<string> protoFiles,
            string outputDirectory,
            string clientRegistration,
            string serverRegistration,
            out string error)
        {
            for (int fileIndex = 0; fileIndex < protoFiles.Count; fileIndex++)
            {
                string protoFile = protoFiles[fileIndex];
                string stem = Path.GetFileNameWithoutExtension(protoFile);
                string scopeDirectory = ResolveScopeDirectory(protoFile);
                string generatedDirectory = Path.Combine(outputDirectory, scopeDirectory);
                string messagePath = Path.Combine(generatedDirectory, stem + ".cs");
                if (!File.Exists(messagePath))
                {
                    error = $"缺少 Proto 消息生成文件：{ToProjectPath(protoFile)}。";
                    return false;
                }

                string protoContent = File.ReadAllText(protoFile);
                MatchCollection matches = MessageRegex.Matches(protoContent);
                if (matches.Count == 0)
                {
                    continue;
                }

                string rolePath = Path.Combine(generatedDirectory, stem + ".ProtocolRole.g.cs");
                string registrationPath = Path.Combine(generatedDirectory, stem + ".ProtocolRegistration.g.cs");
                if (!File.Exists(rolePath) || !File.Exists(registrationPath))
                {
                    error = $"缺少 {stem} 的角色或协议注册生成文件。";
                    return false;
                }

                string roleContent = File.ReadAllText(rolePath);
                string registrationContent = File.ReadAllText(registrationPath);
                if (string.Equals(scopeDirectory, "Inner", StringComparison.Ordinal)
                    && serverRegistration != null
                    && serverRegistration.IndexOf(stem + "ProtocolRegistration.Register", StringComparison.Ordinal) < 0)
                {
                    error = $"业务服务端统一协议注册入口缺少 {stem}。";
                    return false;
                }

                if (!string.Equals(scopeDirectory, "Inner", StringComparison.Ordinal)
                    && clientRegistration != null
                    && clientRegistration.IndexOf(stem + "ProtocolRegistration.Register", StringComparison.Ordinal) < 0)
                {
                    error = $"业务客户端统一协议注册入口缺少 {stem}。";
                    return false;
                }

                for (int messageIndex = 0; messageIndex < matches.Count; messageIndex++)
                {
                    Match match = matches[messageIndex];
                    string role = match.Groups[1].Value;
                    string name = match.Groups[2].Value;
                    string body = match.Groups["body"].Value;
                    if (role == "IRpcResponse" && (!CodeFieldRegex.IsMatch(body) || !MsgFieldRegex.IsMatch(body)))
                    {
                        error = $"RPC 响应 {name} 缺少固定 code/msg 字段：{ToProjectPath(protoFile)}。";
                        return false;
                    }

                    if (roleContent.IndexOf($"partial class {name} : {role}", StringComparison.Ordinal) < 0 ||
                        registrationContent.IndexOf($"RegisterMessage<{name}>", StringComparison.Ordinal) < 0)
                    {
                        error = $"协议 {name} 的角色或注册生成代码已过期。";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        /// <summary>
        /// 根据 Proto 所在目录解析 Common、Outer 或 Inner 生成目录。
        /// </summary>
        /// <param name="protoFile">Proto 文件完整路径。</param>
        /// <returns>协议作用域目录名称。</returns>
        private static string ResolveScopeDirectory(string protoFile)
        {
            string normalized = protoFile.Replace('\\', '/');
            if (normalized.IndexOf("/Inner/", StringComparison.Ordinal) >= 0)
            {
                return "Inner";
            }

            if (normalized.IndexOf("/Common/", StringComparison.Ordinal) >= 0)
            {
                return "Common";
            }

            return "Outer";
        }

        /// <summary>
        /// 将项目相对路径转换为完整路径。
        /// </summary>
        /// <param name="path">项目相对路径。</param>
        /// <returns>规范化完整路径。</returns>
        private static string GetFullPath(string path)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
        }

        /// <summary>
        /// 将完整路径转换为项目相对路径。
        /// </summary>
        /// <param name="path">完整路径。</param>
        /// <returns>项目相对路径。</returns>
        private static string ToProjectPath(string path)
        {
            string root = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return path.StartsWith(root, StringComparison.Ordinal) ? path.Substring(root.Length).Replace('\\', '/') : path;
        }

        #endregion
    }
}
