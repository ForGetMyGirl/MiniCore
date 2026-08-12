#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using AOT;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Platform.Browser
{
    /// <summary>
    /// 使用浏览器 IndexedDB 保存逻辑键二进制数据的 WebGL 后端。
    /// </summary>
    internal sealed class BrowserIndexedDbStorageBackend : IStorageBackend
    {
        #region Private 私有成员

        private static readonly Dictionary<int, MTaskCompletionSource<byte[]>> ReadRequests
            = new Dictionary<int, MTaskCompletionSource<byte[]>>(); // 等待读取结果的请求。
        private static readonly Dictionary<int, MTaskCompletionSource<bool>> BooleanRequests
            = new Dictionary<int, MTaskCompletionSource<bool>>(); // 等待写入、删除或存在性结果的请求。
        private static int nextRequestId; // 单调递增请求标识。

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ReadCallback(int requestId, IntPtr data, int length, int found);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void BooleanCallback(int requestId, int value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ErrorCallback(int requestId, IntPtr message);

        private static readonly ReadCallback ReadHandler = HandleRead;
        private static readonly BooleanCallback BooleanHandler = HandleBoolean;
        private static readonly ErrorCallback ErrorHandler = HandleError;

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 从 IndexedDB 读取指定逻辑键。
        /// </summary>
        /// <param name="key">逻辑键。</param>
        /// <returns>键不存在时返回空。</returns>
        public MTask<byte[]> ReadAsync(string key)
        {
            ValidateKey(key);
            int requestId = Interlocked.Increment(ref nextRequestId);
            var completion = new MTaskCompletionSource<byte[]>();
            ReadRequests.Add(requestId, completion);
            MiniCoreStorageRead(requestId, key, ReadHandler, ErrorHandler);
            return completion.Task;
        }

        /// <summary>
        /// 将完整字节数组覆盖写入 IndexedDB。
        /// </summary>
        /// <param name="key">逻辑键。</param>
        /// <param name="bytes">需要保存的字节数组。</param>
        /// <returns>事务提交完成任务。</returns>
        public MTask WriteAsync(string key, byte[] bytes)
        {
            ValidateKey(key);
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            int requestId = Interlocked.Increment(ref nextRequestId);
            var completion = new MTaskCompletionSource<bool>();
            BooleanRequests.Add(requestId, completion);
            MiniCoreStorageWrite(requestId, key, bytes, bytes.Length, BooleanHandler, ErrorHandler);
            return AwaitBooleanAsync(completion);
        }

        /// <summary>
        /// 从 IndexedDB 删除指定逻辑键。
        /// </summary>
        /// <param name="key">逻辑键。</param>
        /// <returns>事务提交完成任务。</returns>
        public MTask DeleteAsync(string key)
        {
            ValidateKey(key);
            int requestId = Interlocked.Increment(ref nextRequestId);
            var completion = new MTaskCompletionSource<bool>();
            BooleanRequests.Add(requestId, completion);
            MiniCoreStorageDelete(requestId, key, BooleanHandler, ErrorHandler);
            return AwaitBooleanAsync(completion);
        }

        /// <summary>
        /// 查询 IndexedDB 中是否存在指定逻辑键。
        /// </summary>
        /// <param name="key">逻辑键。</param>
        /// <returns>键存在时返回 true。</returns>
        public MTask<bool> ExistsAsync(string key)
        {
            ValidateKey(key);
            int requestId = Interlocked.Increment(ref nextRequestId);
            var completion = new MTaskCompletionSource<bool>();
            BooleanRequests.Add(requestId, completion);
            MiniCoreStorageExists(requestId, key, BooleanHandler, ErrorHandler);
            return completion.Task;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 等待布尔完成源并丢弃内部结果。
        /// </summary>
        /// <param name="completion">写入或删除完成源。</param>
        /// <returns>操作结果任务。</returns>
        private static async MTask AwaitBooleanAsync(MTaskCompletionSource<bool> completion)
        {
            await completion.Task;
        }

        /// <summary>
        /// 完成 IndexedDB 读取请求。
        /// </summary>
        /// <param name="requestId">请求标识。</param>
        /// <param name="data">WebAssembly 数据地址。</param>
        /// <param name="length">数据长度。</param>
        /// <param name="found">键是否存在。</param>
        [MonoPInvokeCallback(typeof(ReadCallback))]
        private static void HandleRead(int requestId, IntPtr data, int length, int found)
        {
            if (!ReadRequests.TryGetValue(requestId, out MTaskCompletionSource<byte[]> completion))
            {
                return;
            }

            ReadRequests.Remove(requestId);
            if (found == 0)
            {
                completion.TrySetResult(null);
                return;
            }

            byte[] result = new byte[length];
            if (length > 0)
            {
                Marshal.Copy(data, result, 0, length);
            }

            completion.TrySetResult(result);
        }

        /// <summary>
        /// 完成写入、删除或存在性请求。
        /// </summary>
        /// <param name="requestId">请求标识。</param>
        /// <param name="value">布尔结果。</param>
        [MonoPInvokeCallback(typeof(BooleanCallback))]
        private static void HandleBoolean(int requestId, int value)
        {
            if (BooleanRequests.TryGetValue(requestId, out MTaskCompletionSource<bool> completion))
            {
                BooleanRequests.Remove(requestId);
                completion.TrySetResult(value != 0);
            }
        }

        /// <summary>
        /// 以异常结束对应类型的 IndexedDB 请求。
        /// </summary>
        /// <param name="requestId">请求标识。</param>
        /// <param name="message">UTF-8 错误字符串地址。</param>
        [MonoPInvokeCallback(typeof(ErrorCallback))]
        private static void HandleError(int requestId, IntPtr message)
        {
            var exception = new InvalidOperationException(Marshal.PtrToStringAnsi(message) ?? "IndexedDB operation failed.");
            if (ReadRequests.TryGetValue(requestId, out MTaskCompletionSource<byte[]> readCompletion))
            {
                ReadRequests.Remove(requestId);
                readCompletion.TrySetException(exception);
            }

            if (BooleanRequests.TryGetValue(requestId, out MTaskCompletionSource<bool> booleanCompletion))
            {
                BooleanRequests.Remove(requestId);
                booleanCompletion.TrySetException(exception);
            }
        }

        /// <summary>
        /// 校验逻辑键不能为空。
        /// </summary>
        /// <param name="key">待校验逻辑键。</param>
        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("存储逻辑键不能为空。", nameof(key));
            }
        }

        /// <summary>
        /// 请求 JavaScript 从 IndexedDB 读取逻辑键。
        /// </summary>
        [DllImport("__Internal")]
        private static extern void MiniCoreStorageRead(int requestId, string key, ReadCallback completed, ErrorCallback failed);

        /// <summary>
        /// 请求 JavaScript 将二进制数据写入 IndexedDB。
        /// </summary>
        [DllImport("__Internal")]
        private static extern void MiniCoreStorageWrite(
            int requestId,
            string key,
            byte[] bytes,
            int length,
            BooleanCallback completed,
            ErrorCallback failed);

        /// <summary>
        /// 请求 JavaScript 删除 IndexedDB 逻辑键。
        /// </summary>
        [DllImport("__Internal")]
        private static extern void MiniCoreStorageDelete(int requestId, string key, BooleanCallback completed, ErrorCallback failed);

        /// <summary>
        /// 请求 JavaScript 查询 IndexedDB 逻辑键是否存在。
        /// </summary>
        [DllImport("__Internal")]
        private static extern void MiniCoreStorageExists(int requestId, string key, BooleanCallback completed, ErrorCallback failed);

        #endregion
    }
}
#endif
