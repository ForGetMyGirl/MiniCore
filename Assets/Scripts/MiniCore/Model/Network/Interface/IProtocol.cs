namespace MiniCore.Model
{
    /// <summary>
    /// 所有协议的基础接口，包含协议号。
    /// </summary>
    public interface IProtocol
    {
        uint Opcode { get; }
    }

    public interface IRequest : IProtocol
    {
        long RpcId { get; set; }
    }

    public interface IResponse : IProtocol
    {
        long RpcId { get; set; }
        int ErrorCode { get; set; }
        string Message { get; set; }
    }
}
