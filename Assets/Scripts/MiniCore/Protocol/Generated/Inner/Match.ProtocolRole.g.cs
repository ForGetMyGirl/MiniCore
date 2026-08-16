// Auto-generated from Proto/Business/Inner/Match.proto. Do not edit by hand.
using MiniCore.Model;

namespace MiniCore.Protocol.Generated
{
    /// <summary>
    /// 为生成协议补充 IRpcRequest 网络角色。
    /// </summary>
    public sealed partial class EnqueueMatchRequest : IRpcRequest
    {
        /// <summary>
        /// 获取或设置请求关联标识。
        /// </summary>
        public long RpcId { get; set; }
    }

    /// <summary>
    /// 为生成协议补充 IRpcResponse 网络角色。
    /// </summary>
    public sealed partial class EnqueueMatchResponse : IRpcResponse
    {
        /// <summary>
        /// 获取或设置请求关联标识。
        /// </summary>
        public long RpcId { get; set; }
    }

    /// <summary>
    /// 为生成协议补充 IRpcRequest 网络角色。
    /// </summary>
    public sealed partial class CancelMatchRequest : IRpcRequest
    {
        /// <summary>
        /// 获取或设置请求关联标识。
        /// </summary>
        public long RpcId { get; set; }
    }

    /// <summary>
    /// 为生成协议补充 IRpcResponse 网络角色。
    /// </summary>
    public sealed partial class CancelMatchResponse : IRpcResponse
    {
        /// <summary>
        /// 获取或设置请求关联标识。
        /// </summary>
        public long RpcId { get; set; }
    }

    /// <summary>
    /// 为生成协议补充 IRpcRequest 网络角色。
    /// </summary>
    public sealed partial class TakeMatchRequest : IRpcRequest
    {
        /// <summary>
        /// 获取或设置请求关联标识。
        /// </summary>
        public long RpcId { get; set; }
    }

    /// <summary>
    /// 为生成协议补充 IRpcResponse 网络角色。
    /// </summary>
    public sealed partial class TakeMatchResponse : IRpcResponse
    {
        /// <summary>
        /// 获取或设置请求关联标识。
        /// </summary>
        public long RpcId { get; set; }
    }

}
