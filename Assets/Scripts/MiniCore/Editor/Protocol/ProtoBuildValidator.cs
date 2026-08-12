using System;
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

                string outputDirectory = GetFullPath(MiniCoreProtocolSettings.instance.ProjectOutputDirectory);
                if (!Directory.Exists(outputDirectory))
                {
                    error = "项目业务 Proto 输出目录不存在，请先生成协议。";
                    return false;
                }

                string projectRegistrationPath = Path.Combine(outputDirectory, "ProjectProtocolRegistration.g.cs");
                if (!File.Exists(projectRegistrationPath))
                {
                    error = "缺少项目统一协议注册入口，请重新生成协议。";
                    return false;
                }

                string projectRegistration = File.ReadAllText(projectRegistrationPath);
                string[] protoFiles = ProtoCodeGenerator.GetBusinessProtoFiles(GetFullPath(ProtoRoot));
                for (int fileIndex = 0; fileIndex < protoFiles.Length; fileIndex++)
                {
                    string protoFile = protoFiles[fileIndex];
                    string stem = Path.GetFileNameWithoutExtension(protoFile);
                    string messagePath = Path.Combine(outputDirectory, stem + ".cs");
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

                    string rolePath = Path.Combine(outputDirectory, stem + ".ProtocolRole.g.cs");
                    string registrationPath = Path.Combine(outputDirectory, stem + ".ProtocolRegistration.g.cs");
                    if (!File.Exists(rolePath) || !File.Exists(registrationPath))
                    {
                        error = $"缺少 {stem} 的角色或协议注册生成文件。";
                        return false;
                    }

                    string roleContent = File.ReadAllText(rolePath);
                    string registrationContent = File.ReadAllText(registrationPath);
                    if (projectRegistration.IndexOf(stem + "ProtocolRegistration.Register", StringComparison.Ordinal) < 0)
                    {
                        error = $"项目统一协议注册入口缺少 {stem}。";
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
            catch (Exception exception)
            {
                error = $"Proto 构建校验失败：{exception.Message}";
                return false;
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将项目相对路径转换为完整路径。
        /// </summary>
        private static string GetFullPath(string path)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
        }

        /// <summary>
        /// 将完整路径转换为项目相对路径。
        /// </summary>
        private static string ToProjectPath(string path)
        {
            string root = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return path.StartsWith(root, StringComparison.Ordinal) ? path.Substring(root.Length).Replace('\\', '/') : path;
        }

        #endregion
    }
}
