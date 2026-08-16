using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 客户端独有的联网业务配置，只保存可替换的认证入口。
    /// </summary>
    [CreateAssetMenu(fileName = "MiniBomberClientNetworkProfile", menuName = "MiniCore/Demos/MiniBomber/Client Network Profile")]
    public sealed class MiniBomberClientNetworkProfile : ScriptableObject
    {
        #region Private 私有成员

        [SerializeField] private bool enableNetwork = true; // 是否启动 MiniBomber 联网业务。
        [SerializeField] private bool enableAuthentication = true; // 是否使用当前 HTTP 认证实现。
        [SerializeField] private string authenticationBaseUrl = "https://auth.example.com"; // 唯一客户端静态服务器入口。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取是否启动 MiniBomber 联网业务。
        /// </summary>
        public bool EnableNetwork => enableNetwork;

        /// <summary>
        /// 获取是否使用 HTTP 账号认证。
        /// </summary>
        public bool EnableAuthentication => enableAuthentication;

        /// <summary>
        /// 获取认证服务器 HTTPS 基地址。
        /// </summary>
        public string AuthenticationBaseUrl => authenticationBaseUrl;

        #endregion
    }
}
