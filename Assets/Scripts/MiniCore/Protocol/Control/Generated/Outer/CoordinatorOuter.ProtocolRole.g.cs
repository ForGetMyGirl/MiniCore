// Auto-generated from Proto/Control/Outer/CoordinatorOuter.proto. Do not edit by hand.
using MiniCore.Model;

namespace MiniCore.Protocol.Generated
{
    /// <summary>
    /// 为生成协议补充 IRpcRequest 网络角色。
    /// </summary>
    public sealed partial class ResolveServiceRequest : IRpcRequest
    {
        /// <summary>
        /// 获取或设置请求关联标识。
        /// </summary>
        public long RpcId { get; set; }
    }

    /// <summary>
    /// 为生成协议补充 IRpcResponse 网络角色。
    /// </summary>
    public sealed partial class ResolveServiceResponse : IRpcResponse
    {
        /// <summary>
        /// 获取或设置请求关联标识。
        /// </summary>
        public long RpcId { get; set; }
    }

}
