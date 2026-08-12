using System;

namespace MiniCore.Service
{
    /// <summary>
    /// 保存当前运行平台的键值存储后端工厂。
    /// </summary>
    public static class StorageBackendRegistry
    {
        #region Private 私有成员

        private static readonly object Gate = new object(); // 保护平台工厂替换。
        private static Func<IStorageBackend> factory; // 当前平台后端工厂。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 注册当前平台的二进制存储后端工厂。
        /// </summary>
        /// <param name="backendFactory">每次调用都返回全新后端实例的工厂。</param>
        public static void RegisterFactory(Func<IStorageBackend> backendFactory)
        {
            lock (Gate)
            {
                factory = backendFactory ?? throw new ArgumentNullException(nameof(backendFactory));
            }
        }

        /// <summary>
        /// 尝试创建当前平台已经注册的存储后端。
        /// </summary>
        /// <param name="backend">成功时返回新后端实例。</param>
        /// <returns>当前平台已经注册工厂时返回 true。</returns>
        public static bool TryCreate(out IStorageBackend backend)
        {
            Func<IStorageBackend> current;
            lock (Gate)
            {
                current = factory;
            }

            backend = current?.Invoke();
            return backend != null;
        }

        #endregion
    }
}
