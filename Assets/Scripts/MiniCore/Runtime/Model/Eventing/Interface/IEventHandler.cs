using System;
using System.Collections.Generic;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Eventing
{

    /// <summary>
    /// 接收同步事件的无分配对象监听器契约。
    /// 性能敏感路径应优先让已有对象实现此接口，而不是创建委托或 lambda。
    /// </summary>
    /// <typeparam name="TEvent">要处理的同步事件类型。</typeparam>
    public interface IEventHandler<in TEvent> where TEvent : IEvent
    {
        /// <summary>
        /// 处理一条同步事件。
        /// </summary>
        /// <param name="@event">本次派发的事件数据。</param>
        void Handle(TEvent @event);
    }
}
