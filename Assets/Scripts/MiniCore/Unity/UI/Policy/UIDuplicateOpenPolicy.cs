using System;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.UI
{

    /// <summary>
    /// 重复打开同一逻辑窗口时的处理方式。
    /// </summary>
    public enum UIDuplicateOpenPolicy
    {
        Focus,
        Refresh,
        Ignore,
        Reject
    }
}
