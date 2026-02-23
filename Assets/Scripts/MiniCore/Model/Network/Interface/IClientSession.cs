namespace MiniCore.Model
{
    /// <summary>
    /// Client-side session abstraction.
    /// </summary>
    public interface IClientSession : ISession
    {
        INetworkTransport Transport { get; }
    }
}
