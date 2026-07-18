using Cysharp.Threading.Tasks;
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
        UniTask HandleAsync(NetworkSession session, INormalMessage message);
    }

    /// <summary>
    /// 提供无反射消息派发和响应创建能力的 RPC 处理器契约。
    /// </summary>
    public interface INetworkRpcHandlerInvoker
    {
        /// <summary>
        /// 处理器接收的 RPC 请求运行时类型。
        /// </summary>
        Type RequestType { get; }

        /// <summary>
        /// 处理器创建的 RPC 响应运行时类型。
        /// </summary>
        Type ResponseType { get; }

        /// <summary>
        /// 创建当前 RPC 请求对应的空响应对象。
        /// </summary>
        IRpcResponse CreateResponse();

        /// <summary>
        /// 将已反序列化的 RPC 请求和响应派发给具体处理器。
        /// </summary>
        UniTask HandleAsync(NetworkSession session, IRpcRequest request, IRpcResponse response);
    }
}
