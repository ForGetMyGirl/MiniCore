namespace MiniCore.Model
{
    /// <summary>
    /// 客户端逻辑会话抽象。
    /// </summary>
    public interface IClientSession : ISession
    {
        /// <summary>
        /// 会话使用的底层网络传输实现。
        /// </summary>
        INetworkTransport Transport { get; }
    }
}
