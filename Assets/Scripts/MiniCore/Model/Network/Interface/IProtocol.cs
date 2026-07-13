namespace MiniCore.Model
{
    /// <summary>
    /// 所有协议的基础接口，包含协议号。
    /// </summary>
    public interface IProtocol
    {
        /// <summary>
        /// 协议在网络包头中使用的操作码。
        /// </summary>
        uint Opcode { get; }
    }

    /// <summary>
    /// 带有请求标识的 RPC 请求协议。
    /// </summary>
    public interface IRequest : IProtocol
    {
        /// <summary>
        /// 用于匹配 RPC 响应的请求标识。
        /// </summary>
        long RpcId { get; set; }
    }

    /// <summary>
    /// 带有执行结果的 RPC 响应协议。
    /// </summary>
    public interface IResponse : IProtocol
    {
        /// <summary>
        /// 与请求匹配的 RPC 标识。
        /// </summary>
        long RpcId { get; set; }
        /// <summary>
        /// 业务错误码，零通常表示成功。
        /// </summary>
        int ErrorCode { get; set; }
        /// <summary>
        /// 业务结果或错误描述。
        /// </summary>
        string Message { get; set; }
    }
}
