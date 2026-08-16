// Auto-generated from Proto/Business/Inner/Database.proto. Do not edit by hand.
using MiniCore.Model;

namespace MiniCore.Protocol.Generated
{
    /// <summary>
    /// 为生成协议补充 IRpcRequest 网络角色。
    /// </summary>
    public sealed partial class LoadPlayerDataRequest : IRpcRequest
    {
        /// <summary>
        /// 获取或设置请求关联标识。
        /// </summary>
        public long RpcId { get; set; }
    }

    /// <summary>
    /// 为生成协议补充 IRpcResponse 网络角色。
    /// </summary>
    public sealed partial class LoadPlayerDataResponse : IRpcResponse
    {
        /// <summary>
        /// 获取或设置请求关联标识。
        /// </summary>
        public long RpcId { get; set; }
    }

    /// <summary>
    /// 为生成协议补充 IRpcRequest 网络角色。
    /// </summary>
    public sealed partial class SavePlayerDataRequest : IRpcRequest
    {
        /// <summary>
        /// 获取或设置请求关联标识。
        /// </summary>
        public long RpcId { get; set; }
    }

    /// <summary>
    /// 为生成协议补充 IRpcResponse 网络角色。
    /// </summary>
    public sealed partial class SavePlayerDataResponse : IRpcResponse
    {
        /// <summary>
        /// 获取或设置请求关联标识。
        /// </summary>
        public long RpcId { get; set; }
    }

}
