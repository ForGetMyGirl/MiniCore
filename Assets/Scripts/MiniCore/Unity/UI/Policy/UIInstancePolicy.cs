using System;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.UI
{

    /// <summary>
    /// 同一窗口定义允许存在的实例形式。
    /// </summary>
    public enum UIInstancePolicy
    {
        Singleton,
        SingletonPerKey,
        Multiple,
        Queue,
        Replace
    }
}
