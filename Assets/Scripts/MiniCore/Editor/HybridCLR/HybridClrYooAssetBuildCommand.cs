using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Settings;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 同步 HybridCLR 产物到 YooAsset，并构建可直接打入 Player 的 DefaultPackage。
    /// </summary>
    public static class HybridClrYooAssetBuildCommand
    {
        #region Private 私有成员

        private const string PackageName = "DefaultPackage";
        private const string HotUpdateAssetDirectory = "Assets/AssetRes/Dlls/HotUpdate";
        private const string ObsoleteHotUpdateAssetPath = "Assets/AssetRes/Dlls/HotUpdate.bytes";
        private const string ObsoleteNamedHotUpdateAssetPath = "Assets/AssetRes/Dlls/MiniCore.HotUpdate.dll.bytes";
        private const string AotMetadataAssetDirectory = "Assets/AssetRes/Dlls/AOT";
        private const string AotMetadataRegistryPath = "Assets/Scripts/Project/Bootstrap/Generated/HybridClrAotMetadata.Generated.cs";
        private const string AotGenericReferenceTypeName = "AOTGenericReferences";
        private const string PatchedAotAssemblyListFieldName = "PatchedAOTAssemblyList";
        private const string BuildinCatalogPath = "Assets/StreamingAssets/yoo/DefaultPackage/BuildinCatalog.json";

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 执行 HybridCLR 完整生成流程，并用最新产物构建 DefaultPackage。
        /// </summary>
        [MenuItem("MiniCore/Build/DefaultPackage/完整生成 (Generate All + Build)", priority = 2200)]
        public static void GenerateAllAndBuildDefaultPackage()
        {
            HybridClrBuildValidator.EnsureConfigured();
            PrebuildCommand.GenerateAll();
            BuildDefaultPackageFromGeneratedArtifacts();
            Debug.Log("MiniCore DefaultPackage 完整构建完成：已执行 HybridCLR Generate All 并打包。");
        }

        /// <summary>
        /// 仅编译当前平台热更新 DLL，并用现有 AOT 产物构建 DefaultPackage。
        /// </summary>
        [MenuItem("MiniCore/Build/DefaultPackage/热更编译 (Compile Active Target + Build)", priority = 2201)]
        public static void CompileActiveTargetAndBuildDefaultPackage()
        {
            HybridClrBuildValidator.EnsureConfigured();
            CompileDllCommand.CompileDllActiveBuildTarget();
            BuildDefaultPackageFromGeneratedArtifacts();
            Debug.Log("MiniCore DefaultPackage 热更构建完成：已编译当前平台 HotUpdate DLL 并打包。");
        }

        /// <summary>
        /// 验证当前目标平台的 HybridCLR 与 YooAsset 首包产物是否一致。
        /// </summary>
        /// <param name="error">校验失败原因。</param>
        /// <returns>所有运行时产物均可发布时返回 true。</returns>
        public static bool ValidateRuntimeArtifacts(out string error)
        {
            MiniCoreHotUpdateAssemblySettings settings = MiniCoreHotUpdateAssemblySettings.Current;
            settings.EnsureDefaultEntries();
            if (!settings.TryValidate(out error))
            {
                return false;
            }

            string[] aotAssemblyPaths;
            try
            {
                aotAssemblyPaths = GetAotAssemblyPaths();
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

            return ValidateRuntimeArtifacts(settings.GetEntriesInLoadOrder(), aotAssemblyPaths, out error);
        }

        /// <summary>
        /// 使用当前项目登记和 AOT 资源目录重新生成 Bootstrap 运行时地址表。
        /// </summary>
        [MenuItem("MiniCore/HotUpdate/同步 HybridCLR 与 Bootstrap 登记", priority = 2151)]
        public static void RegenerateRuntimeRegistryFromCurrentAssets()
        {
            HybridClrBuildValidator.EnsureConfigured();
            MiniCoreHotUpdateAssemblyEntry[] entries = MiniCoreHotUpdateAssemblySettings.Current.GetEntriesInLoadOrder();
            string[] aotAssemblyPaths = GetCurrentAotMetadataAssetPaths();
            WriteAotMetadataRegistry(entries, aotAssemblyPaths);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 同步当前 HybridCLR 产物、生成 AOT 地址表、构建并校验 DefaultPackage。
        /// </summary>
        private static void BuildDefaultPackageFromGeneratedArtifacts()
        {
            HybridClrBuildValidator.EnsureConfigured();
            MiniCoreHotUpdateAssemblyEntry[] entries = MiniCoreHotUpdateAssemblySettings.Current.GetEntriesInLoadOrder();
            string[] aotAssemblyPaths = SynchronizeHybridClrArtifacts(entries);
            BuildDefaultPackage();
            if (!ValidateRuntimeArtifacts(entries, aotAssemblyPaths, out string error))
            {
                throw new BuildFailedException(error);
            }
        }

        /// <summary>
        /// 同步当前目标平台的热更新 DLL 和 AOT 元数据，并写出运行时地址表。
        /// </summary>
        /// <param name="entries">按依赖顺序排列的热更新程序集登记。</param>
        /// <returns>当前目标平台的 AOT 程序集完整路径。</returns>
        private static string[] SynchronizeHybridClrArtifacts(MiniCoreHotUpdateAssemblyEntry[] entries)
        {
            string[] aotAssemblyPaths = GetAotAssemblyPaths();

            RecreateAssetDirectory(HotUpdateAssetDirectory);
            for (int index = 0; index < entries.Length; index++)
            {
                MiniCoreHotUpdateAssemblyEntry entry = entries[index];
                string sourcePath = GetHotUpdateDllPath(entry.AssemblyName);
                string targetPath = GetHotUpdateAssetPath(entry.AssemblyName);
                File.Copy(sourcePath, targetPath, true);
            }

            DeleteAssetIfPresent(ObsoleteHotUpdateAssetPath);
            DeleteAssetIfPresent(ObsoleteNamedHotUpdateAssetPath);

            if (Directory.Exists(AotMetadataAssetDirectory))
            {
                Directory.Delete(AotMetadataAssetDirectory, true);
            }

            Directory.CreateDirectory(AotMetadataAssetDirectory);
            for (int index = 0; index < aotAssemblyPaths.Length; index++)
            {
                string sourcePath = aotAssemblyPaths[index];
                string targetPath = Path.Combine(AotMetadataAssetDirectory, Path.GetFileName(sourcePath) + ".bytes");
                File.Copy(sourcePath, targetPath, true);
            }

            WriteAotMetadataRegistry(entries, aotAssemblyPaths);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return aotAssemblyPaths;
        }

        /// <summary>
        /// 使用 Scriptable Build Pipeline 构建并清理 DefaultPackage 的首包目录。
        /// </summary>
        private static void BuildDefaultPackage()
        {
            ScriptableBuildParameters buildParameters = new ScriptableBuildParameters
            {
                BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot(),
                BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot(),
                BuildPipeline = EBuildPipeline.ScriptableBuildPipeline.ToString(),
                BuildBundleType = (int)EBuildBundleType.AssetBundle,
                BuildTarget = EditorUserBuildSettings.activeBuildTarget,
                PackageName = PackageName,
                PackageVersion = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
                EnableSharePackRule = true,
                VerifyBuildingResult = true,
                FileNameStyle = EFileNameStyle.HashName,
                BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll,
                CompressOption = ECompressOption.LZ4,
                ClearBuildCacheFiles = true,
                UseAssetDependencyDB = true,
                EncryptionServices = new EncryptionNone(),
                ManifestProcessServices = new ManifestProcessNone(),
                ManifestRestoreServices = new ManifestRestoreNone(),
                BuiltinShadersBundleName = GetBuiltinShadersBundleName(),
            };

            BuildResult result = new ScriptableBuildPipeline().Run(buildParameters, true);
            if (!result.Success)
            {
                throw new BuildFailedException($"YooAsset 构建 {PackageName} 失败：{result.ErrorInfo}");
            }
        }

        /// <summary>
        /// 校验已同步 DLL、生成地址表和首包清单的完整性。
        /// </summary>
        /// <param name="entries">按依赖顺序排列的热更新程序集登记。</param>
        /// <param name="aotAssemblyPaths">当前目标平台的 AOT 程序集完整路径。</param>
        /// <param name="error">校验失败原因。</param>
        /// <returns>产物完全一致时返回 true。</returns>
        private static bool ValidateRuntimeArtifacts(
            MiniCoreHotUpdateAssemblyEntry[] entries,
            string[] aotAssemblyPaths,
            out string error)
        {
            for (int index = 0; index < entries.Length; index++)
            {
                MiniCoreHotUpdateAssemblyEntry entry = entries[index];
                string hotUpdateSourcePath;
                try
                {
                    hotUpdateSourcePath = GetHotUpdateDllPath(entry.AssemblyName);
                }
                catch (Exception exception)
                {
                    error = exception.Message;
                    return false;
                }

                string hotUpdateAssetPath = GetHotUpdateAssetPath(entry.AssemblyName);
                if (!FileContentsEqual(hotUpdateSourcePath, hotUpdateAssetPath))
                {
                    error = $"YooAsset 热更新 DLL 未同步：{hotUpdateAssetPath}";
                    return false;
                }
            }

            if (!HasExactHotUpdateAssets(entries, out error))
            {
                return false;
            }

            for (int index = 0; index < aotAssemblyPaths.Length; index++)
            {
                string sourcePath = aotAssemblyPaths[index];
                string targetPath = Path.Combine(AotMetadataAssetDirectory, Path.GetFileName(sourcePath) + ".bytes");
                if (!FileContentsEqual(sourcePath, targetPath))
                {
                    error = $"YooAsset AOT 元数据未同步：{targetPath}";
                    return false;
                }
            }

            if (!HasExactAotMetadataAssets(aotAssemblyPaths, out error))
            {
                return false;
            }

            if (!File.Exists(AotMetadataRegistryPath))
            {
                error = $"缺少 AOT 元数据地址表：{AotMetadataRegistryPath}";
                return false;
            }

            if (!File.Exists(BuildinCatalogPath))
            {
                error = $"缺少 YooAsset 首包清单：{BuildinCatalogPath}";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// 获取当前目标平台生成的热更新 DLL 路径。
        /// </summary>
        /// <param name="assemblyName">不含 DLL 后缀的程序集名称。</param>
        /// <returns>热更新 DLL 的完整路径。</returns>
        private static string GetHotUpdateDllPath(string assemblyName)
        {
            string outputDirectory = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(EditorUserBuildSettings.activeBuildTarget);
            string hotUpdatePath = Path.Combine(outputDirectory, assemblyName + ".dll");
            if (File.Exists(hotUpdatePath))
            {
                return hotUpdatePath;
            }

            hotUpdatePath = Path.Combine(outputDirectory, assemblyName + ".dll.bytes");
            if (!File.Exists(hotUpdatePath))
            {
                throw new FileNotFoundException($"缺少 HybridCLR 热更新 DLL：{assemblyName}。请先编译当前目标平台热更新程序集。", hotUpdatePath);
            }

            return hotUpdatePath;
        }

        /// <summary>
        /// 获取当前 HotUpdate 泛型引用实际需要补充元数据的 AOT 程序集，并保持稳定顺序。
        /// </summary>
        /// <returns>AOT 程序集完整路径数组。</returns>
        private static string[] GetAotAssemblyPaths()
        {
            string outputDirectory = SettingsUtil.GetAssembliesPostIl2CppStripDir(EditorUserBuildSettings.activeBuildTarget);
            if (!Directory.Exists(outputDirectory))
            {
                throw new DirectoryNotFoundException($"缺少 HybridCLR AOT 元数据目录：{outputDirectory}。请先生成 AOT 元数据。");
            }

            IReadOnlyList<string> assemblyNames = GetPatchedAotAssemblyNames();
            string[] assemblyPaths = new string[assemblyNames.Count];
            for (int index = 0; index < assemblyNames.Count; index++)
            {
                string assemblyName = assemblyNames[index];
                string assemblyPath = Path.Combine(outputDirectory, assemblyName);
                if (!File.Exists(assemblyPath))
                {
                    throw new FileNotFoundException($"HybridCLR 未生成需要补充元数据的程序集：{assemblyName}。请重新执行 HybridCLR/Generate/All。", assemblyPath);
                }

                assemblyPaths[index] = assemblyPath;
            }

            Array.Sort(assemblyPaths, StringComparer.Ordinal);
            return assemblyPaths;
        }

        /// <summary>
        /// 读取 HybridCLR 自动分析生成的最小补充元数据程序集名单。
        /// </summary>
        /// <returns>当前 HotUpdate 实际需要补充元数据的程序集名称。</returns>
        private static IReadOnlyList<string> GetPatchedAotAssemblyNames()
        {
            Type referenceType = FindAotGenericReferenceType();
            if (referenceType == null)
            {
                throw new InvalidOperationException("缺少 HybridCLR 生成的 AOTGenericReferences。请先执行 HybridCLR/Generate/AOTGenericReference 或 HybridCLR/Generate/All。");
            }

            FieldInfo field = referenceType.GetField(PatchedAotAssemblyListFieldName, BindingFlags.Public | BindingFlags.Static);
            if (!(field?.GetValue(null) is IReadOnlyList<string> assemblyNames) || assemblyNames.Count == 0)
            {
                throw new InvalidOperationException("HybridCLR 生成的 PatchedAOTAssemblyList 为空，无法确定补充元数据程序集。");
            }

            HashSet<string> uniqueNames = new HashSet<string>(StringComparer.Ordinal);
            string[] result = new string[assemblyNames.Count];
            for (int index = 0; index < assemblyNames.Count; index++)
            {
                string assemblyName = assemblyNames[index];
                if (string.IsNullOrWhiteSpace(assemblyName) || Path.GetExtension(assemblyName) != ".dll" || !uniqueNames.Add(assemblyName))
                {
                    throw new InvalidOperationException($"HybridCLR 生成了无效的补充元数据程序集名称：{assemblyName ?? "<null>"}");
                }

                result[index] = assemblyName;
            }

            return result;
        }

        /// <summary>
        /// 在已加载程序集内定位 HybridCLR 自动生成的泛型引用类型。
        /// </summary>
        /// <returns>找到的泛型引用类型；不存在时返回 null。</returns>
        private static Type FindAotGenericReferenceType()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int index = 0; index < assemblies.Length; index++)
            {
                Type type = assemblies[index].GetType(AotGenericReferenceTypeName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        /// <summary>
        /// 验证 AOT 资源目录中不存在任何过期或未被当前名单引用的 DLL。
        /// </summary>
        /// <param name="aotAssemblyPaths">当前目标平台需要补充元数据的程序集完整路径。</param>
        /// <param name="error">校验失败原因。</param>
        /// <returns>目录内容与当前补充元数据名单完全一致时返回 true。</returns>
        private static bool HasExactAotMetadataAssets(string[] aotAssemblyPaths, out string error)
        {
            HashSet<string> expectedNames = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < aotAssemblyPaths.Length; index++)
            {
                expectedNames.Add(Path.GetFileName(aotAssemblyPaths[index]));
            }

            string[] assetPaths = Directory.GetFiles(AotMetadataAssetDirectory, "*.dll.bytes", SearchOption.TopDirectoryOnly);
            if (assetPaths.Length != expectedNames.Count)
            {
                error = $"YooAsset AOT 元数据数量错误：期望 {expectedNames.Count}，实际 {assetPaths.Length}。请执行 MiniCore/Build/DefaultPackage/完整生成 (Generate All + Build)。";
                return false;
            }

            for (int index = 0; index < assetPaths.Length; index++)
            {
                string assetName = Path.GetFileNameWithoutExtension(assetPaths[index]);
                if (!expectedNames.Contains(assetName))
                {
                    error = $"YooAsset 存在过期 AOT 元数据：{assetName}";
                    return false;
                }
            }

            error = null;
            return true;
        }

        /// <summary>
        /// 验证热更新资源目录只包含当前登记的独立 DLL bytes 资产。
        /// </summary>
        /// <param name="entries">当前按依赖顺序排列的程序集登记。</param>
        /// <param name="error">校验失败原因。</param>
        /// <returns>目录内容与登记表完全一致时返回 true。</returns>
        private static bool HasExactHotUpdateAssets(
            MiniCoreHotUpdateAssemblyEntry[] entries,
            out string error)
        {
            if (!Directory.Exists(HotUpdateAssetDirectory))
            {
                error = $"缺少 YooAsset 热更新 DLL 目录：{HotUpdateAssetDirectory}";
                return false;
            }

            var expectedNames = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < entries.Length; index++)
            {
                expectedNames.Add(entries[index].AssemblyName + ".dll.bytes");
            }

            string[] assetPaths = Directory.GetFiles(
                HotUpdateAssetDirectory,
                "*.dll.bytes",
                SearchOption.TopDirectoryOnly);
            if (assetPaths.Length != expectedNames.Count)
            {
                error = $"YooAsset 热更新 DLL 数量错误：期望 {expectedNames.Count}，实际 {assetPaths.Length}。";
                return false;
            }

            for (int index = 0; index < assetPaths.Length; index++)
            {
                string assetName = Path.GetFileName(assetPaths[index]);
                if (!expectedNames.Contains(assetName))
                {
                    error = $"YooAsset 存在未登记热更新 DLL：{assetName}";
                    return false;
                }
            }

            if (File.Exists(ObsoleteHotUpdateAssetPath)
                || File.Exists(ObsoleteNamedHotUpdateAssetPath))
            {
                error = "YooAsset 目录仍存在已经废弃的单 DLL 热更新资产，请重新执行完整构建。";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// 写出 Bootstrap 在 Player 中加载 AOT 元数据所需的稳定地址表。
        /// </summary>
        /// <param name="aotAssemblyPaths">当前目标平台的 AOT 程序集完整路径。</param>
        /// <param name="entries">按依赖顺序排列的热更新程序集登记。</param>
        private static void WriteAotMetadataRegistry(
            MiniCoreHotUpdateAssemblyEntry[] entries,
            string[] aotAssemblyPaths)
        {
            MiniCoreHotUpdateAssemblyEntry startupEntry = null;
            for (int index = 0; index < entries.Length; index++)
            {
                if (entries[index].IsStartup)
                {
                    startupEntry = entries[index];
                    break;
                }
            }

            if (startupEntry == null)
            {
                throw new InvalidOperationException("热更新程序集登记缺少启动入口。");
            }

            StringBuilder builder = new StringBuilder(4096);
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine();
            builder.AppendLine("namespace MiniCore.Bootstrap");
            builder.AppendLine("{");
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 由 HybridCLR 与 YooAsset 发布准备流程生成的 AOT 元数据地址表。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    public static class HybridClrAotMetadata");
            builder.AppendLine("    {");
            builder.AppendLine("        #region Public 公共成员");
            builder.AppendLine();
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 包含最终启动入口的程序集名称。");
            builder.AppendLine("        /// </summary>");
            builder.Append("        public const string StartupAssemblyName = \"");
            builder.Append(EscapeCSharpString(startupEntry.AssemblyName));
            builder.AppendLine("\";");
            builder.AppendLine();
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// Bootstrap 反射调用的启动类型完整名称。");
            builder.AppendLine("        /// </summary>");
            builder.Append("        public const string StartupTypeName = \"");
            builder.Append(EscapeCSharpString(startupEntry.StartupTypeName));
            builder.AppendLine("\";");
            builder.AppendLine();
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// Bootstrap 反射调用的启动静态方法名称。");
            builder.AppendLine("        /// </summary>");
            builder.Append("        public const string StartupMethodName = \"");
            builder.Append(EscapeCSharpString(startupEntry.StartupMethodName));
            builder.AppendLine("\";");
            builder.AppendLine();
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// YooAsset 中按依赖顺序加载的热更新程序集独立地址。");
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        public static IReadOnlyList<string> HotUpdateAssemblyAddresses => _hotUpdateAssemblyAddresses;");
            builder.AppendLine();
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 当前构建目标需要在加载热更新程序集前补充的 AOT 元数据地址。");
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        public static IReadOnlyList<string> AotMetadataAddresses => _aotMetadataAddresses;");
            builder.AppendLine();
            builder.AppendLine("        #endregion");
            builder.AppendLine();
            builder.AppendLine("        #region Private 私有成员");
            builder.AppendLine();
            builder.AppendLine("        private static readonly string[] _hotUpdateAssemblyAddresses =");
            builder.AppendLine("        {");
            for (int index = 0; index < entries.Length; index++)
            {
                builder.Append("            \"");
                builder.Append(EscapeCSharpString(GetHotUpdateAssetAddress(entries[index].AssemblyName)));
                builder.AppendLine("\",");
            }

            builder.AppendLine("        };");
            builder.AppendLine();
            builder.AppendLine("        private static readonly string[] _aotMetadataAddresses =");
            builder.AppendLine("        {");
            for (int index = 0; index < aotAssemblyPaths.Length; index++)
            {
                builder.Append("            \"");
                builder.Append(EscapeCSharpString(GetAotMetadataAddress(aotAssemblyPaths[index])));
                builder.AppendLine("\",");
            }

            builder.AppendLine("        };");
            builder.AppendLine();
            builder.AppendLine("        #endregion");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            WriteAllTextIfChanged(AotMetadataRegistryPath, builder.ToString());
        }

        /// <summary>
        /// 读取当前 AOT 资源目录中的 DLL bytes 路径并保持稳定顺序。
        /// </summary>
        /// <returns>用于重新生成运行时表的当前 AOT 资产路径。</returns>
        private static string[] GetCurrentAotMetadataAssetPaths()
        {
            if (!Directory.Exists(AotMetadataAssetDirectory))
            {
                return Array.Empty<string>();
            }

            string[] paths = Directory.GetFiles(
                AotMetadataAssetDirectory,
                "*.dll.bytes",
                SearchOption.TopDirectoryOnly);
            Array.Sort(paths, StringComparer.Ordinal);
            return paths;
        }

        /// <summary>
        /// 将 AOT 源 DLL 或已同步 bytes 路径转换为 YooAsset 地址。
        /// </summary>
        /// <param name="path">AOT 源文件或 bytes 资产路径。</param>
        /// <returns>AddressByFileName 规则对应的 DLL 地址。</returns>
        private static string GetAotMetadataAddress(string path)
        {
            string fileName = Path.GetFileName(path);
            return fileName.EndsWith(".bytes", StringComparison.Ordinal)
                ? fileName.Substring(0, fileName.Length - ".bytes".Length)
                : fileName;
        }

        /// <summary>
        /// 获取一个热更新 DLL 在 YooAsset 中的固定独立地址。
        /// </summary>
        /// <param name="assemblyName">不含 DLL 后缀的程序集名称。</param>
        /// <returns>AddressByFileName 规则生成的资源地址。</returns>
        private static string GetHotUpdateAssetAddress(string assemblyName)
        {
            return assemblyName + ".dll";
        }

        /// <summary>
        /// 获取一个热更新 DLL 在 Assets 下的 bytes 文件路径。
        /// </summary>
        /// <param name="assemblyName">不含 DLL 后缀的程序集名称。</param>
        /// <returns>独立 DLL bytes 资产路径。</returns>
        private static string GetHotUpdateAssetPath(string assemblyName)
        {
            return Path.Combine(
                HotUpdateAssetDirectory,
                GetHotUpdateAssetAddress(assemblyName) + ".bytes");
        }

        /// <summary>
        /// 删除旧目录后创建空的资产输出目录。
        /// </summary>
        /// <param name="assetDirectory">Assets 下的目标目录。</param>
        private static void RecreateAssetDirectory(string assetDirectory)
        {
            if (AssetDatabase.IsValidFolder(assetDirectory))
            {
                AssetDatabase.DeleteAsset(assetDirectory);
            }
            else if (Directory.Exists(assetDirectory))
            {
                Directory.Delete(assetDirectory, true);
            }

            Directory.CreateDirectory(assetDirectory);
        }

        /// <summary>
        /// 删除一个已经废弃的单 DLL 资产及其 meta。
        /// </summary>
        /// <param name="assetPath">待删除 Assets 路径。</param>
        private static void DeleteAssetIfPresent(string assetPath)
        {
            if (File.Exists(assetPath) || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        /// <summary>
        /// 转义写入 C# 字符串常量的项目配置文本。
        /// </summary>
        /// <param name="value">待转义文本。</param>
        /// <returns>可安全置于双引号中的文本。</returns>
        private static string EscapeCSharpString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        /// <summary>
        /// 获取 YooAsset 自动收集的内置着色器资源包名称。
        /// </summary>
        /// <returns>内置着色器资源包名称。</returns>
        private static string GetBuiltinShadersBundleName()
        {
            PackRuleResult packRuleData = DefaultPackRule.CreateShadersPackRuleResult();
            return packRuleData.GetBundleName(PackageName, AssetBundleCollectorSettingData.Setting.UniqueBundleName);
        }

        /// <summary>
        /// 确保指定文件路径的父目录存在。
        /// </summary>
        /// <param name="filePath">需要写入的文件路径。</param>
        private static void EnsureParentDirectory(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// 仅当文本内容变化时写出文件，避免无意义触发脚本重编译。
        /// </summary>
        /// <param name="filePath">目标文件路径。</param>
        /// <param name="contents">需要写入的文本内容。</param>
        private static void WriteAllTextIfChanged(string filePath, string contents)
        {
            EnsureParentDirectory(filePath);
            if (File.Exists(filePath) && File.ReadAllText(filePath) == contents)
            {
                return;
            }

            File.WriteAllText(filePath, contents, new UTF8Encoding(false));
        }

        /// <summary>
        /// 比较两个文件是否均存在且字节内容完全一致。
        /// </summary>
        /// <param name="leftPath">第一个文件路径。</param>
        /// <param name="rightPath">第二个文件路径。</param>
        /// <returns>两个文件内容一致时返回 true。</returns>
        private static bool FileContentsEqual(string leftPath, string rightPath)
        {
            if (!File.Exists(leftPath) || !File.Exists(rightPath))
            {
                return false;
            }

            FileInfo leftInfo = new FileInfo(leftPath);
            FileInfo rightInfo = new FileInfo(rightPath);
            if (leftInfo.Length != rightInfo.Length)
            {
                return false;
            }

            const int bufferLength = 81920;
            byte[] leftBuffer = new byte[bufferLength];
            byte[] rightBuffer = new byte[bufferLength];
            using (FileStream leftStream = File.OpenRead(leftPath))
            using (FileStream rightStream = File.OpenRead(rightPath))
            {
                while (true)
                {
                    int leftRead = leftStream.Read(leftBuffer, 0, leftBuffer.Length);
                    int rightRead = rightStream.Read(rightBuffer, 0, rightBuffer.Length);
                    if (leftRead != rightRead)
                    {
                        return false;
                    }

                    if (leftRead == 0)
                    {
                        return true;
                    }

                    for (int index = 0; index < leftRead; index++)
                    {
                        if (leftBuffer[index] != rightBuffer[index])
                        {
                            return false;
                        }
                    }
                }
            }
        }

        #endregion
    }
}
