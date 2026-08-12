using System;

namespace MiniCore.Model
{
    /// <summary>
    /// 保存当前运行环境的 WebSocket 客户端适配器工厂。
    /// </summary>
    public static class WebSocketClientAdapterRegistry
    {
        #region Private 私有成员

        private static readonly object Gate = new object(); // 保护适配器工厂替换。
        private static Func<IWebSocketClientAdapter> clientFactory; // 当前平台的客户端适配器工厂。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 注册当前平台的 WebSocket 客户端适配器工厂。
        /// 后注册的平台实现会替换默认实现，供浏览器或小游戏 SDK 接管。
        /// </summary>
        /// <param name="factory">每次调用都返回全新客户端适配器实例的工厂。</param>
        public static void RegisterClientFactory(Func<IWebSocketClientAdapter> factory)
        {
            lock (Gate)
            {
                clientFactory = factory ?? throw new ArgumentNullException(nameof(factory));
            }
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建当前平台已注册的 WebSocket 客户端适配器。
        /// </summary>
        /// <returns>可供单条客户端连接独占使用的适配器实例。</returns>
        internal static IWebSocketClientAdapter CreateClient()
        {
            Func<IWebSocketClientAdapter> factory;
            lock (Gate)
            {
                factory = clientFactory;
            }

            if (factory != null)
            {
                return factory();
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            throw new PlatformNotSupportedException("当前 WebGL 宿主尚未注册 IWebSocketClientAdapter。请启用 MiniCore 浏览器平台模块。");
#else
            return new NativeWebSocketClientAdapter();
#endif
        }

        #endregion
    }
}
