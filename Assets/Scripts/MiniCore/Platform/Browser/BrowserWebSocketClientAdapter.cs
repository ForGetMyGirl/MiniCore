#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using AOT;
using MiniCore.Model;
using MiniCore.Threading;

namespace MiniCore.Platform.Browser
{
    /// <summary>
    /// 通过浏览器 JavaScript WebSocket API 实现的 WebGL 客户端适配器。
    /// </summary>
    internal sealed class BrowserWebSocketClientAdapter : IWebSocketClientAdapter
    {
        #region Private 私有成员

        private const int MaximumBufferedSendBytes = 1024 * 1024; // 浏览器发送缓冲区背压上限。
        private static readonly Dictionary<int, BrowserWebSocketClientAdapter> Instances = new Dictionary<int, BrowserWebSocketClientAdapter>(); // JavaScript 句柄到实例的映射。
        private static int nextInstanceId; // 单调递增实例标识。
        private readonly int instanceId; // 当前 JavaScript WebSocket 句柄。
        private MTaskCompletionSource<bool> connectCompletion; // 当前握手等待者。
        private bool disposed; // 是否已经释放。

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void OpenCallback(int id);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void MessageCallback(int id, IntPtr data, int length);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ErrorCallback(int id, IntPtr message);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void CloseCallback(int id, int code, IntPtr reason);

        private static readonly OpenCallback OpenHandler = HandleOpen;
        private static readonly MessageCallback MessageHandler = HandleMessage;
        private static readonly ErrorCallback ErrorHandler = HandleError;
        private static readonly CloseCallback CloseHandler = HandleClose;

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 创建并登记一个浏览器 WebSocket 实例。
        /// </summary>
        public BrowserWebSocketClientAdapter()
        {
            instanceId = Interlocked.Increment(ref nextInstanceId);
            Instances.Add(instanceId, this);
        }

        /// <summary>
        /// 获取浏览器 WebSocket 是否处于打开状态。
        /// </summary>
        public bool IsOpen => !disposed && MiniCoreWebSocketGetState(instanceId) == 1;

        /// <summary>
        /// 收到完整二进制消息时触发。
        /// </summary>
        public event Action<ArraySegment<byte>> BinaryMessageReceived;

        /// <summary>
        /// 浏览器连接关闭时触发。
        /// </summary>
        public event Action<ushort, string> Closed;

        /// <summary>
        /// 创建浏览器 WebSocket 并等待握手回调。
        /// </summary>
        /// <param name="url">完整 WS/WSS 地址。</param>
        /// <param name="maximumMessageSize">单条二进制消息最大字节数。</param>
        /// <returns>握手成功或失败时完成的任务。</returns>
        public MTask ConnectAsync(string url, int maximumMessageSize)
        {
            ThrowIfDisposed();
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri)
                || (uri.Scheme != "ws" && uri.Scheme != "wss"))
            {
                throw new ArgumentException("WebSocket 地址必须是有效的 ws:// 或 wss:// 绝对地址。", nameof(url));
            }

            if (maximumMessageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumMessageSize));
            }

            connectCompletion = new MTaskCompletionSource<bool>();
            int result = MiniCoreWebSocketConnect(
                instanceId,
                url,
                maximumMessageSize,
                OpenHandler,
                MessageHandler,
                ErrorHandler,
                CloseHandler);
            if (result == 0)
            {
                connectCompletion.TrySetException(new InvalidOperationException("浏览器拒绝创建 WebSocket。"));
            }

            return AwaitConnectedAsync(connectCompletion);
        }

        /// <summary>
        /// 将连续字节复制到浏览器并发送；浏览器缓冲区过高时立即报告背压。
        /// </summary>
        /// <param name="data">完整二进制消息。</param>
        /// <returns>浏览器接管消息后完成的任务。</returns>
        public MTask SendAsync(ArraySegment<byte> data)
        {
            ThrowIfDisposed();
            if (data.Array == null)
            {
                throw new ArgumentException("待发送消息没有底层数组。", nameof(data));
            }

            int result = MiniCoreWebSocketSend(
                instanceId,
                data.Array,
                data.Offset,
                data.Count,
                MaximumBufferedSendBytes);
            if (result == -2)
            {
                throw new InvalidOperationException("浏览器 WebSocket 发送缓冲区已达到背压上限。");
            }

            if (result <= 0)
            {
                throw new InvalidOperationException("浏览器 WebSocket 未处于可发送状态。");
            }

            return MTask.CompletedTask;
        }

        /// <summary>
        /// 使用正常状态关闭浏览器连接。
        /// </summary>
        public void Close()
        {
            if (!disposed)
            {
                MiniCoreWebSocketClose(instanceId, 1000, "Closing");
            }
        }

        /// <summary>
        /// 关闭 JavaScript 对象并移除回调映射。
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            Close();
            disposed = true;
            MiniCoreWebSocketDestroy(instanceId);
            Instances.Remove(instanceId);
            connectCompletion?.TrySetException(new ObjectDisposedException(nameof(BrowserWebSocketClientAdapter)));
            connectCompletion = null;
            BinaryMessageReceived = null;
            Closed = null;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 等待握手完成源并丢弃内部布尔结果。
        /// </summary>
        /// <param name="completion">握手完成源。</param>
        /// <returns>握手结果任务。</returns>
        private static async MTask AwaitConnectedAsync(MTaskCompletionSource<bool> completion)
        {
            await completion.Task;
        }

        /// <summary>
        /// 完成指定实例的握手等待。
        /// </summary>
        /// <param name="id">JavaScript 实例标识。</param>
        [MonoPInvokeCallback(typeof(OpenCallback))]
        private static void HandleOpen(int id)
        {
            if (Instances.TryGetValue(id, out BrowserWebSocketClientAdapter instance))
            {
                instance.connectCompletion?.TrySetResult(true);
                instance.connectCompletion = null;
            }
        }

        /// <summary>
        /// 将 WebAssembly 内存中的二进制消息复制到托管数组并同步转交传输层。
        /// </summary>
        /// <param name="id">JavaScript 实例标识。</param>
        /// <param name="data">WebAssembly 消息地址。</param>
        /// <param name="length">消息长度。</param>
        [MonoPInvokeCallback(typeof(MessageCallback))]
        private static void HandleMessage(int id, IntPtr data, int length)
        {
            if (length <= 0 || !Instances.TryGetValue(id, out BrowserWebSocketClientAdapter instance))
            {
                return;
            }

            byte[] message = new byte[length];
            Marshal.Copy(data, message, 0, length);
            instance.BinaryMessageReceived?.Invoke(new ArraySegment<byte>(message));
        }

        /// <summary>
        /// 将浏览器错误反馈给尚未完成的握手等待者。
        /// </summary>
        /// <param name="id">JavaScript 实例标识。</param>
        /// <param name="message">UTF-8 错误字符串地址。</param>
        [MonoPInvokeCallback(typeof(ErrorCallback))]
        private static void HandleError(int id, IntPtr message)
        {
            if (Instances.TryGetValue(id, out BrowserWebSocketClientAdapter instance))
            {
                string text = Marshal.PtrToStringAnsi(message) ?? "Browser WebSocket error.";
                instance.connectCompletion?.TrySetException(new InvalidOperationException(text));
                instance.connectCompletion = null;
            }
        }

        /// <summary>
        /// 完成未结束的握手并转发 RFC 6455 关闭状态。
        /// </summary>
        /// <param name="id">JavaScript 实例标识。</param>
        /// <param name="code">关闭状态码。</param>
        /// <param name="reason">UTF-8 关闭原因地址。</param>
        [MonoPInvokeCallback(typeof(CloseCallback))]
        private static void HandleClose(int id, int code, IntPtr reason)
        {
            if (Instances.TryGetValue(id, out BrowserWebSocketClientAdapter instance))
            {
                string text = Marshal.PtrToStringAnsi(reason) ?? string.Empty;
                instance.connectCompletion?.TrySetException(
                    new InvalidOperationException($"WebSocket 握手前关闭：{code} {text}"));
                instance.connectCompletion = null;
                instance.Closed?.Invoke((ushort)code, text);
            }
        }

        /// <summary>
        /// 已释放后禁止继续使用适配器。
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(BrowserWebSocketClientAdapter));
            }
        }

        /// <summary>
        /// 请求 JavaScript 宿主创建连接并登记托管回调。
        /// </summary>
        [DllImport("__Internal")]
        private static extern int MiniCoreWebSocketConnect(
            int id,
            string url,
            int maximumMessageSize,
            OpenCallback open,
            MessageCallback message,
            ErrorCallback error,
            CloseCallback close);

        /// <summary>
        /// 将指定数组片段复制到 JavaScript 并发送。
        /// </summary>
        [DllImport("__Internal")]
        private static extern int MiniCoreWebSocketSend(
            int id,
            byte[] data,
            int offset,
            int length,
            int maximumBufferedBytes);

        /// <summary>
        /// 查询 JavaScript WebSocket 的 RFC 6455 状态值。
        /// </summary>
        [DllImport("__Internal")]
        private static extern int MiniCoreWebSocketGetState(int id);

        /// <summary>
        /// 请求 JavaScript WebSocket 执行关闭握手。
        /// </summary>
        [DllImport("__Internal")]
        private static extern void MiniCoreWebSocketClose(int id, int code, string reason);

        /// <summary>
        /// 释放 JavaScript WebSocket 句柄和全部回调。
        /// </summary>
        [DllImport("__Internal")]
        private static extern void MiniCoreWebSocketDestroy(int id);

        #endregion
    }
}
#endif
