using MiniCore.Threading;
using System;

namespace MiniCore.Model
{
    /// <summary>
    /// 入站 RPC 请求处理器基类，负责写入对应响应对象。
    /// </summary>
    public abstract class ARpcHandler<TRequest, TResponse> : INetworkRpcHandlerInvoker
        where TRequest : IRpcRequest
        where TResponse : IRpcResponse, new()
    {
        /// <summary>
        /// 从 opcode 注册表解析当前请求类型对应的协议号。
        /// </summary>
        public virtual uint Opcode
        {
            get
            {
                var msgType = typeof(TRequest);
                if (OpcodeRegistry.TryGetOpcodeByMessage(msgType, out uint code))
                {
                    return code;
                }
                throw new InvalidOperationException($"Missing opcode mapping for {msgType.FullName}.");
            }
        }

        /// <summary>
        /// 处理 RPC 请求并填充响应对象。
        /// </summary>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        /// <param name="request">执行该方法所需的 request 参数。</param>
        /// <param name="response">执行该方法所需的 response 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public abstract MTask HandleAsync(NetworkSession session, TRequest request, TResponse response);

        Type INetworkRpcHandlerInvoker.RequestType => typeof(TRequest);

        Type INetworkRpcHandlerInvoker.ResponseType => typeof(TResponse);

        IRpcResponse INetworkRpcHandlerInvoker.CreateResponse()
        {
            return new TResponse();
        }

        MTask INetworkRpcHandlerInvoker.HandleAsync(NetworkSession session, IRpcRequest request, IRpcResponse response)
        {
            if (!(request is TRequest typedRequest))
            {
                throw new ArgumentException($"RPC请求类型不匹配，期望:{typeof(TRequest).FullName} 实际:{request?.GetType().FullName}", nameof(request));
            }

            if (!(response is TResponse typedResponse))
            {
                throw new ArgumentException($"RPC响应类型不匹配，期望:{typeof(TResponse).FullName} 实际:{response?.GetType().FullName}", nameof(response));
            }

            return HandleAsync(session, typedRequest, typedResponse);
        }
    }
}
