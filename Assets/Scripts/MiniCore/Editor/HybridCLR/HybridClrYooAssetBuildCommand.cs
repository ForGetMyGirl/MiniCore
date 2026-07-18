using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using HybridCLR.Editor;
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
        private const string HotUpdateAssemblyName = "MiniCore.HotUpdate";
        private const string HotUpdateAssetPath = "Assets/AssetRes/Dlls/HotUpdate.bytes";
        private const string LegacyHotUpdateAssetPath = "Assets/AssetRes/Dlls/MiniCore.HotUpdate.dll.bytes";
        private const string AotMetadataAssetDirectory = "Assets/AssetRes/Dlls/AOT";
        private const string AotMetadataRegistryPath = "Assets/Scripts/Project/Bootstrap/Generated/HybridClrAotMetadata.Generated.cs";
        private const string AotGenericReferenceTypeName = "AOTGenericReferences";
        private const string PatchedAotAssemblyListFieldName = "PatchedAOTAssemblyList";
        private const string BuildinCatalogPath = "Assets/StreamingAssets/yoo/DefaultPackage/BuildinCatalog.json";

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 在 Unity 菜单中同步 HybridCLR 产物并重建 DefaultPackage。
        /// </summary>
        [MenuItem("MiniCore/Build/Prepare DefaultPackage", priority = 2200)]
        public static void PrepareDefaultPackage()
        {
            BuildDefaultPackageForActiveTarget();
            Debug.Log("MiniCore DefaultPackage 已同步 HybridCLR DLL 并构建完成。");
        }

        /// <summary>
        /// 为当前 Unity 构建目标同步 DLL、生成 AOT 地址表并构建 DefaultPackage。
        /// </summary>
        public static void BuildDefaultPackageForActiveTarget()
        {
            string[] aotAssemblyPaths = SynchronizeHybridClrArtifacts();
            BuildDefaultPackage();
            if (!ValidateRuntimeArtifacts(aotAssemblyPaths, out string error))
            {
                throw new BuildFailedException(error);
            }
        }

        /// <summary>
        /// 验证当前目标平台的 HybridCLR 与 YooAsset 首包产物是否一致。
        /// </summary>
        /// <param name="error">校验失败原因。</param>
        /// <returns>所有运行时产物均可发布时返回 true。</returns>
        public static bool ValidateRuntimeArtifacts(out string error)
        {
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

            return ValidateRuntimeArtifacts(aotAssemblyPaths, out error);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 同步当前目标平台的热更新 DLL 和 AOT 元数据，并写出运行时地址表。
        /// </summary>
        /// <returns>当前目标平台的 AOT 程序集完整路径。</returns>
        private static string[] SynchronizeHybridClrArtifacts()
        {
            string hotUpdateSourcePath = GetHotUpdateDllPath();
            string[] aotAssemblyPaths = GetAotAssemblyPaths();

            EnsureParentDirectory(HotUpdateAssetPath);
            File.Copy(hotUpdateSourcePath, HotUpdateAssetPath, true);

            if (File.Exists(LegacyHotUpdateAssetPath))
            {
                AssetDatabase.DeleteAsset(LegacyHotUpdateAssetPath);
            }

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

            WriteAotMetadataRegistry(aotAssemblyPaths);
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
        /// <param name="aotAssemblyPaths">当前目标平台的 AOT 程序集完整路径。</param>
        /// <param name="error">校验失败原因。</param>
        /// <returns>产物完全一致时返回 true。</returns>
        private static bool ValidateRuntimeArtifacts(string[] aotAssemblyPaths, out string error)
        {
            string hotUpdateSourcePath;
            try
            {
                hotUpdateSourcePath = GetHotUpdateDllPath();
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

            if (!FileContentsEqual(hotUpdateSourcePath, HotUpdateAssetPath))
            {
                error = $"YooAsset 热更新 DLL 未同步：{HotUpdateAssetPath}";
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
        /// <returns>热更新 DLL 的完整路径。</returns>
        private static string GetHotUpdateDllPath()
        {
            string outputDirectory = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(EditorUserBuildSettings.activeBuildTarget);
            string hotUpdatePath = Path.Combine(outputDirectory, HotUpdateAssemblyName + ".dll");
            if (File.Exists(hotUpdatePath))
            {
                return hotUpdatePath;
            }

            hotUpdatePath = Path.Combine(outputDirectory, HotUpdateAssemblyName + ".dll.bytes");
            if (!File.Exists(hotUpdatePath))
            {
                throw new FileNotFoundException("缺少 HybridCLR HotUpdate DLL，请先执行 HybridCLR/CompileDll/当前目标平台。", hotUpdatePath);
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
                error = $"YooAsset AOT 元数据数量错误：期望 {expectedNames.Count}，实际 {assetPaths.Length}。请执行 MiniCore/Build/Prepare DefaultPackage。";
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
        /// 写出 Bootstrap 在 Player 中加载 AOT 元数据所需的稳定地址表。
        /// </summary>
        /// <param name="aotAssemblyPaths">当前目标平台的 AOT 程序集完整路径。</param>
        private static void WriteAotMetadataRegistry(string[] aotAssemblyPaths)
        {
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
            builder.AppendLine("        /// YooAsset 中热更新程序集的固定加载地址。");
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        public const string HotUpdateDllAddress = \"HotUpdate\";");
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
            builder.AppendLine("        private static readonly string[] _aotMetadataAddresses =");
            builder.AppendLine("        {");
            for (int index = 0; index < aotAssemblyPaths.Length; index++)
            {
                builder.Append("            \"");
                builder.Append(Path.GetFileName(aotAssemblyPaths[index]));
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
