using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MiniCore.Threading
{

    /// <summary>
    /// 延迟派发句柄。
    /// </summary>
    public interface IMTaskScheduledHandle
    {
        /// <summary>
        /// 尝试取消尚未执行的延迟回调。
        /// </summary>
        void Cancel();
    }
}
