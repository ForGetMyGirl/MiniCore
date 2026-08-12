using MiniCore.Threading;
using System.Net.Sockets;
using System.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// 客户端 TCP 传输实现，使用长度前缀解决粘包和拆包。
    /// </summary>
    public class TcpTransport : LengthPrefixedTcpTransportBase
    {
        /// <summary>
        /// 创建使用指定执行器的 TCP 客户端传输。
        /// </summary>
        /// <param name="executor">接收循环使用的执行器；为空时按当前环境选择默认执行器。</param>
        public TcpTransport(IMTaskExecutor executor = null)
            : base(executor: executor)
        {
        }

        /// <summary>
        /// 连接远端 TCP 服务并启动接收循环。
        /// </summary>
        /// <param name="host">执行该方法所需的 host 参数。</param>
        /// <param name="port">执行该方法所需的 port 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public override async MTask ConnectAsync(string host, int port)
        {
            Disconnect();
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };
            await socket.ConnectAsync(host, port);
            AttachConnectedSocket(socket);
        }
    }
}
