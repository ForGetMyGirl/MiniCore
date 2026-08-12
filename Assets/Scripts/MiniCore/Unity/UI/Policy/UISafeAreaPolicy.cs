using System;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.UI
{

    /// <summary>
    /// 窗口内容相对设备安全区域的适配方式。
    /// </summary>
    public enum UISafeAreaPolicy
    {
        Inherit,
        ConstrainContent,
        ConstrainWindow,
        Ignore,
        Custom
    }
}
