using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// Unified session contract shared by client/server session roles.
    /// </summary>
    public interface ISession : IDisposable
    {
        string SessionId { get; }
        bool IsConnected { get; }
        UniTask SendAsync(ArraySegment<byte> data, CancellationToken token = default);
        void Close();
        event Action OnDisconnected;
    }
}
