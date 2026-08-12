using System;
using System.Collections.Generic;
using MiniCore.Serialization;

namespace MiniCore.Model
{
    /// <summary>
    /// 在网络服务启动前原子收集并校验项目协议与处理器。
    /// </summary>
    public sealed class NetworkProtocolBuilder
    {
        #region Private 私有成员

        private const uint ReservedOpcodeMaximum = 2; // Ping 与 Pong 使用的框架保留 Opcode 上限。
        private const uint NormalOpcodeMinimum = 100001; // 普通业务消息 Opcode 起始值。
        private const uint RpcOpcodeMinimum = 200001; // RPC 请求和响应 Opcode 起始值。
        private readonly Dictionary<Type, NetworkProtocolRegistry.MessageBinding> messagesByType = new Dictionary<Type, NetworkProtocolRegistry.MessageBinding>(); // 类型到消息绑定映射。
        private readonly Dictionary<uint, NetworkProtocolRegistry.MessageBinding> messagesByOpcode = new Dictionary<uint, NetworkProtocolRegistry.MessageBinding>(); // Opcode 到消息绑定映射。
        private readonly List<INetworkMessageHandlerInvoker> pendingMessageHandlers = new List<INetworkMessageHandlerInvoker>(); // 尚未封存的普通消息处理器。
        private readonly List<INetworkRpcHandlerInvoker> pendingRpcHandlers = new List<INetworkRpcHandlerInvoker>(); // 尚未封存的 RPC 处理器。
        private bool built; // 是否已经生成不可变 Registry。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 注册一条具有稳定 Opcode、网络角色和解析器的消息。
        /// </summary>
        /// <typeparam name="TMessage">需要注册的网络消息类型。</typeparam>
        /// <param name="opcode">消息稳定 Opcode。</param>
        /// <param name="role">消息在普通消息或 RPC 流程中的角色。</param>
        /// <param name="parser">消息字节解析器。</param>
        public void RegisterMessage<TMessage>(uint opcode, NetworkMessageRole role, IMessageParser parser)
            where TMessage : INetworkMessage
        {
            EnsureMutable();
            Type messageType = typeof(TMessage);
            ValidateMessage(messageType, opcode, role, parser);
            if (messagesByType.ContainsKey(messageType))
            {
                throw new InvalidOperationException($"网络消息类型重复注册：{messageType.FullName}。");
            }

            if (messagesByOpcode.ContainsKey(opcode))
            {
                throw new InvalidOperationException($"网络消息 Opcode 重复注册：{opcode}。");
            }

            var binding = new NetworkProtocolRegistry.MessageBinding
            {
                MessageType = messageType,
                Opcode = opcode,
                Role = role,
                Parser = parser
            };
            messagesByType.Add(messageType, binding);
            messagesByOpcode.Add(opcode, binding);
        }

        /// <summary>
        /// 注册一个普通消息处理器，最终封存时验证其消息绑定。
        /// </summary>
        /// <param name="handler">需要注册的普通消息处理器。</param>
        public void RegisterHandler(INetworkMessageHandlerInvoker handler)
        {
            EnsureMutable();
            pendingMessageHandlers.Add(handler ?? throw new ArgumentNullException(nameof(handler)));
        }

        /// <summary>
        /// 注册一个 RPC 处理器，最终封存时验证请求与响应绑定。
        /// </summary>
        /// <param name="handler">需要注册的 RPC 处理器。</param>
        public void RegisterHandler(INetworkRpcHandlerInvoker handler)
        {
            EnsureMutable();
            pendingRpcHandlers.Add(handler ?? throw new ArgumentNullException(nameof(handler)));
        }

        /// <summary>
        /// 校验全部消息和处理器并生成不可变协议 Registry。
        /// </summary>
        /// <returns>可以提交给单个网络服务实例的不可变 Registry。</returns>
        public NetworkProtocolRegistry Build()
        {
            EnsureMutable();
            var messageHandlers = new Dictionary<uint, NetworkProtocolRegistry.MessageHandlerBinding>();
            var rpcHandlers = new Dictionary<uint, NetworkProtocolRegistry.RpcHandlerBinding>();
            BindMessageHandlers(messageHandlers);
            BindRpcHandlers(rpcHandlers);
            built = true;
            return new NetworkProtocolRegistry(
                new Dictionary<Type, NetworkProtocolRegistry.MessageBinding>(messagesByType),
                new Dictionary<uint, NetworkProtocolRegistry.MessageBinding>(messagesByOpcode),
                messageHandlers,
                rpcHandlers);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 验证待注册消息的类型、角色、Opcode 和解析器。
        /// </summary>
        /// <param name="messageType">目标消息类型。</param>
        /// <param name="opcode">目标消息 Opcode。</param>
        /// <param name="role">目标消息角色。</param>
        /// <param name="parser">目标消息解析器。</param>
        private static void ValidateMessage(Type messageType, uint opcode, NetworkMessageRole role, IMessageParser parser)
        {
            if (opcode <= ReservedOpcodeMaximum)
            {
                throw new ArgumentOutOfRangeException(nameof(opcode), $"Opcode {opcode} 属于框架保留范围。");
            }

            if (parser == null || parser.MessageType != messageType)
            {
                throw new ArgumentException($"消息 {messageType.FullName} 的解析器类型不匹配。", nameof(parser));
            }

            switch (role)
            {
                case NetworkMessageRole.Normal:
                    if (!typeof(INormalMessage).IsAssignableFrom(messageType) || opcode < NormalOpcodeMinimum || opcode >= RpcOpcodeMinimum)
                    {
                        throw new InvalidOperationException($"普通消息 {messageType.FullName} 的角色或 Opcode {opcode} 无效。");
                    }
                    break;
                case NetworkMessageRole.RpcRequest:
                    if (!typeof(IRpcRequest).IsAssignableFrom(messageType) || opcode < RpcOpcodeMinimum)
                    {
                        throw new InvalidOperationException($"RPC 请求 {messageType.FullName} 的角色或 Opcode {opcode} 无效。");
                    }
                    break;
                case NetworkMessageRole.RpcResponse:
                    if (!typeof(IRpcResponse).IsAssignableFrom(messageType) || opcode < RpcOpcodeMinimum)
                    {
                        throw new InvalidOperationException($"RPC 响应 {messageType.FullName} 的角色或 Opcode {opcode} 无效。");
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, "未指定有效的网络消息角色。");
            }
        }

        /// <summary>
        /// 将普通处理器绑定到已注册普通消息并检查重复项。
        /// </summary>
        /// <param name="result">目标普通处理器映射。</param>
        private void BindMessageHandlers(Dictionary<uint, NetworkProtocolRegistry.MessageHandlerBinding> result)
        {
            for (int index = 0; index < pendingMessageHandlers.Count; index++)
            {
                INetworkMessageHandlerInvoker handler = pendingMessageHandlers[index];
                if (!messagesByType.TryGetValue(handler.MessageType, out NetworkProtocolRegistry.MessageBinding message) || message.Role != NetworkMessageRole.Normal)
                {
                    throw new InvalidOperationException($"普通处理器 {handler.GetType().FullName} 引用了未注册或角色错误的消息 {handler.MessageType?.FullName}。");
                }

                if (result.ContainsKey(message.Opcode))
                {
                    throw new InvalidOperationException($"普通消息 {handler.MessageType.FullName} 重复绑定 Handler。");
                }

                result.Add(message.Opcode, new NetworkProtocolRegistry.MessageHandlerBinding
                {
                    Message = message,
                    Invoker = handler
                });
            }
        }

        /// <summary>
        /// 将 RPC 处理器绑定到已注册请求和响应并检查重复项。
        /// </summary>
        /// <param name="result">目标 RPC 处理器映射。</param>
        private void BindRpcHandlers(Dictionary<uint, NetworkProtocolRegistry.RpcHandlerBinding> result)
        {
            for (int index = 0; index < pendingRpcHandlers.Count; index++)
            {
                INetworkRpcHandlerInvoker handler = pendingRpcHandlers[index];
                if (!messagesByType.TryGetValue(handler.RequestType, out NetworkProtocolRegistry.MessageBinding request) || request.Role != NetworkMessageRole.RpcRequest)
                {
                    throw new InvalidOperationException($"RPC 处理器 {handler.GetType().FullName} 引用了未注册或角色错误的请求 {handler.RequestType?.FullName}。");
                }

                if (!messagesByType.TryGetValue(handler.ResponseType, out NetworkProtocolRegistry.MessageBinding response) || response.Role != NetworkMessageRole.RpcResponse)
                {
                    throw new InvalidOperationException($"RPC 处理器 {handler.GetType().FullName} 引用了未注册或角色错误的响应 {handler.ResponseType?.FullName}。");
                }

                if (result.ContainsKey(request.Opcode))
                {
                    throw new InvalidOperationException($"RPC 请求 {handler.RequestType.FullName} 重复绑定 Handler。");
                }

                result.Add(request.Opcode, new NetworkProtocolRegistry.RpcHandlerBinding
                {
                    Request = request,
                    Response = response,
                    Invoker = handler
                });
            }
        }

        /// <summary>
        /// 确保当前 Builder 尚未封存。
        /// </summary>
        private void EnsureMutable()
        {
            if (built)
            {
                throw new InvalidOperationException("网络协议 Builder 已经封存，不能继续修改。");
            }
        }

        #endregion
    }
}
