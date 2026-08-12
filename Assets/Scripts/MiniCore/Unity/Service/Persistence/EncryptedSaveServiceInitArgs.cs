using MiniCore.Model;

namespace MiniCore.Service
{
    /// <summary>
    /// 保护二进制存档服务的启动参数。
    /// </summary>
    public sealed class EncryptedSaveServiceInitArgs : ComponentInitArgs
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置由开发者填写的稳定加密口令。
        /// 修改该值后旧存档将无法读取；该值会进入客户端构建，只用于提高本地读取和篡改成本。
        /// </summary>
        public string EncryptionKey { get; set; } = string.Empty;

        #endregion
    }
}
