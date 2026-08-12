using System;
using System.Collections.Generic;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Eventing
{

    /// <summary>
    /// 标记仅允许异步派发和异步监听的事件。
    /// 同一事件类型不能同时实现 <see cref="ISyncEvent"/>。
    /// </summary>
    public interface IAsyncEvent : IEvent
    {
    }
}
