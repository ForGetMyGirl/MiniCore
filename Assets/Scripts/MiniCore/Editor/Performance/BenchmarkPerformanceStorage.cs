using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.PerformanceTesting.Data;
using UnityEditor;
using UnityEngine;
using SampleUnit = Unity.PerformanceTesting.SampleUnit;

namespace MiniCore.EditorTools
{

    /// <summary>
    /// 负责性能测试结果的项目内归档、CSV 导出和历史记录读取。
    /// </summary>
    internal static class BenchmarkPerformanceStorage
    {
        #region Private 私有成员

        private const string BenchmarkDirectoryName = "BenchmarkPerformance"; // 项目根目录下的性能记录目录名称。
        private const string HistoryDirectoryName = "History"; // 自动归档的子目录名称。
        private const string JsonFileName = "PerformanceTestResults.json"; // 性能包生成的 JSON 文件名称。
        private const string CsvFileName = "PerformanceTestResults.csv"; // 项目归档生成的 CSV 文件名称。
        private const string XmlFileName = "TestResults.xml"; // Unity Test Runner 生成的原始 XML 文件名称。
        private const string LastArchivedFingerprintKey = "MiniCore.Performance.LastArchivedFingerprint"; // 防止重复回调重复归档同一运行的会话键。
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false); // 归档文本文件的固定编码。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 获取项目根目录下自动归档性能记录的历史目录。
        /// </summary>
        /// <returns>绝对历史目录路径。</returns>
        internal static string GetHistoryDirectory()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", BenchmarkDirectoryName, HistoryDirectoryName));
        }

        /// <summary>
        /// 将性能包刚写入的最新结果归档为独立时间戳目录。
        /// </summary>
        /// <param name="runStartedUtc">本次 Test Runner 运行开始的 UTC 时间，用于排除旧结果。</param>
        /// <param name="archiveDirectory">成功时返回新建的归档目录。</param>
        /// <returns>是否发现并成功归档了有效性能测试结果。</returns>
        internal static bool TryArchiveLatestRun(DateTime runStartedUtc, out string archiveDirectory)
        {
            archiveDirectory = null;
            try
            {
                string sourceJsonPath = Path.Combine(Application.persistentDataPath, JsonFileName);
                if (!File.Exists(sourceJsonPath))
                {
                    return false;
                }

                DateTime sourceWriteTimeUtc = File.GetLastWriteTimeUtc(sourceJsonPath);
                if (sourceWriteTimeUtc < runStartedUtc.AddSeconds(-2))
                {
                    return false;
                }

                string json = File.ReadAllText(sourceJsonPath);
                Run run = JsonUtility.FromJson<Run>(json);
                if (!HasPerformanceResults(run))
                {
                    return false;
                }

                string fingerprint = BuildArchiveFingerprint(run, sourceWriteTimeUtc, new FileInfo(sourceJsonPath).Length);
                if (string.Equals(SessionState.GetString(LastArchivedFingerprintKey, string.Empty), fingerprint, StringComparison.Ordinal))
                {
                    return false;
                }

                archiveDirectory = CreateArchiveDirectory();
                File.WriteAllText(Path.Combine(archiveDirectory, JsonFileName), json, Utf8WithoutBom);
                CopyLatestXml(archiveDirectory);
                WriteCsv(run, Path.Combine(archiveDirectory, CsvFileName));
                SessionState.SetString(LastArchivedFingerprintKey, fingerprint);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MiniCore Performance] 自动归档性能结果失败：{exception}");
                archiveDirectory = null;
                return false;
            }
        }

        /// <summary>
        /// 读取全部已归档的性能测试历史，并按最新记录优先排序。
        /// </summary>
        /// <returns>可用于历史窗口展示的性能运行记录列表。</returns>
        internal static List<BenchmarkPerformanceHistoryEntry> LoadHistory()
        {
            var entries = new List<BenchmarkPerformanceHistoryEntry>();
            var fingerprints = new HashSet<string>();
            string historyDirectory = GetHistoryDirectory();
            if (!Directory.Exists(historyDirectory))
            {
                return entries;
            }

            string[] directories = Directory.GetDirectories(historyDirectory);
            for (int index = 0; index < directories.Length; index++)
            {
                TryAddHistoryEntry(directories[index], entries, fingerprints);
            }

            entries.Sort(CompareHistoryEntryByTimeDescending);
            return entries;
        }

        /// <summary>
        /// 删除一条自动归档的性能历史记录目录。
        /// </summary>
        /// <param name="directoryPath">需要删除的历史记录目录绝对路径。</param>
        /// <returns>目录存在且成功删除时返回 true。</returns>
        internal static bool DeleteHistoryEntry(string directoryPath)
        {
            if (!IsHistoryChildDirectory(directoryPath) || !Directory.Exists(directoryPath))
            {
                return false;
            }

            try
            {
                Directory.Delete(directoryPath, true);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MiniCore Performance] 删除性能历史失败：{directoryPath}\n{exception}");
                return false;
            }
        }

        /// <summary>
        /// 删除一组自动归档的性能历史记录目录。
        /// </summary>
        /// <param name="entries">需要删除的历史记录集合。</param>
        /// <returns>实际成功删除的记录数量。</returns>
        internal static int DeleteHistoryEntries(IList<BenchmarkPerformanceHistoryEntry> entries)
        {
            int deletedCount = 0;
            for (int index = 0; index < entries.Count; index++)
            {
                if (DeleteHistoryEntry(entries[index].DirectoryPath))
                {
                    deletedCount++;
                }
            }

            return deletedCount;
        }

        /// <summary>
        /// 清空自动归档历史目录中的全部运行记录，不影响 BenchmarkPerformance 根目录的手动文件。
        /// </summary>
        /// <returns>实际成功删除的记录数量。</returns>
        internal static int ClearHistory()
        {
            string historyDirectory = GetHistoryDirectory();
            if (!Directory.Exists(historyDirectory))
            {
                return 0;
            }

            string[] directories = Directory.GetDirectories(historyDirectory);
            int deletedCount = 0;
            for (int index = 0; index < directories.Length; index++)
            {
                if (DeleteHistoryEntry(directories[index]))
                {
                    deletedCount++;
                }
            }

            return deletedCount;
        }

        /// <summary>
        /// 将性能数值格式化为适合编辑器窗口显示的文本。
        /// </summary>
        /// <param name="value">待显示的数值。</param>
        /// <param name="unit">数值的性能采样单位。</param>
        /// <returns>包含数值和单位的显示文本。</returns>
        internal static string FormatValue(double value, SampleUnit unit)
        {
            string format = unit == SampleUnit.Undefined ? "0.000000" : "0.000";
            string suffix = GetUnitSuffix(unit);
            return string.IsNullOrEmpty(suffix)
                ? value.ToString(format, CultureInfo.InvariantCulture)
                : $"{value.ToString(format, CultureInfo.InvariantCulture)} {suffix}";
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 判断解析出的运行报告是否包含至少一项性能测试结果。
        /// </summary>
        /// <param name="run">待检查的性能运行报告。</param>
        /// <returns>存在有效性能测试结果时返回 true。</returns>
        private static bool HasPerformanceResults(Run run)
        {
            return run != null && run.Results != null && run.Results.Count > 0;
        }

        /// <summary>
        /// 判断路径是否为自动归档历史根目录下的直接子目录，防止删除范围越界。
        /// </summary>
        /// <param name="directoryPath">需要校验的目录绝对路径。</param>
        /// <returns>路径可作为自动归档记录安全删除时返回 true。</returns>
        private static bool IsHistoryChildDirectory(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                return false;
            }

            string historyDirectory = GetHistoryDirectory();
            string normalizedHistoryDirectory = Path.GetFullPath(historyDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string normalizedDirectoryPath = Path.GetFullPath(directoryPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string parentDirectory = Path.GetDirectoryName(normalizedDirectoryPath);
            return string.Equals(parentDirectory, normalizedHistoryDirectory, StringComparison.Ordinal);
        }

        /// <summary>
        /// 构造用于阻止同一次 Test Runner 结果重复归档的会话指纹。
        /// </summary>
        /// <param name="run">性能包解析出的运行报告。</param>
        /// <param name="sourceWriteTimeUtc">最新结果 JSON 的 UTC 写入时间。</param>
        /// <param name="sourceLength">最新结果 JSON 的字节长度。</param>
        /// <returns>可唯一识别当前归档来源的字符串。</returns>
        private static string BuildArchiveFingerprint(Run run, DateTime sourceWriteTimeUtc, long sourceLength)
        {
            return $"{run.Date}:{sourceWriteTimeUtc.Ticks}:{sourceLength}:{run.Results.Count}";
        }

        /// <summary>
        /// 构造用于历史窗口隐藏重复归档目录的运行指纹。
        /// </summary>
        /// <param name="run">需要标识的性能运行报告。</param>
        /// <returns>可识别同一次性能运行的字符串。</returns>
        private static string BuildHistoryFingerprint(Run run)
        {
            return $"{run.TestSuite}:{run.Date}:{run.Results.Count}";
        }

        /// <summary>
        /// 创建当前性能运行的独立时间戳归档目录。
        /// </summary>
        /// <returns>已创建的归档目录绝对路径。</returns>
        private static string CreateArchiveDirectory()
        {
            string historyDirectory = GetHistoryDirectory();
            Directory.CreateDirectory(historyDirectory);
            string directoryName = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
            string archiveDirectory = Path.Combine(historyDirectory, directoryName);
            if (Directory.Exists(archiveDirectory))
            {
                archiveDirectory = Path.Combine(historyDirectory, $"{directoryName}_{Guid.NewGuid():N}");
            }

            Directory.CreateDirectory(archiveDirectory);
            return archiveDirectory;
        }

        /// <summary>
        /// 复制性能包最近一次 Test Runner XML，以保留原始测试输出。
        /// </summary>
        /// <param name="archiveDirectory">当前性能运行的归档目录。</param>
        private static void CopyLatestXml(string archiveDirectory)
        {
            string sourceXmlPath = Path.Combine(Application.persistentDataPath, XmlFileName);
            if (!File.Exists(sourceXmlPath))
            {
                return;
            }

            File.Copy(sourceXmlPath, Path.Combine(archiveDirectory, XmlFileName), true);
        }

        /// <summary>
        /// 将运行报告转换为可由表格工具读取的扁平 CSV 文件。
        /// </summary>
        /// <param name="run">需要导出的性能运行报告。</param>
        /// <param name="outputPath">目标 CSV 文件绝对路径。</param>
        private static void WriteCsv(Run run, string outputPath)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Test Name,Class Name,Method Name,Sample Group,Unit,Median,Average,Min,Max,Standard Deviation,Sample Count");
            for (int resultIndex = 0; resultIndex < run.Results.Count; resultIndex++)
            {
                PerformanceTestResult result = run.Results[resultIndex];
                if (result.SampleGroups == null)
                {
                    continue;
                }

                for (int groupIndex = 0; groupIndex < result.SampleGroups.Count; groupIndex++)
                {
                    SampleGroup sampleGroup = result.SampleGroups[groupIndex];
                    AppendCsvRow(builder, result, sampleGroup);
                }
            }

            File.WriteAllText(outputPath, builder.ToString(), Utf8WithoutBom);
        }

        /// <summary>
        /// 向 CSV 内容写入一个测试指标行。
        /// </summary>
        /// <param name="builder">承接 CSV 内容的字符串构建器。</param>
        /// <param name="result">当前性能测试结果。</param>
        /// <param name="sampleGroup">当前性能采样组。</param>
        private static void AppendCsvRow(StringBuilder builder, PerformanceTestResult result, SampleGroup sampleGroup)
        {
            AppendCsvText(builder, result.Name);
            AppendCsvText(builder, result.ClassName);
            AppendCsvText(builder, result.MethodName);
            AppendCsvText(builder, sampleGroup.Name);
            AppendCsvText(builder, sampleGroup.Unit.ToString());
            AppendCsvNumber(builder, sampleGroup.Median);
            AppendCsvNumber(builder, sampleGroup.Average);
            AppendCsvNumber(builder, sampleGroup.Min);
            AppendCsvNumber(builder, sampleGroup.Max);
            AppendCsvNumber(builder, sampleGroup.StandardDeviation);
            builder.Append(sampleGroup.Samples == null ? 0 : sampleGroup.Samples.Count);
            builder.AppendLine();
        }

        /// <summary>
        /// 向 CSV 内容写入经过转义的文本列。
        /// </summary>
        /// <param name="builder">承接 CSV 内容的字符串构建器。</param>
        /// <param name="value">待写入的文本内容。</param>
        private static void AppendCsvText(StringBuilder builder, string value)
        {
            builder.Append('"');
            builder.Append((value ?? string.Empty).Replace("\"", "\"\""));
            builder.Append("\",");
        }

        /// <summary>
        /// 向 CSV 内容写入使用固定小数点规则的数值列。
        /// </summary>
        /// <param name="builder">承接 CSV 内容的字符串构建器。</param>
        /// <param name="value">待写入的数值。</param>
        private static void AppendCsvNumber(StringBuilder builder, double value)
        {
            builder.Append(value.ToString("R", CultureInfo.InvariantCulture));
            builder.Append(',');
        }

        /// <summary>
        /// 读取单个历史目录中的 JSON 文件，并在有效时加入结果列表。
        /// </summary>
        /// <param name="directoryPath">待读取的归档目录。</param>
        /// <param name="entries">承接有效历史记录的列表。</param>
        /// <param name="fingerprints">已展示运行的指纹集合，用于隐藏重复归档。</param>
        private static void TryAddHistoryEntry(string directoryPath, List<BenchmarkPerformanceHistoryEntry> entries, HashSet<string> fingerprints)
        {
            try
            {
                string jsonPath = Path.Combine(directoryPath, JsonFileName);
                if (!File.Exists(jsonPath))
                {
                    return;
                }

                Run run = JsonUtility.FromJson<Run>(File.ReadAllText(jsonPath));
                if (!HasPerformanceResults(run))
                {
                    return;
                }

                if (!fingerprints.Add(BuildHistoryFingerprint(run)))
                {
                    return;
                }

                entries.Add(new BenchmarkPerformanceHistoryEntry(
                    directoryPath,
                    File.GetLastWriteTime(jsonPath),
                    run));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[MiniCore Performance] 读取性能历史失败：{directoryPath}\n{exception.Message}");
            }
        }

        /// <summary>
        /// 按归档创建时间将性能历史记录从新到旧排序。
        /// </summary>
        /// <param name="left">排序比较左侧记录。</param>
        /// <param name="right">排序比较右侧记录。</param>
        /// <returns>排序比较结果。</returns>
        private static int CompareHistoryEntryByTimeDescending(BenchmarkPerformanceHistoryEntry left, BenchmarkPerformanceHistoryEntry right)
        {
            return right.CreatedTime.CompareTo(left.CreatedTime);
        }

        /// <summary>
        /// 获取性能采样单位对应的简短显示后缀。
        /// </summary>
        /// <param name="unit">需要转换的性能采样单位。</param>
        /// <returns>用于编辑器窗口显示的单位后缀。</returns>
        private static string GetUnitSuffix(SampleUnit unit)
        {
            switch (unit)
            {
                case SampleUnit.Nanosecond:
                    return "ns";
                case SampleUnit.Microsecond:
                    return "us";
                case SampleUnit.Millisecond:
                    return "ms";
                case SampleUnit.Second:
                    return "s";
                case SampleUnit.Byte:
                    return "B";
                case SampleUnit.Kilobyte:
                    return "KB";
                case SampleUnit.Megabyte:
                    return "MB";
                case SampleUnit.Gigabyte:
                    return "GB";
                default:
                    return string.Empty;
            }
        }

        #endregion
    }
}
