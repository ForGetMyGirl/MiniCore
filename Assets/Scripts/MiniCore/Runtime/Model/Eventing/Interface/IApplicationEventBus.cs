using System;
using System.Collections.Generic;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Eventing
{

    /// <summary>
    /// 标记由 Global 模块注册表按需创建的应用级事件频道。
    /// 应用级频道仅用于跨模块、低频的应用通知。
    /// </summary>
    public interface IApplicationEventBus : IEventBus, IAppModule
    {
    }
}
