using Cysharp.Threading.Tasks;
using System;

namespace MiniCore.Model
{
    /// <summary>
    /// 按订阅顺序异步派发传输层数据接收事件。
    /// </summary>
    public static class TransportEventDispatcher
    {
        /// <summary>
        /// 向无发送者参数的数据接收事件逐个派发数据包。
        /// </summary>
        /// <param name="handler">执行该方法所需的 handler 参数。</param>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public static async UniTask DispatchAsync(Func<ReadOnlyMemory<byte>, UniTask> handler, ReadOnlyMemory<byte> data)
        {
            if (handler == null)
            {
                return;
            }

            foreach (var del in handler.GetInvocationList())
            {
                var callback = (Func<ReadOnlyMemory<byte>, UniTask>)del;
                await callback(data);
            }
        }

        /// <summary>
        /// 向带发送者参数的数据接收事件逐个派发数据包。
        /// </summary>
        /// <param name="handler">执行该方法所需的 handler 参数。</param>
        /// <param name="sender">执行该方法所需的 sender 参数。</param>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public static async UniTask DispatchAsync<TSender>(Func<TSender, ReadOnlyMemory<byte>, UniTask> handler, TSender sender, ReadOnlyMemory<byte> data)
        {
            if (handler == null)
            {
                return;
            }

            foreach (var del in handler.GetInvocationList())
            {
                var callback = (Func<TSender, ReadOnlyMemory<byte>, UniTask>)del;
                await callback(sender, data);
            }
        }
    }
}
