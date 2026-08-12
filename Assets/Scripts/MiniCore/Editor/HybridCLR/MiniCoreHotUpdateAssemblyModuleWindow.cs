using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 创建包含 asmdef 和标识源码的热更新模块，并完成启动程序集引用和 HybridCLR 登记。
    /// </summary>
    public sealed class MiniCoreHotUpdateAssemblyModuleWindow : EditorWindow
    {
        #region Private 私有成员

        private const string DefaultModuleDirectory = "Assets/Scripts/Project/HotUpdateModules"; // 新模块默认父目录。
        private const string YooAssetReference = "GUID:e34a5702dd353724aa315fb8011f08c3"; // YooAsset asmdef 引用。
        private const string TextMeshProReference = "GUID:6055be8ebefd69e48b49212b09b47b2f"; // TextMeshPro asmdef 引用。

        private string assemblyName = "Project.HotUpdate.Module"; // 待创建程序集名称。
        private string parentDirectory = DefaultModuleDirectory; // 待创建模块父目录。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 打开热更新模块创建窗口。
        /// </summary>
        [MenuItem("MiniCore/HotUpdate/创建并登记热更新模块", priority = 2150)]
        public static void Open()
        {
            MiniCoreHotUpdateAssemblyModuleWindow window = GetWindow<MiniCoreHotUpdateAssemblyModuleWindow>();
            window.titleContent = new GUIContent("Hot Update Module");
            window.minSize = new Vector2(520f, 190f);
            window.InitializeFromSelection();
            window.Show();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 绘制模块名称、输出目录和创建操作。
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField("创建完整热更新模块", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "工具会创建独立 asmdef 和标识源码、登记加载顺序、加入 HybridCLR，并让启动程序集引用该模块。新模块默认依赖 Protocol 和常用运行程序集。",
                MessageType.Info);

            assemblyName = EditorGUILayout.TextField("程序集名称", assemblyName);
            using (new EditorGUILayout.HorizontalScope())
            {
                parentDirectory = EditorGUILayout.TextField("父目录", parentDirectory);
                if (GUILayout.Button("选择", GUILayout.Width(72f)))
                {
                    SelectParentDirectory();
                }
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(assemblyName)
                                               || string.IsNullOrWhiteSpace(parentDirectory)))
            {
                if (GUILayout.Button("创建并登记"))
                {
                    CreateAndRegisterModule();
                }
            }
        }

        /// <summary>
        /// 使用 Project 窗口当前选中目录作为创建起点。
        /// </summary>
        private void InitializeFromSelection()
        {
            string selected = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrWhiteSpace(selected))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(selected))
            {
                selected = Path.GetDirectoryName(selected)?.Replace('\\', '/');
            }

            if (AssetDatabase.IsValidFolder(selected)
                && (string.Equals(selected, "Assets", StringComparison.Ordinal)
                    || selected.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                parentDirectory = selected;
            }
        }

        /// <summary>
        /// 选择 Assets 下的模块父目录。
        /// </summary>
        private void SelectParentDirectory()
        {
            string selected = EditorUtility.OpenFolderPanel("选择热更新模块父目录", Application.dataPath, string.Empty);
            if (string.IsNullOrWhiteSpace(selected))
            {
                return;
            }

            string normalized = selected.Replace('\\', '/').TrimEnd('/');
            string assetsRoot = Application.dataPath.Replace('\\', '/').TrimEnd('/');
            if (!normalized.StartsWith(assetsRoot + "/", StringComparison.Ordinal)
                && !string.Equals(normalized, assetsRoot, StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog("目录无效", "热更新模块必须创建在当前项目 Assets 目录下。", "确定");
                return;
            }

            parentDirectory = "Assets" + normalized.Substring(assetsRoot.Length);
        }

        /// <summary>
        /// 创建模块文件、更新启动程序集引用并同步项目设置。
        /// </summary>
        private void CreateAndRegisterModule()
        {
            string normalizedAssemblyName = assemblyName.Trim();
            if (!IsValidAssemblyName(normalizedAssemblyName))
            {
                EditorUtility.DisplayDialog("名称无效", "程序集名称只能包含字母、数字、下划线和点，且各段不能以数字开头。", "确定");
                return;
            }

            string normalizedParent = parentDirectory.Replace('\\', '/').TrimEnd('/');
            if (!string.Equals(normalizedParent, "Assets", StringComparison.Ordinal)
                && !normalizedParent.StartsWith("Assets/", StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog("目录无效", "模块父目录必须位于 Assets 下。", "确定");
                return;
            }

            MiniCoreHotUpdateAssemblySettings settings = MiniCoreHotUpdateAssemblySettings.Current;
            settings.EnsureDefaultEntries();
            if (!settings.TryValidate(out string validationError))
            {
                EditorUtility.DisplayDialog("热更新程序集配置无效", validationError, "确定");
                return;
            }

            string moduleDirectory = normalizedParent + "/" + normalizedAssemblyName;
            string assemblyDefinitionPath = moduleDirectory + "/" + normalizedAssemblyName + ".asmdef";
            if (IsAlreadyRegistered(settings.Entries, normalizedAssemblyName, assemblyDefinitionPath))
            {
                EditorUtility.DisplayDialog("模块已登记", $"热更新程序集已经登记：{normalizedAssemblyName}", "确定");
                return;
            }

            if (File.Exists(assemblyDefinitionPath))
            {
                EditorUtility.DisplayDialog("模块已存在", assemblyDefinitionPath, "确定");
                return;
            }

            MiniCoreHotUpdateAssemblyEntry startupEntry = FindStartupEntry(settings.Entries);
            int loadOrder = FindAvailableLoadOrder(settings.Entries, startupEntry.LoadOrder);
            Directory.CreateDirectory(moduleDirectory);
            WriteAssemblyDefinition(assemblyDefinitionPath, normalizedAssemblyName);
            WriteModuleIdentity(moduleDirectory, normalizedAssemblyName);
            AddReferenceToAssemblyDefinition(startupEntry.AssemblyDefinitionPath, normalizedAssemblyName);
            settings.Register(new MiniCoreHotUpdateAssemblyEntry(
                normalizedAssemblyName,
                assemblyDefinitionPath,
                loadOrder));

            HybridClrBuildValidator.EnsureConfigured();
            HybridClrYooAssetBuildCommand.RegenerateRuntimeRegistryFromCurrentAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assemblyDefinitionPath);
            EditorUtility.DisplayDialog("创建完成", $"已经创建并登记热更新模块：{normalizedAssemblyName}", "确定");
            Close();
        }

        /// <summary>
        /// 获取唯一启动程序集记录。
        /// </summary>
        /// <param name="entries">当前项目登记表。</param>
        /// <returns>唯一启动程序集记录。</returns>
        private static MiniCoreHotUpdateAssemblyEntry FindStartupEntry(
            IReadOnlyList<MiniCoreHotUpdateAssemblyEntry> entries)
        {
            MiniCoreHotUpdateAssemblyEntry startup = null;
            for (int index = 0; index < entries.Count; index++)
            {
                MiniCoreHotUpdateAssemblyEntry entry = entries[index];
                if (!entry.IsStartup)
                {
                    continue;
                }

                if (startup != null)
                {
                    throw new InvalidOperationException("热更新程序集配置了多个启动入口。请先修复 Project Settings。 ");
                }

                startup = entry;
            }

            return startup ?? throw new InvalidOperationException("热更新程序集尚未配置启动入口。");
        }

        /// <summary>
        /// 在写入任何文件前确认程序集名称和 asmdef 路径尚未登记。
        /// </summary>
        /// <param name="entries">当前项目登记表。</param>
        /// <param name="name">待创建程序集名称。</param>
        /// <param name="assemblyDefinitionPath">待创建 asmdef 路径。</param>
        /// <returns>名称或路径已存在时返回 true。</returns>
        private static bool IsAlreadyRegistered(
            IReadOnlyList<MiniCoreHotUpdateAssemblyEntry> entries,
            string name,
            string assemblyDefinitionPath)
        {
            for (int index = 0; index < entries.Count; index++)
            {
                MiniCoreHotUpdateAssemblyEntry entry = entries[index];
                if (string.Equals(entry.AssemblyName, name, StringComparison.Ordinal)
                    || string.Equals(
                        entry.AssemblyDefinitionPath.Replace('\\', '/'),
                        assemblyDefinitionPath,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 在启动程序集之前分配一个未使用的加载顺序。
        /// </summary>
        /// <param name="entries">当前项目登记表。</param>
        /// <param name="startupLoadOrder">启动程序集加载顺序。</param>
        /// <returns>可用于新模块的加载顺序。</returns>
        private static int FindAvailableLoadOrder(
            IReadOnlyList<MiniCoreHotUpdateAssemblyEntry> entries,
            int startupLoadOrder)
        {
            int candidate = startupLoadOrder - 1;
            while (candidate > int.MinValue)
            {
                bool used = false;
                for (int index = 0; index < entries.Count; index++)
                {
                    if (entries[index].LoadOrder == candidate)
                    {
                        used = true;
                        break;
                    }
                }

                if (!used)
                {
                    return candidate;
                }

                candidate--;
            }

            throw new InvalidOperationException("无法在启动程序集之前分配新的 LoadOrder。");
        }

        /// <summary>
        /// 写出默认依赖完整且不自动引用的热更新 asmdef。
        /// </summary>
        /// <param name="path">目标 asmdef 路径。</param>
        /// <param name="name">程序集名称。</param>
        private static void WriteAssemblyDefinition(string path, string name)
        {
            var definition = new AssemblyDefinitionData
            {
                name = name,
                rootNamespace = string.Empty,
                references = new[]
                {
                    "MiniCore.Runtime",
                    "MiniCore.Protocol",
                    "MiniCore.Serialization",
                    "MiniCore.Network",
                    "MiniCore.Unity",
                    "Unity.InputSystem",
                    YooAssetReference,
                    TextMeshProReference
                },
                includePlatforms = Array.Empty<string>(),
                excludePlatforms = Array.Empty<string>(),
                allowUnsafeCode = false,
                overrideReferences = false,
                precompiledReferences = Array.Empty<string>(),
                autoReferenced = false,
                defineConstraints = Array.Empty<string>(),
                versionDefines = Array.Empty<VersionDefineData>(),
                noEngineReferences = false
            };
            File.WriteAllText(path, JsonUtility.ToJson(definition, true) + "\n", new UTF8Encoding(false));
        }

        /// <summary>
        /// 写出确保新 asmdef 会生成 DLL 的零运行开销模块标识类型。
        /// </summary>
        /// <param name="moduleDirectory">模块目录。</param>
        /// <param name="name">程序集名称。</param>
        private static void WriteModuleIdentity(string moduleDirectory, string name)
        {
            string namespaceName = name;
            string typeName = GetLastAssemblyNameSegment(name) + "ModuleIdentity";
            string source =
                "namespace " + namespaceName + "\n"
                + "{\n"
                + "    /// <summary>\n"
                + "    /// 标识由 MiniCore 热更新模块工具创建的程序集。\n"
                + "    /// </summary>\n"
                + "    public static class " + typeName + "\n"
                + "    {\n"
                + "        #region Public 公共成员\n\n"
                + "        /// <summary>\n"
                + "        /// 当前热更新程序集名称。\n"
                + "        /// </summary>\n"
                + "        public const string AssemblyName = \"" + name + "\";\n\n"
                + "        #endregion\n"
                + "    }\n"
                + "}\n";
            File.WriteAllText(
                Path.Combine(moduleDirectory, typeName + ".cs"),
                source,
                new UTF8Encoding(false));
        }

        /// <summary>
        /// 将新模块加入启动程序集的直接引用，保证业务入口可静态编排该模块。
        /// </summary>
        /// <param name="assemblyDefinitionPath">启动程序集 asmdef 路径。</param>
        /// <param name="referenceName">新增模块程序集名称。</param>
        private static void AddReferenceToAssemblyDefinition(
            string assemblyDefinitionPath,
            string referenceName)
        {
            AssemblyDefinitionData definition = JsonUtility.FromJson<AssemblyDefinitionData>(
                File.ReadAllText(assemblyDefinitionPath));
            var references = new List<string>(definition.references ?? Array.Empty<string>());
            if (!references.Contains(referenceName))
            {
                references.Add(referenceName);
                definition.references = references.ToArray();
                File.WriteAllText(
                    assemblyDefinitionPath,
                    JsonUtility.ToJson(definition, true) + "\n",
                    new UTF8Encoding(false));
            }
        }

        /// <summary>
        /// 校验程序集名称各段都可安全用于 asmdef、目录和命名空间。
        /// </summary>
        /// <param name="name">待校验程序集名称。</param>
        /// <returns>名称有效时返回 true。</returns>
        private static bool IsValidAssemblyName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            string[] segments = name.Split('.');
            for (int segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
            {
                string segment = segments[segmentIndex];
                if (segment.Length == 0 || (!char.IsLetter(segment[0]) && segment[0] != '_'))
                {
                    return false;
                }

                for (int characterIndex = 1; characterIndex < segment.Length; characterIndex++)
                {
                    char character = segment[characterIndex];
                    if (!char.IsLetterOrDigit(character) && character != '_')
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 获取程序集名称最后一段，作为生成类型前缀。
        /// </summary>
        /// <param name="name">有效程序集名称。</param>
        /// <returns>最后一个点号后的名称段。</returns>
        private static string GetLastAssemblyNameSegment(string name)
        {
            int separatorIndex = name.LastIndexOf('.');
            return separatorIndex < 0 ? name : name.Substring(separatorIndex + 1);
        }

        /// <summary>
        /// asmdef 创建和更新所需的 JSON 模型。
        /// </summary>
        [Serializable]
        private sealed class AssemblyDefinitionData
        {
            [SerializeField] internal string name;
            [SerializeField] internal string rootNamespace;
            [SerializeField] internal string[] references;
            [SerializeField] internal string[] includePlatforms;
            [SerializeField] internal string[] excludePlatforms;
            [SerializeField] internal bool allowUnsafeCode;
            [SerializeField] internal bool overrideReferences;
            [SerializeField] internal string[] precompiledReferences;
            [SerializeField] internal bool autoReferenced;
            [SerializeField] internal string[] defineConstraints;
            [SerializeField] internal VersionDefineData[] versionDefines;
            [SerializeField] internal bool noEngineReferences;
        }

        /// <summary>
        /// asmdef versionDefines 的最小 JSON 模型。
        /// </summary>
        [Serializable]
        private sealed class VersionDefineData
        {
            [SerializeField] private string name;
            [SerializeField] private string expression;
            [SerializeField] private string define;
        }

        #endregion
    }
}
