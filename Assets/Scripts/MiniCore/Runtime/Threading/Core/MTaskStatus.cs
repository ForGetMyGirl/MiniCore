using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// MTask 异步结果状态。
    /// </summary>
    public enum MTaskStatus : byte
    {
        /// <summary>
        /// 任务仍在运行。
        /// </summary>
        Pending = 0,

        /// <summary>
        /// 任务成功完成。
        /// </summary>
        Succeeded = 1,

        /// <summary>
        /// 任务因异常失败。
        /// </summary>
        Faulted = 2,

        /// <summary>
        /// 任务被协作式取消。
        /// </summary>
        Canceled = 3
    }
}
