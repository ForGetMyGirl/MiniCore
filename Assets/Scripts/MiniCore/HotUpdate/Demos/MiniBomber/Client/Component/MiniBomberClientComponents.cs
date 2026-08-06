using System;
using System.Collections.Generic;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using MiniCore.Threading;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 客户端加密保存的 MiniBomber 恢复会话数据。
    /// </summary>
    [Serializable]
    public sealed class MiniBomberSavedSession
    {
        #region Public 公共成员

        /// <summary>最近连接的服务器地址。</summary>
        public string Host { get; set; }

        /// <summary>最近连接的服务器端口。</summary>
        public int Port { get; set; }

        /// <summary>已登录玩家身份。</summary>
        public long PlayerId { get; set; }

        /// <summary>已登录玩家显示名。</summary>
        public string PlayerName { get; set; }

        /// <summary>服务器签发的恢复令牌。</summary>
        public string SessionToken { get; set; }

        #endregion
    }

    /// <summary>
    /// MiniBomber 客户端连接、登录与断线恢复状态组件。
    /// </summary>
    public sealed class AccountSessionComponent : AComponent
    {
        #region Private 私有成员

        private INetworkService network; // 项目网络服务。
        private ISaveService saveService; // 客户端加密存档服务。
        private MiniBomberSavedSession savedSession; // 当前恢复会话数据。
        private Action transportDisconnectedHandler; // 当前客户端传输断开回调。

        #endregion

        #region Public 公共成员

        /// <summary>登录或恢复状态变化事件。</summary>
        public event Action Changed;

        /// <summary>底层客户端传输断开事件。</summary>
        public event Action Disconnected;

        /// <summary>当前网络是否已经建立连接。</summary>
        public bool IsConnected { get; private set; }

        /// <summary>当前是否已经通过服务器认证。</summary>
        public bool IsAuthenticated => savedSession != null && savedSession.PlayerId > 0 && !string.IsNullOrEmpty(savedSession.SessionToken);

        /// <summary>当前玩家身份。</summary>
        public long PlayerId => savedSession?.PlayerId ?? 0;

        /// <summary>当前玩家显示名。</summary>
        public string PlayerName => savedSession?.PlayerName ?? string.Empty;

        /// <summary>当前连接服务器地址。</summary>
        public string Host => savedSession?.Host ?? string.Empty;

        /// <summary>当前连接服务器端口。</summary>
        public int Port => savedSession?.Port ?? MiniBomberConstants.DefaultServerPort;

        /// <summary>
        /// 加载本地加密恢复会话并取得网络服务。
        /// </summary>
        /// <returns>初始化完成任务。</returns>
        public async MTask InitializeAsync()
        {
            network = Global.GetService<INetworkService>(this);
            saveService = Global.GetService<ISaveService>(this);
            savedSession = await saveService.LoadAsync<MiniBomberSavedSession>(MiniBomberConstants.ClientSessionSlot) ?? new MiniBomberSavedSession
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
                savedSession ??= new MiniBomberSavedSession();
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
                await saveService.SaveAsync(MiniBomberConstants.ClientSessionSlot, savedSession);
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
                await saveService.SaveAsync(MiniBomberConstants.ClientSessionSlot, savedSession);
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
            savedSession = new MiniBomberSavedSession
            {
                Host = previousHost,
                Port = previousPort
            };
            if (network != null)
            {
                network.DisconnectSession(MiniBomberConstants.DefaultSessionId);
            }

            IsConnected = false;
            await saveService.SaveAsync(MiniBomberConstants.ClientSessionSlot, savedSession);
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

    /// <summary>
    /// MiniBomber 客户端大厅列表状态和命令组件。
    /// </summary>
    public sealed class LobbyComponent : AComponent
    {
        #region Private 私有成员

        private readonly List<MiniBomberRoomSummaryDto> rooms = new List<MiniBomberRoomSummaryDto>(32); // 当前大厅房间列表。
        private INetworkService network; // 项目网络服务。
        private AccountSessionComponent account; // 当前账号会话。

        #endregion

        #region Public 公共成员

        /// <summary>大厅列表变化事件。</summary>
        public event Action Changed;

        /// <summary>只读房间列表。</summary>
        public IReadOnlyList<MiniBomberRoomSummaryDto> Rooms => rooms;

        /// <summary>大厅修订号。</summary>
        public long Revision { get; private set; }

        /// <summary>服务器报告的在线人数。</summary>
        public int OnlinePlayerCount { get; private set; }

        /// <summary>
        /// 取得账号和网络依赖。
        /// </summary>
        public override void Awake()
        {
            network = Global.GetService<INetworkService>(this);
            account = Global.Get<AccountSessionComponent>(this);
        }

        /// <summary>
        /// 请求并替换完整大厅快照。
        /// </summary>
        /// <returns>服务器响应。</returns>
        public async MTask<MiniBomberLobbySnapshotResponse> RefreshAsync()
        {
            MiniBomberLobbySnapshotResponse response = await network.CallAsync<MiniBomberLobbySnapshotRequest, MiniBomberLobbySnapshotResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberLobbySnapshotRequest
            {
                PlayerId = account.PlayerId
            });
            if (response.Code == MiniBomberErrorCode.Success)
            {
                rooms.Clear();
                rooms.AddRange(response.Rooms);
                Revision = response.Revision;
                OnlinePlayerCount = response.OnlinePlayerCount;
                Changed?.Invoke();
            }

            return response;
        }

        /// <summary>
        /// 记录服务器大厅修订通知；Presenter 可据此决定立即刷新。
        /// </summary>
        /// <param name="revision">服务器最新修订号。</param>
        public void ApplyChangedNotice(long revision)
        {
            if (revision > Revision)
            {
                Revision = revision;
                Changed?.Invoke();
            }
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 清空大厅状态和依赖引用。
        /// </summary>
        protected override void OnDispose()
        {
            Changed = null;
            rooms.Clear();
            network = null;
            account = null;
            Global.ReleaseAll(this);
            base.OnDispose();
        }

        #endregion
    }

    /// <summary>
    /// MiniBomber 客户端当前房间权威快照和房间命令组件。
    /// </summary>
    public sealed class RoomComponent : AComponent
    {
        #region Private 私有成员

        private INetworkService network; // 项目网络服务。
        private AccountSessionComponent account; // 当前账号会话。

        #endregion

        #region Public 公共成员

        /// <summary>房间权威状态变化事件。</summary>
        public event Action Changed;

        /// <summary>当前房间权威快照。</summary>
        public MiniBomberRoomSnapshotDto Current { get; private set; }

        /// <summary>当前玩家是否为房主。</summary>
        public bool IsOwner => Current != null && Current.OwnerPlayerId == account.PlayerId;

        /// <summary>
        /// 取得账号和网络依赖。
        /// </summary>
        public override void Awake()
        {
            network = Global.GetService<INetworkService>(this);
            account = Global.Get<AccountSessionComponent>(this);
        }

        /// <summary>
        /// 创建房间并应用返回的权威快照。
        /// </summary>
        /// <param name="roomName">房间名。</param>
        /// <param name="durationSeconds">局时长秒数。</param>
        /// <returns>服务器响应。</returns>
        public async MTask<MiniBomberCreateRoomResponse> CreateAsync(string roomName, int durationSeconds)
        {
            MiniBomberCreateRoomResponse response = await network.CallAsync<MiniBomberCreateRoomRequest, MiniBomberCreateRoomResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberCreateRoomRequest
            {
                PlayerId = account.PlayerId,
                RoomName = roomName ?? string.Empty,
                DurationSeconds = durationSeconds
            });
            ApplySuccessfulRoom(response.Code, response.Room);
            return response;
        }

        /// <summary>
        /// 加入指定房间并应用返回的权威快照。
        /// </summary>
        /// <param name="roomId">房间身份。</param>
        /// <returns>服务器响应。</returns>
        public async MTask<MiniBomberJoinRoomResponse> JoinAsync(long roomId)
        {
            MiniBomberJoinRoomResponse response = await network.CallAsync<MiniBomberJoinRoomRequest, MiniBomberJoinRoomResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberJoinRoomRequest
            {
                PlayerId = account.PlayerId,
                RoomId = roomId
            });
            ApplySuccessfulRoom(response.Code, response.Room);
            return response;
        }

        /// <summary>
        /// 离开当前等待状态房间。
        /// </summary>
        /// <returns>服务器响应。</returns>
        public async MTask<MiniBomberLeaveRoomResponse> LeaveAsync()
        {
            long roomId = Current?.RoomId ?? 0;
            MiniBomberLeaveRoomResponse response = await network.CallAsync<MiniBomberLeaveRoomRequest, MiniBomberLeaveRoomResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberLeaveRoomRequest
            {
                PlayerId = account.PlayerId,
                RoomId = roomId
            });
            if (response.Code == MiniBomberErrorCode.Success)
            {
                Current = null;
                Changed?.Invoke();
            }

            return response;
        }

        /// <summary>
        /// 由房主修改名称和局时长。
        /// </summary>
        /// <param name="roomName">新房间名。</param>
        /// <param name="durationSeconds">新局时长。</param>
        /// <returns>服务器响应。</returns>
        public async MTask<MiniBomberUpdateRoomResponse> UpdateSettingsAsync(string roomName, int durationSeconds)
        {
            MiniBomberUpdateRoomResponse response = await network.CallAsync<MiniBomberUpdateRoomRequest, MiniBomberUpdateRoomResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberUpdateRoomRequest
            {
                PlayerId = account.PlayerId,
                RoomId = Current?.RoomId ?? 0,
                RoomName = roomName ?? string.Empty,
                DurationSeconds = durationSeconds,
                ExpectedRevision = Current?.Revision ?? 0
            });
            if (response.Room != null)
            {
                ApplySnapshot(response.Room);
            }

            return response;
        }

        /// <summary>
        /// 修改当前玩家准备状态。
        /// </summary>
        /// <param name="ready">目标准备状态。</param>
        /// <returns>服务器响应。</returns>
        public async MTask<MiniBomberSetReadyResponse> SetReadyAsync(bool ready)
        {
            MiniBomberSetReadyResponse response = await network.CallAsync<MiniBomberSetReadyRequest, MiniBomberSetReadyResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberSetReadyRequest
            {
                PlayerId = account.PlayerId,
                RoomId = Current?.RoomId ?? 0,
                IsReady = ready
            });
            ApplySuccessfulRoom(response.Code, response.Room);
            return response;
        }

        /// <summary>
        /// 房主请求开始比赛。
        /// </summary>
        /// <returns>服务器响应。</returns>
        public MTask<MiniBomberStartMatchResponse> StartMatchAsync()
        {
            return network.CallAsync<MiniBomberStartMatchRequest, MiniBomberStartMatchResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberStartMatchRequest
            {
                PlayerId = account.PlayerId,
                RoomId = Current?.RoomId ?? 0
            });
        }

        /// <summary>
        /// 应用服务器推送或恢复返回的权威房间快照。
        /// </summary>
        /// <param name="snapshot">新房间快照。</param>
        public void ApplySnapshot(MiniBomberRoomSnapshotDto snapshot)
        {
            if (snapshot == null || (Current != null && snapshot.Revision < Current.Revision))
            {
                return;
            }

            Current = snapshot;
            Changed?.Invoke();
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 清空房间状态和依赖引用。
        /// </summary>
        protected override void OnDispose()
        {
            Changed = null;
            Current = null;
            network = null;
            account = null;
            Global.ReleaseAll(this);
            base.OnDispose();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 在 RPC 成功时应用房间快照。
        /// </summary>
        /// <param name="code">业务错误码。</param>
        /// <param name="snapshot">服务器房间快照。</param>
        private void ApplySuccessfulRoom(int code, MiniBomberRoomSnapshotDto snapshot)
        {
            if (code == MiniBomberErrorCode.Success)
            {
                ApplySnapshot(snapshot);
            }
        }

        #endregion
    }

    /// <summary>
    /// MiniBomber 客户端战斗快照、即时事件、成绩和输入发送组件。
    /// </summary>
    public sealed class BattleClientComponent : AComponent
    {
        #region Private 私有成员

        private readonly List<MiniBomberBattleEventDto> recentEvents = new List<MiniBomberBattleEventDto>(32); // Presenter 消费的近期即时事件。
        private readonly MiniBomberBattleReplicationState replication = new MiniBomberBattleReplicationState(); // 纯 C# 基线和事件序列状态机。
        private INetworkService network; // 项目网络服务。
        private AccountSessionComponent account; // 当前账号会话。
        private long nextInputSequence = 1; // 下一个客户端输入序号。
        private bool resyncPending; // 是否已有完整关键帧请求在途。

        #endregion

        #region Public 公共成员

        /// <summary>战斗快照变化事件。</summary>
        public event Action SnapshotChanged;

        /// <summary>收到即时战斗事件时触发。</summary>
        public event Action EventsChanged;

        /// <summary>收到服务器唯一比赛结果时触发。</summary>
        public event Action ResultChanged;

        /// <summary>当前最新权威快照。</summary>
        public MiniBomberBattleSnapshot Snapshot => replication.Snapshot;

        /// <summary>最新权威快照到达客户端的单调时间。</summary>
        public double LastSnapshotReceiveTime { get; private set; }

        /// <summary>当前近期即时事件。</summary>
        public IReadOnlyList<MiniBomberBattleEventDto> RecentEvents => recentEvents;

        /// <summary>当前比赛最终结果。</summary>
        public MiniBomberMatchResultNotice Result { get; private set; }

        /// <summary>
        /// 取得账号和网络依赖。
        /// </summary>
        public override void Awake()
        {
            network = Global.GetService<INetworkService>(this);
            account = Global.Get<AccountSessionComponent>(this);
        }

        /// <summary>
        /// 发送单个量化输入帧；客户端预测可以立即使用相同输入，最终以服务器快照校正。
        /// </summary>
        /// <param name="matchId">比赛身份。</param>
        /// <param name="clientTick">客户端本地 Tick。</param>
        /// <param name="moveX">负一千到一千的横向输入。</param>
        /// <param name="moveZ">负一千到一千的纵向输入。</param>
        /// <param name="placeBomb">本帧是否按下炸弹按钮。</param>
        /// <returns>网络队列接受状态。</returns>
        public NetworkSendResult SendInput(long matchId, long clientTick, int moveX, int moveZ, bool placeBomb)
        {
            var batch = new MiniBomberBattleInputBatch
            {
                PlayerId = account.PlayerId,
                MatchId = matchId
            };
            batch.Frames.Add(new MiniBomberInputFrameDto
            {
                Sequence = nextInputSequence++,
                ClientTick = clientTick,
                MoveX = Mathf.Clamp(moveX, -1000, 1000),
                MoveZ = Mathf.Clamp(moveZ, -1000, 1000),
                PlaceBomb = placeBomb
            });
            return network.TrySend(MiniBomberConstants.DefaultSessionId, batch);
        }

        /// <summary>
        /// 应用顺序更新的服务器权威快照。
        /// </summary>
        /// <param name="snapshot">服务器快照。</param>
        public void ApplySnapshot(MiniBomberBattleSnapshot snapshot)
        {
            if (replication.ApplyKeyframe(snapshot) != MiniBomberReplicationApplyResult.Applied)
            {
                return;
            }

            resyncPending = false;
            LastSnapshotReceiveTime = Global.Time.UnscaledTime;
            SnapshotChanged?.Invoke();
        }

        /// <summary>
        /// 应用房间级玩家动态增量，检测到基线丢失时自动请求完整关键帧。
        /// </summary>
        /// <param name="delta">服务器玩家动态增量。</param>
        public void ApplyDelta(MiniBomberBattleDelta delta)
        {
            MiniBomberReplicationApplyResult result = replication.ApplyDelta(delta);
            if (result == MiniBomberReplicationApplyResult.RequiresResync)
            {
                RequestResyncAsync(delta?.MatchId ?? Snapshot?.MatchId ?? 0).Forget();
                return;
            }

            if (result == MiniBomberReplicationApplyResult.Applied)
            {
                LastSnapshotReceiveTime = Global.Time.UnscaledTime;
                SnapshotChanged?.Invoke();
            }
        }

        /// <summary>
        /// 应用服务器即时事件批次并限制本地缓存长度。
        /// </summary>
        /// <param name="batch">即时事件批次。</param>
        public void ApplyEvents(MiniBomberBattleEventBatch batch)
        {
            MiniBomberReplicationApplyResult result = replication.ApplyEvents(batch);
            if (result == MiniBomberReplicationApplyResult.RequiresResync)
            {
                RequestResyncAsync(batch?.MatchId ?? Snapshot?.MatchId ?? 0).Forget();
                return;
            }

            if (result != MiniBomberReplicationApplyResult.Applied)
            {
                return;
            }

            recentEvents.AddRange(batch.Events);
            if (recentEvents.Count > 32)
            {
                recentEvents.RemoveRange(0, recentEvents.Count - 32);
            }

            EventsChanged?.Invoke();
        }

        /// <summary>
        /// 应用服务器最终成绩，客户端不重新排序或计算名次。
        /// </summary>
        /// <param name="result">服务器唯一成绩消息。</param>
        public void ApplyResult(MiniBomberMatchResultNotice result)
        {
            Result = result;
            ResultChanged?.Invoke();
        }

        /// <summary>
        /// 清空上一局客户端战斗状态。
        /// </summary>
        public void ResetBattle()
        {
            replication.Reset();
            LastSnapshotReceiveTime = 0d;
            Result = null;
            recentEvents.Clear();
            nextInputSequence = 1;
            resyncPending = false;
            SnapshotChanged?.Invoke();
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 清空战斗状态、事件和依赖引用。
        /// </summary>
        protected override void OnDispose()
        {
            SnapshotChanged = null;
            EventsChanged = null;
            ResultChanged = null;
            replication.Reset();
            LastSnapshotReceiveTime = 0d;
            Result = null;
            recentEvents.Clear();
            network = null;
            account = null;
            resyncPending = false;
            Global.ReleaseAll(this);
            base.OnDispose();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 合并重复请求并要求服务器随后发送完整战斗关键帧。
        /// </summary>
        /// <param name="matchId">需要重同步的比赛身份。</param>
        /// <returns>服务器接受请求后的任务。</returns>
        private async MTask RequestResyncAsync(long matchId)
        {
            if (resyncPending || matchId <= 0 || account == null || network == null)
            {
                return;
            }

            resyncPending = true;
            MiniBomberBattleSnapshot snapshot = Snapshot;
            MiniBomberBattleResyncResponse response = await network.CallAsync<MiniBomberBattleResyncRequest, MiniBomberBattleResyncResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberBattleResyncRequest
            {
                PlayerId = account.PlayerId,
                MatchId = matchId,
                KnownServerTick = snapshot?.ServerTick ?? 0,
                KnownRevision = snapshot?.Revision ?? 0,
                KnownEventId = replication.LastEventId
            });
            if (response == null || response.Code != MiniBomberErrorCode.Success)
            {
                resyncPending = false;
                return;
            }

            if (response.Snapshot != null)
            {
                ApplySnapshot(response.Snapshot);
            }
        }

        #endregion
    }
}
