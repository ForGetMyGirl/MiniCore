using MiniCore.Threading;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// 按远端 EndPoint 区分的 UDP 服务端逻辑会话。
    /// </summary>
    public sealed class UdpServerSession : IServerSession
    {
        private readonly Socket socket; // 所属 UDP 服务端套接字。
        private bool closed; // 会话关闭状态。

        /// <summary>
        /// 该会话对应的远端网络终结点。
        /// </summary>
        public EndPoint RemoteEndPoint { get; }
        /// <summary>
        /// 由远端终结点派生的会话标识。
        /// </summary>
        public string SessionId => $"udp:{RemoteEndPoint}";
        /// <summary>
        /// 会话是否仍可发送数据报。
        /// </summary>
        public bool IsConnected => !closed;

        /// <summary>
        /// 会话关闭时触发。
        /// </summary>
        public event Action OnDisconnected;

        /// <summary>
        /// 使用远端终结点和服务端套接字创建会话。
        /// </summary>
        /// <param name="remoteEndPoint">执行该方法所需的 remoteEndPoint 参数。</param>
        /// <param name="socket">执行该方法所需的 socket 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public UdpServerSession(EndPoint remoteEndPoint, Socket socket)
        {
            RemoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
        }

        /// <summary>
        /// 向该会话远端发送一个 UDP 数据报。
        /// </summary>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public MTask SendAsync(ArraySegment<byte> data)
        {
            if (closed)
            {
                throw new InvalidOperationException("UdpServerSession is closed.");
            }

            if (data.Array == null)
            {
                throw new ArgumentException("ArraySegment has no backing array.", nameof(data));
            }

            socket.SendTo(data.Array, data.Offset, data.Count, SocketFlags.None, RemoteEndPoint);
            return MTask.CompletedTask;
        }

        /// <summary>
        /// 标记会话关闭并通知订阅者。
        /// </summary>
        public void Close()
        {
            if (closed)
            {
                return;
            }

            closed = true;
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
