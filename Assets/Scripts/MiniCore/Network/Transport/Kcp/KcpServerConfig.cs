using MiniCore.Threading;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;
using MiniCore.Core;

namespace MiniCore.Model
{
    /// <summary>
    /// KCP 服务端监听和会话参数配置。
    /// </summary>
    public class KcpServerConfig
    {
        /// <summary>
        /// KCP 最大传输单元。
        /// </summary>
        public int Mtu = 1400;
        /// <summary>
        /// KCP 发送窗口大小。
        /// </summary>
        public int SendWindow = 128;
        /// <summary>
        /// KCP 接收窗口大小。
        /// </summary>
        public int ReceiveWindow = 128;
        /// <summary>
        /// KCP 无延迟模式开关。
        /// </summary>
        public int NoDelay = 1;
        /// <summary>
        /// KCP 刷新间隔（毫秒）。
        /// </summary>
        public int Interval = 10;
        /// <summary>
        /// KCP 快速重传阈值。
        /// </summary>
        public int Resend = 2;
        /// <summary>
        /// 是否禁用 KCP 拥塞控制。
        /// </summary>
        public int NoCongestion = 1;
        /// <summary>
        /// 最小重传超时（毫秒）。
        /// </summary>
        public int MinRto = 30;
        /// <summary>
        /// 快速重传触发参数。
        /// </summary>
        public int FastResend = 2;
        /// <summary>
        /// 快速确认次数上限。
        /// </summary>
        public int FastAck = 1;
        /// <summary>
        /// 判定 KCP 链路失效的重传次数。
        /// </summary>
        public int DeadLink = 20;
        /// <summary>
        /// 是否启用流模式。
        /// </summary>
        public bool Stream = false;
        /// <summary>
        /// 服务端判定会话空闲超时的时长（毫秒）。
        /// </summary>
        public int SessionTimeoutMs = 30000;
    }
}
