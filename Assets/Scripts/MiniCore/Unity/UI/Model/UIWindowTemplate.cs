using System;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.UI
{

    /// <summary>
    /// 窗口创建向导使用的业务模板。
    /// </summary>
    public enum UIWindowTemplate
    {
        Screen = 0,
        FloatingWindow = 1,
        ModalPopup = 2,
        Toast = 3,
        Hud = 4,
        Guide = 5,
        System = 6,
        Custom = 7
    }
}
