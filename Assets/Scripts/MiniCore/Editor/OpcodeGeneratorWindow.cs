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
    /// <summary>
    /// Scans the HotUpdate assembly for AMHandler and ARpcHandler implementations,
    /// assigns incremental opcodes, and writes the generated partial class before compilation.
    /// </summary>
    public class OpcodeGeneratorWindow : EditorWindow
    {
        private const string DefaultAssemblyKeyword = "HotUpdate";
        private const string DefaultOutputPath = "Assets/Scripts/MiniCore/Model/Generated/OpcodeRegistry.Generated.cs";
        private const int DefaultNormalStartOpcode = 100001;
        private const int DefaultRpcStartOpcode = 200001;

        private string assemblyKeyword = DefaultAssemblyKeyword;
        private string outputPath = DefaultOutputPath;
        private int normalStartOpcode = DefaultNormalStartOpcode;
        private int rpcStartOpcode = DefaultRpcStartOpcode;
        private Vector2 scroll;
        private string log = string.Empty;

        [MenuItem("MiniCore/Opcode/Generate (HotUpdate)", priority = 2100)]
        private static void Open()
        {
            GetWindow<OpcodeGeneratorWindow>("Opcode Generator").Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("HotUpdate Opcode Generator", EditorStyles.boldLabel);
            assemblyKeyword = EditorGUILayout.TextField("Assembly keyword", assemblyKeyword);
            outputPath = EditorGUILayout.TextField("Output path", outputPath);
            normalStartOpcode = EditorGUILayout.IntField("Normal start opcode", normalStartOpcode);
            rpcStartOpcode = EditorGUILayout.IntField("RPC start opcode", rpcStartOpcode);
            EditorGUILayout.HelpBox("Opcodes now use 4 bytes (uint). 请确保客户端/服务端协议头一致。", MessageType.Info);

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate", GUILayout.Height(30)))
            {
                Generate();
            }

            GUILayout.Space(8);
            GUILayout.Label("Log", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(240));
            EditorGUILayout.TextArea(log, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void Generate()
        {
            var sb = new StringBuilder();
            try
            {
                var asm = FindAssembly(assemblyKeyword);
                if (asm == null)
                {
                    sb.AppendLine($"Assembly containing \"{assemblyKeyword}\" not found. Make sure HotUpdate is compiled.");
                    log = sb.ToString();
                    return;
                }

                var bindings = BuildBindings(asm, normalStartOpcode, rpcStartOpcode, sb, out var messageOpcodes, out _);
                string generated = BuildGeneratedContent(bindings, messageOpcodes);
                WriteFile(outputPath, generated);
                AssetDatabase.Refresh();
                sb.AppendLine($"Done. Generated {bindings.Count} handler bindings and {messageOpcodes.Count} opcode constants.");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Generation failed: {ex.Message}\n{ex.StackTrace}");
            }

            log = sb.ToString();
        }

        private Assembly FindAssembly(string keyword)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private List<HandlerBinding> BuildBindings(
            Assembly asm,
            int normalStart,
            int rpcStart,
            StringBuilder logBuilder,
            out Dictionary<Type, uint> messageOpcodes,
            out Dictionary<string, Type> constNameMap)
        {
            var normalHandlers = asm.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Select(t => new { Type = t, Base = FindGenericBase(t, typeof(AMHandler<>)) })
                .Where(x => x.Base != null)
                .Select(x => new { HandlerType = x.Type, MessageType = x.Base.GetGenericArguments()[0] })
                .OrderBy(x => x.MessageType.FullName)
                .ToList();

            var rpcHandlers = asm.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Select(t => new { Type = t, Base = FindGenericBase(t, typeof(ARpcHandler<,>)) })
                .Where(x => x.Base != null)
                .Select(x => new
                {
                    HandlerType = x.Type,
                    RequestType = x.Base.GetGenericArguments()[0],
                    ResponseType = x.Base.GetGenericArguments()[1]
                })
                .OrderBy(x => x.RequestType.FullName)
                .ToList();

            messageOpcodes = new Dictionary<Type, uint>();
            constNameMap = new Dictionary<string, Type>();
            var bindings = new List<HandlerBinding>();

            uint nextOpcode = (uint)normalStart;
            foreach (var item in normalHandlers)
            {
                uint opcode = AllocateOpcode(item.MessageType, ref nextOpcode, messageOpcodes, constNameMap);
                bindings.Add(new HandlerBinding
                {
                    HandlerType = item.HandlerType,
                    RequestType = item.MessageType,
                    ResponseType = null,
                    Opcode = opcode,
                    IsRpc = false
                });
                logBuilder.AppendLine($"Normal: {opcode} -> {item.HandlerType.FullName} ({item.MessageType.FullName})");
            }

            uint rpcOpcode = Math.Max(nextOpcode, (uint)rpcStart);
            foreach (var item in rpcHandlers)
            {
                uint opcode = AllocateOpcode(item.RequestType, ref rpcOpcode, messageOpcodes, constNameMap);
                BindMessageToOpcode(item.ResponseType, opcode, messageOpcodes, constNameMap);

                bindings.Add(new HandlerBinding
                {
                    HandlerType = item.HandlerType,
                    RequestType = item.RequestType,
                    ResponseType = item.ResponseType,
                    Opcode = opcode,
                    IsRpc = true
                });

                logBuilder.AppendLine($"RPC: {opcode} -> {item.HandlerType.FullName} ({item.RequestType.FullName} / {item.ResponseType.FullName})");
            }

            // Bind remaining IProtocol implementations so outgoing messages without handlers still get opcodes.
            nextOpcode = Math.Max(rpcOpcode, nextOpcode);
            var protocolTypes = asm.GetTypes()
                .Where(t => typeof(IProtocol).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .OrderBy(t => t.FullName);
            foreach (var type in protocolTypes)
            {
                if (messageOpcodes.ContainsKey(type))
                {
                    continue;
                }
                uint opcode = AllocateOpcode(type, ref nextOpcode, messageOpcodes, constNameMap);
                logBuilder.AppendLine($"Orphan message: {opcode} -> {type.FullName}");
            }

            return bindings;
        }

        private Type FindGenericBase(Type type, Type genericDefinition)
        {
            var bt = type.BaseType;
            while (bt != null)
            {
                if (bt.IsGenericType && bt.GetGenericTypeDefinition() == genericDefinition)
                {
                    return bt;
                }
                bt = bt.BaseType;
            }
            return null;
        }

        private uint AllocateOpcode(Type messageType, ref uint cursor, Dictionary<Type, uint> map, Dictionary<string, Type> constNameMap)
        {
            if (map.TryGetValue(messageType, out uint existing))
            {
                return existing;
            }

            string constName = BuildConstName(messageType);
            if (constNameMap.TryGetValue(constName, out var other) && other != messageType)
            {
                throw new InvalidOperationException($"Const name collision: {messageType.FullName} and {other.FullName} both map to {constName}.");
            }

            constNameMap[constName] = messageType;
            map[messageType] = cursor;
            return cursor++;
        }

        private void BindMessageToOpcode(Type messageType, uint opcode, Dictionary<Type, uint> map, Dictionary<string, Type> constNameMap)
        {
            if (map.TryGetValue(messageType, out uint existing) && existing != opcode)
            {
                throw new InvalidOperationException($"Message type {messageType.FullName} already bound to opcode {existing}, cannot bind to {opcode}.");
            }

            string constName = BuildConstName(messageType);
            if (constNameMap.TryGetValue(constName, out var other) && other != messageType)
            {
                throw new InvalidOperationException($"Const name collision: {messageType.FullName} and {other.FullName} both map to {constName}.");
            }

            constNameMap[constName] = messageType;
            map[messageType] = opcode;
        }

        private string BuildGeneratedContent(
            List<HandlerBinding> bindings,
            Dictionary<Type, uint> messageOpcodes)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// Auto-generated by OpcodeGeneratorWindow. Do not modify by hand.");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine("namespace MiniCore.Model");
            sb.AppendLine("{");
            sb.AppendLine("    public static partial class OpcodeRegistry");
            sb.AppendLine("    {");

            foreach (var kv in messageOpcodes.OrderBy(k => k.Value).ThenBy(k => k.Key.FullName))
            {
                string constName = BuildConstName(kv.Key);
                sb.AppendLine($"        public const uint {constName} = {kv.Value}u;");
            }

            sb.AppendLine();
            sb.AppendLine("        static partial void RegisterGenerated(Dictionary<string, uint> handlerToOpcode, Dictionary<uint, HandlerInfo> opcodeToHandler, Dictionary<string, uint> messageToOpcode)");
            sb.AppendLine("        {");

            foreach (var binding in bindings.OrderBy(b => b.Opcode))
            {
                sb.AppendLine($"            handlerToOpcode[\"{binding.HandlerType.FullName}\"] = {binding.Opcode};");
            }

            foreach (var binding in bindings.OrderBy(b => b.Opcode))
            {
                string responseType = binding.ResponseType != null ? binding.ResponseType.FullName : string.Empty;
                sb.AppendLine($"            opcodeToHandler[{binding.Opcode}] = new HandlerInfo {{ HandlerType = \"{binding.HandlerType.FullName}\", RequestType = \"{binding.RequestType.FullName}\", ResponseType = \"{responseType}\", IsRpc = {(binding.IsRpc ? "true" : "false")} }};");
            }

            foreach (var kv in messageOpcodes.OrderBy(k => k.Value).ThenBy(k => k.Key.FullName))
            {
                sb.AppendLine($"            messageToOpcode[\"{kv.Key.FullName}\"] = {kv.Value};");
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private void WriteFile(string path, string content)
        {
            string fullPath = Path.IsPathRooted(path) ? path : Path.GetFullPath(path, Directory.GetCurrentDirectory());
            var dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(fullPath, content, new UTF8Encoding(false));
        }

        private string BuildConstName(Type type)
        {
            string name = type.Name;
            int tick = name.IndexOf('`');
            if (tick >= 0)
            {
                name = name.Substring(0, tick);
            }
            return name.Replace('+', '_');
        }

        private class HandlerBinding
        {
            public Type HandlerType;
            public Type RequestType;
            public Type ResponseType;
            public uint Opcode;
            public bool IsRpc;
        }
    }
}
