using System;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.UI
{

    /// <summary>
    /// 窗口关闭后的 View 处理方式。
    /// </summary>
    public enum UICachePolicy
    {
        DestroyOnClose,
        CacheOnClose,
        Resident
    }
}
