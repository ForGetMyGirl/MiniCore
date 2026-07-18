using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 校验 Proto 注解、RPC 固定字段与已提交生成代码的一致性。
    /// </summary>
    internal static class ProtoBuildValidator
    {
        #region Private 私有成员

        private const string ProtoRoot = "Proto";
        private const string GeneratedMessageDirectory = "Assets/Scripts/MiniCore/Protocol/Generated/Message";
        private const string GeneratedRoleDirectory = "Assets/Scripts/MiniCore/Protocol/Generated/Role";
        private const string GeneratedRegistryDirectory = "Assets/Scripts/MiniCore/Protocol/Generated/Registry";
        private static readonly Regex MessageRegex = new Regex(@"//\[(INormalMessage|IRpcRequest|IRpcResponse)\]\s*\r?\n\s*message\s+(\w+)\s*\{(?<body>.*?)\}", RegexOptions.Singleline | RegexOptions.Compiled); // Proto 注解消息匹配器。
        private static readonly Regex CodeFieldRegex = new Regex(@"\bint32\s+code\s*=\s*1\s*;", RegexOptions.Compiled); // RPC Code 固定字段匹配器。
        private static readonly Regex MsgFieldRegex = new Regex(@"\bstring\s+msg\s*=\s*2\s*;", RegexOptions.Compiled); // RPC Msg 固定字段匹配器。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 校验所有 Proto 文件及其 C# 生成产物。
        /// </summary>
        /// <param name="error">校验失败原因。</param>
        /// <returns>全部内容有效时返回 true。</returns>
        internal static bool Validate(out string error)
        {
            try
            {
                string root = GetFullPath(ProtoRoot);
                if (!Directory.Exists(root))
                {
                    error = $"缺少 Proto 根目录：{ProtoRoot}";
                    return false;
                }

                string registryDirectory = GetFullPath(GeneratedRegistryDirectory);
                if (!Directory.Exists(GetFullPath(GeneratedMessageDirectory)) || !Directory.Exists(GetFullPath(GeneratedRoleDirectory)) || !Directory.Exists(registryDirectory))
                {
                    error = "缺少 Protocol 生成目录，请先执行 protoc 并生成角色与 Parser 注册表。";
                    return false;
                }

                string registryContent = ReadAllFiles(registryDirectory);
                string[] protoFiles = ProtoCodeGenerator.GetBusinessProtoFiles(root);
                if (protoFiles.Length == 0)
                {
                    error = $"未找到 Proto 文件：{ProtoRoot}";
                    return false;
                }

                foreach (string protoFile in protoFiles)
                {
                    string content = File.ReadAllText(protoFile);
                    MatchCollection matches = MessageRegex.Matches(content);
                    if (matches.Count == 0)
                    {
                        error = $"Proto 文件未找到角色注解消息：{ToProjectPath(protoFile)}";
                        return false;
                    }

                    string generatedMessage = Path.Combine(GetFullPath(GeneratedMessageDirectory), Path.GetFileNameWithoutExtension(protoFile) + ".cs");
                    string generatedRole = Path.Combine(GetFullPath(GeneratedRoleDirectory), Path.GetFileNameWithoutExtension(protoFile) + ".ProtocolRole.g.cs");
                    if (!File.Exists(generatedMessage) || !File.Exists(generatedRole))
                    {
                        error = $"Proto 生成产物缺失：{ToProjectPath(protoFile)}";
                        return false;
                    }

                    string roleContent = File.ReadAllText(generatedRole);
                    for (int index = 0; index < matches.Count; index++)
                    {
                        Match match = matches[index];
                        string role = match.Groups[1].Value;
                        string messageName = match.Groups[2].Value;
                        string body = match.Groups["body"].Value;
                        if (role == "IRpcResponse" && (!CodeFieldRegex.IsMatch(body) || !MsgFieldRegex.IsMatch(body)))
                        {
                            error = $"RPC 响应 {messageName} 必须定义 int32 code = 1; 与 string msg = 2;：{ToProjectPath(protoFile)}";
                            return false;
                        }

                        string expectedRole = role == "INormalMessage" ? "INormalMessage" : role;
                        if (roleContent.IndexOf($"partial class {messageName} : {expectedRole}", StringComparison.Ordinal) < 0)
                        {
                            error = $"Proto 角色生成代码过期：{messageName} 未实现 {expectedRole}。";
                            return false;
                        }

                        if (registryContent.IndexOf($"Register({messageName}.Parser);", StringComparison.Ordinal) < 0)
                        {
                            error = $"Protobuf Parser 注册表缺少消息：{messageName}。";
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
        /// 读取目录中全部 C# 生成文件内容。
        /// </summary>
        /// <param name="directory">目标目录。</param>
        /// <returns>拼接后的文本。</returns>
        private static string ReadAllFiles(string directory)
        {
            var contents = new List<string>();
            string[] files = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);
            for (int index = 0; index < files.Length; index++)
            {
                contents.Add(File.ReadAllText(files[index]));
            }

            return string.Join(Environment.NewLine, contents);
        }

        /// <summary>
        /// 将项目相对路径转换为绝对路径。
        /// </summary>
        /// <param name="path">项目相对路径。</param>
        /// <returns>绝对路径。</returns>
        private static string GetFullPath(string path)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
        }

        /// <summary>
        /// 将绝对路径转换为项目相对路径供错误信息使用。
        /// </summary>
        /// <param name="path">绝对路径。</param>
        /// <returns>项目相对路径。</returns>
        private static string ToProjectPath(string path)
        {
            string projectRoot = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return path.StartsWith(projectRoot, StringComparison.Ordinal) ? path.Substring(projectRoot.Length) : path;
        }

        #endregion
    }
}
