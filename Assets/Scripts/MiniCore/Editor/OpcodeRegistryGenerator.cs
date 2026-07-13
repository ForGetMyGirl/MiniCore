using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MiniCore.Model;
using UnityEditor;
using UnityEngine;

namespace MiniCore.EditorTools
{
    internal static class OpcodeRegistryGenerator
    {
        #region Private 私有成员

        private const string AssemblyKeyword = "HotUpdate";
        private const string OutputPath = "Assets/Scripts/MiniCore/Model/Generated/OpcodeRegistry.Generated.cs";
        private const string ManifestPath = "Assets/Scripts/MiniCore/Model/Generated/OpcodeManifest.json";
        private const uint NormalStartOpcode = 100001;
        private const uint RpcStartOpcode = 200001;
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false); // 生成文件的固定编码。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 执行 Synchronize 相关处理。
        /// </summary>
        /// <param name="refreshAssets">执行该方法所需的 refreshAssets 参数。</param>
        /// <param name="log">执行该方法所需的 log 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        internal static bool Synchronize(bool refreshAssets, out string log)
        {
            var logBuilder = new StringBuilder();
            try
            {
                Assembly assembly = FindAssembly();
                if (assembly == null)
                {
                    log = $"未找到包含 {AssemblyKeyword} 的程序集，跳过 opcode 自动同步。";
                    return false;
                }

                OpcodeManifest manifest = LoadManifest(assembly, out bool createdManifest);
                List<HandlerBinding> bindings = BuildBindings(assembly, manifest, true, logBuilder, out Dictionary<Type, uint> messageOpcodes);
                string manifestContent = JsonUtility.ToJson(manifest, true) + Environment.NewLine;
                string generatedContent = BuildGeneratedContent(bindings, messageOpcodes, manifest);

                bool manifestWritten = WriteFileIfChanged(ManifestPath, manifestContent);
                bool manifestChanged = createdManifest || manifestWritten;
                bool generatedChanged = WriteFileIfChanged(OutputPath, generatedContent);
                if (refreshAssets && (manifestChanged || generatedChanged))
                {
                    AssetDatabase.Refresh();
                }

                logBuilder.AppendLine($"稳定清单: {manifest.Entries.Count} 条（含已删除协议保留项）。");
                logBuilder.AppendLine($"当前绑定: {bindings.Count} 个处理器，{messageOpcodes.Count} 个协议。");
                logBuilder.AppendLine(manifestChanged || generatedChanged ? "已同步 opcode 清单和生成映射。" : "opcode 清单和生成映射无需更新。");
                log = logBuilder.ToString();
                return true;
            }
            catch (Exception exception)
            {
                logBuilder.AppendLine($"Opcode 同步失败: {exception.Message}");
                log = logBuilder.ToString();
                return false;
            }
        }

        /// <summary>
        /// 执行 Validate 相关处理。
        /// </summary>
        /// <param name="error">执行该方法所需的 error 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        internal static bool Validate(out string error)
        {
            try
            {
                Assembly assembly = FindAssembly();
                if (assembly == null)
                {
                    error = $"未找到包含 {AssemblyKeyword} 的程序集，无法校验 opcode。";
                    return false;
                }

                if (!File.Exists(GetFullPath(ManifestPath)))
                {
                    error = $"缺少 opcode 稳定清单: {ManifestPath}";
                    return false;
                }

                OpcodeManifest manifest = LoadManifest(assembly, out _);
                var logBuilder = new StringBuilder();
                List<HandlerBinding> bindings = BuildBindings(assembly, manifest, false, logBuilder, out Dictionary<Type, uint> messageOpcodes);
                string expectedGeneratedContent = BuildGeneratedContent(bindings, messageOpcodes, manifest);
                string outputFullPath = GetFullPath(OutputPath);
                if (!File.Exists(outputFullPath))
                {
                    error = $"缺少 opcode 生成文件: {OutputPath}";
                    return false;
                }

                string actualGeneratedContent = File.ReadAllText(outputFullPath, Utf8WithoutBom);
                if (!string.Equals(actualGeneratedContent, expectedGeneratedContent, StringComparison.Ordinal))
                {
                    error = "Opcode 生成文件与稳定清单不一致，请等待编辑器自动同步或执行 MiniCore/Opcode/Generate (HotUpdate)。";
                    return false;
                }

                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = $"Opcode 校验失败: {exception.Message}";
                return false;
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 执行 FindAssembly 相关处理。
        /// </summary>
        /// <returns>执行处理后的结果。</returns>
        private static Assembly FindAssembly()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name.IndexOf(AssemblyKeyword, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// 执行 LoadManifest 相关处理。
        /// </summary>
        /// <param name="assembly">执行该方法所需的 assembly 参数。</param>
        /// <param name="createdManifest">执行该方法所需的 createdManifest 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private static OpcodeManifest LoadManifest(Assembly assembly, out bool createdManifest)
        {
            string manifestFullPath = GetFullPath(ManifestPath);
            createdManifest = !File.Exists(manifestFullPath);
            OpcodeManifest manifest;
            if (createdManifest)
            {
                manifest = new OpcodeManifest
                {
                    NormalStartOpcode = NormalStartOpcode,
                    RpcStartOpcode = RpcStartOpcode
                };
                SeedLegacyMappings(assembly, manifest);
            }
            else
            {
                manifest = JsonUtility.FromJson<OpcodeManifest>(File.ReadAllText(manifestFullPath, Utf8WithoutBom));
                if (manifest == null)
                {
                    throw new InvalidOperationException($"无法读取 opcode 稳定清单: {ManifestPath}");
                }
            }

            manifest.EnsureDefaults();
            ValidateManifest(manifest);
            return manifest;
        }

        /// <summary>
        /// 执行 SeedLegacyMappings 相关处理。
        /// </summary>
        /// <param name="assembly">执行该方法所需的 assembly 参数。</param>
        /// <param name="manifest">执行该方法所需的 manifest 参数。</param>
        private static void SeedLegacyMappings(Assembly assembly, OpcodeManifest manifest)
        {
            foreach (Type protocolType in GetProtocolTypes(assembly))
            {
                if (OpcodeRegistry.TryGetOpcodeByMessage(protocolType, out uint opcode))
                {
                    manifest.Entries.Add(new OpcodeManifestEntry
                    {
                        TypeName = protocolType.FullName,
                        Opcode = opcode
                    });
                }
            }
        }

        /// <summary>
        /// 执行 BuildBindings 相关处理。
        /// </summary>
        /// <param name="assembly">执行该方法所需的 assembly 参数。</param>
        /// <param name="manifest">执行该方法所需的 manifest 参数。</param>
        /// <param name="allowAllocate">执行该方法所需的 allowAllocate 参数。</param>
        /// <param name="logBuilder">执行该方法所需的 logBuilder 参数。</param>
        /// <param name="messageOpcodes">执行该方法所需的 messageOpcodes 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private static List<HandlerBinding> BuildBindings(
            Assembly assembly,
            OpcodeManifest manifest,
            bool allowAllocate,
            StringBuilder logBuilder,
            out Dictionary<Type, uint> messageOpcodes)
        {
            List<HandlerBinding> bindings = DiscoverBindings(assembly);
            ValidateBindings(bindings);

            messageOpcodes = new Dictionary<Type, uint>();
            foreach (HandlerBinding binding in bindings)
            {
                binding.Opcode = GetOrAllocateOpcode(binding.RequestType, binding.IsRpc, manifest, messageOpcodes, allowAllocate);
                if (binding.IsRpc)
                {
                    GetOrAllocateOpcode(binding.ResponseType, true, manifest, messageOpcodes, allowAllocate);
                    logBuilder.AppendLine($"RPC: {binding.Opcode} -> {binding.HandlerType.FullName}");
                }
                else
                {
                    logBuilder.AppendLine($"Normal: {binding.Opcode} -> {binding.HandlerType.FullName}");
                }
            }

            foreach (Type protocolType in GetProtocolTypes(assembly))
            {
                if (messageOpcodes.ContainsKey(protocolType))
                {
                    continue;
                }

                bool isRpcProtocol = typeof(IRequest).IsAssignableFrom(protocolType) || typeof(IResponse).IsAssignableFrom(protocolType);
                GetOrAllocateOpcode(protocolType, isRpcProtocol, manifest, messageOpcodes, allowAllocate);
            }

            manifest.Entries.Sort((left, right) => string.CompareOrdinal(left.TypeName, right.TypeName));
            return bindings;
        }

        /// <summary>
        /// 执行 DiscoverBindings 相关处理。
        /// </summary>
        /// <param name="assembly">执行该方法所需的 assembly 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private static List<HandlerBinding> DiscoverBindings(Assembly assembly)
        {
            var bindings = new List<HandlerBinding>();
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                Type normalBase = FindGenericBase(type, typeof(AMHandler<>));
                if (normalBase != null)
                {
                    bindings.Add(new HandlerBinding
                    {
                        HandlerType = type,
                        RequestType = normalBase.GetGenericArguments()[0],
                        IsRpc = false
                    });
                    continue;
                }

                Type rpcBase = FindGenericBase(type, typeof(ARpcHandler<,>));
                if (rpcBase != null)
                {
                    Type[] arguments = rpcBase.GetGenericArguments();
                    bindings.Add(new HandlerBinding
                    {
                        HandlerType = type,
                        RequestType = arguments[0],
                        ResponseType = arguments[1],
                        IsRpc = true
                    });
                }
            }

            return bindings;
        }

        /// <summary>
        /// 执行 ValidateBindings 相关处理。
        /// </summary>
        /// <param name="bindings">执行该方法所需的 bindings 参数。</param>
        private static void ValidateBindings(List<HandlerBinding> bindings)
        {
            var requestOwners = new Dictionary<Type, HandlerBinding>();
            foreach (HandlerBinding binding in bindings)
            {
                if (!typeof(IProtocol).IsAssignableFrom(binding.RequestType))
                {
                    throw new InvalidOperationException($"处理器请求类型未实现 IProtocol: {binding.HandlerType.FullName}");
                }

                if (binding.IsRpc)
                {
                    if (!typeof(IRequest).IsAssignableFrom(binding.RequestType) || !typeof(IResponse).IsAssignableFrom(binding.ResponseType))
                    {
                        throw new InvalidOperationException($"RPC处理器泛型类型无效: {binding.HandlerType.FullName}");
                    }
                }

                if (requestOwners.TryGetValue(binding.RequestType, out HandlerBinding existing))
                {
                    throw new InvalidOperationException($"请求协议存在多个处理器: {binding.RequestType.FullName} -> {existing.HandlerType.FullName} / {binding.HandlerType.FullName}");
                }

                requestOwners.Add(binding.RequestType, binding);
            }
        }

        /// <summary>
        /// 执行 GetOrAllocateOpcode 相关处理。
        /// </summary>
        /// <param name="protocolType">执行该方法所需的 protocolType 参数。</param>
        /// <param name="isRpc">执行该方法所需的 isRpc 参数。</param>
        /// <param name="manifest">执行该方法所需的 manifest 参数。</param>
        /// <param name="messageOpcodes">执行该方法所需的 messageOpcodes 参数。</param>
        /// <param name="allowAllocate">执行该方法所需的 allowAllocate 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private static uint GetOrAllocateOpcode(
            Type protocolType,
            bool isRpc,
            OpcodeManifest manifest,
            Dictionary<Type, uint> messageOpcodes,
            bool allowAllocate)
        {
            if (messageOpcodes.TryGetValue(protocolType, out uint existing))
            {
                return existing;
            }

            OpcodeManifestEntry entry = manifest.Entries.FirstOrDefault(item => item.TypeName == protocolType.FullName);
            if (entry == null)
            {
                if (!allowAllocate)
                {
                    throw new InvalidOperationException($"协议未登记到稳定 opcode 清单: {protocolType.FullName}");
                }

                entry = new OpcodeManifestEntry
                {
                    TypeName = protocolType.FullName,
                    Opcode = AllocateNextOpcode(manifest, isRpc)
                };
                manifest.Entries.Add(entry);
            }

            messageOpcodes.Add(protocolType, entry.Opcode);
            return entry.Opcode;
        }

        /// <summary>
        /// 执行 AllocateNextOpcode 相关处理。
        /// </summary>
        /// <param name="manifest">执行该方法所需的 manifest 参数。</param>
        /// <param name="isRpc">执行该方法所需的 isRpc 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private static uint AllocateNextOpcode(OpcodeManifest manifest, bool isRpc)
        {
            uint rangeStart = isRpc ? manifest.RpcStartOpcode : manifest.NormalStartOpcode;
            uint rangeEndExclusive = isRpc ? uint.MaxValue : manifest.RpcStartOpcode;
            uint maxOpcode = rangeStart - 1;
            var occupiedOpcodes = new HashSet<uint>();
            foreach (OpcodeManifestEntry entry in manifest.Entries)
            {
                occupiedOpcodes.Add(entry.Opcode);
                if (entry.Opcode >= rangeStart && entry.Opcode < rangeEndExclusive && entry.Opcode > maxOpcode)
                {
                    maxOpcode = entry.Opcode;
                }
            }

            if (maxOpcode == uint.MaxValue)
            {
                throw new InvalidOperationException("opcode 可分配范围已耗尽。");
            }

            uint candidate = maxOpcode + 1;
            while (occupiedOpcodes.Contains(candidate))
            {
                if (candidate == uint.MaxValue)
                {
                    throw new InvalidOperationException("opcode 可分配范围已耗尽。");
                }

                candidate++;
            }

            if (!isRpc && candidate >= manifest.RpcStartOpcode)
            {
                throw new InvalidOperationException("普通消息 opcode 范围已耗尽，请调整稳定清单中的 RPC 起始 opcode。");
            }

            return candidate;
        }

        /// <summary>
        /// 执行 ValidateManifest 相关处理。
        /// </summary>
        /// <param name="manifest">执行该方法所需的 manifest 参数。</param>
        private static void ValidateManifest(OpcodeManifest manifest)
        {
            if (manifest.NormalStartOpcode == 0 || manifest.RpcStartOpcode <= manifest.NormalStartOpcode)
            {
                throw new InvalidOperationException("opcode 稳定清单的起始范围无效。");
            }

            var typeNames = new HashSet<string>();
            var opcodes = new HashSet<uint>();
            foreach (OpcodeManifestEntry entry in manifest.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.TypeName) || entry.Opcode == 0)
                {
                    throw new InvalidOperationException("opcode 稳定清单存在空类型名或零 opcode。");
                }

                if (!typeNames.Add(entry.TypeName))
                {
                    throw new InvalidOperationException($"opcode 稳定清单存在重复类型: {entry.TypeName}");
                }

                if (!opcodes.Add(entry.Opcode))
                {
                    throw new InvalidOperationException($"opcode 稳定清单存在重复 opcode: {entry.Opcode}");
                }
            }
        }

        /// <summary>
        /// 执行 GetProtocolTypes 相关处理。
        /// </summary>
        /// <param name="assembly">执行该方法所需的 assembly 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private static IEnumerable<Type> GetProtocolTypes(Assembly assembly)
        {
            return assembly.GetTypes()
                .Where(type => typeof(IProtocol).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                .OrderBy(type => type.FullName);
        }

        /// <summary>
        /// 执行 FindGenericBase 相关处理。
        /// </summary>
        /// <param name="type">执行该方法所需的 type 参数。</param>
        /// <param name="genericDefinition">执行该方法所需的 genericDefinition 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private static Type FindGenericBase(Type type, Type genericDefinition)
        {
            Type baseType = type.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == genericDefinition)
                {
                    return baseType;
                }

                baseType = baseType.BaseType;
            }

            return null;
        }

        /// <summary>
        /// 执行 BuildGeneratedContent 相关处理。
        /// </summary>
        /// <param name="bindings">执行该方法所需的 bindings 参数。</param>
        /// <param name="messageOpcodes">执行该方法所需的 messageOpcodes 参数。</param>
        /// <param name="manifest">执行该方法所需的 manifest 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private static string BuildGeneratedContent(List<HandlerBinding> bindings, Dictionary<Type, uint> messageOpcodes, OpcodeManifest manifest)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// Auto-generated by OpcodeRegistryGenerator. Do not modify by hand.");
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine();
            builder.AppendLine("namespace MiniCore.Model");
            builder.AppendLine("{");
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 自动生成的协议号注册表扩展。");
            builder.AppendLine("    /// 由 OpcodeRegistryGenerator 根据稳定清单维护，禁止手动修改。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    public static partial class OpcodeRegistry");
            builder.AppendLine("    {");

            foreach (KeyValuePair<Type, uint> pair in messageOpcodes.OrderBy(item => item.Value).ThenBy(item => item.Key.FullName))
            {
                builder.AppendLine("        /// <summary>");
                builder.AppendLine($"        /// {pair.Key.FullName} 的稳定协议号。");
                builder.AppendLine("        /// </summary>");
                builder.AppendLine($"        public const uint {BuildConstName(pair.Key)} = {pair.Value}u;");
            }

            foreach (OpcodeManifestEntry entry in manifest.Entries.Where(item => !messageOpcodes.Keys.Any(type => type.FullName == item.TypeName)).OrderBy(item => item.Opcode))
            {
                builder.AppendLine($"        // 已删除协议保留 opcode:{entry.Opcode} type:{entry.TypeName}");
            }

            builder.AppendLine();
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 注册自动生成的处理器和协议类型映射。");
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        /// <param name=\"handlerToOpcode\">处理器类型到协议号的映射表。</param>");
            builder.AppendLine("        /// <param name=\"opcodeToHandler\">协议号到处理器元数据的映射表。</param>");
            builder.AppendLine("        /// <param name=\"messageToOpcode\">协议类型到协议号的映射表。</param>");
            builder.AppendLine("        static partial void RegisterGenerated(Dictionary<string, uint> handlerToOpcode, Dictionary<uint, HandlerInfo> opcodeToHandler, Dictionary<string, uint> messageToOpcode)");
            builder.AppendLine("        {");

            foreach (HandlerBinding binding in bindings.OrderBy(item => item.Opcode))
            {
                builder.AppendLine($"            handlerToOpcode[\"{binding.HandlerType.FullName}\"] = {binding.Opcode};");
            }

            foreach (HandlerBinding binding in bindings.OrderBy(item => item.Opcode))
            {
                string responseType = binding.ResponseType == null ? string.Empty : binding.ResponseType.FullName;
                builder.AppendLine($"            opcodeToHandler[{binding.Opcode}] = new HandlerInfo {{ HandlerType = \"{binding.HandlerType.FullName}\", RequestType = \"{binding.RequestType.FullName}\", ResponseType = \"{responseType}\", IsRpc = {binding.IsRpc.ToString().ToLowerInvariant()} }};");
            }

            foreach (KeyValuePair<Type, uint> pair in messageOpcodes.OrderBy(item => item.Value).ThenBy(item => item.Key.FullName))
            {
                builder.AppendLine($"            messageToOpcode[\"{pair.Key.FullName}\"] = {pair.Value};");
            }

            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        /// <summary>
        /// 执行 BuildConstName 相关处理。
        /// </summary>
        /// <param name="type">执行该方法所需的 type 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private static string BuildConstName(Type type)
        {
            string name = type.Name;
            int tickIndex = name.IndexOf('`');
            if (tickIndex >= 0)
            {
                name = name.Substring(0, tickIndex);
            }

            return name.Replace('+', '_');
        }

        /// <summary>
        /// 执行 WriteFileIfChanged 相关处理。
        /// </summary>
        /// <param name="path">执行该方法所需的 path 参数。</param>
        /// <param name="content">执行该方法所需的 content 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private static bool WriteFileIfChanged(string path, string content)
        {
            string fullPath = GetFullPath(path);
            string directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(fullPath) && string.Equals(File.ReadAllText(fullPath, Utf8WithoutBom), content, StringComparison.Ordinal))
            {
                return false;
            }

            File.WriteAllText(fullPath, content, Utf8WithoutBom);
            return true;
        }

        /// <summary>
        /// 执行 GetFullPath 相关处理。
        /// </summary>
        /// <param name="path">执行该方法所需的 path 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private static string GetFullPath(string path)
        {
            return Path.IsPathRooted(path) ? path : Path.GetFullPath(path, Directory.GetCurrentDirectory());
        }

        [Serializable]
        private sealed class OpcodeManifest
        {
            /// <summary>
            /// 网络模块公开成员 NormalStartOpcode 的说明。
            /// </summary>
            public uint NormalStartOpcode;
            public uint RpcStartOpcode;
            public List<OpcodeManifestEntry> Entries = new List<OpcodeManifestEntry>();

            /// <summary>
            /// 执行 EnsureDefaults 相关处理。
            /// </summary>
            public void EnsureDefaults()
            {
                if (NormalStartOpcode == 0)
                {
                    NormalStartOpcode = OpcodeRegistryGenerator.NormalStartOpcode;
                }

                if (RpcStartOpcode == 0)
                {
                    RpcStartOpcode = OpcodeRegistryGenerator.RpcStartOpcode;
                }

                if (Entries == null)
                {
                    Entries = new List<OpcodeManifestEntry>();
                }
            }
        }

        [Serializable]
        private sealed class OpcodeManifestEntry
        {
            /// <summary>
            /// 网络模块公开成员 TypeName 的说明。
            /// </summary>
            public string TypeName;
            public uint Opcode;
        }

        private sealed class HandlerBinding
        {
            /// <summary>
            /// 网络模块公开成员 HandlerType 的说明。
            /// </summary>
            public Type HandlerType;
            public Type RequestType;
            public Type ResponseType;
            public uint Opcode;
            /// <summary>
            /// 网络模块公开成员 IsRpc 的说明。
            /// </summary>
            public bool IsRpc;
        }

        #endregion
    }
}
