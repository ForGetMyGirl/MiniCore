#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using MiniCore.Threading;
using WebSocketSharp;

namespace MiniCore.Model
{
    /// <summary>
    /// 使用固定版本 websocket-sharp 的原生 WebSocket 客户端适配器。
    /// </summary>
    internal sealed class NativeWebSocketClientAdapter : IWebSocketClientAdapter
    {
        #region Private 私有成员

        private WebSocket socket; // 当前底层连接。
        private MTaskCompletionSource<bool> connectCompletion; // 当前握手等待者。
        private int maximumMessageSize; // 单条二进制消息大小上限。
        private bool disposed; // 是否已经释放。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取底层连接是否处于打开状态。
        /// </summary>
        public bool IsOpen => socket != null && socket.ReadyState == WebSocketState.Open;

        /// <summary>
        /// 收到通过大小校验的二进制消息时触发。
        /// </summary>
        public event Action<ArraySegment<byte>> BinaryMessageReceived;

        /// <summary>
        /// 底层连接关闭时触发。
        /// </summary>
        public event Action<ushort, string> Closed;

        /// <summary>
        /// 创建并异步连接 WS 或 WSS 地址。
        /// </summary>
        /// <param name="url">包含路径的完整 WebSocket 地址。</param>
        /// <param name="maximumMessageSize">允许接收的单条二进制消息最大字节数。</param>
        /// <returns>握手完成或失败时结束的任务。</returns>
        public MTask ConnectAsync(string url, int maximumMessageSize)
        {
            ThrowIfDisposed();
            ValidateUrl(url);
            if (maximumMessageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumMessageSize));
            }

            CloseSocket();
            this.maximumMessageSize = maximumMessageSize;
            connectCompletion = new MTaskCompletionSource<bool>();
            socket = new WebSocket(url)
            {
                Compression = CompressionMethod.None,
                EnableRedirection = false,
                WaitTime = TimeSpan.FromSeconds(5)
            };
            socket.OnOpen += HandleOpen;
            socket.OnMessage += HandleMessage;
            socket.OnError += HandleError;
            socket.OnClose += HandleClose;
            socket.ConnectAsync();
            return AwaitConnectedAsync(connectCompletion);
        }

        /// <summary>
        /// 异步发送一条二进制 WebSocket 消息。
        /// </summary>
        /// <param name="data">需要发送的连续字节。</param>
        /// <returns>发送回调成功或失败时完成的任务。</returns>
        public MTask SendAsync(ArraySegment<byte> data)
        {
            ThrowIfDisposed();
            if (!IsOpen)
            {
                throw new InvalidOperationException("WebSocket 尚未连接。");
            }

            if (data.Array == null)
            {
                throw new ArgumentException("待发送消息没有底层数组。", nameof(data));
            }

            // websocket-sharp 的 byte[] SendAsync 会异步持有完整数组且没有片段重载，
            // 因此必须在第三方边界复制为精确长度数组，不能提前归还调用方的池化缓冲区。
            byte[] message = new byte[data.Count];
            Buffer.BlockCopy(data.Array, data.Offset, message, 0, data.Count);
            var completion = new MTaskCompletionSource<bool>();
            socket.SendAsync(message, succeeded =>
            {
                if (succeeded)
                {
                    completion.TrySetResult(true);
                }
                else
                {
                    completion.TrySetException(new InvalidOperationException("WebSocket 二进制消息发送失败。"));
                }
            });
            return AwaitSentAsync(completion);
        }

        /// <summary>
        /// 正常关闭当前连接。
        /// </summary>
        public void Close()
        {
            CloseSocket();
        }

        /// <summary>
        /// 关闭连接并清理事件引用。
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            CloseSocket();
            BinaryMessageReceived = null;
            Closed = null;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 等待握手完成源并丢弃内部布尔结果。
        /// </summary>
        /// <param name="completion">当前握手完成源。</param>
        /// <returns>握手结果任务。</returns>
        private static async MTask AwaitConnectedAsync(MTaskCompletionSource<bool> completion)
        {
            await completion.Task;
        }

        /// <summary>
        /// 等待发送完成源并丢弃内部布尔结果。
        /// </summary>
        /// <param name="completion">当前发送完成源。</param>
        /// <returns>发送结果任务。</returns>
        private static async MTask AwaitSentAsync(MTaskCompletionSource<bool> completion)
        {
            await completion.Task;
        }

        /// <summary>
        /// 校验客户端只接受包含主机和路径的 WS/WSS 地址。
        /// </summary>
        /// <param name="url">需要校验的完整地址。</param>
        private static void ValidateUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri)
                || (uri.Scheme != "ws" && uri.Scheme != "wss")
                || string.IsNullOrEmpty(uri.Host))
            {
                throw new ArgumentException("WebSocket 地址必须是有效的 ws:// 或 wss:// 绝对地址。", nameof(url));
            }
        }

        /// <summary>
        /// 完成当前握手等待。
        /// </summary>
        /// <param name="sender">事件发送方。</param>
        /// <param name="args">事件参数。</param>
        private void HandleOpen(object sender, EventArgs args)
        {
            connectCompletion?.TrySetResult(true);
            connectCompletion = null;
        }

        /// <summary>
        /// 校验并转发完整二进制消息；文本消息会以不支持的数据类型关闭连接。
        /// </summary>
        /// <param name="sender">事件发送方。</param>
        /// <param name="args">收到的消息参数。</param>
        private void HandleMessage(object sender, MessageEventArgs args)
        {
            if (!args.IsBinary || args.RawData == null)
            {
                socket?.Close(CloseStatusCode.UnsupportedData, "Binary messages only.");
                return;
            }

            if (args.RawData.Length > maximumMessageSize)
            {
                socket?.Close(CloseStatusCode.TooBig, "Message is too large.");
                return;
            }

            BinaryMessageReceived?.Invoke(new ArraySegment<byte>(args.RawData));
        }

        /// <summary>
        /// 将握手阶段错误反馈给等待者。
        /// </summary>
        /// <param name="sender">事件发送方。</param>
        /// <param name="args">错误参数。</param>
        private void HandleError(object sender, ErrorEventArgs args)
        {
            connectCompletion?.TrySetException(args.Exception ?? new InvalidOperationException(args.Message));
            connectCompletion = null;
        }

        /// <summary>
        /// 完成未结束握手并转发关闭状态。
        /// </summary>
        /// <param name="sender">事件发送方。</param>
        /// <param name="args">关闭参数。</param>
        private void HandleClose(object sender, CloseEventArgs args)
        {
            connectCompletion?.TrySetException(new InvalidOperationException($"WebSocket 握手前关闭：{args.Code} {args.Reason}"));
            connectCompletion = null;
            Closed?.Invoke(args.Code, args.Reason);
        }

        /// <summary>
        /// 解绑事件并释放当前底层连接。
        /// </summary>
        private void CloseSocket()
        {
            WebSocket current = socket;
            socket = null;
            if (current == null)
            {
                return;
            }

            current.OnOpen -= HandleOpen;
            current.OnMessage -= HandleMessage;
            current.OnError -= HandleError;
            current.OnClose -= HandleClose;
            if (current.ReadyState == WebSocketState.Open || current.ReadyState == WebSocketState.Closing)
            {
                current.Close(CloseStatusCode.Normal, "Closing");
            }
        }

        /// <summary>
        /// 已释放后禁止继续连接或发送。
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(NativeWebSocketClientAdapter));
            }
        }

        #endregion
    }
}
#endif
