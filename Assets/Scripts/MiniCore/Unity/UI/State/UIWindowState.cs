using System;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.UI
{

    /// <summary>
    /// 窗口当前所处的生命周期状态。
    /// </summary>
    public enum UIWindowState
    {
        None,
        Loading,
        Staging,
        Opening,
        Active,
        Closing,
        Cached,
        Destroyed,
        Failed
    }
}
