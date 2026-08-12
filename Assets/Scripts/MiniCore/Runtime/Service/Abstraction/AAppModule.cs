using System;
using MiniCore.Model;
using MiniCore.Threading;

namespace MiniCore.Service
{

    /// <summary>
    /// 公共应用模块的组件基类。
    /// 模块由业务按需创建，可由接口隐藏具体实现。
    /// </summary>
    public abstract class AAppModule : AComponent
    {
    }
}
