using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 调用 protoc 生成消息代码，并由 Proto 注解生成 MiniCore 协议角色和 Parser 注册表。
    /// </summary>
    internal static class ProtoCodeGenerator
    {
        #region Private 私有成员

        private const string ProtoRoot = "Proto";
        private const string ProtocVersion = "29.5";
        private const string ProtocToolRoot = "Proto/Tools/protoc-29.5";
        private const string ProtocIncludeDirectory = "Proto/Tools/protoc-29.5/include";
        private const string GeneratedMessageDirectory = "Assets/Scripts/MiniCore/Protocol/Generated/Message";
        private const string GeneratedRoleDirectory = "Assets/Scripts/MiniCore/Protocol/Generated/Role";
        private const string GeneratedRegistryPath = "Assets/Scripts/MiniCore/Protocol/Generated/Registry/ProtobufMessageRegistry.Generated.cs";
        private static readonly Regex MessageRegex = new Regex(@"//\[(INormalMessage|IRpcRequest|IRpcResponse)\]\s*\r?\n\s*message\s+(\w+)\s*\{(?<body>.*?)\}", RegexOptions.Singleline | RegexOptions.Compiled); // Proto 注解消息匹配器。
        private static readonly Regex CodeFieldRegex = new Regex(@"\bint32\s+code\s*=\s*1\s*;", RegexOptions.Compiled); // RPC Code 固定字段匹配器。
        private static readonly Regex MsgFieldRegex = new Regex(@"\bstring\s+msg\s*=\s*2\s*;", RegexOptions.Compiled); // RPC Msg 固定字段匹配器。
        private static readonly Regex GeneratedSourceRegex = new Regex(@"^//\s+source:\s+(.+\.proto)\s*$", RegexOptions.Multiline | RegexOptions.Compiled); // protoc 生成文件来源匹配器。
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false); // 生成文件的固定编码。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 使用仓库内置 protoc 生成全部协议代码，并在编译完成后自动同步 Opcode 和 Handler 表。
        /// </summary>
        [MenuItem("MiniCore/Protocol/Generate All", priority = 2100)]
        public static void Generate()
        {
            try
            {
                string log = GenerateAll();
                OpcodeAutoGenerator.RequestSynchronization();
                AssetDatabase.Refresh();
                UnityEngine.Debug.Log($"{log}\nUnity 编译完成后将自动同步 Opcode 和 Handler 注册表。");
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError($"Proto 生成失败：{exception}");
            }
        }

        /// <summary>
        /// 从已编译的 HotUpdate Handler 同步稳定 Opcode 和直接注册表，供持续集成验证使用。
        /// </summary>
        public static void SynchronizeOpcodeAndHandlersFromCommandLine()
        {
            if (!OpcodeRegistryGenerator.Synchronize(true, out string log))
            {
                throw new InvalidOperationException(log);
            }

            UnityEngine.Debug.Log(log);
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 生成全部 Protobuf 消息、角色扩展和 Parser 注册表。
        /// </summary>
        /// <returns>生成结果日志。</returns>
        internal static string GenerateAll()
        {
            string protoRoot = GetFullPath(ProtoRoot);
            string[] protoFiles = GetBusinessProtoFiles(protoRoot);
            if (protoFiles.Length == 0)
            {
                throw new InvalidOperationException($"未找到 Proto 文件：{ProtoRoot}");
            }

            string protoc = GetProtocPath();
            ValidateProtocVersion(protoc);
            Directory.CreateDirectory(GetFullPath(GeneratedMessageDirectory));
            Directory.CreateDirectory(GetFullPath(GeneratedRoleDirectory));
            var allMessages = new List<MessageDefinition>();
            var expectedRolePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < protoFiles.Length; index++)
            {
                string protoFile = protoFiles[index];
                RunProtoc(protoc, protoRoot, protoFile);
                List<MessageDefinition> messages = ReadMessages(protoFile);
                string rolePath = GetRolePath(protoFile);
                WriteFileIfChanged(rolePath, BuildRoleContent(protoFile, messages));
                expectedRolePaths.Add(rolePath);
                allMessages.AddRange(messages);
            }

            DeleteStaleMessageFiles(protoFiles);
            DeleteStaleRoleFiles(expectedRolePaths);
            allMessages.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
            WriteFileIfChanged(GetFullPath(GeneratedRegistryPath), BuildRegistryContent(allMessages));
            return $"已生成 {protoFiles.Length} 个 Proto 文件、{allMessages.Count} 个协议角色和 Parser 注册表。";
        }

        /// <summary>
        /// 获取 Proto 根目录中的业务协议文件，并排除随工具附带的标准协议定义。
        /// </summary>
        /// <param name="protoRoot">Proto 根目录完整路径。</param>
        /// <returns>按路径稳定排序的业务 Proto 文件。</returns>
        internal static string[] GetBusinessProtoFiles(string protoRoot)
        {
            return CollectBusinessProtoFiles(protoRoot);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 调用 protoc 生成单个 Proto 的 C# 消息代码。
        /// </summary>
        /// <param name="protoc">当前编辑器平台对应的 protoc 完整路径。</param>
        /// <param name="protoRoot">Proto 根目录。</param>
        /// <param name="protoFile">待生成 Proto 文件。</param>
        private static void RunProtoc(string protoc, string protoRoot, string protoFile)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = protoc,
                Arguments = $"--proto_path=\"{protoRoot}\" --proto_path=\"{GetFullPath(ProtocIncludeDirectory)}\" --csharp_out=\"{GetFullPath(GeneratedMessageDirectory)}\" \"{protoFile}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            using (Process process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException($"无法启动 protoc：{protoc}");
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"protoc 生成失败：{ToProjectPath(protoFile)}\n{output}\n{error}");
                }
            }
        }

        /// <summary>
        /// 获取当前 Unity 编辑器平台对应的仓库内置 protoc 路径。
        /// </summary>
        /// <returns>仓库内置 protoc 的完整路径。</returns>
        private static string GetProtocPath()
        {
#if UNITY_EDITOR_WIN
            const string relativePath = ProtocToolRoot + "/windows-x64/protoc.exe";
#elif UNITY_EDITOR_OSX
            string relativePath = RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                ? ProtocToolRoot + "/macos-arm64/protoc"
                : ProtocToolRoot + "/macos-x64/protoc";
#else
            throw new PlatformNotSupportedException("当前内置 protoc 仅支持 Windows x64、macOS Intel 和 macOS Apple Silicon。");
#endif
            string protocPath = GetFullPath(relativePath);
            if (!File.Exists(protocPath))
            {
                throw new FileNotFoundException($"缺少仓库内置 protoc：{relativePath}", protocPath);
            }

            return protocPath;
        }

        /// <summary>
        /// 收集 Proto 根目录中的业务协议文件，并排除随工具附带的标准协议定义。
        /// </summary>
        /// <param name="protoRoot">Proto 根目录完整路径。</param>
        /// <returns>按路径稳定排序的业务 Proto 文件。</returns>
        private static string[] CollectBusinessProtoFiles(string protoRoot)
        {
            string toolsRoot = Path.Combine(protoRoot, "Tools") + Path.DirectorySeparatorChar;
            string[] allFiles = Directory.GetFiles(protoRoot, "*.proto", SearchOption.AllDirectories);
            var result = new List<string>(allFiles.Length);
            var sourceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < allFiles.Length; index++)
            {
                string fullPath = Path.GetFullPath(allFiles[index]);
                if (!fullPath.StartsWith(toolsRoot, StringComparison.OrdinalIgnoreCase))
                {
                    string sourceName = Path.GetFileName(fullPath);
                    if (!sourceNames.Add(sourceName))
                    {
                        throw new InvalidOperationException($"业务 Proto 文件名重复：{sourceName}。当前生成目录要求文件名全局唯一。");
                    }

                    result.Add(fullPath);
                }
            }

            result.Sort(StringComparer.Ordinal);
            return result.ToArray();
        }

        /// <summary>
        /// 删除已无业务 Proto 来源的旧 Protobuf 消息生成文件。
        /// </summary>
        /// <param name="protoFiles">当前全部业务 Proto 文件。</param>
        private static void DeleteStaleMessageFiles(IReadOnlyList<string> protoFiles)
        {
            var expectedSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < protoFiles.Count; index++)
            {
                expectedSources.Add(Path.GetFileName(protoFiles[index]));
            }

            string[] generatedFiles = Directory.GetFiles(GetFullPath(GeneratedMessageDirectory), "*.cs", SearchOption.AllDirectories);
            for (int index = 0; index < generatedFiles.Length; index++)
            {
                string generatedFile = generatedFiles[index];
                Match sourceMatch = GeneratedSourceRegex.Match(File.ReadAllText(generatedFile));
                if (sourceMatch.Success && !expectedSources.Contains(Path.GetFileName(sourceMatch.Groups[1].Value)))
                {
                    AssetDatabase.DeleteAsset(ToProjectPath(generatedFile));
                }
            }
        }

        /// <summary>
        /// 删除已无业务 Proto 来源的旧协议角色生成文件。
        /// </summary>
        /// <param name="expectedRolePaths">当前业务 Proto 对应的角色文件完整路径。</param>
        private static void DeleteStaleRoleFiles(ISet<string> expectedRolePaths)
        {
            string[] generatedFiles = Directory.GetFiles(GetFullPath(GeneratedRoleDirectory), "*.ProtocolRole.g.cs", SearchOption.AllDirectories);
            for (int index = 0; index < generatedFiles.Length; index++)
            {
                string generatedFile = Path.GetFullPath(generatedFiles[index]);
                if (!expectedRolePaths.Contains(generatedFile))
                {
                    AssetDatabase.DeleteAsset(ToProjectPath(generatedFile));
                }
            }
        }

        /// <summary>
        /// 验证仓库内置 protoc 的版本与项目锁定版本一致。
        /// </summary>
        /// <param name="protoc">当前平台 protoc 的完整路径。</param>
        private static void ValidateProtocVersion(string protoc)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = protoc,
                Arguments = "--version",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using (Process process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException($"无法启动 protoc：{protoc}");
                }

                string output = process.StandardOutput.ReadToEnd().Trim();
                string error = process.StandardError.ReadToEnd().Trim();
                process.WaitForExit();
                if (process.ExitCode != 0 || !string.Equals(output, $"libprotoc {ProtocVersion}", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"protoc 版本校验失败，期望 libprotoc {ProtocVersion}，实际 {output}。{error}");
                }
            }
        }

        /// <summary>
        /// 从单个 Proto 中读取带 MiniCore 角色注解的消息定义。
        /// </summary>
        /// <param name="protoFile">Proto 文件路径。</param>
        /// <returns>消息定义集合。</returns>
        private static List<MessageDefinition> ReadMessages(string protoFile)
        {
            string content = File.ReadAllText(protoFile);
            MatchCollection matches = MessageRegex.Matches(content);
            if (matches.Count == 0)
            {
                throw new InvalidOperationException($"Proto 文件未找到角色注解消息：{ToProjectPath(protoFile)}");
            }

            var result = new List<MessageDefinition>(matches.Count);
            for (int index = 0; index < matches.Count; index++)
            {
                Match match = matches[index];
                string role = match.Groups[1].Value;
                string name = match.Groups[2].Value;
                string body = match.Groups["body"].Value;
                if (role == "IRpcResponse" && (!CodeFieldRegex.IsMatch(body) || !MsgFieldRegex.IsMatch(body)))
                {
                    throw new InvalidOperationException($"RPC 响应 {name} 必须定义 int32 code = 1; 与 string msg = 2;：{ToProjectPath(protoFile)}");
                }

                result.Add(new MessageDefinition(name, role));
            }

            return result;
        }

        /// <summary>
        /// 生成单个 Proto 的协议角色分部类代码。
        /// </summary>
        /// <param name="protoFile">来源 Proto 文件。</param>
        /// <param name="messages">该文件中的消息定义。</param>
        /// <returns>角色生成代码。</returns>
        private static string BuildRoleContent(string protoFile, IReadOnlyList<MessageDefinition> messages)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"// Auto-generated from {ToProjectPath(protoFile)}. Do not edit by hand.");
            builder.AppendLine("using MiniCore.Model;");
            builder.AppendLine();
            builder.AppendLine("namespace MiniCore.Protocol.Generated");
            builder.AppendLine("{");
            for (int index = 0; index < messages.Count; index++)
            {
                MessageDefinition message = messages[index];
                builder.AppendLine("    /// <summary>");
                builder.AppendLine($"    /// 为生成协议补充 {message.Role} 网络角色。");
                builder.AppendLine("    /// </summary>");
                builder.AppendLine($"    public sealed partial class {message.Name} : {message.Role}");
                builder.AppendLine("    {");
                if (message.Role != "INormalMessage")
                {
                    builder.AppendLine("        /// <summary>");
                    builder.AppendLine("        /// 获取或设置网络包头关联的请求标识。");
                    builder.AppendLine("        /// </summary>");
                    builder.AppendLine("        public long RpcId { get; set; }");
                }
                builder.AppendLine("    }");
                builder.AppendLine();
            }
            builder.AppendLine("}");
            return builder.ToString();
        }

        /// <summary>
        /// 生成全部 Protobuf Parser 的注册表分部实现。
        /// </summary>
        /// <param name="messages">所有消息定义。</param>
        /// <returns>Parser 注册表代码。</returns>
        private static string BuildRegistryContent(IReadOnlyList<MessageDefinition> messages)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// Auto-generated by ProtoCodeGenerator. Do not edit by hand.");
            builder.AppendLine("using MiniCore.Protocol.Generated;");
            builder.AppendLine();
            builder.AppendLine("namespace MiniCore.Model");
            builder.AppendLine("{");
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 生成协议 Parser 的注册表扩展。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    public static partial class ProtobufMessageRegistry");
            builder.AppendLine("    {");
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 注册当前项目的全部生成协议 Parser。");
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        static partial void RegisterGenerated()");
            builder.AppendLine("        {");
            for (int index = 0; index < messages.Count; index++)
            {
                builder.AppendLine($"            Register({messages[index].Name}.Parser);");
            }
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        /// <summary>
        /// 获取单个 Proto 对应的角色生成文件路径。
        /// </summary>
        /// <param name="protoFile">来源 Proto 文件。</param>
        /// <returns>角色生成文件绝对路径。</returns>
        private static string GetRolePath(string protoFile)
        {
            return Path.Combine(GetFullPath(GeneratedRoleDirectory), Path.GetFileNameWithoutExtension(protoFile) + ".ProtocolRole.g.cs");
        }

        /// <summary>
        /// 仅在内容发生变化时写入生成文件。
        /// </summary>
        /// <param name="path">生成文件绝对路径。</param>
        /// <param name="content">生成内容。</param>
        private static void WriteFileIfChanged(string path, string content)
        {
            if (File.Exists(path) && string.Equals(File.ReadAllText(path), content, StringComparison.Ordinal))
            {
                return;
            }

            File.WriteAllText(path, content, Utf8WithoutBom);
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
        /// 将绝对路径转换为项目相对路径供日志使用。
        /// </summary>
        /// <param name="path">绝对路径。</param>
        /// <returns>项目相对路径。</returns>
        private static string ToProjectPath(string path)
        {
            string root = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return path.StartsWith(root, StringComparison.Ordinal) ? path.Substring(root.Length) : path;
        }

        /// <summary>
        /// 保存 Proto 中一个带角色注解的消息定义。
        /// </summary>
        private sealed class MessageDefinition
        {
            #region Public 公共成员

            /// <summary>
            /// 获取消息名称。
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// 获取 MiniCore 网络角色接口名称。
            /// </summary>
            public string Role { get; }

            /// <summary>
            /// 创建消息定义。
            /// </summary>
            /// <param name="name">消息名称。</param>
            /// <param name="role">网络角色名称。</param>
            public MessageDefinition(string name, string role)
            {
                Name = name;
                Role = role;
            }

            #endregion
        }

        #endregion
    }
}
