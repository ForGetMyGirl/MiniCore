using System;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 监听 Unity Test Runner，并将每次有效性能测试结果自动归档到项目目录。
    /// </summary>
    [InitializeOnLoad]
    internal static class BenchmarkPerformanceRecorder
    {
        #region Private 私有成员

        private static readonly TestRunnerApi TestRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>(); // 维持 Test Runner 回调注册的 API 实例。
        private static readonly BenchmarkPerformanceCallbacks Callbacks = ScriptableObject.CreateInstance<BenchmarkPerformanceCallbacks>(); // 接收 Test Runner 生命周期事件的回调对象。
        private static DateTime runStartedUtc; // 当前 Test Runner 运行的开始时间。
        private static bool hasActiveRun; // 标识是否存在等待归档判定的测试运行。
        private static bool archiveQueued; // 防止同一次运行重复安排延迟归档。
        private static bool callbacksRegistered; // 标识当前域是否已注册 Test Runner 回调。

        /// <summary>
        /// 在 Unity 域重载后注册 Test Runner 回调。
        /// </summary>
        static BenchmarkPerformanceRecorder()
        {
            RegisterCallbacks();
            AssemblyReloadEvents.beforeAssemblyReload += UnregisterCallbacks;
            EditorApplication.quitting += UnregisterCallbacks;
        }

        /// <summary>
        /// 注册当前域的 Test Runner 回调，避免同一回调对象被重复注册。
        /// </summary>
        private static void RegisterCallbacks()
        {
            if (callbacksRegistered)
            {
                return;
            }

            TestRunnerApi.RegisterCallbacks(Callbacks);
            callbacksRegistered = true;
        }

        /// <summary>
        /// 在域重载或编辑器退出前注销当前回调，避免旧域回调残留导致重复归档。
        /// </summary>
        private static void UnregisterCallbacks()
        {
            if (!callbacksRegistered)
            {
                return;
            }

            TestRunnerApi.UnregisterCallbacks(Callbacks);
            callbacksRegistered = false;
            AssemblyReloadEvents.beforeAssemblyReload -= UnregisterCallbacks;
            EditorApplication.quitting -= UnregisterCallbacks;
        }

        /// <summary>
        /// 记录一次 Test Runner 运行的开始时间。
        /// </summary>
        private static void RecordRunStarted()
        {
            runStartedUtc = DateTime.UtcNow;
            hasActiveRun = true;
        }

        /// <summary>
        /// 在运行结束后延迟归档，确保性能包已先写完最新 JSON 结果。
        /// </summary>
        private static void QueueArchive()
        {
            if (!hasActiveRun || archiveQueued)
            {
                return;
            }

            archiveQueued = true;
            EditorApplication.delayCall += ArchiveAfterTestRunnerCallbacks;
        }

        /// <summary>
        /// 读取性能包写入的最新结果，并在结果确属本次运行时创建项目内归档。
        /// </summary>
        private static void ArchiveAfterTestRunnerCallbacks()
        {
            archiveQueued = false;
            if (!hasActiveRun)
            {
                return;
            }

            hasActiveRun = false;
            if (BenchmarkPerformanceStorage.TryArchiveLatestRun(runStartedUtc, out string archiveDirectory))
            {
                Debug.Log($"[MiniCore Performance] 已自动归档性能结果：{archiveDirectory}");
            }
        }

        /// <summary>
        /// 接收 Unity Test Runner 生命周期回调的 ScriptableObject 实现。
        /// </summary>
        private sealed class BenchmarkPerformanceCallbacks : ScriptableObject, ICallbacks
        {
            /// <summary>
            /// 在 Test Runner 开始执行前记录归档判定所需的开始时间。
            /// </summary>
            /// <param name="testsToRun">本次将要执行的测试集合。</param>
            void ICallbacks.RunStarted(ITestAdaptor testsToRun)
            {
                RecordRunStarted();
            }

            /// <summary>
            /// 在 Test Runner 结束后安排性能结果归档。
            /// </summary>
            /// <param name="result">本次测试运行的总结果。</param>
            void ICallbacks.RunFinished(ITestResultAdaptor result)
            {
                QueueArchive();
            }

            /// <summary>
            /// 单个测试开始时不需要额外处理。
            /// </summary>
            /// <param name="test">当前开始执行的测试。</param>
            void ICallbacks.TestStarted(ITestAdaptor test)
            {
            }

            /// <summary>
            /// 单个测试结束时不需要额外处理。
            /// </summary>
            /// <param name="result">当前结束测试的执行结果。</param>
            void ICallbacks.TestFinished(ITestResultAdaptor result)
            {
            }
        }

        #endregion
    }
}
