using System;
using System.Collections.Generic;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Eventing
{

    /// <summary>
    /// 接收异步事件的对象监听器契约。
    /// 事件频道会按注册顺序等待每个监听器返回的任务。
    /// </summary>
    /// <typeparam name="TEvent">要处理的异步事件类型。</typeparam>
    public interface IAsyncEventHandler<in TEvent> where TEvent : IEvent
    {
        /// <summary>
        /// 异步处理一条事件。
        /// </summary>
        /// <param name="@event">本次派发的事件数据。</param>
        /// <returns>处理完成任务。</returns>
        MTask HandleAsync(TEvent @event);
    }
}
