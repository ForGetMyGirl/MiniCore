using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Unity;
using UnityEditor;
using UnityEngine;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 根据 MiniCoreStartupSettings 生成 HotUpdate 启动代码。
    /// 该生成器只运行于编辑器，Player 运行时只执行生成后的静态 Pin 调用，不进行反射扫描。
    /// </summary>
    internal static class MiniCoreStartupCodeGenerator
    {
        #region Private 私有成员

        private const string SettingsPath = "Assets/Settings/MiniCoreStartupSettings.asset"; // 默认启动配置资源路径。
        private const string GeneratedOutputPath = "Assets/Scripts/MiniCore/HotUpdate/Generated/Startup/MiniCoreStartup.Generated.cs"; // 启动代码生成路径。
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false); // 生成文件固定编码。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 获取或创建项目唯一的启动配置资源，并同步当前可发现模块。
        /// </summary>
        /// <returns>已同步的项目启动配置。</returns>
        internal static MiniCoreStartupSettings GetOrCreateSettings()
        {
            MiniCoreStartupSettings settings = AssetDatabase.LoadAssetAtPath<MiniCoreStartupSettings>(SettingsPath);
            if (settings == null)
            {
                EnsureAssetFolders();
                settings = ScriptableObject.CreateInstance<MiniCoreStartupSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            SynchronizeSettings(settings);
            return settings;
        }

        /// <summary>
        /// 同步配置资源中的模块和初始化参数成员。
        /// 已保存的勾选状态与参数值会保留，新发现模块只补充默认配置。
        /// </summary>
        /// <param name="settings">要同步的项目启动配置。</param>
        internal static void SynchronizeSettings(MiniCoreStartupSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            List<StartupModuleInfo> modules = DiscoverModules();
            var existing = new Dictionary<string, MiniCoreStartupModuleSettings>(StringComparer.Ordinal);
            for (int i = 0; i < settings.Modules.Count; i++)
            {
                MiniCoreStartupModuleSettings module = settings.Modules[i];
                if (module != null && !string.IsNullOrEmpty(module.AssemblyQualifiedTypeName))
                {
                    existing[module.AssemblyQualifiedTypeName] = module;
                }
            }

            var synchronizedModules = new List<MiniCoreStartupModuleSettings>(modules.Count);
            for (int i = 0; i < modules.Count; i++)
            {
                StartupModuleInfo moduleInfo = modules[i];
                if (!existing.TryGetValue(moduleInfo.Type.AssemblyQualifiedName, out MiniCoreStartupModuleSettings moduleSettings))
                {
                    moduleSettings = new MiniCoreStartupModuleSettings
                    {
                        AssemblyQualifiedTypeName = moduleInfo.Type.AssemblyQualifiedName,
                        EnableClient = true,
                        EnableServer = true
                    };
                }

                SynchronizeArguments(moduleSettings, moduleInfo.ArgsType);
                synchronizedModules.Add(moduleSettings);
            }

            settings.Modules = synchronizedModules;
            EditorUtility.SetDirty(settings);
        }

        /// <summary>
        /// 生成并写入客户端、服务端共用的 HotUpdate 启动代码。
        /// </summary>
        /// <param name="settings">要生成代码的项目启动配置。</param>
        /// <param name="error">生成失败时的可读错误信息。</param>
        /// <returns>代码成功写入或内容无需更新时返回 true。</returns>
        internal static bool Generate(MiniCoreStartupSettings settings, out string error)
        {
            try
            {
                SynchronizeSettings(settings);
                List<StartupModuleInfo> discoveredModules = DiscoverModules();
                var moduleByName = discoveredModules.ToDictionary(item => item.Type.AssemblyQualifiedName, StringComparer.Ordinal);
                string content = BuildGeneratedContent(settings, moduleByName);
                string fullPath = System.IO.Path.Combine(GetProjectRootPath(), GeneratedOutputPath);
                string directoryPath = System.IO.Path.GetDirectoryName(fullPath);
                if (!System.IO.Directory.Exists(directoryPath))
                {
                    System.IO.Directory.CreateDirectory(directoryPath);
                }

                if (!System.IO.File.Exists(fullPath) || !string.Equals(System.IO.File.ReadAllText(fullPath, Utf8WithoutBom), content, StringComparison.Ordinal))
                {
                    System.IO.File.WriteAllText(fullPath, content, Utf8WithoutBom);
                    AssetDatabase.Refresh();
                }

                AssetDatabase.SaveAssets();
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        /// <summary>
        /// 发现所有显式标记为启动模块的 AComponent 类型。
        /// </summary>
        /// <returns>按显示名称和完整类型名稳定排序的模块信息。</returns>
        internal static List<StartupModuleInfo> DiscoverModules()
        {
            var result = new List<StartupModuleInfo>();
            foreach (Type type in TypeCache.GetTypesWithAttribute<MiniCoreStartupModuleAttribute>())
            {
                if (type == null || type.IsAbstract || !typeof(AComponent).IsAssignableFrom(type))
                {
                    continue;
                }

                MiniCoreStartupModuleAttribute attribute = type.GetCustomAttribute<MiniCoreStartupModuleAttribute>();
                if (attribute == null)
                {
                    continue;
                }

                result.Add(new StartupModuleInfo(type, attribute, FindArgsType(type)));
            }

            result.Sort((left, right) =>
            {
                int displayNameResult = string.CompareOrdinal(left.Attribute.DisplayName, right.Attribute.DisplayName);
                return displayNameResult != 0 ? displayNameResult : string.CompareOrdinal(left.Type.FullName, right.Type.FullName);
            });
            return result;
        }

        /// <summary>
        /// 获取 Args 类型中可由启动配置覆盖的公共字段和可写属性。
        /// </summary>
        /// <param name="argsType">组件声明的初始化参数类型。</param>
        /// <returns>按成员名排序的可编辑成员集合。</returns>
        internal static List<MemberInfo> GetEditableArgumentMembers(Type argsType)
        {
            var result = new List<MemberInfo>();
            if (argsType == null)
            {
                return result;
            }

            foreach (FieldInfo field in argsType.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!field.IsInitOnly && IsSupportedArgumentType(field.FieldType))
                {
                    result.Add(field);
                }
            }

            foreach (PropertyInfo property in argsType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.CanWrite && property.GetIndexParameters().Length == 0 && IsSupportedArgumentType(property.PropertyType))
                {
                    result.Add(property);
                }
            }

            result.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
            return result;
        }

        /// <summary>
        /// 判断参数成员类型是否能被当前编辑器稳定编辑并生成 C# 字面量。
        /// </summary>
        /// <param name="type">待判断的成员类型。</param>
        /// <returns>当前版本支持时返回 true。</returns>
        internal static bool IsSupportedArgumentType(Type type)
        {
            return type == typeof(string) || type == typeof(bool) || type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double) || type.IsEnum;
        }

        /// <summary>
        /// 将字符串配置值转换为 C# 初始化器字面量。
        /// </summary>
        /// <param name="type">参数成员类型。</param>
        /// <param name="value">编辑器保存的文本值。</param>
        /// <param name="literal">成功时输出的 C# 字面量。</param>
        /// <param name="error">转换失败时的错误说明。</param>
        /// <returns>转换成功时返回 true。</returns>
        internal static bool TryBuildLiteral(Type type, string value, out string literal, out string error)
        {
            value = value ?? string.Empty;
            if (type == typeof(string))
            {
                literal = "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
                error = null;
                return true;
            }

            if (type == typeof(bool) && bool.TryParse(value, out bool booleanValue))
            {
                literal = booleanValue ? "true" : "false";
                error = null;
                return true;
            }

            if (type == typeof(int) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
            {
                literal = intValue.ToString(CultureInfo.InvariantCulture);
                error = null;
                return true;
            }

            if (type == typeof(long) && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue))
            {
                literal = longValue.ToString(CultureInfo.InvariantCulture) + "L";
                error = null;
                return true;
            }

            if (type == typeof(float) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
            {
                literal = floatValue.ToString("R", CultureInfo.InvariantCulture) + "f";
                error = null;
                return true;
            }

            if (type == typeof(double) && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
            {
                literal = doubleValue.ToString("R", CultureInfo.InvariantCulture) + "d";
                error = null;
                return true;
            }

            if (type.IsEnum && Enum.TryParse(type, value, false, out object enumValue))
            {
                literal = "global::" + type.FullName.Replace('+', '.') + "." + enumValue;
                error = null;
                return true;
            }

            literal = null;
            error = $"无法将“{value}”转换为 {type.FullName}。";
            return false;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 创建不存在的配置资源目录。
        /// </summary>
        private static void EnsureAssetFolders()
        {
            const string settingsDirectory = "Assets/Settings";
            if (!AssetDatabase.IsValidFolder(settingsDirectory))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }
        }

        /// <summary>
        /// 同步一个模块的初始化参数成员配置。
        /// </summary>
        /// <param name="settings">要同步的模块配置。</param>
        /// <param name="argsType">模块初始化参数类型。</param>
        private static void SynchronizeArguments(MiniCoreStartupModuleSettings settings, Type argsType)
        {
            if (argsType == null)
            {
                settings.Arguments.Clear();
                return;
            }

            var existing = new Dictionary<string, MiniCoreStartupArgumentSettings>(StringComparer.Ordinal);
            for (int i = 0; i < settings.Arguments.Count; i++)
            {
                MiniCoreStartupArgumentSettings argument = settings.Arguments[i];
                if (argument != null && !string.IsNullOrEmpty(argument.MemberName))
                {
                    existing[argument.MemberName] = argument;
                }
            }

            List<MemberInfo> members = GetEditableArgumentMembers(argsType);
            var synchronizedArguments = new List<MiniCoreStartupArgumentSettings>(members.Count);
            for (int i = 0; i < members.Count; i++)
            {
                MemberInfo member = members[i];
                if (!existing.TryGetValue(member.Name, out MiniCoreStartupArgumentSettings argument))
                {
                    argument = new MiniCoreStartupArgumentSettings { MemberName = member.Name, UseCodeDefault = true };
                }

                synchronizedArguments.Add(argument);
            }

            settings.Arguments = synchronizedArguments;
        }

        /// <summary>
        /// 查找组件继承链声明的 AComponent&lt;TArgs&gt; 参数类型。
        /// </summary>
        /// <param name="componentType">待检查的组件类型。</param>
        /// <returns>存在强类型参数时返回对应类型，否则返回 null。</returns>
        private static Type FindArgsType(Type componentType)
        {
            for (Type current = componentType; current != null && current != typeof(AComponent); current = current.BaseType)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AComponent<>))
                {
                    return current.GetGenericArguments()[0];
                }
            }

            return null;
        }

        /// <summary>
        /// 构建完整的自动生成启动源代码。
        /// </summary>
        /// <param name="settings">项目启动配置。</param>
        /// <param name="moduleByName">程序集限定类型名到发现模块的映射。</param>
        /// <returns>可直接编译的 C# 源代码。</returns>
        private static string BuildGeneratedContent(MiniCoreStartupSettings settings, Dictionary<string, StartupModuleInfo> moduleByName)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// Auto-generated by MiniCoreStartupCodeGenerator. Do not modify by hand.");
            builder.AppendLine("using System.Threading.Tasks;");
            builder.AppendLine("using MiniCore.Core;");
            builder.AppendLine("using UnityEngine;");
            builder.AppendLine();
            builder.AppendLine("namespace MiniCore.HotUpdate");
            builder.AppendLine("{");
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 自动生成的项目启动模块装配代码。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    public static class MiniCoreStartup");
            builder.AppendLine("    {");
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 根据当前 Player 模式初始化已配置模块，并执行项目 GameStartup。 ");
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        /// <returns>项目启动完成任务。</returns>");
            builder.AppendLine("        public static async Task StartAsync()");
            builder.AppendLine("        {");
            builder.AppendLine("            if (Application.isBatchMode)");
            builder.AppendLine("            {");
            builder.AppendLine("                await StartServerAsync();");
            builder.AppendLine("            }");
            builder.AppendLine("            else");
            builder.AppendLine("            {");
            builder.AppendLine("                await StartClientAsync();");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            GameStartup gameStartup = Global.Pin<GameStartup>();");
            builder.AppendLine("            await gameStartup.StartAsync();");
            builder.AppendLine("        }");
            builder.AppendLine();
            AppendStartupMethod(builder, "Client", false, settings, moduleByName);
            builder.AppendLine();
            AppendStartupMethod(builder, "Server", true, settings, moduleByName);
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        /// <summary>
        /// 向生成源中写入一个目标的启动方法。
        /// </summary>
        /// <param name="builder">目标源代码构建器。</param>
        /// <param name="targetName">用于方法名和注释的目标名称。</param>
        /// <param name="isServer">当前是否生成 Dedicated Server 模块列表。</param>
        /// <param name="settings">项目启动配置。</param>
        /// <param name="moduleByName">程序集限定类型名到发现模块的映射。</param>
        private static void AppendStartupMethod(StringBuilder builder, string targetName, bool isServer, MiniCoreStartupSettings settings, Dictionary<string, StartupModuleInfo> moduleByName)
        {
            List<StartupModuleInfo> orderedModules = ResolveOrderedModules(isServer, settings, moduleByName);
            builder.AppendLine("        /// <summary>");
            builder.AppendLine($"        /// 初始化 {targetName} 已启用的全局模块。 ");
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        /// <returns>项目启动完成任务。</returns>");
            builder.AppendLine($"        private static Task Start{targetName}Async()");
            builder.AppendLine("        {");
            for (int i = 0; i < orderedModules.Count; i++)
            {
                AppendPinStatement(builder, orderedModules[i], settings);
            }

            builder.AppendLine("            return Task.CompletedTask;");
            builder.AppendLine("        }");
        }

        /// <summary>
        /// 根据勾选状态和 DependsOn 解析当前目标的稳定依赖顺序。
        /// </summary>
        /// <param name="isServer">当前是否解析 Dedicated Server 的模块勾选状态。</param>
        /// <param name="settings">项目启动配置。</param>
        /// <param name="moduleByName">程序集限定类型名到发现模块的映射。</param>
        /// <returns>依赖在前的启动模块顺序。</returns>
        private static List<StartupModuleInfo> ResolveOrderedModules(bool isServer, MiniCoreStartupSettings settings, Dictionary<string, StartupModuleInfo> moduleByName)
        {
            var selected = new HashSet<Type>();
            for (int i = 0; i < settings.Modules.Count; i++)
            {
                MiniCoreStartupModuleSettings moduleSettings = settings.Modules[i];
                if (moduleSettings == null || !moduleByName.TryGetValue(moduleSettings.AssemblyQualifiedTypeName, out StartupModuleInfo module))
                {
                    continue;
                }

                bool enabled = isServer ? moduleSettings.EnableServer : moduleSettings.EnableClient;
                if (enabled)
                {
                    AddModuleAndDependencies(module, moduleByName, selected, new HashSet<Type>());
                }
            }

            var result = new List<StartupModuleInfo>(selected.Count);
            var visited = new HashSet<Type>();
            var visiting = new HashSet<Type>();
            foreach (Type type in selected.OrderBy(item => item.FullName, StringComparer.Ordinal))
            {
                VisitModule(type, moduleByName, selected, visited, visiting, result);
            }

            return result;
        }

        /// <summary>
        /// 将模块及其依赖加入选中集合，并检查依赖是否也声明为启动模块。
        /// </summary>
        /// <param name="module">要加入的模块。</param>
        /// <param name="moduleByName">程序集限定类型名到发现模块的映射。</param>
        /// <param name="selected">当前已选模块类型集合。</param>
        /// <param name="path">用于识别递归依赖的当前访问路径。</param>
        private static void AddModuleAndDependencies(StartupModuleInfo module, Dictionary<string, StartupModuleInfo> moduleByName, HashSet<Type> selected, HashSet<Type> path)
        {
            if (!path.Add(module.Type))
            {
                throw new InvalidOperationException($"启动模块依赖存在循环：{module.Type.FullName}。");
            }

            selected.Add(module.Type);
            Type[] dependencies = module.Attribute.DependsOn;
            if (dependencies != null)
            {
                for (int i = 0; i < dependencies.Length; i++)
                {
                    Type dependency = dependencies[i];
                    if (dependency == null || !moduleByName.TryGetValue(dependency.AssemblyQualifiedName, out StartupModuleInfo dependencyModule))
                    {
                        throw new InvalidOperationException($"启动模块 {module.Type.FullName} 依赖的 {dependency?.FullName ?? "<null>"} 未标记 MiniCoreStartupModuleAttribute。 ");
                    }

                    AddModuleAndDependencies(dependencyModule, moduleByName, selected, path);
                }
            }

            path.Remove(module.Type);
        }

        /// <summary>
        /// 深度优先写入模块，确保所有依赖都位于当前模块之前。
        /// </summary>
        /// <param name="type">当前访问的模块类型。</param>
        /// <param name="moduleByName">程序集限定类型名到发现模块的映射。</param>
        /// <param name="selected">当前已选模块类型集合。</param>
        /// <param name="visited">已完成排序的模块类型集合。</param>
        /// <param name="visiting">正在访问的模块类型集合。</param>
        /// <param name="result">输出的依赖有序模块列表。</param>
        private static void VisitModule(Type type, Dictionary<string, StartupModuleInfo> moduleByName, HashSet<Type> selected, HashSet<Type> visited, HashSet<Type> visiting, List<StartupModuleInfo> result)
        {
            if (visited.Contains(type))
            {
                return;
            }

            if (!visiting.Add(type))
            {
                throw new InvalidOperationException($"启动模块依赖存在循环：{type.FullName}。");
            }

            StartupModuleInfo module = moduleByName[type.AssemblyQualifiedName];
            Type[] dependencies = module.Attribute.DependsOn;
            if (dependencies != null)
            {
                foreach (Type dependency in dependencies.OrderBy(item => item.FullName, StringComparer.Ordinal))
                {
                    if (selected.Contains(dependency))
                    {
                        VisitModule(dependency, moduleByName, selected, visited, visiting, result);
                    }
                }
            }

            visiting.Remove(type);
            visited.Add(type);
            result.Add(module);
        }

        /// <summary>
        /// 写入单个模块的 Pin 代码与网络 Handler 特殊桥接代码。
        /// </summary>
        /// <param name="builder">目标源代码构建器。</param>
        /// <param name="module">要初始化的模块。</param>
        /// <param name="settings">项目启动配置。</param>
        private static void AppendPinStatement(StringBuilder builder, StartupModuleInfo module, MiniCoreStartupSettings settings)
        {
            string componentTypeName = GetTypeCodeName(module.Type);
            if (module.ArgsType == null)
            {
                if (module.Type == typeof(NetworkMessageComponent))
                {
                    builder.AppendLine($"            NetworkMessageComponent network = Global.Pin<{componentTypeName}>();");
                    builder.AppendLine("            HotUpdateHandlerRegistry.Register(network);");
                    return;
                }

                builder.AppendLine($"            Global.Pin<{componentTypeName}>();");
                return;
            }

            MiniCoreStartupModuleSettings moduleSettings = settings.Modules.FirstOrDefault(item => item.AssemblyQualifiedTypeName == module.Type.AssemblyQualifiedName);
            string argumentExpression = BuildArgumentExpression(module, moduleSettings);
            builder.AppendLine($"            Global.Pin<{componentTypeName}>({argumentExpression});");
        }

        /// <summary>
        /// 构建带强类型 Args 模块的初始化参数表达式。
        /// </summary>
        /// <param name="module">拥有 Args 类型的模块。</param>
        /// <param name="settings">该模块保存的配置。</param>
        /// <returns>可编译的 Args 创建表达式。</returns>
        private static string BuildArgumentExpression(StartupModuleInfo module, MiniCoreStartupModuleSettings settings)
        {
            string argsTypeName = GetTypeCodeName(module.ArgsType);
            if (settings == null || settings.Arguments.Count == 0)
            {
                return "new " + argsTypeName + "()";
            }

            var assignments = new List<string>();
            List<MemberInfo> members = GetEditableArgumentMembers(module.ArgsType);
            for (int i = 0; i < members.Count; i++)
            {
                MemberInfo member = members[i];
                MiniCoreStartupArgumentSettings argument = settings.Arguments.FirstOrDefault(item => item.MemberName == member.Name);
                if (argument == null || argument.UseCodeDefault)
                {
                    continue;
                }

                Type memberType = GetMemberType(member);
                if (!TryBuildLiteral(memberType, argument.Value, out string literal, out string error))
                {
                    throw new InvalidOperationException($"模块 {module.Attribute.DisplayName} 的参数 {member.Name} 无效：{error}");
                }

                assignments.Add(member.Name + " = " + literal);
            }

            if (assignments.Count == 0)
            {
                return "new " + argsTypeName + "()";
            }

            return "new " + argsTypeName + " { " + string.Join(", ", assignments) + " }";
        }

        /// <summary>
        /// 获取字段或属性的声明类型。
        /// </summary>
        /// <param name="member">参数成员反射信息。</param>
        /// <returns>成员对应的 CLR 类型。</returns>
        private static Type GetMemberType(MemberInfo member)
        {
            if (member is FieldInfo field)
            {
                return field.FieldType;
            }

            return ((PropertyInfo)member).PropertyType;
        }

        /// <summary>
        /// 获取类型在生成源中使用的全局限定名称。
        /// </summary>
        /// <param name="type">要转换的 CLR 类型。</param>
        /// <returns>可直接写入 C# 泛型或构造调用的类型名称。</returns>
        private static string GetTypeCodeName(Type type)
        {
            return "global::" + type.FullName.Replace('+', '.');
        }

        /// <summary>
        /// 获取 Unity 工程根目录的绝对路径。
        /// </summary>
        /// <returns>包含 Assets 目录的工程根目录。</returns>
        private static string GetProjectRootPath()
        {
            return System.IO.Directory.GetParent(Application.dataPath).FullName;
        }

        #endregion

        /// <summary>
        /// 编辑器中使用的启动模块反射描述。
        /// </summary>
        internal sealed class StartupModuleInfo
        {
            #region Internal 内部成员

            internal Type Type { get; }
            internal MiniCoreStartupModuleAttribute Attribute { get; }
            internal Type ArgsType { get; }

            /// <summary>
            /// 使用扫描结果创建模块描述。
            /// </summary>
            /// <param name="type">组件类型。</param>
            /// <param name="attribute">组件上的启动模块特性。</param>
            /// <param name="argsType">组件声明的初始化参数类型。</param>
            internal StartupModuleInfo(Type type, MiniCoreStartupModuleAttribute attribute, Type argsType)
            {
                Type = type;
                Attribute = attribute;
                ArgsType = argsType;
            }

            #endregion
        }
    }
}
