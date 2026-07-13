using System;

namespace MiniCore.Core
{
    /// <summary>
    /// 为网络消息组件提供可替换会话服务的全局解析器注册表。
    /// </summary>
    public static class NetworkSessionServiceRegistry
    {
        private static readonly object SyncRoot = new object(); // 解析器读写同步锁。
        private static Func<INetworkSessionService> resolver; // 外部注入的会话服务解析器。

        /// <summary>
        /// 注册会话服务解析器，用于替换默认 Global 组件查找。
        /// </summary>
        /// <param name="serviceResolver">执行该方法所需的 serviceResolver 参数。</param>
        public static void RegisterResolver(Func<INetworkSessionService> serviceResolver)
        {
            lock (SyncRoot)
            {
                resolver = serviceResolver;
            }
        }

        /// <summary>
        /// 清除已注册的会话服务解析器。
        /// </summary>
        public static void ClearResolver()
        {
            lock (SyncRoot)
            {
                resolver = null;
            }
        }

        /// <summary>
        /// 尝试解析当前注册的会话服务。
        /// </summary>
        /// <param name="service">执行该方法所需的 service 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public static bool TryResolve(out INetworkSessionService service)
        {
            Func<INetworkSessionService> current;
            lock (SyncRoot)
            {
                current = resolver;
            }

            if (current == null)
            {
                service = null;
                return false;
            }

            service = current.Invoke();
            return service != null;
        }
    }
}
