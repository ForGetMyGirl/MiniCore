using System;
using System.Security.Cryptography.X509Certificates;

namespace MiniCore.Model
{
    /// <summary>
    /// 原生 WebSocket 服务端的握手、路径、消息大小和 TLS 配置。
    /// </summary>
    public sealed class WebSocketServerConfig
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置监听路径；必须以斜杠开头。
        /// </summary>
        public string Path { get; set; } = "/";

        /// <summary>
        /// 获取或设置单个业务包正文最大字节数。
        /// </summary>
        public int MaximumPacketSize { get; set; } = 4 * 1024 * 1024;

        /// <summary>
        /// 获取或设置单条 WebSocket 二进制消息最大字节数；一条消息可以包含多个业务帧。
        /// </summary>
        public int MaximumMessageSize { get; set; } = 16 * 1024 * 1024;

        /// <summary>
        /// 获取或设置每个连接等待串行派发的业务包数量上限。
        /// </summary>
        public int MaximumPendingPacketCount { get; set; } = 1024;

        /// <summary>
        /// 获取或设置是否启用 WSS。
        /// </summary>
        public bool Secure { get; set; }

        /// <summary>
        /// 获取或设置 WSS 服务端证书；启用安全模式时不能为空。
        /// </summary>
        public X509Certificate2 ServerCertificate { get; set; }

        /// <summary>
        /// 获取或设置握手 Host 校验器；为空表示接受任意 Host。
        /// </summary>
        public Func<string, bool> HostValidator { get; set; }

        /// <summary>
        /// 获取或设置握手 Origin 校验器；为空表示接受任意 Origin。
        /// </summary>
        public Func<string, bool> OriginValidator { get; set; }

        #endregion
    }
}
