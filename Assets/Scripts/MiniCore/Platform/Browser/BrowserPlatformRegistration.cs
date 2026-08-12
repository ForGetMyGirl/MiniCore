#if UNITY_WEBGL && !UNITY_EDITOR
using MiniCore.Model;
using MiniCore.Service;
using UnityEngine;

namespace MiniCore.Platform.Browser
{
    /// <summary>
    /// 在浏览器 Player 启动前注册 WebSocket 客户端适配器和 IndexedDB 存储后端。
    /// </summary>
    internal static class BrowserPlatformRegistration
    {
        #region Private 私有成员

        /// <summary>
        /// 将浏览器 WebSocket 适配器与存储后端工厂安装到对应注册表。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            WebSocketClientAdapterRegistry.RegisterClientFactory(() => new BrowserWebSocketClientAdapter());
            StorageBackendRegistry.RegisterFactory(() => new BrowserIndexedDbStorageBackend());
        }

        #endregion
    }
}
#endif
