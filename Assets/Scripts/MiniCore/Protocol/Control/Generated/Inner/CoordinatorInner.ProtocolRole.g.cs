// Auto-generated from Proto/Control/Inner/CoordinatorInner.proto. Do not edit by hand.
using MiniCore.Model;

namespace MiniCore.Protocol.Generated
{
    /// <summary>
    /// 为生成协议补充 IRpcRequest 网络角色。
    /// </summary>
    public sealed partial class RegisterServerRequest : IRpcRequest
    {
        /// <summary>
        /// 获取或设置请求关联标识。
        /// </summary>
        public long RpcId { get; set; }
    }

    /// <summary>
    /// 为生成协议补充 IRpcResponse 网络角色。
    /// </summary>
    public sealed partial class RegisterServerResponse : IRpcResponse
    {
        /// <summary>
        /// 获取或设置请求关联标识。
        /// </summary>
        public long RpcId { get; set; }
    }

    /// <summary>
    /// 为生成协议补充 IRpcRequest 网络角色。
    /// </summary>
    public sealed partial class ServerHeartbeatRequest : IRpcRequest
    {
        /// <summary>
        /// 获取或设置请求关联标识。
        /// </summary>
        public long RpcId { get; set; }
    }

    /// <summary>
    /// 为生成协议补充 IRpcResponse 网络角色。
    /// </summary>
    public sealed partial class ServerHeartbeatResponse : IRpcResponse
    {
        /// <summary>
        /// 获取或设置请求关联标识。
        /// </summary>
        public long RpcId { get; set; }
    }

    /// <summary>
    /// 为生成协议补充 IRpcRequest 网络角色。
    /// </summary>
    public sealed partial class SetServerStateRequest : IRpcRequest
    {
        /// <summary>
        /// 获取或设置请求关联标识。
        /// </summary>
        public long RpcId { get; set; }
    }

    /// <summary>
    /// 为生成协议补充 IRpcResponse 网络角色。
    /// </summary>
    public sealed partial class SetServerStateResponse : IRpcResponse
    {
        /// <summary>
        /// 获取或设置请求关联标识。
        /// </summary>
        public long RpcId { get; set; }
    }

    /// <summary>
    /// 为生成协议补充 IRpcRequest 网络角色。
    /// </summary>
    public sealed partial class ResolveInnerServiceRequest : IRpcRequest
    {
        /// <summary>
        /// 获取或设置请求关联标识。
        /// </summary>
        public long RpcId { get; set; }
    }

    /// <summary>
    /// 为生成协议补充 IRpcResponse 网络角色。
    /// </summary>
    public sealed partial class ResolveInnerServiceResponse : IRpcResponse
    {
        /// <summary>
        /// 获取或设置请求关联标识。
        /// </summary>
        public long RpcId { get; set; }
    }

}
