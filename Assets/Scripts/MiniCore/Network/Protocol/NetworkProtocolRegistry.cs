using System;
using System.Collections.Generic;
using MiniCore.Serialization;

namespace MiniCore.Model
{
    /// <summary>
    /// 保存单个网络服务实例使用的不可变协议、解析器与处理器映射。
    /// </summary>
    public sealed class NetworkProtocolRegistry : IMessageParserResolver
    {
        #region Internal 内部成员

        /// <summary>
        /// 单条消息的稳定协议绑定。
        /// </summary>
        internal sealed class MessageBinding
        {
            public Type MessageType; // 消息运行时类型。
            public uint Opcode; // 消息稳定协议号。
            public NetworkMessageRole Role; // 消息网络角色。
            public IMessageParser Parser; // 消息字节解析器。
        }

        /// <summary>
        /// 普通消息处理器绑定。
        /// </summary>
        internal sealed class MessageHandlerBinding
        {
            public MessageBinding Message; // 处理器消费的消息绑定。
            public INetworkMessageHandlerInvoker Invoker; // 无反射消息调用器。
        }

        /// <summary>
        /// RPC 处理器绑定。
        /// </summary>
        internal sealed class RpcHandlerBinding
        {
            public MessageBinding Request; // RPC 请求消息绑定。
            public MessageBinding Response; // RPC 响应消息绑定。
            public INetworkRpcHandlerInvoker Invoker; // 无反射 RPC 调用器。
        }

        #endregion

        #region Private 私有成员

        private readonly Dictionary<Type, MessageBinding> messagesByType; // 类型到协议绑定映射。
        private readonly Dictionary<uint, MessageBinding> messagesByOpcode; // Opcode 到协议绑定映射。
        private readonly Dictionary<uint, MessageHandlerBinding> messageHandlers; // 普通消息处理器映射。
        private readonly Dictionary<uint, RpcHandlerBinding> rpcHandlers; // RPC 请求处理器映射。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 使用 Builder 已完成校验的独立字典创建不可变 Registry。
        /// </summary>
        /// <param name="messagesByType">类型到协议绑定映射。</param>
        /// <param name="messagesByOpcode">Opcode 到协议绑定映射。</param>
        /// <param name="messageHandlers">普通消息处理器映射。</param>
        /// <param name="rpcHandlers">RPC 请求处理器映射。</param>
        internal NetworkProtocolRegistry(
            Dictionary<Type, MessageBinding> messagesByType,
            Dictionary<uint, MessageBinding> messagesByOpcode,
            Dictionary<uint, MessageHandlerBinding> messageHandlers,
            Dictionary<uint, RpcHandlerBinding> rpcHandlers)
        {
            this.messagesByType = messagesByType ?? throw new ArgumentNullException(nameof(messagesByType));
            this.messagesByOpcode = messagesByOpcode ?? throw new ArgumentNullException(nameof(messagesByOpcode));
            this.messageHandlers = messageHandlers ?? throw new ArgumentNullException(nameof(messageHandlers));
            this.rpcHandlers = rpcHandlers ?? throw new ArgumentNullException(nameof(rpcHandlers));
        }

        /// <summary>
        /// 尝试按消息类型取得完整协议绑定。
        /// </summary>
        /// <param name="messageType">消息运行时类型。</param>
        /// <param name="binding">找到的协议绑定。</param>
        /// <returns>存在绑定时返回 true。</returns>
        internal bool TryGetMessage(Type messageType, out MessageBinding binding)
        {
            if (messageType != null)
            {
                return messagesByType.TryGetValue(messageType, out binding);
            }

            binding = null;
            return false;
        }

        /// <summary>
        /// 尝试按 Opcode 取得完整协议绑定。
        /// </summary>
        /// <param name="opcode">消息 Opcode。</param>
        /// <param name="binding">找到的协议绑定。</param>
        /// <returns>存在绑定时返回 true。</returns>
        internal bool TryGetMessage(uint opcode, out MessageBinding binding)
        {
            return messagesByOpcode.TryGetValue(opcode, out binding);
        }

        /// <summary>
        /// 尝试按 Opcode 取得普通消息处理器。
        /// </summary>
        /// <param name="opcode">普通消息 Opcode。</param>
        /// <param name="binding">找到的处理器绑定。</param>
        /// <returns>存在处理器时返回 true。</returns>
        internal bool TryGetMessageHandler(uint opcode, out MessageHandlerBinding binding)
        {
            return messageHandlers.TryGetValue(opcode, out binding);
        }

        /// <summary>
        /// 尝试按 Opcode 取得 RPC 处理器。
        /// </summary>
        /// <param name="opcode">RPC 请求 Opcode。</param>
        /// <param name="binding">找到的处理器绑定。</param>
        /// <returns>存在处理器时返回 true。</returns>
        internal bool TryGetRpcHandler(uint opcode, out RpcHandlerBinding binding)
        {
            return rpcHandlers.TryGetValue(opcode, out binding);
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 尝试取得消息类型对应的稳定 Opcode。
        /// </summary>
        /// <param name="messageType">消息运行时类型。</param>
        /// <param name="opcode">找到的稳定 Opcode。</param>
        /// <returns>存在消息注册时返回 true。</returns>
        public bool TryGetOpcode(Type messageType, out uint opcode)
        {
            if (TryGetMessage(messageType, out MessageBinding binding))
            {
                opcode = binding.Opcode;
                return true;
            }

            opcode = 0;
            return false;
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 使用消息类型对应的已注册解析器解析字节。
        /// </summary>
        /// <param name="messageType">目标消息运行时类型。</param>
        /// <param name="data">需要解析的消息字节。</param>
        /// <returns>解析完成的消息对象。</returns>
        object IMessageParserResolver.Parse(Type messageType, ReadOnlyMemory<byte> data)
        {
            if (!TryGetMessage(messageType, out MessageBinding binding))
            {
                throw new InvalidOperationException($"未注册网络消息：{messageType?.FullName ?? "<null>"}。");
            }

            return binding.Parser.Parse(data);
        }

        #endregion
    }
}
