using MiniCore.Threading;
using System;
using System.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// 客户端和服务端共用的逻辑会话契约。
    /// </summary>
    public interface ISession : IDisposable
    {
        /// <summary>
        /// 会话的唯一标识。
        /// </summary>
        string SessionId { get; }
        /// <summary>
        /// 会话底层传输是否可用。
        /// </summary>
        bool IsConnected { get; }
        /// <summary>
        /// 发送一个完整业务数据包。
        /// </summary>
        MTask SendAsync(ArraySegment<byte> data);
        /// <summary>
        /// 关闭会话。
        /// </summary>
        void Close();
        /// <summary>
        /// 会话断开时触发。
        /// </summary>
        event Action OnDisconnected;
    }
}
