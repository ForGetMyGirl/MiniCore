namespace MiniCore.Model
{
    /// <summary>
    /// 所有可由网络层发送和接收的协议消息标记。
    /// Opcode 由网络服务实例的协议 Registry 解析，不属于消息对象状态。
    /// </summary>
    public interface INetworkMessage
    {
    }
}
