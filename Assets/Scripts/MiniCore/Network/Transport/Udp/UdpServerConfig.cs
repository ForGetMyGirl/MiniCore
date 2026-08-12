using MiniCore.Threading;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MiniCore.Core;

namespace MiniCore.Model
{
    /// <summary>
    /// UDP 服务端接收行为配置。
    /// </summary>
    public class UdpServerConfig
    {
        /// <summary>
        /// 单个 UDP 数据报允许接收的最大字节数。
        /// </summary>
        public int MaxDatagramSize = 65507;
    }
}
