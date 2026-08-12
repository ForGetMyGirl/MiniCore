using System;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.UI
{

    /// <summary>
    /// ApplicationUIRoot 中的逻辑显示层。
    /// </summary>
    public enum UILayer
    {
        Background = 0,
        Hud = 1,
        Screen = 2,
        Window = 3,
        Popup = 4,
        Toast = 5,
        Guide = 6,
        Drag = 7,
        Transition = 8,
        System = 9,
        Debug = 10,
        Tooltip = 11
    }
}
