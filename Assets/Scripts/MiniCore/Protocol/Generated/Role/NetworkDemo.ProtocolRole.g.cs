// Auto-generated from Proto/NetworkDemo.proto. Do not edit by hand.
using MiniCore.Model;

namespace MiniCore.Protocol.Generated
{
    /// <summary>
    /// 为生成协议补充 INormalMessage 网络角色。
    /// </summary>
    public sealed partial class DemoNormalMessage : INormalMessage
    {
    }

    /// <summary>
    /// 为生成协议补充 IRpcRequest 网络角色。
    /// </summary>
    public sealed partial class DemoRpcRequest : IRpcRequest
    {
        /// <summary>
        /// 获取或设置网络包头关联的请求标识。
        /// </summary>
        public long RpcId { get; set; }
    }

    /// <summary>
    /// 为生成协议补充 IRpcResponse 网络角色。
    /// </summary>
    public sealed partial class DemoRpcResponse : IRpcResponse
    {
        /// <summary>
        /// 获取或设置网络包头关联的请求标识。
        /// </summary>
        public long RpcId { get; set; }
    }

    /// <summary>
    /// 为生成协议补充 INormalMessage 网络角色。
    /// </summary>
    public sealed partial class DisconnectNotice : INormalMessage
    {
    }

    /// <summary>
    /// 为生成协议补充 INormalMessage 网络角色。
    /// </summary>
    public sealed partial class TestNetworkData : INormalMessage
    {
    }

}
