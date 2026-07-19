using MiniCore.Threading;
using System;

namespace MiniCore.Model
{
    /// <summary>
    /// 普通消息处理器基类，由网络消息组件派发反序列化后的协议对象。
    /// </summary>
    public abstract class AMHandler<TMessage> : INetworkMessageHandlerInvoker where TMessage : INormalMessage
    {
        /// <summary>
        /// 从 opcode 注册表解析当前消息类型对应的协议号。
        /// </summary>
        public virtual uint Opcode
        {
            get
            {
                var msgType = typeof(TMessage);
                if (OpcodeRegistry.TryGetOpcodeByMessage(msgType, out uint code))
                {
                    return code;
                }
                throw new InvalidOperationException($"Missing opcode mapping for {msgType.FullName}.");
            }
        }

        /// <summary>
        /// 处理已反序列化的普通消息。
        /// </summary>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        /// <param name="message">执行该方法所需的 message 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public abstract MTask HandleAsync(NetworkSession session, TMessage message);

        Type INetworkMessageHandlerInvoker.MessageType => typeof(TMessage);

        MTask INetworkMessageHandlerInvoker.HandleAsync(NetworkSession session, INormalMessage message)
        {
            if (!(message is TMessage typedMessage))
            {
                throw new ArgumentException($"消息类型不匹配，期望:{typeof(TMessage).FullName} 实际:{message?.GetType().FullName}", nameof(message));
            }

            return HandleAsync(session, typedMessage);
        }
    }
}
