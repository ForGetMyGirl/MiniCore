using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 生成框架内部与项目业务 Protobuf 代码，并维护稳定 Opcode 和无状态注册入口。
    /// </summary>
    internal static class ProtoCodeGenerator
    {
        #region Private 私有成员

        private const string ProtoRoot = "Proto";
        private const string ControlProtoDirectory = "Proto/Control";
        private const string BusinessProtoDirectory = "Proto/Business";
        private const string ClientSettingsProtoPath = "Proto/Internal/ClientSettings.proto";
        private const string ClientSettingsOutputDirectory = "Assets/Scripts/MiniCore/Unity/Service/Persistence/Generated";
        private const string ControlOutputDirectory = "Assets/Scripts/MiniCore/Protocol/Control/Generated";
        private const string ProtocolControlAssemblyName = "MiniCore.Protocol.Control";
        private const string ProtocolControlInnerAssemblyName = "MiniCore.Protocol.Control.Inner";
        private const string ProtocolCommonAssemblyName = "MiniCore.Protocol.Common";
        private const string ProtocolOuterAssemblyName = "MiniCore.Protocol.Outer";
        private const string ProtocolInnerAssemblyName = "MiniCore.Protocol.Inner";
        private const string OpcodeManifestPath = "Proto/Manifest/OpcodeManifest.json";
        private const string OwnershipManifestPath = "ProjectSettings/MiniCoreProtocolGeneratedFiles.json";
        private const int OwnershipManifestVersion = 3;
        private const string OwnershipManifestGenerator = "MiniCore.Protocol.ProtoCodeGenerator";
        private const string PendingHandlerSynchronizationKey = "MiniCore.Protocol.PendingOpcodeSynchronization";
        private const string ProtocVersion = "29.5";
        private const string ProtocToolRoot = "Proto/Tools/protoc-29.5";
        private const string ProtocIncludeDirectory = "Proto/Tools/protoc-29.5/include";
        private const uint NormalStartOpcode = 100001;
        private const uint RpcStartOpcode = 200001;
        private static readonly Regex MessageRegex = new Regex(@"//\[(INormalMessage|IRpcRequest|IRpcResponse)\]\s*\r?\n\s*message\s+(\w+)\s*\{(?<body>.*?)\}", RegexOptions.Singleline | RegexOptions.Compiled); // Proto 网络角色注解匹配器。
        private static readonly Regex NamespaceRegex = new Regex(@"option\s+csharp_namespace\s*=\s*""([^""]+)""\s*;", RegexOptions.Compiled); // C# 命名空间匹配器。
        private static readonly Regex CodeFieldRegex = new Regex(@"\bint32\s+code\s*=\s*1\s*;", RegexOptions.Compiled); // RPC 响应固定 Code 字段匹配器。
        private static readonly Regex MsgFieldRegex = new Regex(@"\bstring\s+msg\s*=\s*2\s*;", RegexOptions.Compiled); // RPC 响应固定 Msg 字段匹配器。
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false); // 生成文件固定编码。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 生成框架内部存档、固定控制面与当前项目的全部业务协议代码。
        /// </summary>
        [MenuItem("MiniCore/Protocol/Generate All", priority = 2100)]
        public static void Generate()
        {
            try
            {
                string log = GenerateAll();
                SessionState.SetBool(PendingHandlerSynchronizationKey, true);
                AssetDatabase.Refresh();
                UnityEngine.Debug.Log($"{log}\nUnity 编译完成后将自动同步 Handler 注册代码。");
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError($"Proto 生成失败：{exception}");
            }
        }

        /// <summary>
        /// 在命令行中生成全部协议；失败时直接让 Unity 返回非零结果。
        /// </summary>
        public static void GenerateFromCommandLine()
        {
            string log = GenerateAll();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            UnityEngine.Debug.Log(log);
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 执行全部协议生成，并返回生成摘要。
        /// </summary>
        /// <returns>生成摘要。</returns>
        internal static string GenerateAll()
        {
            string protoRoot = GetFullPath(ProtoRoot);
            string businessOutputDirectory = MiniCoreProtocolSettings.instance.ProjectOutputDirectory;
            ValidateProjectOutputDirectory(businessOutputDirectory);
            ValidateControlOutputDirectory();
            string businessOutputFullPath = GetFullPath(businessOutputDirectory);
            string controlOutputFullPath = GetFullPath(ControlOutputDirectory);
            CreateScopeOutputDirectories(businessOutputFullPath);
            CreateScopeOutputDirectories(controlOutputFullPath);
            Directory.CreateDirectory(GetFullPath(ClientSettingsOutputDirectory));

            string protoc = GetProtocPath();
            ValidateProtocVersion(protoc);
            RunProtoc(protoc, protoRoot, GetFullPath(ClientSettingsProtoPath), GetFullPath(ClientSettingsOutputDirectory));

            string[] controlProtoFiles = GetControlProtoFiles(protoRoot);
            string[] businessProtoFiles = GetBusinessProtoFiles(protoRoot);
            ValidateUniqueProtoFileNames(controlProtoFiles, businessProtoFiles);
            var controlDefinitions = new List<ProtoDefinition>(controlProtoFiles.Length);
            var businessDefinitions = new List<ProtoDefinition>(businessProtoFiles.Length);
            GenerateProtocolDefinitions(protoc, protoRoot, controlOutputFullPath, controlProtoFiles, controlDefinitions);
            GenerateProtocolDefinitions(protoc, protoRoot, businessOutputFullPath, businessProtoFiles, businessDefinitions);

            var allDefinitions = new List<ProtoDefinition>(controlDefinitions.Count + businessDefinitions.Count);
            allDefinitions.AddRange(controlDefinitions);
            allDefinitions.AddRange(businessDefinitions);

            OpcodeManifest opcodeManifest = LoadOpcodeManifest();
            AssignOpcodes(allDefinitions, opcodeManifest);
            SaveOpcodeManifest(opcodeManifest);
            WriteGeneratedProtocolFiles(controlOutputFullPath, controlDefinitions, false);
            WriteGeneratedProtocolFiles(businessOutputFullPath, businessDefinitions, true);
            SaveOwnershipManifest(businessOutputDirectory, controlDefinitions, businessDefinitions);
            return $"已生成框架内部存档 PB、{controlProtoFiles.Length} 个固定控制面 Proto、{businessProtoFiles.Length} 个业务 Proto 和 {CountMessages(allDefinitions)} 个网络协议注册项。";
        }

        /// <summary>
        /// 获取固定控制面 Proto。
        /// </summary>
        /// <param name="protoRoot">Proto 根目录完整路径。</param>
        /// <returns>稳定排序的控制面 Proto 路径。</returns>
        internal static string[] GetControlProtoFiles(string protoRoot)
        {
            return GetProtoFiles(Path.Combine(protoRoot, "Control"));
        }

        /// <summary>
        /// 获取项目业务 Proto。
        /// </summary>
        /// <param name="protoRoot">Proto 根目录完整路径。</param>
        /// <returns>稳定排序的业务 Proto 路径。</returns>
        internal static string[] GetBusinessProtoFiles(string protoRoot)
        {
            return GetProtoFiles(Path.Combine(protoRoot, "Business"));
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 创建 Common、Outer 与 Inner 三个协议输出目录。
        /// </summary>
        /// <param name="outputRoot">协议生成根目录完整路径。</param>
        private static void CreateScopeOutputDirectories(string outputRoot)
        {
            Directory.CreateDirectory(outputRoot);
            Directory.CreateDirectory(Path.Combine(outputRoot, "Common"));
            Directory.CreateDirectory(Path.Combine(outputRoot, "Outer"));
            Directory.CreateDirectory(Path.Combine(outputRoot, "Inner"));
        }

        /// <summary>
        /// 调用 protoc 生成一组协议并读取网络角色定义。
        /// </summary>
        /// <param name="protoc">protoc 可执行文件完整路径。</param>
        /// <param name="protoRoot">Proto 根目录完整路径。</param>
        /// <param name="outputRoot">目标生成根目录。</param>
        /// <param name="protoFiles">待生成的 Proto 文件。</param>
        /// <param name="definitions">用于接收协议定义的集合。</param>
        private static void GenerateProtocolDefinitions(
            string protoc,
            string protoRoot,
            string outputRoot,
            IReadOnlyList<string> protoFiles,
            ICollection<ProtoDefinition> definitions)
        {
            for (int index = 0; index < protoFiles.Count; index++)
            {
                string protoFile = protoFiles[index];
                ProtoDefinition definition = ReadDefinition(protoFile);
                RunProtoc(protoc, protoRoot, protoFile, GetScopeOutputDirectory(outputRoot, definition.Scope));
                definitions.Add(definition);
            }
        }

        /// <summary>
        /// 获取指定目录下的全部 Proto，并按完整路径稳定排序。
        /// </summary>
        /// <param name="sourceRoot">待扫描目录完整路径。</param>
        /// <returns>稳定排序的 Proto 路径。</returns>
        private static string[] GetProtoFiles(string sourceRoot)
        {
            if (!Directory.Exists(sourceRoot))
            {
                throw new DirectoryNotFoundException($"缺少协议源码目录：{ToProjectPath(sourceRoot)}。");
            }

            string[] files = Directory.GetFiles(sourceRoot, "*.proto", SearchOption.AllDirectories);
            for (int index = 0; index < files.Length; index++)
            {
                files[index] = Path.GetFullPath(files[index]);
            }

            Array.Sort(files, StringComparer.Ordinal);
            return files;
        }

        /// <summary>
        /// 校验控制面和业务协议文件名在统一 C# 命名空间内不重复。
        /// </summary>
        /// <param name="controlProtoFiles">控制面协议文件。</param>
        /// <param name="businessProtoFiles">业务协议文件。</param>
        private static void ValidateUniqueProtoFileNames(
            IReadOnlyList<string> controlProtoFiles,
            IReadOnlyList<string> businessProtoFiles)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ValidateUniqueProtoFileNames(controlProtoFiles, names);
            ValidateUniqueProtoFileNames(businessProtoFiles, names);
        }

        /// <summary>
        /// 将一组 Proto 文件名加入唯一性集合。
        /// </summary>
        /// <param name="protoFiles">待校验协议文件。</param>
        /// <param name="names">已经占用的文件名集合。</param>
        private static void ValidateUniqueProtoFileNames(
            IReadOnlyList<string> protoFiles,
            ISet<string> names)
        {
            for (int index = 0; index < protoFiles.Count; index++)
            {
                string fileName = Path.GetFileName(protoFiles[index]);
                if (!names.Add(fileName))
                {
                    throw new InvalidOperationException($"Proto 文件名重复：{fileName}。统一生成命名空间要求文件名全局唯一。");
                }
            }
        }

        /// <summary>
        /// 验证业务输出目录位于 Assets 下并由三个热更新业务协议程序集承载。
        /// </summary>
        /// <param name="outputDirectory">项目相对输出目录。</param>
        private static void ValidateProjectOutputDirectory(string outputDirectory)
        {
            ValidateAssetsOutputDirectory(outputDirectory);
            ValidateScopeAssembly(outputDirectory, ProtocolScope.Common, ProtocolCommonAssemblyName);
            ValidateScopeAssembly(outputDirectory, ProtocolScope.Outer, ProtocolOuterAssemblyName);
            ValidateScopeAssembly(outputDirectory, ProtocolScope.Inner, ProtocolInnerAssemblyName);
        }

        /// <summary>
        /// 验证固定控制面输出目录由 AOT Control 程序集承载。
        /// </summary>
        private static void ValidateControlOutputDirectory()
        {
            ValidateAssetsOutputDirectory(ControlOutputDirectory);
            ValidateScopeAssembly(ControlOutputDirectory, ProtocolScope.Common, ProtocolControlAssemblyName);
            ValidateScopeAssembly(ControlOutputDirectory, ProtocolScope.Outer, ProtocolControlAssemblyName);
            ValidateScopeAssembly(ControlOutputDirectory, ProtocolScope.Inner, ProtocolControlInnerAssemblyName);
        }

        /// <summary>
        /// 验证协议生成根目录位于 Assets 下且不会通过链接逃逸。
        /// </summary>
        /// <param name="outputDirectory">项目相对输出目录。</param>
        private static void ValidateAssetsOutputDirectory(string outputDirectory)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory)
                || Path.IsPathRooted(outputDirectory)
                || outputDirectory.IndexOf("..", StringComparison.Ordinal) >= 0
                || (!outputDirectory.Equals("Assets", StringComparison.Ordinal)
                    && !outputDirectory.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("项目 Proto 输出目录必须位于 Assets 下。");
            }

            string assetsRoot = GetFullPath("Assets").TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string outputFullPath = GetFullPath(outputDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!outputFullPath.StartsWith(assetsRoot, StringComparison.Ordinal) || ContainsSymbolicLink(outputFullPath, assetsRoot))
            {
                throw new InvalidOperationException("项目 Proto 输出目录不能通过符号链接或路径跳转离开 Assets。");
            }
        }

        /// <summary>
        /// 验证一个协议作用域输出目录所属的程序集名称。
        /// </summary>
        /// <param name="outputRoot">协议生成根目录。</param>
        /// <param name="scope">Common、Outer 或 Inner 作用域。</param>
        /// <param name="expectedAssemblyName">期望的程序集名称。</param>
        private static void ValidateScopeAssembly(
            string outputRoot,
            ProtocolScope scope,
            string expectedAssemblyName)
        {
            string scopeDirectory = NormalizeAssetPath(outputRoot) + "/" + scope;
            string asmdefPath = FindNearestAssemblyDefinition(scopeDirectory);
            if (!TryReadAssemblyDefinitionName(asmdefPath, out string assemblyName)
                || !string.Equals(assemblyName, expectedAssemblyName, StringComparison.Ordinal))
            {
                string actual = asmdefPath == null ? "没有 asmdef" : ToProjectPath(asmdefPath);
                throw new InvalidOperationException(
                    $"协议输出目录 {scopeDirectory} 必须属于 {expectedAssemblyName}，当前为 {actual}。");
            }
        }

        /// <summary>
        /// 读取程序集定义中的稳定程序集名称。
        /// </summary>
        /// <param name="assemblyDefinitionPath">程序集定义完整路径。</param>
        /// <param name="assemblyName">成功时返回程序集名称。</param>
        /// <returns>文件存在且包含有效名称时返回 true。</returns>
        private static bool TryReadAssemblyDefinitionName(string assemblyDefinitionPath, out string assemblyName)
        {
            assemblyName = null;
            if (string.IsNullOrWhiteSpace(assemblyDefinitionPath) || !File.Exists(assemblyDefinitionPath))
            {
                return false;
            }

            AssemblyDefinitionData definition = JsonUtility.FromJson<AssemblyDefinitionData>(
                File.ReadAllText(assemblyDefinitionPath));
            assemblyName = definition?.Name;
            return !string.IsNullOrWhiteSpace(assemblyName);
        }

        /// <summary>
        /// 从目标目录向上查找最近的 asmdef。
        /// </summary>
        /// <param name="outputDirectory">项目相对目录。</param>
        /// <returns>asmdef 完整路径；未找到时返回 null。</returns>
        private static string FindNearestAssemblyDefinition(string outputDirectory)
        {
            string current = GetFullPath(outputDirectory);
            string assetsRoot = GetFullPath("Assets");
            while (current.StartsWith(assetsRoot, StringComparison.Ordinal))
            {
                if (Directory.Exists(current))
                {
                    string[] references = Directory.GetFiles(current, "*.asmref", SearchOption.TopDirectoryOnly);
                    if (references.Length > 0)
                    {
                        throw new InvalidOperationException($"输出目录受 asmref 重定向，不能作为项目 PB 输出位置：{ToProjectPath(current)}。");
                    }

                    string[] asmdefs = Directory.GetFiles(current, "*.asmdef", SearchOption.TopDirectoryOnly);
                    if (asmdefs.Length > 1)
                    {
                        throw new InvalidOperationException($"目录存在多个 asmdef：{ToProjectPath(current)}。");
                    }

                    if (asmdefs.Length == 1)
                    {
                        return asmdefs[0];
                    }
                }

                current = Path.GetDirectoryName(current);
                if (current == null)
                {
                    break;
                }
            }

            return null;
        }

        /// <summary>
        /// 检查 Assets 根目录到输出目录之间是否包含符号链接或重解析点。
        /// </summary>
        private static bool ContainsSymbolicLink(string outputFullPath, string assetsRoot)
        {
            string current = outputFullPath.TrimEnd(Path.DirectorySeparatorChar);
            string root = assetsRoot.TrimEnd(Path.DirectorySeparatorChar);
            while (current.Length >= root.Length)
            {
                if (Directory.Exists(current) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                {
                    return true;
                }

                if (string.Equals(current, root, StringComparison.Ordinal))
                {
                    break;
                }

                current = Path.GetDirectoryName(current);
                if (current == null)
                {
                    break;
                }
            }

            return false;
        }

        /// <summary>
        /// 调用 protoc 生成单个 C# 文件。
        /// </summary>
        private static void RunProtoc(string protoc, string protoRoot, string protoFile, string outputDirectory)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = protoc,
                Arguments = $"--proto_path=\"{protoRoot}\" --proto_path=\"{GetFullPath(ProtocIncludeDirectory)}\" --csharp_out=\"{outputDirectory}\" \"{protoFile}\"",
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
        /// 读取一个业务 Proto 的命名空间和网络角色定义。
        /// </summary>
        private static ProtoDefinition ReadDefinition(string protoFile)
        {
            string content = File.ReadAllText(protoFile);
            Match namespaceMatch = NamespaceRegex.Match(content);
            if (!namespaceMatch.Success)
            {
                throw new InvalidOperationException($"业务 Proto 必须声明 csharp_namespace：{ToProjectPath(protoFile)}。");
            }

            var definition = new ProtoDefinition(protoFile, namespaceMatch.Groups[1].Value, ResolveScope(protoFile));
            MatchCollection matches = MessageRegex.Matches(content);
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

                definition.Messages.Add(new MessageDefinition(definition.Namespace, name, role));
            }

            return definition;
        }

        /// <summary>
        /// 根据 Control/Business 下的方向目录确定协议作用域。
        /// </summary>
        /// <param name="protoFile">Proto 文件完整路径。</param>
        /// <returns>目标协议作用域。</returns>
        private static ProtocolScope ResolveScope(string protoFile)
        {
            string relative = ToProjectPath(protoFile).Replace('\\', '/');
            if (relative.StartsWith(ControlProtoDirectory + "/Inner/", StringComparison.Ordinal)
                || relative.StartsWith(BusinessProtoDirectory + "/Inner/", StringComparison.Ordinal))
            {
                return ProtocolScope.Inner;
            }

            if (relative.StartsWith(ControlProtoDirectory + "/Common/", StringComparison.Ordinal)
                || relative.StartsWith(BusinessProtoDirectory + "/Common/", StringComparison.Ordinal))
            {
                return ProtocolScope.Common;
            }

            if (!relative.StartsWith(ControlProtoDirectory + "/Outer/", StringComparison.Ordinal)
                && !relative.StartsWith(BusinessProtoDirectory + "/Outer/", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Proto 必须位于 Control/Business 的 Common、Outer 或 Inner 目录：{relative}。");
            }

            return ProtocolScope.Outer;
        }

        /// <summary>
        /// 取得协议作用域对应的代码输出目录。
        /// </summary>
        private static string GetScopeOutputDirectory(string outputRoot, ProtocolScope scope)
        {
            return Path.Combine(outputRoot, scope.ToString());
        }

        /// <summary>
        /// 为全部网络消息复用或分配稳定 Opcode。
        /// </summary>
        private static void AssignOpcodes(IReadOnlyList<ProtoDefinition> definitions, OpcodeManifest manifest)
        {
            if (manifest.NormalStartOpcode >= manifest.RpcStartOpcode)
            {
                throw new InvalidOperationException("Opcode 稳定清单的普通消息起点必须小于 RPC 起点。");
            }

            var occupied = new HashSet<uint>();
            var byType = new Dictionary<string, OpcodeManifestEntry>(StringComparer.Ordinal);
            for (int index = 0; index < manifest.Entries.Count; index++)
            {
                OpcodeManifestEntry entry = manifest.Entries[index];
                if (entry == null
                    || string.IsNullOrWhiteSpace(entry.TypeName)
                    || entry.Opcode < manifest.NormalStartOpcode
                    || entry.Opcode == uint.MaxValue
                    || !occupied.Add(entry.Opcode)
                    || byType.ContainsKey(entry.TypeName))
                {
                    throw new InvalidOperationException("Opcode 稳定清单存在空记录、越界编号、重复类型或重复编号。");
                }

                byType.Add(entry.TypeName, entry);
            }

            for (int definitionIndex = 0; definitionIndex < definitions.Count; definitionIndex++)
            {
                List<MessageDefinition> messages = definitions[definitionIndex].Messages;
                for (int messageIndex = 0; messageIndex < messages.Count; messageIndex++)
                {
                    MessageDefinition message = messages[messageIndex];
                    if (!byType.TryGetValue(message.TypeName, out OpcodeManifestEntry entry))
                    {
                        bool rpc = message.Role != "INormalMessage";
                        entry = new OpcodeManifestEntry
                        {
                            TypeName = message.TypeName,
                            Opcode = AllocateOpcode(manifest, occupied, rpc)
                        };
                        manifest.Entries.Add(entry);
                        byType.Add(entry.TypeName, entry);
                        occupied.Add(entry.Opcode);
                    }
                    else
                    {
                        bool expectsRpcRange = !string.Equals(message.Role, "INormalMessage", StringComparison.Ordinal);
                        bool usesRpcRange = entry.Opcode >= manifest.RpcStartOpcode;
                        if (expectsRpcRange != usesRpcRange)
                        {
                            string expectedRange = expectsRpcRange ? "RPC" : "Normal";
                            string actualRange = usesRpcRange ? "RPC" : "Normal";
                            throw new InvalidOperationException(
                                $"消息 {message.TypeName} 的网络角色要求 {expectedRange} Opcode 区间，"
                                + $"但稳定清单中的 {entry.Opcode} 位于 {actualRange} 区间。"
                                + "消息角色跨区间变更会破坏既有协议，请显式设计新消息类型。");
                        }
                    }

                    message.Opcode = entry.Opcode;
                }
            }

            manifest.Entries.Sort((left, right) => string.CompareOrdinal(left.TypeName, right.TypeName));
        }

        /// <summary>
        /// 在普通消息或 RPC 区间中分配下一个稳定编号。
        /// </summary>
        private static uint AllocateOpcode(OpcodeManifest manifest, ISet<uint> occupied, bool rpc)
        {
            uint candidate = rpc ? manifest.RpcStartOpcode : manifest.NormalStartOpcode;
            uint end = rpc ? uint.MaxValue : manifest.RpcStartOpcode;
            while (candidate < end && occupied.Contains(candidate))
            {
                candidate++;
            }

            if (candidate >= end || candidate == uint.MaxValue)
            {
                throw new InvalidOperationException(rpc ? "RPC Opcode 区间已耗尽。" : "普通消息 Opcode 区间已耗尽。");
            }

            return candidate;
        }

        /// <summary>
        /// 生成角色、分 Proto 注册入口，并按需生成业务统一注册入口。
        /// </summary>
        /// <param name="outputDirectory">目标生成根目录。</param>
        /// <param name="definitions">待写出的协议定义。</param>
        /// <param name="writeBusinessRegistrations">是否生成业务客户端和服务端统一入口。</param>
        private static void WriteGeneratedProtocolFiles(
            string outputDirectory,
            IReadOnlyList<ProtoDefinition> definitions,
            bool writeBusinessRegistrations)
        {
            var clientRegistrationTypes = new List<string>();
            var serverRegistrationTypes = new List<string>();
            for (int index = 0; index < definitions.Count; index++)
            {
                ProtoDefinition definition = definitions[index];
                string scopeOutputDirectory = GetScopeOutputDirectory(outputDirectory, definition.Scope);
                if (definition.Messages.Count == 0)
                {
                    continue;
                }

                string stem = Path.GetFileNameWithoutExtension(definition.SourcePath);
                WriteFileIfChanged(Path.Combine(scopeOutputDirectory, stem + ".ProtocolRole.g.cs"), BuildRoleContent(definition));
                WriteFileIfChanged(Path.Combine(scopeOutputDirectory, stem + ".ProtocolRegistration.g.cs"), BuildRegistrationContent(definition));
                string registrationType = $"global::{definition.Namespace}.{stem}ProtocolRegistration";
                if (definition.Scope == ProtocolScope.Inner)
                {
                    serverRegistrationTypes.Add(registrationType);
                }
                else
                {
                    clientRegistrationTypes.Add(registrationType);
                }
            }

            if (!writeBusinessRegistrations)
            {
                return;
            }

            WriteFileIfChanged(
                Path.Combine(GetScopeOutputDirectory(outputDirectory, ProtocolScope.Outer), "BusinessClientProtocolRegistration.g.cs"),
                BuildProjectRegistrationContent("BusinessClientProtocolRegistration", "业务客户端 Common 与 Outer", clientRegistrationTypes));
            WriteFileIfChanged(
                Path.Combine(GetScopeOutputDirectory(outputDirectory, ProtocolScope.Inner), "BusinessServerProtocolRegistration.g.cs"),
                BuildProjectRegistrationContent("BusinessServerProtocolRegistration", "业务服务端 Inner", serverRegistrationTypes));
        }

        /// <summary>
        /// 生成一个 Proto 的角色分部类。
        /// </summary>
        private static string BuildRoleContent(ProtoDefinition definition)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"// Auto-generated from {ToProjectPath(definition.SourcePath)}. Do not edit by hand.");
            builder.AppendLine("using MiniCore.Model;");
            builder.AppendLine();
            builder.AppendLine($"namespace {definition.Namespace}");
            builder.AppendLine("{");
            for (int index = 0; index < definition.Messages.Count; index++)
            {
                MessageDefinition message = definition.Messages[index];
                builder.AppendLine("    /// <summary>");
                builder.AppendLine($"    /// 为生成协议补充 {message.Role} 网络角色。");
                builder.AppendLine("    /// </summary>");
                builder.AppendLine($"    public sealed partial class {message.Name} : {message.Role}");
                builder.AppendLine("    {");
                if (message.Role != "INormalMessage")
                {
                    builder.AppendLine("        /// <summary>");
                    builder.AppendLine("        /// 获取或设置请求关联标识。");
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
        /// 生成一个 Proto 的无状态消息注册入口。
        /// </summary>
        private static string BuildRegistrationContent(ProtoDefinition definition)
        {
            string stem = Path.GetFileNameWithoutExtension(definition.SourcePath);
            var builder = new StringBuilder();
            builder.AppendLine($"// Auto-generated from {ToProjectPath(definition.SourcePath)}. Do not edit by hand.");
            builder.AppendLine("using MiniCore.Model;");
            builder.AppendLine("using MiniCore.Serialization;");
            builder.AppendLine();
            builder.AppendLine($"namespace {definition.Namespace}");
            builder.AppendLine("{");
            builder.AppendLine("    /// <summary>");
            builder.AppendLine($"    /// 注册 {stem} Proto 中的全部网络消息。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine($"    public static class {stem}ProtocolRegistration");
            builder.AppendLine("    {");
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 将消息、Opcode、角色和 Parser 写入协议构建器。");
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        /// <param name=\"builder\">目标协议构建器。</param>");
            builder.AppendLine("        public static void Register(NetworkProtocolBuilder builder)");
            builder.AppendLine("        {");
            for (int index = 0; index < definition.Messages.Count; index++)
            {
                MessageDefinition message = definition.Messages[index];
                string role = message.Role == "INormalMessage" ? "Normal" : message.Role == "IRpcRequest" ? "RpcRequest" : "RpcResponse";
                builder.AppendLine($"            builder.RegisterMessage<{message.Name}>({message.Opcode}u, NetworkMessageRole.{role}, new ProtobufMessageParser<{message.Name}>({message.Name}.Parser));");
            }
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        /// <summary>
        /// 生成业务协议的统一注册入口。
        /// </summary>
        /// <param name="className">生成类名。</param>
        /// <param name="description">协议范围说明。</param>
        /// <param name="registrationTypes">按稳定顺序登记的分协议注册类型。</param>
        /// <returns>完整 C# 生成源码。</returns>
        private static string BuildProjectRegistrationContent(string className, string description, IReadOnlyList<string> registrationTypes)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// Auto-generated by ProtoCodeGenerator. Do not edit by hand.");
            builder.AppendLine("using MiniCore.Model;");
            builder.AppendLine();
            builder.AppendLine("namespace MiniCore.Protocol.Generated");
            builder.AppendLine("{");
            builder.AppendLine("    /// <summary>");
            builder.AppendLine($"    /// {description} 协议的无状态统一注册入口。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine($"    public static class {className}");
            builder.AppendLine("    {");
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 按稳定顺序将全部项目消息注册到临时协议构建器。");
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        /// <param name=\"builder\">目标协议构建器。</param>");
            builder.AppendLine("        public static void Register(NetworkProtocolBuilder builder)");
            builder.AppendLine("        {");
            for (int index = 0; index < registrationTypes.Count; index++)
            {
                builder.AppendLine($"            {registrationTypes[index]}.Register(builder);");
            }
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        /// <summary>
        /// 保存本工具拥有的生成文件清单，并只清理上次清单中已失效的文件。
        /// </summary>
        /// <param name="businessOutputDirectory">业务协议生成根目录。</param>
        /// <param name="controlDefinitions">固定控制面协议定义。</param>
        /// <param name="businessDefinitions">业务协议定义。</param>
        private static void SaveOwnershipManifest(
            string businessOutputDirectory,
            IReadOnlyList<ProtoDefinition> controlDefinitions,
            IReadOnlyList<ProtoDefinition> businessDefinitions)
        {
            GeneratedFileManifest previous = File.Exists(GetFullPath(OwnershipManifestPath))
                ? JsonUtility.FromJson<GeneratedFileManifest>(File.ReadAllText(GetFullPath(OwnershipManifestPath)))
                : null;
            string normalizedBusinessOutputDirectory = NormalizeAssetPath(businessOutputDirectory);
            string normalizedControlOutputDirectory = NormalizeAssetPath(ControlOutputDirectory);
            var allDefinitions = new List<ProtoDefinition>(controlDefinitions.Count + businessDefinitions.Count);
            allDefinitions.AddRange(controlDefinitions);
            allDefinitions.AddRange(businessDefinitions);
            var current = new GeneratedFileManifest
            {
                Version = OwnershipManifestVersion,
                Generator = OwnershipManifestGenerator,
                BusinessOutputDirectory = normalizedBusinessOutputDirectory,
                ControlOutputDirectory = normalizedControlOutputDirectory,
                SourceDigest = ComputeSourceDigest(allDefinitions)
            };
            AddOwnedDefinitionFiles(current.Files, normalizedControlOutputDirectory, controlDefinitions);
            AddOwnedDefinitionFiles(current.Files, normalizedBusinessOutputDirectory, businessDefinitions);
            current.Files.Add(normalizedBusinessOutputDirectory + "/Outer/BusinessClientProtocolRegistration.g.cs");
            current.Files.Add(normalizedBusinessOutputDirectory + "/Inner/BusinessServerProtocolRegistration.g.cs");
            current.Files.Sort(StringComparer.Ordinal);

            DeleteObsoleteOwnedFiles(previous, current.Files);

            WriteFileIfChanged(GetFullPath(OwnershipManifestPath), JsonUtility.ToJson(current, true) + Environment.NewLine);
        }

        /// <summary>
        /// 将一组协议定义对应的生成文件加入所有权清单。
        /// </summary>
        /// <param name="files">待追加的所有权路径集合。</param>
        /// <param name="outputDirectory">协议生成根目录。</param>
        /// <param name="definitions">协议定义。</param>
        private static void AddOwnedDefinitionFiles(
            ICollection<string> files,
            string outputDirectory,
            IReadOnlyList<ProtoDefinition> definitions)
        {
            for (int index = 0; index < definitions.Count; index++)
            {
                ProtoDefinition definition = definitions[index];
                string stem = Path.GetFileNameWithoutExtension(definition.SourcePath);
                string scopeDirectory = outputDirectory + "/" + definition.Scope;
                files.Add(scopeDirectory + "/" + stem + ".cs");
                if (definition.Messages.Count > 0)
                {
                    files.Add(scopeDirectory + "/" + stem + ".ProtocolRole.g.cs");
                    files.Add(scopeDirectory + "/" + stem + ".ProtocolRegistration.g.cs");
                }
            }
        }

        /// <summary>
        /// 删除旧清单中不再生成且经过输出根目录、固定文件名和生成标记共同确认的文件。
        /// </summary>
        /// <param name="previous">上一轮生成器所有权清单。</param>
        /// <param name="currentFiles">本轮仍应存在的生成文件。</param>
        private static void DeleteObsoleteOwnedFiles(
            GeneratedFileManifest previous,
            IReadOnlyCollection<string> currentFiles)
        {
            if (!TryGetTrustedOwnershipRoots(previous, out string[] oldOutputDirectories))
            {
                return;
            }

            var expected = new HashSet<string>(currentFiles, StringComparer.Ordinal);
            for (int index = 0; index < previous.Files.Count; index++)
            {
                string oldPath = NormalizeAssetPath(previous.Files[index]);
                if (expected.Contains(oldPath)
                    || !TryFindOwnedRoot(oldOutputDirectories, oldPath, out string oldOutputDirectory)
                    || !IsDirectOwnedGeneratedPath(oldOutputDirectory, oldPath)
                    || !HasExpectedGeneratedMarker(oldPath))
                {
                    continue;
                }

                AssetDatabase.DeleteAsset(oldPath);
            }
        }

        /// <summary>
        /// 验证旧清单版本、生成器来源和输出目录归属，不信任清单中的任意 Assets 路径。
        /// </summary>
        /// <param name="manifest">待验证的旧清单。</param>
        /// <param name="outputDirectories">成功时返回规范化的旧输出根目录。</param>
        /// <returns>旧清单可用于安全清理时返回 true。</returns>
        private static bool TryGetTrustedOwnershipRoots(
            GeneratedFileManifest manifest,
            out string[] outputDirectories)
        {
            outputDirectories = null;
            if (manifest == null
                || !string.Equals(manifest.Generator, OwnershipManifestGenerator, StringComparison.Ordinal)
                || manifest.Files == null)
            {
                return false;
            }

            string[] candidates;
            if (manifest.Version == 2)
            {
                candidates = new[] { manifest.OutputDirectory };
            }
            else if (manifest.Version == OwnershipManifestVersion)
            {
                candidates = new[] { manifest.BusinessOutputDirectory, manifest.ControlOutputDirectory };
            }
            else
            {
                return false;
            }

            var trusted = new List<string>(candidates.Length);
            for (int index = 0; index < candidates.Length; index++)
            {
                string normalized = NormalizeAssetPath(candidates[index]);
                if (!IsTrustedProtocolOutputDirectory(normalized))
                {
                    return false;
                }

                trusted.Add(normalized);
            }

            outputDirectories = trusted.ToArray();
            return outputDirectories.Length > 0;
        }

        /// <summary>
        /// 判断协议生成根目录路径和程序集边界均可信。
        /// </summary>
        /// <param name="outputDirectory">待验证的 Assets 相对目录。</param>
        /// <returns>路径安全且属于已知协议程序集时返回 true。</returns>
        private static bool IsTrustedProtocolOutputDirectory(string outputDirectory)
        {
            string normalized = NormalizeAssetPath(outputDirectory);
            if (string.IsNullOrWhiteSpace(normalized)
                || Path.IsPathRooted(normalized)
                || normalized.IndexOf("..", StringComparison.Ordinal) >= 0
                || !normalized.StartsWith("Assets/", StringComparison.Ordinal)
                || !IsProtocolOutputDirectory(normalized))
            {
                return false;
            }

            string assetsRoot = GetFullPath("Assets").TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string outputFullPath = GetFullPath(normalized).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!outputFullPath.StartsWith(assetsRoot, StringComparison.Ordinal)
                || ContainsSymbolicLink(outputFullPath, assetsRoot))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 判断生成根目录是否属于业务协议或固定控制面协议程序集。
        /// </summary>
        /// <param name="outputDirectory">待检查的 Assets 相对目录。</param>
        /// <returns>三个作用域的程序集边界均匹配时返回 true。</returns>
        private static bool IsProtocolOutputDirectory(string outputDirectory)
        {
            bool control = string.Equals(NormalizeAssetPath(outputDirectory), NormalizeAssetPath(ControlOutputDirectory), StringComparison.Ordinal);
            return IsScopeAssembly(outputDirectory, ProtocolScope.Common, control ? ProtocolControlAssemblyName : ProtocolCommonAssemblyName)
                && IsScopeAssembly(outputDirectory, ProtocolScope.Outer, control ? ProtocolControlAssemblyName : ProtocolOuterAssemblyName)
                && IsScopeAssembly(outputDirectory, ProtocolScope.Inner, control ? ProtocolControlInnerAssemblyName : ProtocolInnerAssemblyName);
        }

        /// <summary>
        /// 判断协议作用域目录是否属于指定程序集。
        /// </summary>
        /// <param name="outputRoot">协议生成根目录。</param>
        /// <param name="scope">协议作用域。</param>
        /// <param name="expectedAssemblyName">期望程序集名称。</param>
        /// <returns>程序集名称匹配时返回 true。</returns>
        private static bool IsScopeAssembly(string outputRoot, ProtocolScope scope, string expectedAssemblyName)
        {
            string scopeDirectory = NormalizeAssetPath(outputRoot) + "/" + scope;
            string assemblyDefinitionPath = FindNearestAssemblyDefinition(scopeDirectory);
            return TryReadAssemblyDefinitionName(assemblyDefinitionPath, out string assemblyName)
                && string.Equals(assemblyName, expectedAssemblyName, StringComparison.Ordinal);
        }

        /// <summary>
        /// 查找生成文件所属的可信生成根目录。
        /// </summary>
        /// <param name="outputDirectories">可信生成根目录。</param>
        /// <param name="assetPath">待检查生成文件。</param>
        /// <param name="outputDirectory">匹配的生成根目录。</param>
        /// <returns>文件位于其中一个生成根目录时返回 true。</returns>
        private static bool TryFindOwnedRoot(
            IReadOnlyList<string> outputDirectories,
            string assetPath,
            out string outputDirectory)
        {
            for (int index = 0; index < outputDirectories.Count; index++)
            {
                string candidate = outputDirectories[index];
                if (assetPath.StartsWith(candidate + "/", StringComparison.Ordinal))
                {
                    outputDirectory = candidate;
                    return true;
                }
            }

            outputDirectory = null;
            return false;
        }

        /// <summary>
        /// 判断旧文件是否为输出根目录中的固定协议生成文件，不允许清理子目录或任意文件名。
        /// </summary>
        /// <param name="outputDirectory">已经验证的旧输出根目录。</param>
        /// <param name="assetPath">旧清单记录的文件路径。</param>
        /// <returns>路径是允许清理的根目录直属生成文件时返回 true。</returns>
        private static bool IsDirectOwnedGeneratedPath(string outputDirectory, string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath)
                || Path.IsPathRooted(assetPath)
                || assetPath.IndexOf("..", StringComparison.Ordinal) >= 0
                || !IsOwnedScopeDirectory(outputDirectory, NormalizeAssetPath(Path.GetDirectoryName(assetPath))))
            {
                return false;
            }

            string fileName = Path.GetFileName(assetPath);
            return string.Equals(fileName, "ProjectProtocolRegistration.g.cs", StringComparison.Ordinal)
                || string.Equals(fileName, "ServerProjectProtocolRegistration.g.cs", StringComparison.Ordinal)
                || string.Equals(fileName, "BusinessClientProtocolRegistration.g.cs", StringComparison.Ordinal)
                || string.Equals(fileName, "BusinessServerProtocolRegistration.g.cs", StringComparison.Ordinal)
                || fileName.EndsWith(".ProtocolRole.g.cs", StringComparison.Ordinal)
                || fileName.EndsWith(".ProtocolRegistration.g.cs", StringComparison.Ordinal)
                || (fileName.EndsWith(".cs", StringComparison.Ordinal)
                    && !fileName.EndsWith(".g.cs", StringComparison.Ordinal));
        }

        /// <summary>
        /// 判断生成文件目录是旧版根目录或固定 Common、Outer、Inner 子目录。
        /// </summary>
        private static bool IsOwnedScopeDirectory(string outputDirectory, string candidate)
        {
            return string.Equals(candidate, outputDirectory, StringComparison.Ordinal)
                || string.Equals(candidate, outputDirectory + "/Common", StringComparison.Ordinal)
                || string.Equals(candidate, outputDirectory + "/Outer", StringComparison.Ordinal)
                || string.Equals(candidate, outputDirectory + "/Inner", StringComparison.Ordinal);
        }

        /// <summary>
        /// 检查待清理文件是否带有对应工具生成的固定头部标记。
        /// </summary>
        /// <param name="assetPath">已经通过路径和文件名校验的资源路径。</param>
        /// <returns>文件存在且生成标记与文件类别匹配时返回 true。</returns>
        private static bool HasExpectedGeneratedMarker(string assetPath)
        {
            string fullPath = GetFullPath(assetPath);
            if (!File.Exists(fullPath))
            {
                return false;
            }

            string firstLine;
            string secondLine;
            using (var reader = new StreamReader(fullPath, Encoding.UTF8, true, 256))
            {
                firstLine = reader.ReadLine();
                secondLine = reader.ReadLine();
            }

            string fileName = Path.GetFileName(assetPath);
            if (string.Equals(fileName, "ProjectProtocolRegistration.g.cs", StringComparison.Ordinal)
                || string.Equals(fileName, "ServerProjectProtocolRegistration.g.cs", StringComparison.Ordinal)
                || string.Equals(fileName, "BusinessClientProtocolRegistration.g.cs", StringComparison.Ordinal)
                || string.Equals(fileName, "BusinessServerProtocolRegistration.g.cs", StringComparison.Ordinal))
            {
                return string.Equals(
                    firstLine,
                    "// Auto-generated by ProtoCodeGenerator. Do not edit by hand.",
                    StringComparison.Ordinal);
            }

            if (fileName.EndsWith(".ProtocolRole.g.cs", StringComparison.Ordinal)
                || fileName.EndsWith(".ProtocolRegistration.g.cs", StringComparison.Ordinal))
            {
                return firstLine != null
                    && firstLine.StartsWith("// Auto-generated from ", StringComparison.Ordinal)
                    && firstLine.EndsWith(". Do not edit by hand.", StringComparison.Ordinal);
            }

            return string.Equals(firstLine, "// <auto-generated>", StringComparison.Ordinal)
                && secondLine != null
                && secondLine.IndexOf("Generated by the protocol buffer compiler", StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// 计算本轮业务 Proto 路径与内容的稳定摘要，便于识别清单来源。
        /// </summary>
        /// <param name="definitions">已经按路径稳定排序的业务 Proto 定义。</param>
        /// <returns>小写十六进制 SHA-256 摘要。</returns>
        private static string ComputeSourceDigest(IReadOnlyList<ProtoDefinition> definitions)
        {
            var source = new StringBuilder();
            for (int index = 0; index < definitions.Count; index++)
            {
                source.Append(ToProjectPath(definitions[index].SourcePath));
                source.Append('\n');
                source.Append(File.ReadAllText(definitions[index].SourcePath));
                source.Append('\n');
            }

            byte[] bytes = Utf8WithoutBom.GetBytes(source.ToString());
            byte[] digest;
            using (SHA256 sha256 = SHA256.Create())
            {
                digest = sha256.ComputeHash(bytes);
            }

            var result = new StringBuilder(digest.Length * 2);
            for (int index = 0; index < digest.Length; index++)
            {
                result.Append(digest[index].ToString("x2"));
            }

            return result.ToString();
        }

        /// <summary>
        /// 将资源路径统一为正斜杠并移除尾部分隔符。
        /// </summary>
        /// <param name="path">待规范化的项目路径。</param>
        /// <returns>规范化路径；输入为空时返回空。</returns>
        private static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? null
                : path.Replace('\\', '/').TrimEnd('/');
        }

        /// <summary>
        /// 读取稳定 Opcode 清单。
        /// </summary>
        private static OpcodeManifest LoadOpcodeManifest()
        {
            OpcodeManifest manifest = File.Exists(GetFullPath(OpcodeManifestPath))
                ? JsonUtility.FromJson<OpcodeManifest>(File.ReadAllText(GetFullPath(OpcodeManifestPath)))
                : new OpcodeManifest();
            manifest ??= new OpcodeManifest();
            manifest.NormalStartOpcode = manifest.NormalStartOpcode == 0 ? NormalStartOpcode : manifest.NormalStartOpcode;
            manifest.RpcStartOpcode = manifest.RpcStartOpcode == 0 ? RpcStartOpcode : manifest.RpcStartOpcode;
            manifest.Entries ??= new List<OpcodeManifestEntry>();
            return manifest;
        }

        /// <summary>
        /// 保存稳定 Opcode 清单。
        /// </summary>
        private static void SaveOpcodeManifest(OpcodeManifest manifest)
        {
            WriteFileIfChanged(GetFullPath(OpcodeManifestPath), JsonUtility.ToJson(manifest, true) + Environment.NewLine);
        }

        /// <summary>
        /// 获取当前平台的仓库内置 protoc。
        /// </summary>
        private static string GetProtocPath()
        {
#if UNITY_EDITOR_WIN
            const string relativePath = ProtocToolRoot + "/windows-x64/protoc.exe";
#elif UNITY_EDITOR_OSX
            string relativePath = RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                ? ProtocToolRoot + "/macos-arm64/protoc"
                : ProtocToolRoot + "/macos-x64/protoc";
#else
            throw new PlatformNotSupportedException("当前内置 protoc 仅支持 Windows x64 与 macOS。");
#endif
            string path = GetFullPath(relativePath);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"缺少仓库内置 protoc：{relativePath}", path);
            }
            return path;
        }

        /// <summary>
        /// 校验仓库 protoc 版本。
        /// </summary>
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
        /// 统计网络消息数量。
        /// </summary>
        private static int CountMessages(IReadOnlyList<ProtoDefinition> definitions)
        {
            int count = 0;
            for (int index = 0; index < definitions.Count; index++)
            {
                count += definitions[index].Messages.Count;
            }
            return count;
        }

        /// <summary>
        /// 内容变化时以 UTF-8 无 BOM 写入文件。
        /// </summary>
        private static void WriteFileIfChanged(string path, string content)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
            if (!File.Exists(path) || !string.Equals(File.ReadAllText(path), content, StringComparison.Ordinal))
            {
                File.WriteAllText(path, content, Utf8WithoutBom);
            }
        }

        /// <summary>
        /// 将项目相对路径转换为完整路径。
        /// </summary>
        private static string GetFullPath(string path)
        {
            return Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
        }

        /// <summary>
        /// 将完整路径转换为项目相对路径。
        /// </summary>
        private static string ToProjectPath(string path)
        {
            string root = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string fullPath = Path.GetFullPath(path);
            return fullPath.StartsWith(root, StringComparison.Ordinal) ? fullPath.Substring(root.Length).Replace('\\', '/') : fullPath;
        }

        [Serializable]
        private sealed class GeneratedFileManifest
        {
            public int Version;
            public string Generator;
            public string BusinessOutputDirectory;
            public string ControlOutputDirectory;
            public string OutputDirectory;
            public string SourceDigest;
            public List<string> Files = new List<string>();
        }

        [Serializable]
        private sealed class OpcodeManifest
        {
            public uint NormalStartOpcode;
            public uint RpcStartOpcode;
            public List<OpcodeManifestEntry> Entries = new List<OpcodeManifestEntry>();
        }

        [Serializable]
        private sealed class OpcodeManifestEntry
        {
            public string TypeName;
            public uint Opcode;
        }

        [Serializable]
        private sealed class AssemblyDefinitionData
        {
            [SerializeField] private string name; // Unity asmdef 的稳定程序集名称。

            /// <summary>
            /// 获取程序集名称。
            /// </summary>
            internal string Name => name;
        }

        private enum ProtocolScope
        {
            Common,
            Outer,
            Inner
        }

        private sealed class ProtoDefinition
        {
            public string SourcePath { get; }
            public string Namespace { get; }
            public ProtocolScope Scope { get; }
            public List<MessageDefinition> Messages { get; } = new List<MessageDefinition>();

            public ProtoDefinition(string sourcePath, string namespaceName, ProtocolScope scope)
            {
                SourcePath = sourcePath;
                Namespace = namespaceName;
                Scope = scope;
            }
        }

        private sealed class MessageDefinition
        {
            public string Name { get; }
            public string Role { get; }
            public string TypeName { get; }
            public uint Opcode { get; set; }

            public MessageDefinition(string namespaceName, string name, string role)
            {
                Name = name;
                Role = role;
                TypeName = namespaceName + "." + name;
            }
        }

        #endregion
    }
}
