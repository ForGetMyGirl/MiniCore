namespace MiniCore.Model
{
    /// <summary>
    /// 所有可由网络层发送和接收的协议消息标记。
    /// Opcode 由协议注册表根据运行时类型解析，不属于消息对象状态。
    /// </summary>
    public interface INetworkMessage
    {
    }

    /// <summary>
    /// 不参与 RPC 应答匹配的普通网络消息。
    /// </summary>
    public interface INormalMessage : INetworkMessage
    {
    }

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
