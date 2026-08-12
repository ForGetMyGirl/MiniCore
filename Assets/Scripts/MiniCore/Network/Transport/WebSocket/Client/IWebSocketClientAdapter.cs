using System;
using MiniCore.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// WebSocket 客户端的平台适配契约，负责调用当前宿主的底层 WebSocket API。
    /// 该类型只属于客户端连接链路，不表示游戏服务端。
    /// </summary>
    public interface IWebSocketClientAdapter : IDisposable
    {
        #region Public 公共成员

        /// <summary>
        /// 获取底层 WebSocket 是否已经完成握手并保持打开。
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// 收到完整二进制 WebSocket 消息时触发。
        /// </summary>
        event Action<ArraySegment<byte>> BinaryMessageReceived;

        /// <summary>
        /// 底层连接关闭时触发。
        /// </summary>
        event Action<ushort, string> Closed;

        /// <summary>
        /// 连接指定 WS 或 WSS 地址并等待握手完成。
        /// </summary>
        /// <param name="url">包含协议、主机、端口和路径的完整地址。</param>
        /// <param name="maximumMessageSize">允许接收的单条二进制消息最大字节数。</param>
        /// <returns>握手成功或失败时完成的任务。</returns>
        MTask ConnectAsync(string url, int maximumMessageSize);

        /// <summary>
        /// 发送一条完整二进制 WebSocket 消息。
        /// </summary>
        /// <param name="data">需要发送的完整消息。</param>
        /// <returns>底层接管消息或发送失败时完成的任务。</returns>
        MTask SendAsync(ArraySegment<byte> data);

        /// <summary>
        /// 使用正常关闭状态结束连接。
        /// </summary>
        void Close();

        #endregion
    }
}
