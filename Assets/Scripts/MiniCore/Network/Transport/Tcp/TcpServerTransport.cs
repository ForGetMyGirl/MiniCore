using MiniCore.Threading;
using System;
using System.Net.Sockets;
using System.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// 将 TCP 服务端会话包装为支持长度前缀拆包的传输层。
    /// </summary>
    public sealed class TcpServerTransport : LengthPrefixedTcpTransportBase
    {
        private readonly IServerSession session; // 被包装的服务端会话。

        /// <summary>
        /// 使用 TCP 服务端会话创建传输层并启动接收循环。
        /// </summary>
        /// <param name="session">需要包装的 TCP 服务端会话。</param>
        /// <param name="executor">接收循环使用的执行器；为空时按当前环境选择默认执行器。</param>
        public TcpServerTransport(IServerSession session, IMTaskExecutor executor = null)
            : base(executor: executor)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            if (!(session is TcpServerSession tcpSession))
            {
                throw new ArgumentException("TcpServerTransport requires TcpServerSession.", nameof(session));
            }

            if (tcpSession.RawSocket == null)
            {
                throw new ArgumentNullException(nameof(tcpSession.RawSocket));
            }

            AttachConnectedSocket(tcpSession.RawSocket);
        }

        /// <summary>
        /// 服务端传输不支持主动连接。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public override MTask ConnectAsync(string host, int port)
        {
            throw new NotSupportedException("TcpServerTransport does not support ConnectAsync.");
        }

        /// <summary>
        /// 断开长度前缀传输并关闭对应服务端会话。
        /// </summary>
        public override void Disconnect()
        {
            base.Disconnect();
            session.Close();
        }
    }
}
