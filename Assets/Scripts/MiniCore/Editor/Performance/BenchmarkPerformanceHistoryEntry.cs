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
    /// 一次已归档性能测试运行的目录、创建时间和报告数据。
    /// </summary>
    internal sealed class BenchmarkPerformanceHistoryEntry
    {
        #region Internal 内部成员

        internal string DirectoryPath { get; }
        internal DateTime CreatedTime { get; }
        internal Run Run { get; }

        /// <summary>
        /// 创建一条性能历史记录。
        /// </summary>
        /// <param name="directoryPath">保存该次运行文件的目录。</param>
        /// <param name="createdTime">归档文件的本地创建时间。</param>
        /// <param name="run">从 JSON 读取出的性能运行报告。</param>
        internal BenchmarkPerformanceHistoryEntry(string directoryPath, DateTime createdTime, Run run)
        {
            DirectoryPath = directoryPath;
            CreatedTime = createdTime;
            Run = run;
        }

        #endregion
    }
}
