using Cysharp.Threading.Tasks;
using System;
using System.Net.Sockets;
using System.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// 服务端接受 TCP 连接后创建的原始服务端会话。
    /// </summary>
    public sealed class TcpServerSession : IServerSession
    {
        private readonly Socket socket; // 此会话独占的已连接套接字。
        private bool closed; // 会话是否已关闭。

        /// <summary>
        /// 服务端分配的会话标识。
        /// </summary>
        public string SessionId { get; }
        /// <summary>
        /// 套接字是否仍保持连接。
        /// </summary>
        public bool IsConnected => !closed && socket != null && socket.Connected;
        internal Socket RawSocket => socket;

        /// <summary>
        /// 会话关闭时触发。
        /// </summary>
        public event Action OnDisconnected;

        /// <summary>
        /// 使用已接受的套接字创建服务端会话。
        /// </summary>
        /// <param name="sessionId">执行该方法所需的 sessionId 参数。</param>
        /// <param name="socket">执行该方法所需的 socket 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public TcpServerSession(string sessionId, Socket socket)
        {
            SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
        }

        /// <summary>
        /// 通过已连接套接字发送数据。
        /// </summary>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <param name="token">执行该方法所需的 token 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public UniTask SendAsync(ArraySegment<byte> data, CancellationToken token = default)
        {
            if (closed)
            {
                throw new InvalidOperationException("TcpServerSession is closed.");
            }

            if (data.Array == null)
            {
                throw new ArgumentException("ArraySegment has no backing array.", nameof(data));
            }

            socket.Send(data.Array, data.Offset, data.Count, SocketFlags.None);
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 关闭套接字并通知断开事件。
        /// </summary>
        public void Close()
        {
            if (closed)
            {
                return;
            }

            closed = true;
            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
            }

            try
            {
                socket.Close();
            }
            catch
            {
            }

            OnDisconnected?.Invoke();
        }

        /// <summary>
        /// 释放会话资源。
        /// </summary>
        public void Dispose()
        {
            Close();
        }
    }
}
