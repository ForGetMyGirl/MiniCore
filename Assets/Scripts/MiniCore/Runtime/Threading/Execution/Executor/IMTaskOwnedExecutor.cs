using System;

namespace MiniCore.Threading
{
    /// <summary>
    /// 表示由具体模块持有正常生命周期、同时受 MTask 统一登记和退出监管的执行器。
    /// </summary>
    public interface IMTaskOwnedExecutor : IMTaskExecutor, IDisposable
    {
        /// <summary>
        /// 获取执行器是否已经收到释放请求。
        /// </summary>
        bool IsDisposed { get; }
    }
}
