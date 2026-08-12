using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets.Kcp;

namespace MiniCore.Model
{
    /// <summary>
    /// KCP 输出分片时调用的 UDP 数据报发送回调。
    /// </summary>
    public delegate void KcpOutput(byte[] buffer, int size);
}
