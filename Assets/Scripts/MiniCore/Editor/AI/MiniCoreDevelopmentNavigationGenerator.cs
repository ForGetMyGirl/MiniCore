using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 从项目结构、程序集定义和源码扩展点生成 MiniCore 开发导航资料。
    /// </summary>
    public static class MiniCoreDevelopmentNavigationGenerator
    {
        #region Private 私有成员

        private const string MenuPath = "MiniCore/AI/Generate Development Navigation";
        private const string OutputDirectory = ".codex/skills/minicore-development/references/generated";
        private static readonly string[] TreeRoots = { "Assets/Scripts/MiniCore", "Assets/Scripts/Project/Bootstrap", "Assets/Tests/Editor", "Proto", "Docs", "Tools/MTaskCodeGen", "Tools/EventAnalyzer", "Assets/Settings", "Packages" }; // 纳入导航的目录根节点。
        private static readonly string[] ExplicitFiles = { "Assets/AssetBundleCollectorSetting.asset", "ProjectSettings/HybridCLRSettings.asset" }; // 纳入导航的单文件设置。
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false); // 生成资料的固定编码。
        private static readonly Regex AttributeRegex = new Regex(@"\[(?<kind>AppService|AppModule|ComponentCatalog|MiniCoreStartupModule|UIWindow)(?:Attribute)?\b", RegexOptions.Compiled); // 扩展点特性匹配器。
        private static readonly Regex TypeRegex = new Regex(@"\b(?:public|internal)?\s*(?:abstract|sealed|static|partial)?\s*(?:class|interface)\s+(?<name>\w+)", RegexOptions.Compiled); // 紧随特性的类型匹配器。
        private static readonly Regex HandlerRegex = new Regex(@"\bclass\s+(?<name>\w+)\s*:\s*(?<base>A?RpcHandler|AMHandler)", RegexOptions.Compiled); // 网络 Handler 匹配器。
        private static readonly Regex MenuRegex = new Regex("\\[MenuItem\\(\\\"(?<name>[^\\\"]+)\\\"", RegexOptions.Compiled); // Unity 菜单匹配器。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 从 Unity 菜单生成开发导航资料。
        /// </summary>
        [MenuItem(MenuPath, priority = 2300)]
        public static void GenerateFromMenu()
        {
            if (Generate(out string summary))
            {
                Debug.Log(summary);
                return;
            }

            Debug.LogError(summary);
        }

        /// <summary>
        /// 生成当前项目的开发导航资料。
        /// </summary>
        /// <param name="summary">生成结果摘要或失败原因。</param>
        /// <returns>全部资料成功生成时返回 true。</returns>
        public static bool Generate(out string summary)
        {
            try
            {
                string root = GetProjectRootPath();
                string outputDirectory = Path.Combine(root, OutputDirectory);
                Directory.CreateDirectory(outputDirectory);
                int changedCount = 0;
                changedCount += WriteIfChanged(Path.Combine(outputDirectory, "project-tree.generated.md"), BuildProjectTree(root)) ? 1 : 0;
                changedCount += WriteIfChanged(Path.Combine(outputDirectory, "assembly-dependencies.generated.md"), BuildAssemblyDependencies(root)) ? 1 : 0;
                changedCount += WriteIfChanged(Path.Combine(outputDirectory, "extension-points.generated.md"), BuildExtensionPoints(root)) ? 1 : 0;
                summary = changedCount == 0 ? "MiniCore 开发导航资料无需更新。" : $"MiniCore 开发导航资料已更新：{changedCount} 个文件。";
                return true;
            }
            catch (Exception exception)
            {
                summary = $"生成 MiniCore 开发导航资料失败：{exception.Message}";
                return false;
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 构建项目文件树资料。
        /// </summary>
        /// <param name="root">项目根目录。</param>
        /// <returns>Markdown 文件树内容。</returns>
        private static string BuildProjectTree(string root)
        {
            var builder = CreateHeader("MiniCore 项目路径树");
            for (int index = 0; index < TreeRoots.Length; index++)
            {
                string relativeRoot = TreeRoots[index];
                string fullRoot = Path.Combine(root, relativeRoot);
                if (!Directory.Exists(fullRoot))
                {
                    continue;
                }

                builder.AppendLine($"## {relativeRoot}");
                foreach (string path in GetFiles(fullRoot))
                {
                    builder.AppendLine($"- `{ToRelativePath(root, path)}`");
                }
                builder.AppendLine();
            }

            builder.AppendLine("## 单文件设置");
            for (int index = 0; index < ExplicitFiles.Length; index++)
            {
                if (File.Exists(Path.Combine(root, ExplicitFiles[index]))) builder.AppendLine($"- `{ExplicitFiles[index]}`");
            }
            return builder.ToString();
        }

        /// <summary>
        /// 构建程序集引用资料。
        /// </summary>
        /// <param name="root">项目根目录。</param>
        /// <returns>Markdown 程序集依赖内容。</returns>
        private static string BuildAssemblyDependencies(string root)
        {
            var builder = CreateHeader("程序集依赖");
            foreach (string path in GetFiles(Path.Combine(root, "Assets"), "*.asmdef"))
            {
                AsmdefData data = JsonUtility.FromJson<AsmdefData>(File.ReadAllText(path));
                if (data == null) continue;
                builder.AppendLine($"## {data.name}");
                builder.AppendLine($"- 路径：`{ToRelativePath(root, path)}`");
                builder.AppendLine($"- 引用：{FormatValues(data.references)}");
                builder.AppendLine($"- 平台：{FormatValues(data.includePlatforms)}");
                builder.AppendLine($"- noEngineReferences：`{data.noEngineReferences}`；autoReferenced：`{data.autoReferenced}`");
                builder.AppendLine();
            }
            return builder.ToString();
        }

        /// <summary>
        /// 构建源码可发现扩展点资料。
        /// </summary>
        /// <param name="root">项目根目录。</param>
        /// <returns>Markdown 扩展点内容。</returns>
        private static string BuildExtensionPoints(string root)
        {
            var entries = new List<string>();
            foreach (string path in GetFiles(Path.Combine(root, "Assets", "Scripts"), "*.cs"))
            {
                string source = File.ReadAllText(path);
                string relativePath = ToRelativePath(root, path);
                foreach (Match match in AttributeRegex.Matches(source))
                {
                    Match type = TypeRegex.Match(source, match.Index);
                    if (type.Success) entries.Add($"- {match.Groups["kind"].Value}：`{type.Groups["name"].Value}` — `{relativePath}`");
                }
                foreach (Match match in HandlerRegex.Matches(source)) entries.Add($"- Handler：`{match.Groups["name"].Value}` ({match.Groups["base"].Value}) — `{relativePath}`");
                foreach (Match match in MenuRegex.Matches(source)) entries.Add($"- MenuItem：`{match.Groups["name"].Value}` — `{relativePath}`");
            }

            entries.Sort(StringComparer.Ordinal);
            var builder = CreateHeader("可发现扩展点");
            foreach (string entry in entries) builder.AppendLine(entry);
            return builder.ToString();
        }

        /// <summary>
        /// 创建自动生成资料头部。
        /// </summary>
        /// <param name="title">资料标题。</param>
        /// <returns>已写入头部的构建器。</returns>
        private static StringBuilder CreateHeader(string title)
        {
            var builder = new StringBuilder(4096);
            builder.AppendLine($"# {title}");
            builder.AppendLine();
            builder.AppendLine("> 自动生成，勿手改。结构变动后在 Unity 点击 `MiniCore/AI/Generate Development Navigation` 更新。");
            builder.AppendLine();
            return builder;
        }

        /// <summary>
        /// 获取指定目录中按稳定顺序排列的文件。
        /// </summary>
        /// <param name="directory">待扫描目录。</param>
        /// <param name="pattern">文件匹配模式。</param>
        /// <returns>完整文件路径集合。</returns>
        private static string[] GetFiles(string directory, string pattern = "*")
        {
            string[] allFiles = Directory.GetFiles(directory, pattern, SearchOption.AllDirectories);
            var files = new List<string>(allFiles.Length);
            for (int index = 0; index < allFiles.Length; index++)
            {
                if (!allFiles[index].EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) files.Add(allFiles[index]);
            }

            files.Sort(StringComparer.Ordinal);
            return files.ToArray();
        }

        /// <summary>
        /// 将完整路径转换为统一分隔符的项目相对路径。
        /// </summary>
        /// <param name="root">项目根目录。</param>
        /// <param name="path">完整文件路径。</param>
        /// <returns>项目相对路径。</returns>
        private static string ToRelativePath(string root, string path)
        {
            return path.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, '/');
        }

        /// <summary>
        /// 格式化程序集数组字段。
        /// </summary>
        /// <param name="values">待格式化值。</param>
        /// <returns>Markdown 行内值。</returns>
        private static string FormatValues(string[] values)
        {
            return values == null || values.Length == 0 ? "无" : $"`{string.Join("`, `", values)}`";
        }

        /// <summary>
        /// 在内容变化时写入 UTF-8 无 BOM 文件。
        /// </summary>
        /// <param name="path">目标文件。</param>
        /// <param name="content">完整文件内容。</param>
        /// <returns>本次写入了新内容时返回 true。</returns>
        private static bool WriteIfChanged(string path, string content)
        {
            if (File.Exists(path) && string.Equals(File.ReadAllText(path, Utf8WithoutBom), content, StringComparison.Ordinal)) return false;
            File.WriteAllText(path, content, Utf8WithoutBom);
            return true;
        }

        /// <summary>
        /// 获取 Unity 项目的根目录。
        /// </summary>
        /// <returns>项目根目录完整路径。</returns>
        private static string GetProjectRootPath()
        {
            return Directory.GetParent(Application.dataPath).FullName;
        }

        /// <summary>
        /// Unity asmdef 的最小反序列化模型。
        /// </summary>
        [Serializable]
        private sealed class AsmdefData
        {
            public string name;
            public string[] references;
            public string[] includePlatforms;
            public bool noEngineReferences;
            public bool autoReferenced;
        }

        #endregion
    }
}
