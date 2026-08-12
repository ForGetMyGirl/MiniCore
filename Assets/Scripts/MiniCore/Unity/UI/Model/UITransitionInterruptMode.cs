using System;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.UI
{

    /// <summary>
    /// 动画被新生命周期操作打断时的收敛方式。
    /// </summary>
    public enum UITransitionInterruptMode
    {
        KeepCurrent,
        CompleteCurrent,
        RestoreOriginal
    }
}
