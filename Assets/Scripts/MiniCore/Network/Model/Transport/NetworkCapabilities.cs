namespace MiniCore.Model
{
    /// <summary>
    /// 查询当前运行环境能够创建的网络连接和监听器能力。
    /// </summary>
    public static class NetworkCapabilities
    {
        #region Public 公共成员

        /// <summary>
        /// 查询当前环境是否支持主动创建指定传输连接。
        /// </summary>
        /// <param name="kind">目标传输类型。</param>
        /// <returns>支持主动连接时返回 true。</returns>
        public static bool SupportsConnect(NetworkTransportKind kind)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return kind == NetworkTransportKind.WebSocket;
#else
            return true;
#endif
        }

        /// <summary>
        /// 查询当前环境是否支持创建指定传输监听器。
        /// </summary>
        /// <param name="kind">目标传输类型。</param>
        /// <returns>支持监听时返回 true。</returns>
        public static bool SupportsListen(NetworkTransportKind kind)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return false;
#else
            return true;
#endif
        }

        #endregion
    }
}
