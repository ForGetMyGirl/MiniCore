using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Threading;
using UnityEngine;

namespace MiniCore.Unity
{
    /// <summary>
    /// Unity 生命周期到纯 C# Global Runtime 的唯一适配入口。
    /// </summary>
    public sealed class UnityGlobalDriver : MonoBehaviour
    {
        #region Private 私有成员

        private static UnityGlobalDriver instance; // 当前持久化 Driver 实例。
        private MTaskMainThreadExecutor mTaskExecutor; // Unity 主线程上的 MTask 续体执行器。

        /// <summary>
        /// 初始化 Runtime，并保证仅保留一个跨场景 Driver。
        /// </summary>
        private void Awake()
        {
            if (instance != null && !ReferenceEquals(instance, this))
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            mTaskExecutor = new MTaskMainThreadExecutor("Unity.Main");
            MTaskExecutors.Unity = mTaskExecutor;
            MTaskRuntime.Initialize(mTaskExecutor);
            MTaskSupervisor.UnhandledException += OnMTaskUnhandledException;
            MTaskSupervisor.OrphanedTask += OnMTaskOrphanedTask;
            Global.Initialize(new UnityTimeProvider());
            LogSwitch.SetSink(new UnityLogSink());
        }

        /// <summary>
        /// 驱动当前激活组件的一次 Runtime Tick。
        /// </summary>
        private void Update()
        {
            mTaskExecutor?.Drain();
            Global.Tick();
            mTaskExecutor?.Drain();
        }

        /// <summary>
        /// 在应用退出时释放全部全局组件。
        /// </summary>
        private void OnApplicationQuit()
        {
            MTaskRuntime.BeginFastShutdown();
            Global.Shutdown();
            MTaskRuntime.CancelApplicationTasks();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            MTaskDiagnosticsSnapshot snapshot = MTaskDiagnostics.Capture();
            if (snapshot.ActiveNodes > 0 || snapshot.ActiveTimers > 0)
            {
                LogSwitch.Warning($"MTask 快速退出：已取消但未等待退场 activeNodes={snapshot.ActiveNodes}, activeTimers={snapshot.ActiveTimers}");
            }
#endif
            mTaskExecutor?.Drain();
            MTaskRuntime.Shutdown();
        }

        /// <summary>
        /// 在 Driver 销毁时清理静态实例引用。
        /// </summary>
        private void OnDestroy()
        {
            if (ReferenceEquals(instance, this))
            {
                MTaskSupervisor.UnhandledException -= OnMTaskUnhandledException;
                MTaskSupervisor.OrphanedTask -= OnMTaskOrphanedTask;
                instance = null;
            }
        }

        /// <summary>
        /// 将未被业务观察的 MTask 异常写入统一日志。
        /// </summary>
        /// <param name="exception">后台任务异常。</param>
        /// <param name="ownerName">任务所属 Owner 名称。</param>
        private static void OnMTaskUnhandledException(System.Exception exception, string ownerName)
        {
            LogSwitch.Error($"MTask 未处理异常 owner:{ownerName}\n{exception}");
        }

        /// <summary>
        /// 将未找到父节点或 Owner 的 MTask 记录为开发警告。
        /// </summary>
        /// <param name="taskName">任务结果源诊断名称。</param>
        private static void OnMTaskOrphanedTask(string taskName)
        {
            LogSwitch.Warning($"MTask 未找到父节点或 Owner，已挂到应用根域：{taskName}");
        }

        #endregion
    }
}
