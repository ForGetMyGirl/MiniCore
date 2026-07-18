using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// 将 KCP 服务端会话包装为统一网络传输接口。
    /// </summary>
    public sealed class KcpServerTransport : INetworkTransport
    {
        private readonly IServerSession session; // 被包装的 KCP 服务端会话。
        private bool closed; // 传输层关闭状态。

        /// <summary>
        /// 使用指定服务端会话创建 KCP 传输包装器。
        /// </summary>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public KcpServerTransport(IServerSession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            session.OnDisconnected += HandleSessionDisconnected;
        }

        /// <summary>
        /// 传输包装器是否仍可用。
        /// </summary>
        public bool IsConnected => !closed;

        /// <summary>
        /// 服务端完成 KCP 重组并收到业务包时触发。
        /// </summary>
        public event Func<ReadOnlyMemory<byte>, UniTask> OnDataReceived;
        /// <summary>
        /// 服务端会话或传输断开时触发。
        /// </summary>
        public event Action OnDisconnected;

        /// <summary>
        /// 服务端传输不支持主动连接。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <param name="token">执行该方法所需的 token 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public UniTask ConnectAsync(string host, int port, CancellationToken token = default)
        {
            throw new InvalidOperationException("Server-side transport does not support ConnectAsync.");
        }

        /// <summary>
        /// 通过关联的 KCP 服务端会话发送业务包。
        /// </summary>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <param name="token">执行该方法所需的 token 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public UniTask SendAsync(ArraySegment<byte> data, CancellationToken token = default)
        {
            return session.SendAsync(data, token);
        }

        /// <summary>
        /// 关闭传输包装器并通知断开事件。
        /// </summary>
        public void Disconnect()
        {
            if (closed)
            {
                return;
            }

            closed = true;
            OnDisconnected?.Invoke();
        }

        /// <summary>
        /// 释放传输包装器资源。
        /// </summary>
        public void Dispose()
        {
            Disconnect();
        }

        /// <summary>
        /// 将 KCP 已重组的业务包派发给订阅者。
        /// </summary>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public UniTask PushReceivedAsync(ReadOnlyMemory<byte> data)
        {
            return InvokeDataReceivedAsync(data);
        }

        /// <summary>
        /// 执行 HandleSessionDisconnected 相关处理。
        /// </summary>
        private void HandleSessionDisconnected()
        {
            if (closed)
            {
                return;
            }

            closed = true;
            OnDisconnected?.Invoke();
        }

        /// <summary>
        /// 执行 InvokeDataReceivedAsync 相关处理。
        /// </summary>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        private async UniTask InvokeDataReceivedAsync(ReadOnlyMemory<byte> data)
        {
            await TransportEventDispatcher.DispatchAsync(OnDataReceived, data);
        }
    }
}
