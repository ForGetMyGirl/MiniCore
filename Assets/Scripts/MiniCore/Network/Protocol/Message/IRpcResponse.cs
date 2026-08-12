namespace MiniCore.Model
{
    /// <summary>
    /// 与 RPC 请求关联的响应消息。
    /// </summary>
    public interface IRpcResponse : INetworkMessage
    {
        /// <summary>
        /// 获取或设置网络层写入的请求标识。
        /// </summary>
        long RpcId { get; set; }

        /// <summary>
        /// 获取或设置业务结果码，零表示成功。
        /// </summary>
        int Code { get; set; }

        /// <summary>
        /// 获取或设置业务结果文本。
        /// </summary>
        string Msg { get; set; }
    }
}
