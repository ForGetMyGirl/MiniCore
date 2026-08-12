using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{

    /// <summary>
    /// MTask 未观察异常的统一监督器。
    /// </summary>
    public static class MTaskSupervisor
    {
        #region Private 私有成员

        private static readonly object OrphanGate = new object(); // 保护已上报的孤立任务类型集合。
        private static readonly HashSet<Type> ReportedOrphanTypes = new HashSet<Type>(); // 每种结果源类型只警告一次，避免开发环境刷屏和稳态分配。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 当 Forget 任务出现未处理异常时触发。
        /// </summary>
        public static event Action<Exception, string> UnhandledException;

        /// <summary>
        /// 当任务无法找到父节点或 Owner 而挂到应用根域时触发。
        /// </summary>
        public static event Action<string> OrphanedTask;

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 上报一个未观察的后台任务异常。
        /// </summary>
        /// <param name="exception">任务抛出的异常。</param>
        /// <param name="ownerName">所属 Owner 的诊断名称。</param>
        internal static void Report(Exception exception, string ownerName)
        {
            Action<Exception, string> handler = UnhandledException;
            if (handler != null)
            {
                handler(exception, ownerName);
                return;
            }

            Console.Error.WriteLine($"[MTask] 未处理异常 owner:{ownerName}\n{exception}");
        }

        /// <summary>
        /// 上报一个无父节点且无 Owner 的应用根任务。
        /// </summary>
        /// <param name="taskType">任务结果源类型。</param>
        internal static void ReportOrphan(Type taskType)
        {
            lock (OrphanGate)
            {
                if (!ReportedOrphanTypes.Add(taskType))
                {
                    return;
                }
            }

            string taskName = taskType.FullName;
            Action<string> handler = OrphanedTask;
            if (handler != null)
            {
                handler(taskName);
                return;
            }

            Console.Error.WriteLine($"[MTask] 任务未找到父节点或 Owner，已挂到应用根域：{taskName}");
        }

        #endregion
    }
}
