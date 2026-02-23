using Cysharp.Threading.Tasks;
using System;

namespace MiniCore.Model
{
    public static class TransportEventDispatcher
    {
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
