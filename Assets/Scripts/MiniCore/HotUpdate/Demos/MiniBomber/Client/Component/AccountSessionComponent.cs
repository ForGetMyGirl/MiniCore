using System;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using MiniCore.Serialization;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 客户端认证、Coordinator 发现、Lobby 直连与断线恢复组件。
    /// </summary>
    public sealed class AccountSessionComponent : AComponent
    {
        #region Private 私有成员

        private const string CoordinatorSessionId = "MiniBomber.Coordinator"; // 临时 Coordinator 会话。
        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5); // 服务连接探测超时。
        private INetworkService network; // 可选网络服务。
        private IHttpService http; // 可选 HTTP 认证服务。
        private ISaveService saveService; // 客户端加密存档服务。
        private MiniBomberClientNetworkProfile profile; // MiniBomber 客户端独有网络配置。
        private MiniBomberSavedSessionData savedSession; // 当前认证会话。
        private string lobbyWebSocketUrl; // 本次认证流程动态发现的 Lobby 地址。
        private Action transportDisconnectedHandler; // 当前 Lobby 传输断开回调。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 登录或恢复状态变化事件。
        /// </summary>
        public event Action Changed;

        /// <summary>
        /// 底层 Lobby 传输断开事件。
        /// </summary>
        public event Action Disconnected;

        /// <summary>
        /// 获取当前是否已经直连 Lobby。
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// 获取当前是否持有认证服务器签发的会话。
        /// </summary>
        public bool IsAuthenticated => savedSession != null && savedSession.PlayerId > 0 && !string.IsNullOrEmpty(savedSession.SessionToken);

        /// <summary>
        /// 获取当前玩家标识。
        /// </summary>
        public long PlayerId => savedSession?.PlayerId ?? 0;

        /// <summary>
        /// 获取当前玩家显示名。
        /// </summary>
        public string PlayerName => savedSession?.PlayerName ?? string.Empty;

        /// <summary>
        /// 应用 MiniBomber 客户端业务配置并取得所需可选服务。
        /// </summary>
        /// <param name="networkProfile">只包含认证入口的客户端业务配置。</param>
        /// <returns>本地会话加载完成任务。</returns>
        public async MTask InitializeAsync(MiniBomberClientNetworkProfile networkProfile)
        {
            profile = networkProfile ?? throw new ArgumentNullException(nameof(networkProfile));
            if (!profile.EnableNetwork)
            {
                return;
            }

            network = Global.GetService<INetworkService>(this);
            saveService = Global.GetService<ISaveService>(this);
            if (profile.EnableAuthentication)
            {
                http = Global.GetService<IHttpService>(this);
                ValidateAuthenticationBaseUrl(profile.AuthenticationBaseUrl);
            }

            savedSession = await saveService.LoadProtobufAsync<MiniBomberSavedSessionData>(MiniBomberConstants.ClientSessionSlot) ?? new MiniBomberSavedSessionData();
            network.DefaultSessionId = MiniBomberConstants.DefaultSessionId;
            Changed?.Invoke();
        }

        /// <summary>
        /// 为登录前 UI 检查 MiniBomber 是否允许联网；已发现 Lobby 时执行真正重连。
        /// </summary>
        /// <returns>允许继续认证或 Lobby 重连成功时返回 true。</returns>
        public async MTask<bool> ConnectAsync()
        {
            if (profile == null || !profile.EnableNetwork || network == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(lobbyWebSocketUrl))
            {
                return profile.EnableAuthentication && http != null;
            }

            return await ConnectLobbyAsync(lobbyWebSocketUrl);
        }

        /// <summary>
        /// 通过独立认证服务器注册新账号。
        /// </summary>
        public async MTask<AuthenticationRegisterResponse> RegisterAsync(string account, string password, string playerName)
        {
            EnsureAuthenticationEnabled();
            AuthenticationRegisterResponse result = await http.SendJsonAsync<AuthenticationRegisterRequest, AuthenticationRegisterResponse>(
                BuildAuthenticationUrl("/api/auth/register"),
                new AuthenticationRegisterRequest
                {
                    Account = account ?? string.Empty,
                    Password = password ?? string.Empty,
                    PlayerName = playerName ?? string.Empty
                });
            return new AuthenticationRegisterResponse
            {
                Code = result?.Code ?? 500,
                Msg = result?.Msg ?? "认证服务器没有返回注册结果"
            };
        }

        /// <summary>
        /// 通过认证服务器登录，再通过 Coordinator 发现并直连 Lobby。
        /// </summary>
        public async MTask<MiniBomberResumeSessionResponse> LoginAsync(string account, string password)
        {
            EnsureAuthenticationEnabled();
            AuthenticationLoginResponse result = await http.SendJsonAsync<AuthenticationLoginRequest, AuthenticationLoginResponse>(
                BuildAuthenticationUrl("/api/auth/login"),
                new AuthenticationLoginRequest
                {
                    Account = account ?? string.Empty,
                    Password = password ?? string.Empty
                });
            if (result == null || result.Code != 0)
            {
                return new MiniBomberResumeSessionResponse
                {
                    Code = result?.Code ?? 500,
                    Msg = result?.Msg ?? "认证服务器没有返回登录结果"
                };
            }

            string lobbyUrl = await ResolveLobbyAsync(result.CoordinatorWebSocketUrl);
            if (!await ConnectLobbyAsync(lobbyUrl))
            {
                return new MiniBomberResumeSessionResponse { Code = 503, Msg = "认证成功，但无法连接 Lobby" };
            }

            savedSession = new MiniBomberSavedSessionData
            {
                PlayerId = result.AccountId,
                PlayerName = result.PlayerName ?? string.Empty,
                SessionToken = result.SessionToken ?? string.Empty
            };
            lobbyWebSocketUrl = lobbyUrl;
            await saveService.SaveProtobufAsync(MiniBomberConstants.ClientSessionSlot, savedSession);

            MiniBomberResumeSessionResponse lobbyResponse = await ResumeAsync();
            Changed?.Invoke();
            return lobbyResponse;
        }

        /// <summary>
        /// 使用认证令牌恢复当前 Lobby 业务会话。
        /// </summary>
        public async MTask<MiniBomberResumeSessionResponse> ResumeAsync()
        {
            if (!IsAuthenticated || !IsConnected)
            {
                return new MiniBomberResumeSessionResponse
                {
                    Code = MiniBomberErrorCode.SessionExpired,
                    Msg = "没有可恢复的 Lobby 会话"
                };
            }

            MiniBomberResumeSessionResponse response = await network.CallAsync<MiniBomberResumeSessionRequest, MiniBomberResumeSessionResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberResumeSessionRequest
            {
                PlayerId = savedSession.PlayerId,
                SessionToken = savedSession.SessionToken,
                Version = CreateVersionInfo(),
                PlayerName = savedSession.PlayerName
            });
            if (response.Code == MiniBomberErrorCode.Success && response.Player != null)
            {
                savedSession.PlayerName = response.Player.PlayerName;
                await saveService.SaveProtobufAsync(MiniBomberConstants.ClientSessionSlot, savedSession);
            }

            Changed?.Invoke();
            return response;
        }

        /// <summary>
        /// 清除本地认证信息并断开当前 Lobby。
        /// </summary>
        public async MTask LogoutAsync()
        {
            UnbindTransport();
            savedSession = new MiniBomberSavedSessionData();
            lobbyWebSocketUrl = null;
            network?.DisconnectSession(MiniBomberConstants.DefaultSessionId);
            IsConnected = false;
            if (saveService != null)
            {
                await saveService.SaveProtobufAsync(MiniBomberConstants.ClientSessionSlot, savedSession);
            }

            Changed?.Invoke();
        }

        /// <summary>
        /// 标记底层 Lobby 连接已经断开。
        /// </summary>
        public void MarkDisconnected()
        {
            IsConnected = false;
            Changed?.Invoke();
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 释放服务引用和事件订阅。
        /// </summary>
        protected override void OnDispose()
        {
            Changed = null;
            Disconnected = null;
            UnbindTransport();
            network = null;
            http = null;
            saveService = null;
            profile = null;
            savedSession = null;
            lobbyWebSocketUrl = null;
            Global.ReleaseAll(this);
            base.OnDispose();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 连接认证响应下发的 Coordinator 并解析一个 Ready Lobby。
        /// </summary>
        private async MTask<string> ResolveLobbyAsync(string coordinatorWebSocketUrl)
        {
            ValidateWebSocketUrl(coordinatorWebSocketUrl, "CoordinatorWebSocketUrl");
            bool connected = await network.ConnectWebSocketSessionAsync(CoordinatorSessionId, coordinatorWebSocketUrl, ConnectTimeout);
            if (!connected)
            {
                throw new InvalidOperationException("无法连接认证服务器下发的 Coordinator。");
            }

            try
            {
                ResolveServiceResponse response = await network.CallAsync<ResolveServiceRequest, ResolveServiceResponse>(CoordinatorSessionId, new ResolveServiceRequest
                {
                    ServiceKind = (ClusterServiceKind)(int)ServiceKind.Lobby
                }, timeoutSeconds: 8);
                if (response.Code != 0 || response.Endpoint == null)
                {
                    throw new InvalidOperationException($"Coordinator 没有返回可用 Lobby：{response.Msg}");
                }

                ValidateWebSocketUrl(response.Endpoint.OuterWebSocketUrl, "LobbyWebSocketUrl");
                return response.Endpoint.OuterWebSocketUrl;
            }
            finally
            {
                network.DisconnectSession(CoordinatorSessionId);
            }
        }

        /// <summary>
        /// 建立 Lobby WebSocket 会话并绑定断开通知。
        /// </summary>
        private async MTask<bool> ConnectLobbyAsync(string url)
        {
            UnbindTransport();
            network.DisconnectSession(MiniBomberConstants.DefaultSessionId);
            IsConnected = await network.ConnectWebSocketSessionAsync(MiniBomberConstants.DefaultSessionId, url, ConnectTimeout);
            if (IsConnected)
            {
                NetworkSession session = network.GetSession(MiniBomberConstants.DefaultSessionId);
                if (session?.Transport != null)
                {
                    transportDisconnectedHandler = NotifyTransportDisconnected;
                    session.Transport.OnDisconnected += transportDisconnectedHandler;
                }
            }

            Changed?.Invoke();
            return IsConnected;
        }

        /// <summary>
        /// 确认当前业务选择了 HTTP 认证实现。
        /// </summary>
        private void EnsureAuthenticationEnabled()
        {
            if (profile == null || !profile.EnableNetwork || !profile.EnableAuthentication || http == null)
            {
                throw new InvalidOperationException("MiniBomber 当前没有启用 HTTP 认证业务。");
            }
        }

        /// <summary>
        /// 拼接认证服务器 API 绝对地址。
        /// </summary>
        private string BuildAuthenticationUrl(string path)
        {
            return profile.AuthenticationBaseUrl.TrimEnd('/') + path;
        }

        /// <summary>
        /// 验证认证地址使用 HTTP 或 HTTPS 绝对地址。
        /// </summary>
        private static void ValidateAuthenticationBaseUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri) ||
                (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("MiniBomber AuthenticationBaseUrl 必须是 HTTP 或 HTTPS 绝对地址。", nameof(url));
            }
        }

        /// <summary>
        /// 验证动态发现地址使用 WebSocket 协议。
        /// </summary>
        private static void ValidateWebSocketUrl(string url, string field)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri) ||
                (!string.Equals(uri.Scheme, "ws", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(uri.Scheme, "wss", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"{field} 不是有效的 ws:// 或 wss:// 地址。");
            }
        }

        /// <summary>
        /// 创建 Lobby 会话恢复使用的客户端版本信息。
        /// </summary>
        private static MiniBomberVersionInfo CreateVersionInfo()
        {
            return new MiniBomberVersionInfo
            {
                BuildVersion = UnityEngine.Application.version,
                ProtocolVersion = MiniBomberConstants.ProtocolVersion,
                RuleVersion = MiniBomberConstants.RuleVersion,
                ContentVersion = UnityEngine.Application.version
            };
        }

        /// <summary>
        /// 将 Lobby 传输断开转为业务事件。
        /// </summary>
        private void NotifyTransportDisconnected()
        {
            IsConnected = false;
            Disconnected?.Invoke();
        }

        /// <summary>
        /// 解除当前 Lobby 传输断开监听。
        /// </summary>
        private void UnbindTransport()
        {
            NetworkSession session = network?.GetSession(MiniBomberConstants.DefaultSessionId);
            if (session?.Transport != null && transportDisconnectedHandler != null)
            {
                session.Transport.OnDisconnected -= transportDisconnectedHandler;
            }

            transportDisconnectedHandler = null;
        }

        #endregion
    }
}
