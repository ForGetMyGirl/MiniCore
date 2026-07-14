using System;
using System.Collections.Generic;
using Unity.PerformanceTesting.Data;
using UnityEditor;
using UnityEngine;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 浏览、筛选、删除项目内自动归档的性能测试记录，并比较两次运行的指标变化。
    /// </summary>
    public sealed class BenchmarkPerformanceHistoryWindow : EditorWindow
    {
        #region Private 私有成员

        private const string AllTestTypes = "全部测试类型"; // 测试类型下拉框的全量选项文本。
        private readonly List<BenchmarkPerformanceHistoryEntry> historyEntries = new List<BenchmarkPerformanceHistoryEntry>(); // 已加载的全部性能历史记录。
        private readonly List<BenchmarkPerformanceHistoryEntry> filteredHistoryEntries = new List<BenchmarkPerformanceHistoryEntry>(); // 当前筛选条件下展示的性能历史记录。
        private readonly List<string> testTypeOptions = new List<string>(); // 可筛选的测试方法名称。
        private Vector2 historyScrollPosition; // 历史记录列表的滚动位置。
        private Vector2 comparisonScrollPosition; // 对比结果区域的滚动位置。
        private HistoryTimeRange timeRange = HistoryTimeRange.All; // 当前选择的历史时间范围。
        private HistorySortOrder sortOrder = HistorySortOrder.NewestFirst; // 当前历史记录排序方式。
        private string selectedTestType = AllTestTypes; // 当前选择的测试类型名称。
        private string baselineDirectoryPath; // 当前选中的基准记录目录。
        private string candidateDirectoryPath; // 当前选中的候选记录目录。

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 历史记录可选择的时间范围。
        /// </summary>
        private enum HistoryTimeRange
        {
            All,
            Today,
            LastSevenDays,
            LastThirtyDays
        }

        /// <summary>
        /// 历史记录可选择的时间排序方式。
        /// </summary>
        private enum HistorySortOrder
        {
            NewestFirst,
            OldestFirst
        }

        /// <summary>
        /// 打开性能历史与对比窗口。
        /// </summary>
        [MenuItem("MiniCore/Performance/History", priority = 2200)]
        private static void Open()
        {
            GetWindow<BenchmarkPerformanceHistoryWindow>("Performance History").Show();
        }

        /// <summary>
        /// 在窗口启用时读取已有性能历史。
        /// </summary>
        private void OnEnable()
        {
            RefreshHistory();
        }

        /// <summary>
        /// 绘制性能历史列表、筛选控件和基准对比结果。
        /// </summary>
        private void OnGUI()
        {
            DrawToolbar();
            DrawFilterBar();
            DrawHistoryList();
            DrawComparison();
        }

        /// <summary>
        /// 绘制刷新、打开目录和全量清空操作的工具栏。
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton))
            {
                RefreshHistory();
            }

            if (GUILayout.Button("打开归档目录", EditorStyles.toolbarButton))
            {
                EditorUtility.RevealInFinder(BenchmarkPerformanceStorage.GetHistoryDirectory());
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("清空全部历史", EditorStyles.toolbarButton))
            {
                ClearAllHistory();
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制时间、测试类型和排序方式筛选控件。
        /// </summary>
        private void DrawFilterBar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            HistoryTimeRange nextTimeRange = (HistoryTimeRange)EditorGUILayout.EnumPopup("时间范围", timeRange);
            int selectedTestTypeIndex = GetSelectedTestTypeIndex();
            int nextTestTypeIndex = EditorGUILayout.Popup("测试类型", selectedTestTypeIndex, testTypeOptions.ToArray());
            HistorySortOrder nextSortOrder = (HistorySortOrder)EditorGUILayout.EnumPopup("排序", sortOrder);
            EditorGUILayout.EndHorizontal();

            if (nextTimeRange != timeRange || nextTestTypeIndex != selectedTestTypeIndex || nextSortOrder != sortOrder)
            {
                timeRange = nextTimeRange;
                selectedTestType = testTypeOptions[nextTestTypeIndex];
                sortOrder = nextSortOrder;
                ApplyFilters();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"显示 {filteredHistoryEntries.Count} / {historyEntries.Count} 条记录", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            GUI.enabled = filteredHistoryEntries.Count > 0;
            if (GUILayout.Button("删除当前筛选结果", GUILayout.Width(150f)))
            {
                DeleteFilteredHistory();
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制当前筛选条件下的全部历史性能记录及其简要指标。
        /// </summary>
        private void DrawHistoryList()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("历史记录", EditorStyles.boldLabel);
            if (historyEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("尚未找到自动归档记录。运行任意 [Performance] 测试后，结果会自动保存到 BenchmarkPerformance/History。", MessageType.Info);
                return;
            }

            if (filteredHistoryEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("没有符合当前时间范围和测试类型的历史记录。", MessageType.Info);
                return;
            }

            historyScrollPosition = EditorGUILayout.BeginScrollView(historyScrollPosition, GUILayout.Height(position.height * 0.46f));
            for (int index = 0; index < filteredHistoryEntries.Count; index++)
            {
                DrawHistoryEntry(filteredHistoryEntries[index]);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制一条历史性能记录的时间、测试摘要和操作按钮。
        /// </summary>
        /// <param name="entry">需要展示的性能历史记录。</param>
        private void DrawHistoryEntry(BenchmarkPerformanceHistoryEntry entry)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(entry.CreatedTime.ToString("yyyy-MM-dd HH:mm:ss"), EditorStyles.boldLabel);
            DrawRunSummary(entry.Run);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("设为基准"))
            {
                baselineDirectoryPath = entry.DirectoryPath;
            }

            if (GUILayout.Button("设为对比"))
            {
                candidateDirectoryPath = entry.DirectoryPath;
            }

            if (GUILayout.Button("打开文件夹"))
            {
                EditorUtility.RevealInFinder(entry.DirectoryPath);
            }

            if (GUILayout.Button("删除此记录"))
            {
                DeleteHistoryEntry(entry);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制一条运行报告中每个测试和采样组的中位数摘要。
        /// </summary>
        /// <param name="run">需要展示的性能运行报告。</param>
        private void DrawRunSummary(Run run)
        {
            for (int resultIndex = 0; resultIndex < run.Results.Count; resultIndex++)
            {
                PerformanceTestResult result = run.Results[resultIndex];
                EditorGUILayout.LabelField(result.MethodName, EditorStyles.miniBoldLabel);
                if (result.SampleGroups == null)
                {
                    continue;
                }

                for (int groupIndex = 0; groupIndex < result.SampleGroups.Count; groupIndex++)
                {
                    SampleGroup group = result.SampleGroups[groupIndex];
                    EditorGUILayout.LabelField($"  {group.Name}: Median {BenchmarkPerformanceStorage.FormatValue(group.Median, group.Unit)}");
                }
            }
        }

        /// <summary>
        /// 绘制当前基准记录和候选记录之间的同名指标对比。
        /// </summary>
        private void DrawComparison()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("基准对比", EditorStyles.boldLabel);
            if (!TryGetSelectedEntries(out BenchmarkPerformanceHistoryEntry baseline, out BenchmarkPerformanceHistoryEntry candidate))
            {
                EditorGUILayout.HelpBox("在两条历史记录上分别点击“设为基准”和“设为对比”。时间指标中，正百分比表示候选记录更慢。", MessageType.Info);
                return;
            }

            if (ReferenceEquals(baseline, candidate))
            {
                EditorGUILayout.HelpBox("基准和对比不能选择同一条记录。", MessageType.Warning);
                return;
            }

            comparisonScrollPosition = EditorGUILayout.BeginScrollView(comparisonScrollPosition);
            bool foundComparableGroup = false;
            for (int resultIndex = 0; resultIndex < baseline.Run.Results.Count; resultIndex++)
            {
                PerformanceTestResult baselineResult = baseline.Run.Results[resultIndex];
                PerformanceTestResult candidateResult = FindResult(candidate.Run, baselineResult.Name);
                if (candidateResult == null || baselineResult.SampleGroups == null || candidateResult.SampleGroups == null)
                {
                    continue;
                }

                for (int groupIndex = 0; groupIndex < baselineResult.SampleGroups.Count; groupIndex++)
                {
                    SampleGroup baselineGroup = baselineResult.SampleGroups[groupIndex];
                    SampleGroup candidateGroup = FindSampleGroup(candidateResult, baselineGroup.Name);
                    if (candidateGroup == null)
                    {
                        continue;
                    }

                    foundComparableGroup = true;
                    DrawGroupComparison(baselineResult.MethodName, baselineGroup, candidateGroup);
                }
            }

            if (!foundComparableGroup)
            {
                EditorGUILayout.HelpBox("两次记录没有同名测试和 Sample Group，无法直接比较。", MessageType.Warning);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制一个同名采样组的基准值、候选值及变化百分比。
        /// </summary>
        /// <param name="methodName">当前性能测试方法名称。</param>
        /// <param name="baselineGroup">基准记录中的采样组。</param>
        /// <param name="candidateGroup">候选记录中的同名采样组。</param>
        private void DrawGroupComparison(string methodName, SampleGroup baselineGroup, SampleGroup candidateGroup)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"{methodName} / {baselineGroup.Name}", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"基准 Median: {BenchmarkPerformanceStorage.FormatValue(baselineGroup.Median, baselineGroup.Unit)}");
            EditorGUILayout.LabelField($"对比 Median: {BenchmarkPerformanceStorage.FormatValue(candidateGroup.Median, candidateGroup.Unit)}");
            if (Math.Abs(baselineGroup.Median) < double.Epsilon)
            {
                EditorGUILayout.LabelField("变化：基准值为 0，不计算百分比。GC 等零基准指标请直接比较数值。");
            }
            else
            {
                double changePercent = (candidateGroup.Median - baselineGroup.Median) / baselineGroup.Median * 100d;
                EditorGUILayout.LabelField($"变化：{changePercent:+0.00;-0.00;0.00}%（时间指标正数表示更慢）");
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 弹出确认框后删除全部自动归档历史记录。
        /// </summary>
        private void ClearAllHistory()
        {
            if (!EditorUtility.DisplayDialog("清空全部性能历史", "将删除 BenchmarkPerformance/History 中的全部自动归档记录。BenchmarkPerformance 根目录的手动文件不会受影响。此操作不可撤销。", "清空全部", "取消"))
            {
                return;
            }

            int deletedCount = BenchmarkPerformanceStorage.ClearHistory();
            Debug.Log($"[MiniCore Performance] 已清空 {deletedCount} 条自动归档性能历史。");
            ClearSelections();
            RefreshHistory();
        }

        /// <summary>
        /// 弹出确认框后删除当前筛选条件下的全部历史记录。
        /// </summary>
        private void DeleteFilteredHistory()
        {
            if (!EditorUtility.DisplayDialog("删除当前筛选结果", $"将删除当前筛选出的 {filteredHistoryEntries.Count} 条自动归档记录。此操作不可撤销。", "删除", "取消"))
            {
                return;
            }

            int deletedCount = BenchmarkPerformanceStorage.DeleteHistoryEntries(filteredHistoryEntries);
            Debug.Log($"[MiniCore Performance] 已删除 {deletedCount} 条筛选后的性能历史。");
            ClearSelections();
            RefreshHistory();
        }

        /// <summary>
        /// 弹出确认框后删除一条自动归档性能历史记录。
        /// </summary>
        /// <param name="entry">需要删除的历史记录。</param>
        private void DeleteHistoryEntry(BenchmarkPerformanceHistoryEntry entry)
        {
            if (!EditorUtility.DisplayDialog("删除性能历史", $"将删除 {entry.CreatedTime:yyyy-MM-dd HH:mm:ss} 的自动归档记录。此操作不可撤销。", "删除", "取消"))
            {
                return;
            }

            if (BenchmarkPerformanceStorage.DeleteHistoryEntry(entry.DirectoryPath))
            {
                Debug.Log($"[MiniCore Performance] 已删除性能历史：{entry.DirectoryPath}");
                ClearSelectionIfMatches(entry.DirectoryPath);
                RefreshHistory();
            }
        }

        /// <summary>
        /// 重新读取归档目录、重建筛选项并校正已失效的选择路径。
        /// </summary>
        private void RefreshHistory()
        {
            historyEntries.Clear();
            historyEntries.AddRange(BenchmarkPerformanceStorage.LoadHistory());
            RebuildTestTypeOptions();
            ClearInvalidSelections();
            ApplyFilters();
        }

        /// <summary>
        /// 根据当前时间范围、测试类型和排序方式重建展示列表。
        /// </summary>
        private void ApplyFilters()
        {
            filteredHistoryEntries.Clear();
            DateTime now = DateTime.Now;
            for (int index = 0; index < historyEntries.Count; index++)
            {
                BenchmarkPerformanceHistoryEntry entry = historyEntries[index];
                if (!MatchesTimeRange(entry, now) || !MatchesTestType(entry))
                {
                    continue;
                }

                filteredHistoryEntries.Add(entry);
            }

            if (sortOrder == HistorySortOrder.OldestFirst)
            {
                filteredHistoryEntries.Reverse();
            }
        }

        /// <summary>
        /// 收集当前历史记录中出现过的测试方法名称，供测试类型下拉框使用。
        /// </summary>
        private void RebuildTestTypeOptions()
        {
            testTypeOptions.Clear();
            testTypeOptions.Add(AllTestTypes);
            var uniqueTypes = new HashSet<string>();
            for (int entryIndex = 0; entryIndex < historyEntries.Count; entryIndex++)
            {
                Run run = historyEntries[entryIndex].Run;
                for (int resultIndex = 0; resultIndex < run.Results.Count; resultIndex++)
                {
                    string methodName = run.Results[resultIndex].MethodName;
                    if (!string.IsNullOrEmpty(methodName))
                    {
                        uniqueTypes.Add(methodName);
                    }
                }
            }

            var sortedTypes = new List<string>(uniqueTypes);
            sortedTypes.Sort(StringComparer.Ordinal);
            testTypeOptions.AddRange(sortedTypes);
            if (!testTypeOptions.Contains(selectedTestType))
            {
                selectedTestType = AllTestTypes;
            }
        }

        /// <summary>
        /// 获取当前选中测试类型在下拉选项中的索引。
        /// </summary>
        /// <returns>当前选中测试类型的有效索引。</returns>
        private int GetSelectedTestTypeIndex()
        {
            int index = testTypeOptions.IndexOf(selectedTestType);
            return index >= 0 ? index : 0;
        }

        /// <summary>
        /// 判断一条历史记录是否位于当前选中的时间范围内。
        /// </summary>
        /// <param name="entry">待判断的性能历史记录。</param>
        /// <param name="now">本次筛选使用的当前本地时间。</param>
        /// <returns>记录满足时间范围时返回 true。</returns>
        private bool MatchesTimeRange(BenchmarkPerformanceHistoryEntry entry, DateTime now)
        {
            switch (timeRange)
            {
                case HistoryTimeRange.Today:
                    return entry.CreatedTime.Date == now.Date;
                case HistoryTimeRange.LastSevenDays:
                    return entry.CreatedTime >= now.AddDays(-7);
                case HistoryTimeRange.LastThirtyDays:
                    return entry.CreatedTime >= now.AddDays(-30);
                default:
                    return true;
            }
        }

        /// <summary>
        /// 判断一条历史记录是否包含当前选中的测试方法名称。
        /// </summary>
        /// <param name="entry">待判断的性能历史记录。</param>
        /// <returns>记录包含当前测试类型时返回 true。</returns>
        private bool MatchesTestType(BenchmarkPerformanceHistoryEntry entry)
        {
            if (string.Equals(selectedTestType, AllTestTypes, StringComparison.Ordinal))
            {
                return true;
            }

            for (int resultIndex = 0; resultIndex < entry.Run.Results.Count; resultIndex++)
            {
                if (string.Equals(entry.Run.Results[resultIndex].MethodName, selectedTestType, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取当前选择的基准与候选历史记录。
        /// </summary>
        /// <param name="baseline">成功时返回选中的基准记录。</param>
        /// <param name="candidate">成功时返回选中的候选记录。</param>
        /// <returns>两条选择均有效时返回 true。</returns>
        private bool TryGetSelectedEntries(out BenchmarkPerformanceHistoryEntry baseline, out BenchmarkPerformanceHistoryEntry candidate)
        {
            baseline = FindEntryByDirectoryPath(baselineDirectoryPath);
            candidate = FindEntryByDirectoryPath(candidateDirectoryPath);
            return baseline != null && candidate != null;
        }

        /// <summary>
        /// 根据自动归档目录查找已加载的历史记录。
        /// </summary>
        /// <param name="directoryPath">需要查找的历史记录目录。</param>
        /// <returns>找到时返回对应历史记录，否则返回 null。</returns>
        private BenchmarkPerformanceHistoryEntry FindEntryByDirectoryPath(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                return null;
            }

            for (int index = 0; index < historyEntries.Count; index++)
            {
                BenchmarkPerformanceHistoryEntry entry = historyEntries[index];
                if (string.Equals(entry.DirectoryPath, directoryPath, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>
        /// 清除已不在当前历史集合中的基准和候选选择。
        /// </summary>
        private void ClearInvalidSelections()
        {
            if (FindEntryByDirectoryPath(baselineDirectoryPath) == null)
            {
                baselineDirectoryPath = null;
            }

            if (FindEntryByDirectoryPath(candidateDirectoryPath) == null)
            {
                candidateDirectoryPath = null;
            }
        }

        /// <summary>
        /// 清除当前基准和候选选择。
        /// </summary>
        private void ClearSelections()
        {
            baselineDirectoryPath = null;
            candidateDirectoryPath = null;
        }

        /// <summary>
        /// 当指定目录被删除时清除与其关联的基准或候选选择。
        /// </summary>
        /// <param name="directoryPath">刚刚删除的历史记录目录。</param>
        private void ClearSelectionIfMatches(string directoryPath)
        {
            if (string.Equals(baselineDirectoryPath, directoryPath, StringComparison.Ordinal))
            {
                baselineDirectoryPath = null;
            }

            if (string.Equals(candidateDirectoryPath, directoryPath, StringComparison.Ordinal))
            {
                candidateDirectoryPath = null;
            }
        }

        /// <summary>
        /// 在运行报告中查找指定完整名称的性能测试结果。
        /// </summary>
        /// <param name="run">需要搜索的候选运行报告。</param>
        /// <param name="resultName">目标性能测试完整名称。</param>
        /// <returns>找到时返回对应测试结果，否则返回 null。</returns>
        private static PerformanceTestResult FindResult(Run run, string resultName)
        {
            for (int index = 0; index < run.Results.Count; index++)
            {
                PerformanceTestResult result = run.Results[index];
                if (string.Equals(result.Name, resultName, StringComparison.Ordinal))
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// 在一个性能测试结果中查找指定名称的采样组。
        /// </summary>
        /// <param name="result">需要搜索的性能测试结果。</param>
        /// <param name="sampleGroupName">目标采样组名称。</param>
        /// <returns>找到时返回对应采样组，否则返回 null。</returns>
        private static SampleGroup FindSampleGroup(PerformanceTestResult result, string sampleGroupName)
        {
            for (int index = 0; index < result.SampleGroups.Count; index++)
            {
                SampleGroup group = result.SampleGroups[index];
                if (string.Equals(group.Name, sampleGroupName, StringComparison.Ordinal))
                {
                    return group;
                }
            }

            return null;
        }

        #endregion
    }
}
