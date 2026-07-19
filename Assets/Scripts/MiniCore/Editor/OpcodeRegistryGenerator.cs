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
        private const string OutputPath = "Assets/Scripts/MiniCore/Protocol/Generated/Registry/OpcodeRegistry.Generated.cs";
        private const string HandlerOutputPath = "Assets/Scripts/MiniCore/HotUpdate/Generated/Network/HotUpdateHandlerRegistry.Generated.cs";
        private const string ManifestPath = "Proto/Manifest/OpcodeManifest.json";
        private const uint NormalStartOpcode = 100001;
        private const uint RpcStartOpcode = 200001;
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false); // 生成文件的固定编码。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 在 HotUpdate 源码变更后的下一次编译前，将直接引用 Handler 类型的生成表替换为空表。
        /// 编译完成后会由自动同步重新生成当前 Handler 的直接注册表，避免删除或改名 Handler 时旧表阻断编译。
        /// </summary>
        internal static void InvalidateGeneratedHandlerRegistry()
        {
            WriteFileIfChanged(HandlerOutputPath, BuildEmptyHandlerRegistryContent());
        }

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

                OpcodeManifest manifest = LoadManifest(out bool createdManifest);
                List<HandlerBinding> bindings = BuildBindings(assembly, manifest, true, logBuilder, out Dictionary<Type, uint> messageOpcodes);
                string manifestContent = JsonUtility.ToJson(manifest, true) + Environment.NewLine;
                string generatedContent = BuildGeneratedContent(bindings, messageOpcodes, manifest);
                string handlerGeneratedContent = BuildHandlerRegistryContent(bindings);

                bool manifestWritten = WriteFileIfChanged(ManifestPath, manifestContent);
                bool manifestChanged = createdManifest || manifestWritten;
                bool generatedChanged = WriteFileIfChanged(OutputPath, generatedContent);
                bool handlerGeneratedChanged = WriteFileIfChanged(HandlerOutputPath, handlerGeneratedContent);
                if (refreshAssets && (manifestChanged || generatedChanged || handlerGeneratedChanged))
                {
                    AssetDatabase.Refresh();
                }

                logBuilder.AppendLine($"稳定清单: {manifest.Entries.Count} 条（含已删除协议保留项）。");
                logBuilder.AppendLine($"当前绑定: {bindings.Count} 个处理器，{messageOpcodes.Count} 个协议。");
                logBuilder.AppendLine(manifestChanged || generatedChanged || handlerGeneratedChanged ? "已同步 opcode、协议和 Handler 生成映射。" : "opcode、协议和 Handler 生成映射无需更新。");
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

                OpcodeManifest manifest = LoadManifest(out _);
                var logBuilder = new StringBuilder();
                List<HandlerBinding> bindings = BuildBindings(assembly, manifest, false, logBuilder, out Dictionary<Type, uint> messageOpcodes);
                string expectedGeneratedContent = BuildGeneratedContent(bindings, messageOpcodes, manifest);
                string expectedHandlerGeneratedContent = BuildHandlerRegistryContent(bindings);
                string outputFullPath = GetFullPath(OutputPath);
                string handlerOutputFullPath = GetFullPath(HandlerOutputPath);
                if (!File.Exists(outputFullPath))
                {
                    error = $"缺少 opcode 生成文件: {OutputPath}";
                    return false;
                }

                string actualGeneratedContent = File.ReadAllText(outputFullPath, Utf8WithoutBom);
                if (!string.Equals(actualGeneratedContent, expectedGeneratedContent, StringComparison.Ordinal))
                {
                    error = "Opcode 生成文件与当前 Handler 不一致，请等待脚本编译后的自动同步完成。";
                    return false;
                }

                if (!File.Exists(handlerOutputFullPath) || !string.Equals(File.ReadAllText(handlerOutputFullPath, Utf8WithoutBom), expectedHandlerGeneratedContent, StringComparison.Ordinal))
                {
                    error = "Handler 注册表生成文件与当前 Handler 不一致，请等待脚本编译后的自动同步完成。";
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
        /// 加载稳定 Opcode 清单；首次创建时仅保留编号区间，不为未绑定 Handler 的协议预分配号码。
        /// </summary>
        /// <param name="createdManifest">是否在本次加载中创建了新的清单文件。</param>
        /// <returns>已完成默认值与唯一性校验的稳定 Opcode 清单。</returns>
        private static OpcodeManifest LoadManifest(out bool createdManifest)
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
        /// 从已编译的 HotUpdate Handler 中建立协议绑定，并只为这些绑定分配或读取 Opcode。
        /// </summary>
        /// <param name="assembly">执行该方法所需的 assembly 参数。</param>
        /// <param name="manifest">执行该方法所需的 manifest 参数。</param>
        /// <param name="allowAllocate">执行该方法所需的 allowAllocate 参数。</param>
        /// <param name="logBuilder">执行该方法所需的 logBuilder 参数。</param>
        /// <param name="messageOpcodes">输出 Handler 实际使用的消息类型与 Opcode 映射。</param>
        /// <returns>按 Handler 类型发现并验证后的协议绑定集合。</returns>
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
                if (binding.IsRpc)
                {
                    if (!typeof(IRpcRequest).IsAssignableFrom(binding.RequestType) || !typeof(IRpcResponse).IsAssignableFrom(binding.ResponseType))
                    {
                        throw new InvalidOperationException($"RPC处理器泛型类型无效: {binding.HandlerType.FullName}");
                    }
                }
                else if (!typeof(INormalMessage).IsAssignableFrom(binding.RequestType))
                {
                    throw new InvalidOperationException($"普通处理器请求类型未实现 INormalMessage: {binding.HandlerType.FullName}");
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
        /// 生成运行时直接注册 Handler 的源文件，避免加载 HotUpdate DLL 后再次反射扫描。
        /// </summary>
        /// <param name="bindings">当前发现的 Handler 绑定。</param>
        /// <returns>Handler 注册表源代码。</returns>
        private static string BuildHandlerRegistryContent(List<HandlerBinding> bindings)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// Auto-generated by OpcodeRegistryGenerator. Do not modify by hand.");
            builder.AppendLine("using MiniCore.Core;");
            builder.AppendLine("using MiniCore.Service;");
            builder.AppendLine();
            builder.AppendLine("namespace MiniCore.HotUpdate");
            builder.AppendLine("{");
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 自动生成的 HotUpdate Handler 直接注册表。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    public static class HotUpdateHandlerRegistry");
            builder.AppendLine("    {");
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 将当前 HotUpdate 程序集中的 Handler 注册到网络组件。");
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        /// <param name=\"network\">目标网络消息组件。</param>");
            builder.AppendLine("        public static void Register(INetworkService network)");
            builder.AppendLine("        {");
            builder.AppendLine("            if (network == null)");
            builder.AppendLine("            {");
            builder.AppendLine("                throw new System.ArgumentNullException(nameof(network));");
            builder.AppendLine("            }");

            foreach (HandlerBinding binding in bindings.OrderBy(item => item.Opcode))
            {
                builder.AppendLine($"            network.RegisterHandler(new global::{binding.HandlerType.FullName}());");
            }

            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        /// <summary>
        /// 生成不引用任何具体 Handler 类型的临时注册表，用于保证 HotUpdate 源码变更时的首轮编译可通过。
        /// </summary>
        /// <returns>可安全参与首轮编译的空 Handler 注册表源代码。</returns>
        private static string BuildEmptyHandlerRegistryContent()
        {
            var builder = new StringBuilder();
            builder.AppendLine("// Auto-generated by OpcodeRegistryGenerator. Do not modify by hand.");
            builder.AppendLine("using MiniCore.Core;");
            builder.AppendLine("using MiniCore.Service;");
            builder.AppendLine();
            builder.AppendLine("namespace MiniCore.HotUpdate");
            builder.AppendLine("{");
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// HotUpdate 源码变更期间使用的临时空 Handler 注册表。");
            builder.AppendLine("    /// 脚本编译完成后会由 OpcodeRegistryGenerator 自动替换为直接注册表。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    public static class HotUpdateHandlerRegistry");
            builder.AppendLine("    {");
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 在首轮编译期间保持调用约定，实际 Handler 会在自动同步后的下一轮编译中注册。");
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        /// <param name=\"network\">目标网络消息组件。</param>");
            builder.AppendLine("        public static void Register(INetworkService network)");
            builder.AppendLine("        {");
            builder.AppendLine("            if (network == null)");
            builder.AppendLine("            {");
            builder.AppendLine("                throw new System.ArgumentNullException(nameof(network));");
            builder.AppendLine("            }");
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
