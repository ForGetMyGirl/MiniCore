using MiniCore.Threading;
using System;

namespace MiniCore.Model
{
    /// <summary>
    /// 提供无反射消息派发能力的普通消息处理器契约。
    /// </summary>
    public interface INetworkMessageHandlerInvoker
    {
        /// <summary>
        /// 处理器接收的协议运行时类型。
        /// </summary>
        Type MessageType { get; }

        /// <summary>
        /// 将已反序列化的普通协议派发给具体处理器。
        /// </summary>
        MTask HandleAsync(NetworkSession session, INormalMessage message);
    }
}
