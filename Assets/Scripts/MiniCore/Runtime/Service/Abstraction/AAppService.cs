using System;
using MiniCore.Model;
using MiniCore.Threading;

namespace MiniCore.Service
{

    /// <summary>
    /// 系统级应用服务的组件基类。
    /// 服务仍使用 AComponent 的 owner 引用计数与 Tick 生命周期，但对外必须以 IAppService 接口暴露。
    /// </summary>
    public abstract class AAppService : AComponent
    {
    }
}
