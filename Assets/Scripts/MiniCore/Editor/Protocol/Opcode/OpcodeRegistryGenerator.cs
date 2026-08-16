using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MiniCore.Model;
using UnityEditor;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 从已登记热更新程序集发现网络 Handler，并生成无反射的直接注册入口。
    /// </summary>
    internal static class OpcodeRegistryGenerator
    {
        #region Private 私有成员

        private const string ClientHandlerOutputPath = "Assets/Scripts/MiniCore/HotUpdate/Generated/Network/HotUpdateHandlerRegistration.Generated.cs";
        private const string ServerHandlerOutputPath = "Assets/Scripts/MiniCore/HotUpdate/Server/Generated/Network/ServerHotUpdateHandlerRegistration.Generated.cs";
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false); // 生成文件固定编码。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 在命令行中同步 Handler 注册代码；失败时直接让 Unity 返回非零结果。
        /// </summary>
        public static void SynchronizeFromCommandLine()
        {
            if (!Synchronize(true, out string log))
            {
                throw new InvalidOperationException(log);
            }

            UnityEngine.Debug.Log(log);
        }

        /// <summary>
        /// 在 Handler 源码变化后写入空注册入口，避免旧类型引用阻断下一轮编译。
        /// </summary>
        internal static void InvalidateGeneratedHandlerRegistry()
        {
            WriteFileIfChanged(ClientHandlerOutputPath, BuildClientHandlerRegistrationContent(Array.Empty<HandlerBinding>()));
            WriteFileIfChanged(ServerHandlerOutputPath, BuildServerHandlerRegistrationContent(Array.Empty<HandlerBinding>()));
        }

        /// <summary>
        /// 同步全部已登记热更新程序集中的 Handler 直接注册代码。
        /// </summary>
        /// <param name="refreshAssets">生成变化后是否刷新 AssetDatabase。</param>
        /// <param name="log">同步摘要或失败原因。</param>
        /// <returns>同步成功时返回 true。</returns>
        internal static bool Synchronize(bool refreshAssets, out string log)
        {
            try
            {
                List<Assembly> assemblies = FindRegisteredAssemblies();
                if (assemblies.Count == 0)
                {
                    log = "尚未加载任何已登记热更新程序集，无法同步 Handler 注册代码。";
                    return false;
                }

                List<HandlerBinding> bindings = DiscoverBindings(assemblies);
                ValidateBindings(bindings);
                HandlerBinding[] clientBindings = bindings.Where(binding => binding.ServerHandler == null).ToArray();
                HandlerBinding[] serverBindings = bindings.Where(binding => binding.ServerHandler != null).ToArray();
                bool changed = WriteFileIfChanged(ClientHandlerOutputPath, BuildClientHandlerRegistrationContent(clientBindings));
                changed |= WriteFileIfChanged(ServerHandlerOutputPath, BuildServerHandlerRegistrationContent(serverBindings));
                if (changed && refreshAssets)
                {
                    AssetDatabase.Refresh();
                }

                log = changed
                    ? $"已同步客户端 {clientBindings.Length} 项、服务端 {serverBindings.Length} 项 Handler 注册代码。"
                    : $"Handler 注册代码无需更新：客户端 {clientBindings.Length} 项、服务端 {serverBindings.Length} 项。";
                return true;
            }
            catch (Exception exception)
            {
                log = $"Handler 注册同步失败：{exception.Message}";
                return false;
            }
        }

        /// <summary>
        /// 校验已提交 Handler 注册代码与当前已登记程序集一致。
        /// </summary>
        /// <param name="error">校验失败原因。</param>
        /// <returns>内容一致时返回 true。</returns>
        internal static bool Validate(out string error)
        {
            try
            {
                List<HandlerBinding> bindings = DiscoverBindings(FindRegisteredAssemblies());
                ValidateBindings(bindings);
                HandlerBinding[] clientBindings = bindings.Where(binding => binding.ServerHandler == null).ToArray();
                HandlerBinding[] serverBindings = bindings.Where(binding => binding.ServerHandler != null).ToArray();
                if (!HasExpectedContent(ClientHandlerOutputPath, BuildClientHandlerRegistrationContent(clientBindings))
                    || !HasExpectedContent(ServerHandlerOutputPath, BuildServerHandlerRegistrationContent(serverBindings)))
                {
                    error = "Handler 注册代码与当前热更新程序集不一致，请等待 Unity 编译后的自动同步。";
                    return false;
                }

                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = $"Handler 注册校验失败：{exception.Message}";
                return false;
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 从 MiniCore 项目设置定位当前域中已登记的程序集。
        /// </summary>
        /// <returns>已加载程序集集合。</returns>
        private static List<Assembly> FindRegisteredAssemblies()
        {
            var registeredNames = new HashSet<string>(
                MiniCoreHotUpdateAssemblySettings.Current.Entries
                    .Where(entry => entry != null)
                    .Select(entry => entry.AssemblyName),
                StringComparer.Ordinal);

            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => registeredNames.Contains(assembly.GetName().Name))
                .OrderBy(assembly => assembly.GetName().Name, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// 发现普通消息与 RPC Handler。
        /// </summary>
        private static List<HandlerBinding> DiscoverBindings(IReadOnlyList<Assembly> assemblies)
        {
            var result = new List<HandlerBinding>();
            for (int assemblyIndex = 0; assemblyIndex < assemblies.Count; assemblyIndex++)
            {
                Type[] types = assemblies[assemblyIndex].GetTypes();
                for (int typeIndex = 0; typeIndex < types.Length; typeIndex++)
                {
                    Type type = types[typeIndex];
                    if (type.IsAbstract || type.IsInterface)
                    {
                        continue;
                    }

                    Type normalBase = FindGenericBase(type, typeof(AMHandler<>));
                    if (normalBase != null)
                    {
                        result.Add(new HandlerBinding(type, normalBase.GetGenericArguments()[0], null, type.GetCustomAttribute<ServerHandlerAttribute>()));
                        continue;
                    }

                    Type rpcBase = FindGenericBase(type, typeof(ARpcHandler<,>));
                    if (rpcBase != null)
                    {
                        Type[] arguments = rpcBase.GetGenericArguments();
                        result.Add(new HandlerBinding(type, arguments[0], arguments[1], type.GetCustomAttribute<ServerHandlerAttribute>()));
                    }
                }
            }

            result.Sort((left, right) => string.CompareOrdinal(left.HandlerType.FullName, right.HandlerType.FullName));
            return result;
        }

        /// <summary>
        /// 校验 Handler 泛型角色及请求类型唯一性。
        /// </summary>
        private static void ValidateBindings(IReadOnlyList<HandlerBinding> bindings)
        {
            var requestOwners = new Dictionary<Type, Type>();
            for (int index = 0; index < bindings.Count; index++)
            {
                HandlerBinding binding = bindings[index];
                if (binding.IsRpc)
                {
                    if (!typeof(IRpcRequest).IsAssignableFrom(binding.RequestType) || !typeof(IRpcResponse).IsAssignableFrom(binding.ResponseType))
                    {
                        throw new InvalidOperationException($"RPC Handler 泛型角色无效：{binding.HandlerType.FullName}。");
                    }
                }
                else if (!typeof(INormalMessage).IsAssignableFrom(binding.RequestType))
                {
                    throw new InvalidOperationException($"普通 Handler 消息未实现 INormalMessage：{binding.HandlerType.FullName}。");
                }

                if (requestOwners.TryGetValue(binding.RequestType, out Type owner))
                {
                    throw new InvalidOperationException($"消息存在多个 Handler：{binding.RequestType.FullName} -> {owner.FullName} / {binding.HandlerType.FullName}。");
                }

                requestOwners.Add(binding.RequestType, binding.HandlerType);
            }
        }

        /// <summary>
        /// 查找目标泛型基类。
        /// </summary>
        private static Type FindGenericBase(Type type, Type genericDefinition)
        {
            for (Type current = type.BaseType; current != null; current = current.BaseType)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == genericDefinition)
                {
                    return current;
                }
            }

            return null;
        }

        /// <summary>
        /// 生成客户端直接实例化 Handler 的注册入口。
        /// </summary>
        private static string BuildClientHandlerRegistrationContent(IReadOnlyList<HandlerBinding> bindings)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// Auto-generated by OpcodeRegistryGenerator. Do not modify by hand.");
            builder.AppendLine("using MiniCore.Model;");
            builder.AppendLine();
            builder.AppendLine("namespace MiniCore.HotUpdate");
            builder.AppendLine("{");
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 将当前已登记热更新程序集的网络 Handler 注册到临时协议构建器。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    public static class HotUpdateHandlerRegistration");
            builder.AppendLine("    {");
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 注册客户端可见的 Outer Handler。");
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        /// <param name=\"builder\">目标协议构建器。</param>");
            builder.AppendLine("        public static void Register(NetworkProtocolBuilder builder)");
            builder.AppendLine("        {");
            builder.AppendLine("            if (builder == null)");
            builder.AppendLine("            {");
            builder.AppendLine("                throw new global::System.ArgumentNullException(nameof(builder));");
            builder.AppendLine("            }");
            for (int index = 0; index < bindings.Count; index++)
            {
                builder.AppendLine($"            builder.RegisterHandler(new global::{bindings[index].HandlerType.FullName}());");
            }
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        /// <summary>
        /// 生成服务端按 Role 直接实例化 Handler 的注册入口。
        /// </summary>
        /// <param name="bindings">全部服务端 Handler 绑定。</param>
        /// <returns>可直接写入服务端热更新程序集的源码。</returns>
        private static string BuildServerHandlerRegistrationContent(IReadOnlyList<HandlerBinding> bindings)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// Auto-generated by OpcodeRegistryGenerator. Do not modify by hand.");
            builder.AppendLine("using MiniCore.Model;");
            builder.AppendLine();
            builder.AppendLine("namespace MiniCore.HotUpdate.Server");
            builder.AppendLine("{");
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 按 Dedicated Server 当前 Role 注册服务端 Handler。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    public static class ServerHotUpdateHandlerRegistration");
            builder.AppendLine("    {");
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 注册与当前 Role 有交集的服务端 Handler。");
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        /// <param name=\"builder\">目标协议构建器。</param>");
            builder.AppendLine("        /// <param name=\"activeRoles\">当前进程启用的 Role。</param>");
            builder.AppendLine("        public static void Register(NetworkProtocolBuilder builder, DedicatedServerRole activeRoles)");
            builder.AppendLine("        {");
            builder.AppendLine("            if (builder == null)");
            builder.AppendLine("            {");
            builder.AppendLine("                throw new global::System.ArgumentNullException(nameof(builder));");
            builder.AppendLine("            }");
            for (int index = 0; index < bindings.Count; index++)
            {
                HandlerBinding binding = bindings[index];
                builder.AppendLine($"            if ((activeRoles & (DedicatedServerRole){(int)binding.ServerHandler.Roles}) != 0)");
                builder.AppendLine("            {");
                builder.AppendLine($"                builder.RegisterHandler(new global::{binding.HandlerType.FullName}());");
                builder.AppendLine("            }");
            }
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        /// <summary>
        /// 判断生成文件是否与期望内容完全一致。
        /// </summary>
        private static bool HasExpectedContent(string path, string expected)
        {
            string fullPath = GetFullPath(path);
            return File.Exists(fullPath)
                && string.Equals(File.ReadAllText(fullPath), expected, StringComparison.Ordinal);
        }

        /// <summary>
        /// 内容变化时写入生成文件。
        /// </summary>
        private static bool WriteFileIfChanged(string path, string content)
        {
            string fullPath = GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            if (File.Exists(fullPath) && string.Equals(File.ReadAllText(fullPath), content, StringComparison.Ordinal))
            {
                return false;
            }

            File.WriteAllText(fullPath, content, Utf8WithoutBom);
            return true;
        }

        /// <summary>
        /// 将项目相对路径转换为完整路径。
        /// </summary>
        private static string GetFullPath(string path)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
        }

        private sealed class HandlerBinding
        {
            public Type HandlerType { get; }
            public Type RequestType { get; }
            public Type ResponseType { get; }
            public bool IsRpc => ResponseType != null;
            public ServerHandlerAttribute ServerHandler { get; }

            public HandlerBinding(Type handlerType, Type requestType, Type responseType, ServerHandlerAttribute serverHandler)
            {
                HandlerType = handlerType;
                RequestType = requestType;
                ResponseType = responseType;
                ServerHandler = serverHandler;
            }
        }

        #endregion
    }
}
