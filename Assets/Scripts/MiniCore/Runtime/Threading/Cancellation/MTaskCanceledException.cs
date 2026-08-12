using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{

    /// <summary>
    /// MTask 的协作式取消异常；同一任务域复用一个实例以避免取消风暴产生大量垃圾。
    /// </summary>
    public sealed class MTaskCanceledException : OperationCanceledException
    {
        #region Internal 内部成员

        /// <summary>
        /// 创建指定任务域使用的取消异常。
        /// </summary>
        /// <param name="domainName">任务域诊断名称。</param>
        internal MTaskCanceledException(string domainName)
            : base($"MTask 任务域已取消：{domainName}")
        {
        }

        #endregion
    }
}
