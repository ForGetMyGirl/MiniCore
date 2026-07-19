using MiniCore.Threading;
using System;
using System.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// 封装底层传输实现的逻辑网络会话。
    /// </summary>
    public class NetworkSession : IClientSession
    {
        private int disposed; // 防止底层传输重复释放的标志。

        /// <summary>
        /// 逻辑会话的唯一标识。
        /// </summary>
        public string SessionId { get; }
        /// <summary>
        /// 会话持有的底层网络传输。
        /// </summary>
        public INetworkTransport Transport { get; }
        /// <summary>
        /// 底层传输当前是否可用。
        /// </summary>
        public bool IsConnected => Transport.IsConnected;

        /// <summary>
        /// 使用给定标识和传输实现创建逻辑会话。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="transport">执行该方法所需的 transport 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public NetworkSession(string sessionId, INetworkTransport transport)
        {
            SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            Transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        /// <summary>
        /// 通过底层传输发送一个完整业务数据包。
        /// </summary>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public MTask SendAsync(ArraySegment<byte> data)
        {
            return Transport.SendAsync(data);
        }

        /// <summary>
        /// 主动断开底层传输。
        /// </summary>
        public void Close()
        {
            Transport.Disconnect();
        }

        /// <summary>
        /// 底层传输断开时触发。
        /// </summary>
        public event Action OnDisconnected
        {
            add => Transport.OnDisconnected += value;
            remove => Transport.OnDisconnected -= value;
        }

        /// <summary>
        /// 释放底层传输资源。
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
            {
                return;
            }

            Transport.Dispose();
        }
    }
}
