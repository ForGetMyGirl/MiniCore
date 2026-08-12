namespace MiniCore.Model
{
    /// <summary>
    /// 由网络包头关联响应的 RPC 请求。
    /// </summary>
    public interface IRpcRequest : INetworkMessage
    {
        /// <summary>
        /// 获取或设置网络层分配的请求标识。
        /// </summary>
        long RpcId { get; set; }
    }
}
