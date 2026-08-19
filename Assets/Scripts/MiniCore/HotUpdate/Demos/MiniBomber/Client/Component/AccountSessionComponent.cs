using System;
using System.Text;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using MiniCore.Serialization;
using MiniCore.Threading;
using Newtonsoft.Json;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 客户端认证、Coordinator 发现、Lobby 直连与断线恢复组件。
    /// </summary>
    public sealed class AccountSessionComponent : AComponent
    {
        #region Private 私有成员

        private const string CoordinatorSessionId = "MiniBomber.Coordinator"; // 临时 Coordinator 会话。
        private const int MinimumAccountLength = 3; // 账号最少字符数。
        private const int MaximumAccountLength = 64; // 账号最多字符数。
        private const int MinimumPasswordLength = 8; // 密码最少字符数。
        private const int MaximumPasswordLength = 128; // 密码最多字符数。
        private const int MaximumPlayerNameLength = 32; // 玩家名最多字符数。
        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5); // 服务连接探测超时。
        private readonly MiniBomberAccountModel model = new MiniBomberAccountModel(); // 当前账号长期业务数据。
        private INetworkService network; // 可选网络服务。
        private IHttpService http; // 可选 HTTP 认证服务。
        private ISaveService saveService; // 客户端加密存档服务。
        private MiniBomberClientNetworkProfile profile; // MiniBomber 客户端独有网络配置。
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
        /// 获取当前账号与会话的只读业务数据。
        /// </summary>
        public MiniBomberAccountModel Model => model;

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

            MiniBomberSavedSessionData savedSession = await saveService.LoadProtobufAsync<MiniBomberSavedSessionData>(MiniBomberConstants.ClientSessionSlot);
            model.PlayerId = savedSession?.PlayerId ?? 0;
            model.PlayerName = savedSession?.PlayerName ?? string.Empty;
            model.SessionToken = savedSession?.SessionToken ?? string.Empty;
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
        /// <param name="account">注册账号。</param>
        /// <param name="password">注册密码。</param>
        /// <param name="playerName">玩家显示名。</param>
        /// <returns>协议无关的注册结果。</returns>
        public async MTask<MiniBomberCommandResult> RegisterAsync(string account, string password, string playerName)
        {
            string normalizedAccount = (account ?? string.Empty).Trim();
            string normalizedPlayerName = (playerName ?? string.Empty).Trim();
            string requestPassword = password ?? string.Empty;
            MiniBomberCommandResult credentialValidation = ValidateAccountAndPassword(normalizedAccount, requestPassword);
            if (!credentialValidation.IsSuccess)
            {
                return credentialValidation;
            }

            MiniBomberCommandResult playerNameValidation = ValidatePlayerName(normalizedPlayerName);
            if (!playerNameValidation.IsSuccess)
            {
                return playerNameValidation;
            }

            EnsureAuthenticationEnabled();
            HttpResponse response = await SendAuthenticationRequestAsync(
                "/api/auth/register",
                new AuthenticationRegisterRequest
                {
                    Account = normalizedAccount,
                    Password = requestPassword,
                    PlayerName = normalizedPlayerName
                });
            AuthenticationRegisterResponse result = DeserializeAuthenticationResponse<AuthenticationRegisterResponse>(response);
            if (response != null && response.IsSuccess && result != null && result.Code == 0)
            {
                return new MiniBomberCommandResult(0, result.Msg ?? "注册成功");
            }

            return CreateAuthenticationFailureResult(
                response,
                result?.Code ?? 0,
                result?.Msg,
                "注册暂时失败，请稍后重试");
        }

        /// <summary>
        /// 通过认证服务器登录，再通过 Coordinator 发现并直连 Lobby。
        /// </summary>
        /// <param name="account">登录账号。</param>
        /// <param name="password">登录密码。</param>
        /// <returns>协议无关的会话恢复结果。</returns>
        public async MTask<MiniBomberSessionResult> LoginAsync(string account, string password)
        {
            string normalizedAccount = (account ?? string.Empty).Trim();
            string requestPassword = password ?? string.Empty;
            MiniBomberCommandResult validation = ValidateAccountAndPassword(normalizedAccount, requestPassword);
            if (!validation.IsSuccess)
            {
                return new MiniBomberSessionResult(validation);
            }

            EnsureAuthenticationEnabled();
            HttpResponse response = await SendAuthenticationRequestAsync(
                "/api/auth/login",
                new AuthenticationLoginRequest
                {
                    Account = normalizedAccount,
                    Password = requestPassword
                });
            AuthenticationLoginResponse result = DeserializeAuthenticationResponse<AuthenticationLoginResponse>(response);
            if (response == null || !response.IsSuccess || result == null || result.Code != 0)
            {
                return new MiniBomberSessionResult(CreateAuthenticationFailureResult(
                    response,
                    result?.Code ?? 0,
                    result?.Msg,
                    "登录暂时失败，请稍后重试"));
            }

            string lobbyUrl = await ResolveLobbyAsync(result.CoordinatorWebSocketUrl);
            if (!await ConnectLobbyAsync(lobbyUrl))
            {
                return new MiniBomberSessionResult(new MiniBomberCommandResult(503, "认证成功，但无法连接 Lobby"));
            }

            model.PlayerId = result.AccountId;
            model.PlayerName = result.PlayerName ?? string.Empty;
            model.SessionToken = result.SessionToken ?? string.Empty;
            lobbyWebSocketUrl = lobbyUrl;
            await SaveSessionAsync();

            MiniBomberSessionResult lobbyResponse = await ResumeAsync();
            Changed?.Invoke();
            return lobbyResponse;
        }

        /// <summary>
        /// 使用认证令牌恢复当前 Lobby 业务会话。
        /// </summary>
        /// <returns>协议无关的会话恢复结果。</returns>
        public async MTask<MiniBomberSessionResult> ResumeAsync()
        {
            if (!model.IsAuthenticated || !model.IsConnected)
            {
                return new MiniBomberSessionResult(new MiniBomberCommandResult(
                    MiniBomberErrorCode.SessionExpired,
                    "没有可恢复的 Lobby 会话"));
            }

            MiniBomberResumeSessionResponse response = await network.CallAsync<MiniBomberResumeSessionRequest, MiniBomberResumeSessionResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberResumeSessionRequest
            {
                PlayerId = model.PlayerId,
                SessionToken = model.SessionToken,
                Version = CreateVersionInfo(),
                PlayerName = model.PlayerName
            });
            if (response.Code == MiniBomberErrorCode.Success && response.Player != null)
            {
                model.PlayerName = response.Player.PlayerName ?? string.Empty;
                await SaveSessionAsync();
            }

            Changed?.Invoke();
            return new MiniBomberSessionResult(
                new MiniBomberCommandResult(response.Code, response.Msg),
                MiniBomberProtocolModelMapper.MapDestination(response.Destination),
                MiniBomberProtocolModelMapper.CreateRoom(response.Room));
        }

        /// <summary>
        /// 清除本地认证信息并断开当前 Lobby。
        /// </summary>
        public async MTask LogoutAsync()
        {
            UnbindTransport();
            model.PlayerId = 0;
            model.PlayerName = string.Empty;
            model.SessionToken = string.Empty;
            lobbyWebSocketUrl = null;
            network?.DisconnectSession(MiniBomberConstants.DefaultSessionId);
            model.IsConnected = false;
            if (saveService != null)
            {
                await SaveSessionAsync();
            }

            Changed?.Invoke();
        }

        /// <summary>
        /// 标记底层 Lobby 连接已经断开。
        /// </summary>
        public void MarkDisconnected()
        {
            model.IsConnected = false;
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
            model.IsConnected = false;
            model.PlayerId = 0;
            model.PlayerName = string.Empty;
            model.SessionToken = string.Empty;
            lobbyWebSocketUrl = null;
            Global.ReleaseAll(this);
            base.OnDispose();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 校验账号和密码长度，并返回可直接展示的中文结果。
        /// </summary>
        /// <param name="account">已经去除首尾空格的账号。</param>
        /// <param name="password">保持原样的密码。</param>
        /// <returns>校验成功时返回零错误码，否则返回具体输入提示。</returns>
        private static MiniBomberCommandResult ValidateAccountAndPassword(string account, string password)
        {
            if (string.IsNullOrEmpty(account))
            {
                return new MiniBomberCommandResult(400, "请输入账号");
            }

            if (account.Length < MinimumAccountLength)
            {
                return new MiniBomberCommandResult(400, $"账号至少需要 {MinimumAccountLength} 个字符");
            }

            if (account.Length > MaximumAccountLength)
            {
                return new MiniBomberCommandResult(400, $"账号不能超过 {MaximumAccountLength} 个字符");
            }

            if (string.IsNullOrEmpty(password))
            {
                return new MiniBomberCommandResult(400, "请输入密码");
            }

            if (password.Length < MinimumPasswordLength)
            {
                return new MiniBomberCommandResult(400, $"密码至少需要 {MinimumPasswordLength} 个字符");
            }

            if (password.Length > MaximumPasswordLength)
            {
                return new MiniBomberCommandResult(400, $"密码不能超过 {MaximumPasswordLength} 个字符");
            }

            return new MiniBomberCommandResult(0, string.Empty);
        }

        /// <summary>
        /// 校验注册玩家名并返回可直接展示的中文结果。
        /// </summary>
        /// <param name="playerName">已经去除首尾空格的玩家名。</param>
        /// <returns>校验成功时返回零错误码，否则返回具体输入提示。</returns>
        private static MiniBomberCommandResult ValidatePlayerName(string playerName)
        {
            if (string.IsNullOrEmpty(playerName))
            {
                return new MiniBomberCommandResult(400, "请输入玩家名");
            }

            if (playerName.Length > MaximumPlayerNameLength)
            {
                return new MiniBomberCommandResult(400, $"玩家名不能超过 {MaximumPlayerNameLength} 个字符");
            }

            return new MiniBomberCommandResult(0, string.Empty);
        }

        /// <summary>
        /// 使用现有 HTTP 原始响应接口发送认证 JSON，使业务层能够读取非成功状态的响应正文。
        /// </summary>
        /// <typeparam name="TRequest">认证请求对象类型。</typeparam>
        /// <param name="path">认证服务器 API 路径。</param>
        /// <param name="request">待序列化的认证请求。</param>
        /// <returns>包含状态码、错误和原始正文的 HTTP 响应。</returns>
        private MTask<HttpResponse> SendAuthenticationRequestAsync<TRequest>(string path, TRequest request)
        {
            return http.SendAsync(new HttpRequest
            {
                Url = BuildAuthenticationUrl(path),
                Method = "POST",
                ContentType = "application/json",
                Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request))
            });
        }

        /// <summary>
        /// 尝试从认证响应正文反序列化现有 JSON 响应对象。
        /// </summary>
        /// <typeparam name="TResponse">认证响应对象类型。</typeparam>
        /// <param name="response">HTTP 原始响应。</param>
        /// <returns>正文有效时返回认证响应；无正文或格式错误时返回空。</returns>
        private static TResponse DeserializeAuthenticationResponse<TResponse>(HttpResponse response) where TResponse : class
        {
            if (response?.Body == null || response.Body.Length == 0)
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<TResponse>(Encoding.UTF8.GetString(response.Body));
            }
            catch (JsonException exception)
            {
                LogSwitch.Warning($"MiniBomber 认证响应无法解析为 {typeof(TResponse).Name}：{exception.Message}");
                return null;
            }
        }

        /// <summary>
        /// 将 HTTP 状态和现有认证响应转换为玩家可理解的业务失败结果。
        /// </summary>
        /// <param name="response">HTTP 原始响应。</param>
        /// <param name="responseCode">认证响应正文中的业务错误码。</param>
        /// <param name="responseMessage">认证响应正文中的已有中文消息。</param>
        /// <param name="fallbackMessage">无法识别错误时的业务兜底提示。</param>
        /// <returns>非零错误码和对应中文提示。</returns>
        private static MiniBomberCommandResult CreateAuthenticationFailureResult(
            HttpResponse response,
            int responseCode,
            string responseMessage,
            string fallbackMessage)
        {
            long statusCode = response?.StatusCode ?? 0;
            int code = responseCode != 0
                ? responseCode
                : statusCode >= 400 && statusCode <= int.MaxValue
                    ? (int)statusCode
                    : -1;
            string message;
            if (statusCode == 0)
            {
                message = "网络连接失败，请检查网络后重试";
            }
            else if (code == 408 || statusCode == 408)
            {
                message = "请求超时，请稍后重试";
            }
            else if (code == 429 || statusCode == 429)
            {
                message = "操作过于频繁，请稍后再试";
            }
            else if (code >= 500 || statusCode >= 500)
            {
                message = "认证服务暂时不可用，请稍后重试";
            }
            else if (code == 401 || statusCode == 401)
            {
                message = "账号或密码错误";
            }
            else if (code == 409 || statusCode == 409)
            {
                message = "账号或玩家名已经存在";
            }
            else if (!string.IsNullOrWhiteSpace(responseMessage))
            {
                message = responseMessage;
            }
            else
            {
                message = fallbackMessage;
            }

            return new MiniBomberCommandResult(code, message);
        }

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
            model.IsConnected = await network.ConnectWebSocketSessionAsync(MiniBomberConstants.DefaultSessionId, url, ConnectTimeout);
            if (model.IsConnected)
            {
                NetworkSession session = network.GetSession(MiniBomberConstants.DefaultSessionId);
                if (session?.Transport != null)
                {
                    transportDisconnectedHandler = NotifyTransportDisconnected;
                    session.Transport.OnDisconnected += transportDisconnectedHandler;
                }
            }

            Changed?.Invoke();
            return model.IsConnected;
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
        /// 将当前账号 Model 转换为持久化 PB 并保存。
        /// </summary>
        /// <returns>本地会话保存完成任务。</returns>
        private MTask SaveSessionAsync()
        {
            return saveService.SaveProtobufAsync(MiniBomberConstants.ClientSessionSlot, new MiniBomberSavedSessionData
            {
                PlayerId = model.PlayerId,
                PlayerName = model.PlayerName,
                SessionToken = model.SessionToken
            });
        }

        /// <summary>
        /// 将 Lobby 传输断开转为业务事件。
        /// </summary>
        private void NotifyTransportDisconnected()
        {
            model.IsConnected = false;
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
