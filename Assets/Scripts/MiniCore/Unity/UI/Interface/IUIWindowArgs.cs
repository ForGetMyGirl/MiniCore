using System;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.UI
{

    /// <summary>
    /// 标记只允许传给指定窗口路由的打开参数。
    /// </summary>
    /// <typeparam name="TRoute">目标窗口路由。</typeparam>
    public interface IUIWindowArgs<TRoute> where TRoute : IUIWindowRoute
    {
    }
}
