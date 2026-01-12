using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// 表示一条网络会话，封装传输层数据。
    /// </summary>
    public class NetworkSession : IDisposable
    {
        public string SessionId { get; }
        public INetworkTransport Transport { get; }

        public NetworkSession(string sessionId, INetworkTransport transport)
        {
            SessionId = sessionId;
            Transport = transport;
        }

        public bool IsConnected => Transport != null && Transport.IsConnected;

        public UniTask SendAsync(ArraySegment<byte> data, CancellationToken token = default)
        {
            return Transport.SendAsync(data, token);
        }

        public void Dispose()
        {
            Transport?.Dispose();
        }
    }
}
