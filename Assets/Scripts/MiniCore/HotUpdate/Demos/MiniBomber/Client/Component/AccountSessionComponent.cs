using System;
using System.Collections.Generic;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using MiniCore.Serialization;
using MiniCore.Threading;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// MiniBomber 客户端连接、登录与断线恢复状态组件。
    /// </summary>
    public sealed class AccountSessionComponent : AComponent
    {
        #region Private 私有成员

        private INetworkService network; // 项目网络服务。
        private ISaveService saveService; // 客户端加密存档服务。
        private MiniBomberSavedSessionData savedSession; // 当前恢复会话的 Protobuf 持久化数据。
        private Action transportDisconnectedHandler; // 当前客户端传输断开回调。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 登录或恢复状态变化事件。
        /// </summary>
        public event Action Changed;

        /// <summary>
        /// 底层客户端传输断开事件。
        /// </summary>
        public event Action Disconnected;

        /// <summary>
        /// 当前网络是否已经建立连接。
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// 当前是否已经通过服务器认证。
        /// </summary>
        public bool IsAuthenticated => savedSession != null && savedSession.PlayerId > 0 && !string.IsNullOrEmpty(savedSession.SessionToken);

        /// <summary>
        /// 当前玩家身份。
        /// </summary>
        public long PlayerId => savedSession?.PlayerId ?? 0;

        /// <summary>
        /// 当前玩家显示名。
        /// </summary>
        public string PlayerName => savedSession?.PlayerName ?? string.Empty;

        /// <summary>
        /// 当前连接服务器地址。
        /// </summary>
        public string Host => savedSession?.Host ?? string.Empty;

        /// <summary>
        /// 当前连接服务器端口。
        /// </summary>
        public int Port => savedSession?.Port ?? MiniBomberConstants.DefaultServerPort;

        /// <summary>
        /// 加载本地加密恢复会话并取得网络服务。
        /// </summary>
        /// <returns>初始化完成任务。</returns>
        public async MTask InitializeAsync()
        {
            network = Global.GetService<INetworkService>(this);
            saveService = Global.GetService<ISaveService>(this);
            savedSession = await saveService.LoadProtobufAsync<MiniBomberSavedSessionData>(MiniBomberConstants.ClientSessionSlot) ?? new MiniBomberSavedSessionData
            {
                Host = "127.0.0.1",
                Port = MiniBomberConstants.DefaultServerPort
            };
            network.DefaultSessionId = MiniBomberConstants.DefaultSessionId;
            Changed?.Invoke();
        }

        /// <summary>
        /// 连接指定 MiniBomber KCP 服务器。
        /// </summary>
        /// <param name="host">服务器地址。</param>
        /// <param name="port">服务器端口。</param>
        /// <returns>握手和探测成功时返回 true。</returns>
        public async MTask<bool> ConnectAsync(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host) || port <= 0 || port > 65535)
            {
                return false;
            }

            string normalizedHost = host.Trim();
            if (IsConnected && string.Equals(Host, normalizedHost, StringComparison.OrdinalIgnoreCase) && Port == port)
            {
                return true;
            }

            if (IsConnected)
            {
                UnbindTransport();
                network.DisconnectSession(MiniBomberConstants.DefaultSessionId);
            }

            IsConnected = await network.ConnectKcpSessionAsync(MiniBomberConstants.DefaultSessionId, normalizedHost, port, MiniBomberConstants.KcpConversation, TimeSpan.FromSeconds(5));
            if (IsConnected)
            {
                savedSession ??= new MiniBomberSavedSessionData();
                savedSession.Host = normalizedHost;
                savedSession.Port = port;
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
        /// 请求服务器注册新账号。
        /// </summary>
        /// <param name="account">登录账号。</param>
        /// <param name="password">密码。</param>
        /// <param name="playerName">唯一玩家名。</param>
        /// <returns>服务器注册响应。</returns>
        public MTask<MiniBomberRegisterResponse> RegisterAsync(string account, string password, string playerName)
        {
            EnsureConnected();
            return network.CallAsync<MiniBomberRegisterRequest, MiniBomberRegisterResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberRegisterRequest
            {
                Account = account ?? string.Empty,
                Password = password ?? string.Empty,
                PlayerName = playerName ?? string.Empty,
                Version = CreateVersionInfo()
            });
        }

        /// <summary>
        /// 登录服务器并加密保存恢复令牌。
        /// </summary>
        /// <param name="account">登录账号。</param>
        /// <param name="password">密码。</param>
        /// <returns>服务器登录响应。</returns>
        public async MTask<MiniBomberLoginResponse> LoginAsync(string account, string password)
        {
            EnsureConnected();
            MiniBomberLoginResponse response = await network.CallAsync<MiniBomberLoginRequest, MiniBomberLoginResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberLoginRequest
            {
                Account = account ?? string.Empty,
                Password = password ?? string.Empty,
                Version = CreateVersionInfo()
            });
            if (response.Code == MiniBomberErrorCode.Success && response.Player != null)
            {
                savedSession.PlayerId = response.Player.PlayerId;
                savedSession.PlayerName = response.Player.PlayerName;
                savedSession.SessionToken = response.SessionToken;
                await saveService.SaveProtobufAsync(MiniBomberConstants.ClientSessionSlot, savedSession);
                Changed?.Invoke();
            }

            return response;
        }

        /// <summary>
        /// 使用本地令牌恢复服务器会话。
        /// </summary>
        /// <returns>服务器恢复响应；无令牌时返回会话过期响应。</returns>
        public async MTask<MiniBomberResumeSessionResponse> ResumeAsync()
        {
            if (!IsAuthenticated)
            {
                return new MiniBomberResumeSessionResponse
                {
                    Code = MiniBomberErrorCode.SessionExpired,
                    Msg = "没有可恢复的登录会话"
                };
            }

            EnsureConnected();
            MiniBomberResumeSessionResponse response = await network.CallAsync<MiniBomberResumeSessionRequest, MiniBomberResumeSessionResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberResumeSessionRequest
            {
                PlayerId = savedSession.PlayerId,
                SessionToken = savedSession.SessionToken,
                Version = CreateVersionInfo()
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
        /// 清除本地认证信息并断开当前连接。
        /// </summary>
        /// <returns>清理存档完成任务。</returns>
        public async MTask LogoutAsync()
        {
            string previousHost = Host;
            int previousPort = Port;
            UnbindTransport();
            savedSession = new MiniBomberSavedSessionData
            {
                Host = previousHost,
                Port = previousPort
            };
            if (network != null)
            {
                network.DisconnectSession(MiniBomberConstants.DefaultSessionId);
            }

            IsConnected = false;
            await saveService.SaveProtobufAsync(MiniBomberConstants.ClientSessionSlot, savedSession);
            Changed?.Invoke();
        }

        /// <summary>
        /// 标记底层连接已经断开，供重连流程调用。
        /// </summary>
        public void MarkDisconnected()
        {
            IsConnected = false;
            Changed?.Invoke();
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 释放服务引用和事件订阅者。
        /// </summary>
        protected override void OnDispose()
        {
            Changed = null;
            Disconnected = null;
            UnbindTransport();
            network = null;
            saveService = null;
            savedSession = null;
            Global.ReleaseAll(this);
            base.OnDispose();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 确认客户端已经连接服务器。
        /// </summary>
        private void EnsureConnected()
        {
            if (!IsConnected || network == null)
            {
                throw new InvalidOperationException("MiniBomber 客户端尚未连接服务器。");
            }
        }

        /// <summary>
        /// 创建登录和恢复共用的版本握手。
        /// </summary>
        /// <returns>当前客户端版本信息。</returns>
        private static MiniBomberVersionInfo CreateVersionInfo()
        {
            return new MiniBomberVersionInfo
            {
                BuildVersion = Application.version,
                ProtocolVersion = MiniBomberConstants.ProtocolVersion,
                RuleVersion = MiniBomberConstants.RuleVersion,
                ContentVersion = Application.version
            };
        }

        /// <summary>
        /// 将底层传输断开转为客户端流程事件。
        /// </summary>
        private void NotifyTransportDisconnected()
        {
            IsConnected = false;
            Disconnected?.Invoke();
        }

        /// <summary>
        /// 解除当前客户端传输断开监听。
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
