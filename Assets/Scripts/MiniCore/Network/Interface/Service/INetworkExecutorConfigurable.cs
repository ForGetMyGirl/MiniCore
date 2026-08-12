using MiniCore.Threading;

namespace MiniCore.Core
{
    /// <summary>
    /// 允许网络中枢向会话服务注入本模块持有的异步执行器。
    /// </summary>
    internal interface INetworkExecutorConfigurable
    {
        #region Internal 内部成员

        /// <summary>
        /// 配置后续创建的监听器、传输和发送队列所使用的执行器。
        /// </summary>
        /// <param name="executor">由网络中枢持有并负责释放的执行器。</param>
        void SetNetworkExecutor(IMTaskExecutor executor);

        #endregion
    }
}
