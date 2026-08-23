using System;
using System.Collections.Generic;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using MiniCore.Server;
using MiniCore.Threading;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber Dedicated Server 的大厅、房间和权威战斗运行时。
    /// </summary>
    public sealed class MiniBomberServerRuntimeComponent : AComponent
    {
        #region Private 私有成员

        private const int MaximumFixedStepsPerUpdate = 2; // 单次 Unity Update 允许补执行的最大逻辑步数。
        private const int MatchCountdownMilliseconds = 3000; // 全员加载完成后的统一倒计时。
        private const int ResultDisplayMilliseconds = 5000; // 客户端成绩面板默认展示时长。
        private readonly Dictionary<string, MiniBomberServerPlayerSession> playerByNetworkSession = new Dictionary<string, MiniBomberServerPlayerSession>(StringComparer.Ordinal); // 网络会话到玩家会话索引。
        private readonly Dictionary<long, MiniBomberServerPlayerSession> playerById = new Dictionary<long, MiniBomberServerPlayerSession>(); // 玩家身份到会话索引。
        private readonly Dictionary<long, MiniBomberServerRoom> rooms = new Dictionary<long, MiniBomberServerRoom>(); // 当前全部房间。
        private readonly Dictionary<long, MiniBomberServerMatch> matches = new Dictionary<long, MiniBomberServerMatch>(); // 当前全部比赛。
        private readonly List<long> cleanupPlayerIds = new List<long>(8); // 断线宽限清理复用列表。
        private readonly List<long> cleanupMatchIds = new List<long>(8); // 已结束比赛清理复用列表。
        private INetworkService network; // 项目网络服务。
        private MiniBomberDatabaseComponent database; // 可选 DatabaseServer 业务直连组件。
        private MiniBomberBattleMap battleMap; // 当前服务器使用的只读地图。
        private MiniBomberBattleRules battleRules; // 当前服务器使用的权威规则。
        private MiniBomberRoomWorkerPool roomWorkers; // 固定数量且由 Demo 自行持有的房间工作池。
        private int maximumPlayers = 4; // 房间最大人数。
        private int minimumPlayers = 2; // 正式开局最少人数。
        private int serverTickRate = 30; // 权威模拟频率。
        private int snapshotRate = 15; // 世界状态快照频率。
        private int roomWorkerCount = 2; // 固定 RoomWorker 数量。
        private int roomWorkerInputQueueCapacity = 1024; // 单 Worker 有界输入容量。
        private int roomWorkerOutputQueueCapacity = 256; // 单 Worker 有界输出容量。
        private int fullKeyframeIntervalMilliseconds = 2000; // 完整关键帧间隔。
        private int reconnectGraceMilliseconds = 15000; // 断线重连宽限时间。
        private int loadingTimeoutMilliseconds = 30000; // 战斗场景加载超时。
        private string battleSceneAddress = "BattleScene"; // 战斗场景资源地址。
        private string mapAddress = "MiniBomberDefaultMap"; // 地图资源地址。
        private long nextRoomId = 1; // 下一个房间身份。
        private long nextMatchId = 1; // 下一场比赛身份。
        private long lobbyRevision; // 大厅房间列表修订号。
        private double previousUpdateTime; // 上一次驱动固定时间步的单调时间。
        private double fixedStepAccumulator; // 尚未消费的固定时间步累计秒数。
        private bool initialized; // 运行时是否完成初始化。
        private bool acceptingNewWork = true; // Drain 后禁止创建或加入新的房间和比赛。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 当前运行时是否完成初始化。
        /// </summary>
        public bool IsInitialized => initialized;

        /// <summary>
        /// 获取当前在线玩家数量。
        /// </summary>
        public int OnlinePlayerCount => CountOnlinePlayers();

        /// <summary>
        /// 获取当前仍存在的房间数量。
        /// </summary>
        public int RoomCount => rooms.Count;

        /// <summary>
        /// 获取当前仍在运行或结算的比赛数量。
        /// </summary>
        public int MatchCount => matches.Count;

        /// <summary>
        /// 停止接受新的房间、加入和开局请求，已有会话继续运行到自然结束。
        /// </summary>
        public void BeginDrain()
        {
            acceptingNewWork = false;
        }

        /// <summary>
        /// 使用项目配置初始化服务器业务运行时；框架服务发现已在此前启动内外网监听。
        /// </summary>
        /// <param name="runtimeConfig">频率和资源地址配置。</param>
        /// <param name="ruleConfig">玩法规则配置；为空时使用计划默认值。</param>
        /// <param name="mapDefinition">17×13 地图配置；为空时创建内置测试地图。</param>
        /// <param name="persistenceMode">当前部署副本选择的持久化模式。</param>
        /// <returns>业务内存状态初始化完成任务。</returns>
        public async MTask InitializeAsync(
            MiniBomberRuntimeConfig runtimeConfig,
            MiniBomberRuleConfig ruleConfig,
            BomberMapDefinition mapDefinition,
            ServerPersistenceMode persistenceMode)
        {
            if (initialized)
            {
                return;
            }

            if (runtimeConfig == null)
            {
                throw new ArgumentNullException(nameof(runtimeConfig));
            }

            ApplyConfiguration(runtimeConfig, ruleConfig, mapDefinition);
            int deltaIntervalTicks = Math.Max(1, serverTickRate / Math.Max(1, snapshotRate));
            int keyframeIntervalTicks = Math.Max(1, (serverTickRate * fullKeyframeIntervalMilliseconds + 999) / 1000);
            roomWorkers = new MiniBomberRoomWorkerPool(roomWorkerCount, roomWorkerInputQueueCapacity, roomWorkerOutputQueueCapacity, deltaIntervalTicks, keyframeIntervalTicks);
            network = Global.GetService<INetworkService>(this);
            network.OnServerSessionClosed += HandleServerSessionClosed;
            if (persistenceMode == ServerPersistenceMode.Database)
            {
                database = AddComponent<MiniBomberDatabaseComponent>();
                await database.InitializeAsync();
            }

            previousUpdateTime = Global.Time.UnscaledTime;
            initialized = true;
            LogSwitch.Info($"MiniBomber 业务运行时已初始化，Roles:{DedicatedServerRuntimeContext.ActiveRoles} Tick:{serverTickRate}Hz Delta:{snapshotRate}Hz Workers:{roomWorkers.WorkerCount}。");
        }

        /// <summary>
        /// 在宽限期内恢复断开的玩家会话和房间或战斗状态。
        /// </summary>
        /// <param name="session">新网络会话。</param>
        /// <param name="request">恢复请求。</param>
        /// <param name="response">待填写响应。</param>
        public async MTask ResumeSessionAsync(NetworkSession session, MiniBomberResumeSessionRequest request, MiniBomberResumeSessionResponse response)
        {
            if (!ValidateVersion(request?.Version, out string versionMessage))
            {
                SetError(response, MiniBomberErrorCode.VersionMismatch, versionMessage);
                return;
            }

            if (request.PlayerId <= 0 || string.IsNullOrWhiteSpace(request.SessionToken))
            {
                SetError(response, MiniBomberErrorCode.SessionExpired, "登录会话已过期，请重新登录");
                return;
            }

            if (database != null)
            {
                LoadPlayerDataResponse databaseResponse = await database.LoadOrCreateAsync(request.PlayerId, request.PlayerName);
                if (databaseResponse.Code != 0)
                {
                    SetError(response, databaseResponse.Code, databaseResponse.Msg);
                    return;
                }

                if (databaseResponse.Player != null && !string.IsNullOrWhiteSpace(databaseResponse.Player.PlayerName))
                {
                    request.PlayerName = databaseResponse.Player.PlayerName;
                }
            }

            if (!playerById.TryGetValue(request.PlayerId, out MiniBomberServerPlayerSession player))
            {
                player = BindAuthenticatedSession(session.SessionId, request.PlayerId, request.PlayerName, request.SessionToken);
            }
            else
            {
                if (!FixedTimeTokenEquals(player.SessionToken, request.SessionToken) ||
                    (!player.IsOnline && Global.Time.UnscaledTime > player.ReconnectDeadline))
                {
                    SetError(response, MiniBomberErrorCode.SessionExpired, "登录会话已过期，请重新登录");
                    return;
                }

                RebindNetworkSession(player, session.SessionId);
            }

            response.Code = MiniBomberErrorCode.Success;
            response.Msg = "已恢复连接";
            response.Player = CreateProfile(player);
            response.Destination = ResolveDestination(player);
            response.MatchId = player.MatchId;
            if (player.RoomId > 0 && rooms.TryGetValue(player.RoomId, out MiniBomberServerRoom room))
            {
                SetRoomOnline(room, player.PlayerId, true);
                response.Room = CreateRoomSnapshot(room);
                BroadcastRoom(room);
            }

            if (player.MatchId > 0 && matches.TryGetValue(player.MatchId, out MiniBomberServerMatch match))
            {
                roomWorkers.TrySetPlayerOnline(match.MatchId, player.PlayerId, true);
                roomWorkers.TryRequestKeyframe(match.MatchId, player.NetworkSessionId);
            }
        }

        /// <summary>
        /// 获取大厅当前完整房间列表。
        /// </summary>
        /// <param name="session">请求网络会话。</param>
        /// <param name="request">大厅快照请求。</param>
        /// <param name="response">待填写响应。</param>
        public void GetLobbySnapshot(NetworkSession session, MiniBomberLobbySnapshotRequest request, MiniBomberLobbySnapshotResponse response)
        {
            if (!TryAuthorize(session, request.PlayerId, response))
            {
                return;
            }

            response.Code = MiniBomberErrorCode.Success;
            response.Msg = "大厅已刷新";
            response.Revision = lobbyRevision;
            response.OnlinePlayerCount = CountOnlinePlayers();
            foreach (KeyValuePair<long, MiniBomberServerRoom> pair in rooms)
            {
                response.Rooms.Add(CreateRoomSummary(pair.Value));
            }
        }

        /// <summary>
        /// 创建房间并把创建者设为房主。
        /// </summary>
        /// <param name="session">请求网络会话。</param>
        /// <param name="request">创建请求。</param>
        /// <param name="response">待填写响应。</param>
        public void CreateRoom(NetworkSession session, MiniBomberCreateRoomRequest request, MiniBomberCreateRoomResponse response)
        {
            if (!acceptingNewWork)
            {
                SetError(response, MiniBomberErrorCode.ServerUnavailable, "服务器正在摘流量，暂不接受新房间");
                return;
            }

            if (!TryAuthorize(session, request.PlayerId, response, out MiniBomberServerPlayerSession player))
            {
                return;
            }

            if (player.RoomId > 0 || !IsRoomNameValid(request.RoomName) || !IsDurationAllowed(request.DurationSeconds))
            {
                SetError(response, MiniBomberErrorCode.InvalidArgument, "房间名、时长或玩家当前状态不合法");
                return;
            }

            var room = new MiniBomberServerRoom
            {
                RoomId = nextRoomId++,
                RoomName = request.RoomName.Trim(),
                OwnerPlayerId = player.PlayerId,
                DurationSeconds = request.DurationSeconds,
                State = MiniBomberRoomState.MiniBomberRoomWaiting,
                Revision = 1
            };
            room.AddMember(CreateRoomMember(player));
            rooms.Add(room.RoomId, room);
            player.RoomId = room.RoomId;
            lobbyRevision++;
            response.Code = MiniBomberErrorCode.Success;
            response.Msg = "房间创建成功";
            response.Room = CreateRoomSnapshot(room);
            BroadcastLobbyChanged();
        }

        /// <summary>
        /// 加入仍处于等待阶段且未满员的房间。
        /// </summary>
        /// <param name="session">请求网络会话。</param>
        /// <param name="request">加入请求。</param>
        /// <param name="response">待填写响应。</param>
        public void JoinRoom(NetworkSession session, MiniBomberJoinRoomRequest request, MiniBomberJoinRoomResponse response)
        {
            if (!acceptingNewWork)
            {
                SetError(response, MiniBomberErrorCode.ServerUnavailable, "服务器正在摘流量，暂不接受新玩家加入房间");
                return;
            }

            if (!TryAuthorize(session, request.PlayerId, response, out MiniBomberServerPlayerSession player))
            {
                return;
            }

            if (player.RoomId > 0 || !rooms.TryGetValue(request.RoomId, out MiniBomberServerRoom room))
            {
                SetError(response, MiniBomberErrorCode.RoomNotFound, "房间不存在或玩家已经在房间中");
                return;
            }

            if (room.State != MiniBomberRoomState.MiniBomberRoomWaiting)
            {
                SetError(response, MiniBomberErrorCode.RoomAlreadyStarted, "比赛已经开始");
                return;
            }

            if (room.Members.Count >= maximumPlayers)
            {
                SetError(response, MiniBomberErrorCode.RoomFull, "房间人数已满");
                return;
            }

            room.AddMember(CreateRoomMember(player));
            player.RoomId = room.RoomId;
            lobbyRevision++;
            response.Code = MiniBomberErrorCode.Success;
            response.Msg = "已加入房间";
            response.Room = CreateRoomSnapshot(room);
            BroadcastRoom(room);
            BroadcastLobbyChanged();
        }

        /// <summary>
        /// 离开等待阶段房间并在需要时转移房主。
        /// </summary>
        /// <param name="session">请求网络会话。</param>
        /// <param name="request">离开请求。</param>
        /// <param name="response">待填写响应。</param>
        public void LeaveRoom(NetworkSession session, MiniBomberLeaveRoomRequest request, MiniBomberLeaveRoomResponse response)
        {
            if (!TryAuthorize(session, request.PlayerId, response, out MiniBomberServerPlayerSession player))
            {
                return;
            }

            if (!rooms.TryGetValue(request.RoomId, out MiniBomberServerRoom room) || room.State != MiniBomberRoomState.MiniBomberRoomWaiting)
            {
                SetError(response, MiniBomberErrorCode.InvalidRoomState, "当前不能离开房间");
                return;
            }

            RemovePlayerFromRoom(player, room);
            response.Code = MiniBomberErrorCode.Success;
            response.Msg = "已离开房间";
        }

        /// <summary>
        /// 由房主修改房间名称和局时长；设置改变后取消全员准备。
        /// </summary>
        /// <param name="session">请求网络会话。</param>
        /// <param name="request">房间更新请求。</param>
        /// <param name="response">待填写响应。</param>
        public void UpdateRoom(NetworkSession session, MiniBomberUpdateRoomRequest request, MiniBomberUpdateRoomResponse response)
        {
            if (!TryAuthorize(session, request.PlayerId, response) || !rooms.TryGetValue(request.RoomId, out MiniBomberServerRoom room))
            {
                SetError(response, MiniBomberErrorCode.RoomNotFound, "房间不存在");
                return;
            }

            if (room.OwnerPlayerId != request.PlayerId)
            {
                SetError(response, MiniBomberErrorCode.PermissionDenied, "只有房主可以修改房间");
                return;
            }

            if (room.State != MiniBomberRoomState.MiniBomberRoomWaiting || !IsRoomNameValid(request.RoomName) || !IsDurationAllowed(request.DurationSeconds))
            {
                SetError(response, MiniBomberErrorCode.InvalidArgument, "房间设置不合法");
                return;
            }

            if (request.ExpectedRevision != room.Revision)
            {
                SetError(response, MiniBomberErrorCode.RevisionConflict, "房间状态已变化，请刷新后重试");
                response.Room = CreateRoomSnapshot(room);
                return;
            }

            room.RoomName = request.RoomName.Trim();
            room.DurationSeconds = request.DurationSeconds;
            room.ResetReadiness();
            room.Revision++;
            lobbyRevision++;
            response.Code = MiniBomberErrorCode.Success;
            response.Msg = "房间设置已同步";
            response.Room = CreateRoomSnapshot(room);
            BroadcastRoom(room);
            BroadcastLobbyChanged();
        }

        /// <summary>
        /// 修改当前玩家准备状态并广播权威房间快照。
        /// </summary>
        /// <param name="session">请求网络会话。</param>
        /// <param name="request">准备状态请求。</param>
        /// <param name="response">待填写响应。</param>
        public void SetReady(NetworkSession session, MiniBomberSetReadyRequest request, MiniBomberSetReadyResponse response)
        {
            if (!TryAuthorize(session, request.PlayerId, response) || !rooms.TryGetValue(request.RoomId, out MiniBomberServerRoom room) || !room.TryGetMember(request.PlayerId, out MiniBomberServerRoomMember member))
            {
                SetError(response, MiniBomberErrorCode.RoomNotFound, "房间不存在");
                return;
            }

            if (room.State != MiniBomberRoomState.MiniBomberRoomWaiting)
            {
                SetError(response, MiniBomberErrorCode.InvalidRoomState, "比赛已经进入加载或战斗阶段");
                return;
            }

            member.IsReady = request.IsReady;
            room.Revision++;
            response.Code = MiniBomberErrorCode.Success;
            response.Msg = request.IsReady ? "已准备" : "已取消准备";
            response.Room = CreateRoomSnapshot(room);
            BroadcastRoom(room);
        }

        /// <summary>
        /// 房主在人数和准备条件满足时启动加载流程。
        /// </summary>
        /// <param name="session">请求网络会话。</param>
        /// <param name="request">开始比赛请求。</param>
        /// <param name="response">待填写响应。</param>
        public void StartMatch(NetworkSession session, MiniBomberStartMatchRequest request, MiniBomberStartMatchResponse response)
        {
            if (!acceptingNewWork)
            {
                SetError(response, MiniBomberErrorCode.ServerUnavailable, "服务器正在摘流量，暂不允许开始新比赛");
                return;
            }

            if (!TryAuthorize(session, request.PlayerId, response) || !rooms.TryGetValue(request.RoomId, out MiniBomberServerRoom room))
            {
                SetError(response, MiniBomberErrorCode.RoomNotFound, "房间不存在");
                return;
            }

            if (room.OwnerPlayerId != request.PlayerId)
            {
                SetError(response, MiniBomberErrorCode.PermissionDenied, "只有房主可以开始比赛");
                return;
            }

            if (!CanStart(room))
            {
                SetError(response, MiniBomberErrorCode.PlayersNotReady, $"至少 {minimumPlayers} 人且所有玩家必须在线并准备");
                return;
            }

            room.State = MiniBomberRoomState.MiniBomberRoomLoading;
            room.MatchId = nextMatchId++;
            room.LoadingDeadline = Global.Time.UnscaledTime + (loadingTimeoutMilliseconds / 1000d);
            room.Revision++;
            for (int index = 0; index < room.Members.Count; index++)
            {
                MiniBomberServerRoomMember member = room.Members[index];
                member.IsSceneReady = false;
                if (playerById.TryGetValue(member.PlayerId, out MiniBomberServerPlayerSession player))
                {
                    player.MatchId = room.MatchId;
                }
            }

            response.Code = MiniBomberErrorCode.Success;
            response.Msg = "开始加载战斗场景";
            response.MatchId = room.MatchId;
            BroadcastRoom(room);
            BroadcastMatchPrepare(room);
        }

        /// <summary>
        /// 标记玩家战斗场景加载完成；全员完成后建立模拟并广播倒计时。
        /// </summary>
        /// <param name="session">请求网络会话。</param>
        /// <param name="request">场景就绪请求。</param>
        /// <param name="response">待填写响应。</param>
        public void SetSceneReady(NetworkSession session, MiniBomberSceneReadyRequest request, MiniBomberSceneReadyResponse response)
        {
            if (!TryAuthorize(session, request.PlayerId, response) || !rooms.TryGetValue(request.RoomId, out MiniBomberServerRoom room) || room.MatchId != request.MatchId || !room.TryGetMember(request.PlayerId, out MiniBomberServerRoomMember member))
            {
                SetError(response, MiniBomberErrorCode.MatchNotFound, "比赛不存在");
                return;
            }

            if (room.State != MiniBomberRoomState.MiniBomberRoomLoading)
            {
                SetError(response, MiniBomberErrorCode.InvalidRoomState, "比赛不在加载阶段");
                return;
            }

            member.IsSceneReady = true;
            room.Revision++;
            response.Code = MiniBomberErrorCode.Success;
            response.Msg = "场景加载完成";
            if (AreAllScenesReady(room))
            {
                BeginBattle(room);
            }
        }

        /// <summary>
        /// 接收并提交一批递增序号输入到服务器权威模拟。
        /// </summary>
        /// <param name="session">输入所属网络会话。</param>
        /// <param name="message">量化输入批次。</param>
        public void SubmitBattleInput(NetworkSession session, MiniBomberBattleInputBatch message)
        {
            if (!TryAuthorize(session, message.PlayerId, out MiniBomberServerPlayerSession player) || player.MatchId != message.MatchId || !matches.TryGetValue(message.MatchId, out MiniBomberServerMatch match))
            {
                return;
            }

            for (int index = 0; index < message.Frames.Count; index++)
            {
                MiniBomberInputFrameDto frame = message.Frames[index];
                if (!roomWorkers.TrySubmitInput(match.MatchId, player.PlayerId, new MiniBomberBattleInput(frame.Sequence, frame.MoveX, frame.MoveZ, frame.PlaceBomb)))
                {
                    LogSwitch.Warning($"MiniBomber RoomWorker 输入队列已满，Match:{match.MatchId} Player:{player.PlayerId} Sequence:{frame.Sequence}。");
                    break;
                }
            }
        }

        /// <summary>
        /// 接受客户端基线不匹配请求，并异步安排完整关键帧回到该会话。
        /// </summary>
        /// <param name="session">请求网络会话。</param>
        /// <param name="request">客户端当前基线信息。</param>
        /// <param name="response">只确认是否接受请求的响应；关键帧随后走普通消息。</param>
        public void RequestBattleResync(NetworkSession session, MiniBomberBattleResyncRequest request, MiniBomberBattleResyncResponse response)
        {
            if (!TryAuthorize(session, request.PlayerId, response, out MiniBomberServerPlayerSession player) || player.MatchId != request.MatchId || !matches.ContainsKey(request.MatchId))
            {
                return;
            }

            if (!roomWorkers.TryRequestKeyframe(request.MatchId, session.SessionId))
            {
                SetError(response, MiniBomberErrorCode.InvalidRoomState, "战斗同步队列繁忙，请稍后重试");
                return;
            }

            response.Code = MiniBomberErrorCode.Success;
            response.Msg = "已安排完整战斗关键帧";
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 以单调时间驱动固定时间步；每次最多补两步，严重积压时丢弃多余时间。
        /// </summary>
        protected override void Update()
        {
            if (!initialized)
            {
                return;
            }

            double now = Global.Time.UnscaledTime;
            double elapsed = now - previousUpdateTime;
            previousUpdateTime = now;
            fixedStepAccumulator += Math.Max(0d, Math.Min(elapsed, 0.25d));
            double fixedStep = 1d / serverTickRate;
            int steps = 0;
            while (fixedStepAccumulator >= fixedStep && steps < MaximumFixedStepsPerUpdate)
            {
                fixedStepAccumulator -= fixedStep;
                TickMatches(now);
                steps++;
            }

            if (fixedStepAccumulator >= fixedStep)
            {
                LogSwitch.Warning($"MiniBomber 服务器固定时间步积压 {fixedStepAccumulator:F3}s，已丢弃超出补步上限的部分。");
                fixedStepAccumulator = 0d;
            }

            UpdateDisconnectedPlayers(now);
            UpdateLoadingTimeouts(now);
            DrainRoomWorkerOutputs();
        }

        /// <summary>
        /// 解除网络事件、服务引用和所有内存状态。
        /// </summary>
        protected override void OnDispose()
        {
            if (network != null)
            {
                network.OnServerSessionClosed -= HandleServerSessionClosed;
            }

            roomWorkers?.Dispose();

            playerByNetworkSession.Clear();
            playerById.Clear();
            rooms.Clear();
            matches.Clear();
            cleanupPlayerIds.Clear();
            cleanupMatchIds.Clear();
            network = null;
            database = null;
            battleMap = null;
            battleRules = null;
            roomWorkers = null;
            initialized = false;
            acceptingNewWork = false;
            Global.ReleaseAll(this);
            base.OnDispose();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 应用 ScriptableObject 配置，缺少资产时生成可运行的默认规则和 17×13 地图。
        /// </summary>
        /// <param name="runtimeConfig">运行时配置。</param>
        /// <param name="ruleConfig">规则配置。</param>
        /// <param name="mapDefinition">地图定义。</param>
        private void ApplyConfiguration(MiniBomberRuntimeConfig runtimeConfig, MiniBomberRuleConfig ruleConfig, BomberMapDefinition mapDefinition)
        {
            serverTickRate = runtimeConfig != null ? runtimeConfig.ServerTickRate : 30;
            snapshotRate = runtimeConfig != null ? runtimeConfig.SnapshotRate : 15;
            roomWorkerCount = runtimeConfig != null ? runtimeConfig.RoomWorkerCount : 2;
            roomWorkerInputQueueCapacity = runtimeConfig != null ? runtimeConfig.RoomWorkerInputQueueCapacity : 1024;
            roomWorkerOutputQueueCapacity = runtimeConfig != null ? runtimeConfig.RoomWorkerOutputQueueCapacity : 256;
            fullKeyframeIntervalMilliseconds = runtimeConfig != null ? runtimeConfig.FullKeyframeIntervalMilliseconds : 2000;
            reconnectGraceMilliseconds = runtimeConfig != null ? runtimeConfig.ReconnectGraceMilliseconds : 15000;
            loadingTimeoutMilliseconds = runtimeConfig != null ? runtimeConfig.SceneLoadingTimeoutMilliseconds : 30000;
            battleSceneAddress = runtimeConfig != null ? runtimeConfig.BattleSceneAddress : "BattleScene";
            mapAddress = runtimeConfig != null ? runtimeConfig.MapAddress : "MiniBomberDefaultMap";
            maximumPlayers = ruleConfig != null ? ruleConfig.MaxPlayers : 4;
            minimumPlayers = ruleConfig != null ? ruleConfig.MinimumPlayers : 2;
            battleRules = new MiniBomberBattleRules
            {
                TickRate = serverTickRate,
                InputHoldMilliseconds = runtimeConfig != null ? runtimeConfig.InputHoldMilliseconds : 100,
                MovementSpeedMillimetersPerSecond = ruleConfig != null ? ruleConfig.MovementSpeedMillimetersPerSecond : 3500,
                PlayerRadiusMillimeters = ruleConfig != null ? ruleConfig.PlayerRadiusMillimeters : 350,
                BombFuseMilliseconds = ruleConfig != null ? ruleConfig.BombFuseMilliseconds : 2500,
                InitialBombCapacity = ruleConfig != null ? ruleConfig.InitialBombCapacity : 1,
                InitialBombRange = ruleConfig != null ? ruleConfig.InitialBombRange : 2,
                RespawnDelayMilliseconds = ruleConfig != null ? ruleConfig.RespawnDelayMilliseconds : 3000,
                RespawnProtectionMilliseconds = ruleConfig != null ? ruleConfig.RespawnProtectionMilliseconds : 2000,
                KillScore = ruleConfig != null ? ruleConfig.KillScore : 2,
                DeathScore = ruleConfig != null ? ruleConfig.DeathScore : -1
            };
            battleMap = mapDefinition != null ? CopyMap(mapDefinition) : CreateDefaultMap();
        }

        /// <summary>
        /// 复制 Unity 地图资产为服务器纯数据。
        /// </summary>
        /// <param name="definition">地图资产。</param>
        /// <returns>不依赖 Unity 对象生命周期的地图。</returns>
        private static MiniBomberBattleMap CopyMap(BomberMapDefinition definition)
        {
            Vector2Int[] sourceSpawns = definition.CopySpawnCells();
            var spawns = new MiniBomberCell[sourceSpawns.Length];
            for (int index = 0; index < sourceSpawns.Length; index++)
            {
                spawns[index] = new MiniBomberCell(sourceSpawns[index].x, sourceSpawns[index].y);
            }

            return new MiniBomberBattleMap(definition.Width, definition.Height, definition.CellSizeMillimeters, definition.CopyCells(), spawns);
        }

        /// <summary>
        /// 创建无需资产即可运行测试的 17×13 经典炸弹人地图。
        /// </summary>
        /// <returns>包含边界墙、固定柱和木箱的地图。</returns>
        private static MiniBomberBattleMap CreateDefaultMap()
        {
            const int width = 17;
            const int height = 13;
            var cells = new byte[width * height];
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool boundary = x == 0 || z == 0 || x == width - 1 || z == height - 1;
                    bool pillar = (x & 1) == 0 && (z & 1) == 0;
                    bool spawnClear = IsSpawnClearCell(x, z, width, height);
                    MiniBomberCellType type = boundary || pillar
                        ? MiniBomberCellType.Solid
                        : !spawnClear && ((x + (z * 3)) % 4 != 0) ? MiniBomberCellType.Breakable : MiniBomberCellType.Road;
                    cells[(z * width) + x] = (byte)type;
                }
            }

            return new MiniBomberBattleMap(width, height, 1000, cells, new[]
            {
                new MiniBomberCell(1, 1),
                new MiniBomberCell(15, 1),
                new MiniBomberCell(1, 11),
                new MiniBomberCell(15, 11)
            });
        }

        /// <summary>
        /// 判断格子是否属于四角出生点及其相邻安全道路。
        /// </summary>
        /// <param name="x">横向格坐标。</param>
        /// <param name="z">纵向格坐标。</param>
        /// <param name="width">地图宽度。</param>
        /// <param name="height">地图高度。</param>
        /// <returns>属于任一出生安全区时返回 true。</returns>
        private static bool IsSpawnClearCell(int x, int z, int width, int height)
        {
            int right = width - 2;
            int top = height - 2;
            return (x <= 2 && z <= 2) || (x >= right - 1 && z <= 2) || (x <= 2 && z >= top - 1) || (x >= right - 1 && z >= top - 1);
        }

        /// <summary>
        /// 将认证服务器签发的玩家身份绑定到当前网络连接，并替换旧连接。
        /// </summary>
        /// <param name="networkSessionId">新网络会话标识。</param>
        /// <param name="playerId">认证服务器签发的玩家标识。</param>
        /// <param name="playerName">认证服务器返回的玩家显示名。</param>
        /// <param name="sessionToken">新恢复令牌。</param>
        /// <returns>绑定后的玩家会话。</returns>
        private MiniBomberServerPlayerSession BindAuthenticatedSession(string networkSessionId, long playerId, string playerName, string sessionToken)
        {
            if (!playerById.TryGetValue(playerId, out MiniBomberServerPlayerSession player))
            {
                player = new MiniBomberServerPlayerSession
                {
                    PlayerId = playerId,
                    PlayerName = string.IsNullOrWhiteSpace(playerName) ? $"Player{playerId}" : playerName
                };
                playerById.Add(player.PlayerId, player);
            }

            string oldNetworkSessionId = player.NetworkSessionId;
            if (!string.IsNullOrEmpty(oldNetworkSessionId))
            {
                playerByNetworkSession.Remove(oldNetworkSessionId);
            }

            player.SessionToken = sessionToken;
            RebindNetworkSession(player, networkSessionId);
            if (!string.IsNullOrEmpty(oldNetworkSessionId) && !string.Equals(oldNetworkSessionId, networkSessionId, StringComparison.Ordinal))
            {
                network.TrySend(oldNetworkSessionId, new MiniBomberDisconnectNotice
                {
                    Code = MiniBomberErrorCode.SessionExpired,
                    Reason = "账号已在另一台设备登录",
                    MayResume = false
                });
                network.DisconnectSession(oldNetworkSessionId);
            }

            return player;
        }

        /// <summary>
        /// 把玩家会话重新绑定到新的网络连接。
        /// </summary>
        /// <param name="player">玩家会话。</param>
        /// <param name="networkSessionId">新网络会话标识。</param>
        private void RebindNetworkSession(MiniBomberServerPlayerSession player, string networkSessionId)
        {
            if (!string.IsNullOrEmpty(player.NetworkSessionId))
            {
                playerByNetworkSession.Remove(player.NetworkSessionId);
            }

            player.NetworkSessionId = networkSessionId;
            player.IsOnline = true;
            player.ReconnectDeadline = 0d;
            playerByNetworkSession[networkSessionId] = player;
        }

        /// <summary>
        /// 处理底层网络连接关闭并进入重连宽限期。
        /// </summary>
        /// <param name="networkSessionId">关闭的网络会话标识。</param>
        private void HandleServerSessionClosed(string networkSessionId)
        {
            if (!playerByNetworkSession.TryGetValue(networkSessionId, out MiniBomberServerPlayerSession player))
            {
                return;
            }

            playerByNetworkSession.Remove(networkSessionId);
            player.NetworkSessionId = string.Empty;
            player.IsOnline = false;
            player.ReconnectDeadline = Global.Time.UnscaledTime + (reconnectGraceMilliseconds / 1000d);
            if (player.RoomId > 0 && rooms.TryGetValue(player.RoomId, out MiniBomberServerRoom room))
            {
                SetRoomOnline(room, player.PlayerId, false);
                BroadcastRoom(room);
            }

            if (player.MatchId > 0 && matches.TryGetValue(player.MatchId, out MiniBomberServerMatch match))
            {
                roomWorkers.TrySetPlayerOnline(match.MatchId, player.PlayerId, false);
            }
        }

        /// <summary>
        /// 清理超过重连宽限的玩家，并在必要时转移房主。
        /// </summary>
        /// <param name="now">当前单调时间。</param>
        private void UpdateDisconnectedPlayers(double now)
        {
            cleanupPlayerIds.Clear();
            foreach (KeyValuePair<long, MiniBomberServerPlayerSession> pair in playerById)
            {
                if (!pair.Value.IsOnline && pair.Value.ReconnectDeadline > 0d && now >= pair.Value.ReconnectDeadline)
                {
                    cleanupPlayerIds.Add(pair.Key);
                }
            }

            for (int index = 0; index < cleanupPlayerIds.Count; index++)
            {
                long playerId = cleanupPlayerIds[index];
                MiniBomberServerPlayerSession player = playerById[playerId];
                if (player.RoomId > 0 && rooms.TryGetValue(player.RoomId, out MiniBomberServerRoom room) && room.State != MiniBomberRoomState.MiniBomberRoomBattle)
                {
                    RemovePlayerFromRoom(player, room);
                }

                playerById.Remove(playerId);
            }
        }

        /// <summary>
        /// 处理战斗加载超时并把房间恢复为等待状态。
        /// </summary>
        /// <param name="now">当前单调时间。</param>
        private void UpdateLoadingTimeouts(double now)
        {
            foreach (KeyValuePair<long, MiniBomberServerRoom> pair in rooms)
            {
                MiniBomberServerRoom room = pair.Value;
                if (room.State != MiniBomberRoomState.MiniBomberRoomLoading || now < room.LoadingDeadline)
                {
                    continue;
                }

                room.State = MiniBomberRoomState.MiniBomberRoomWaiting;
                room.MatchId = 0;
                room.LoadingDeadline = 0d;
                room.ResetReadiness();
                room.Revision++;
                for (int memberIndex = 0; memberIndex < room.Members.Count; memberIndex++)
                {
                    if (playerById.TryGetValue(room.Members[memberIndex].PlayerId, out MiniBomberServerPlayerSession player))
                    {
                        player.MatchId = 0;
                    }
                }

                BroadcastRoom(room);
                lobbyRevision++;
                BroadcastLobbyChanged();
            }
        }

        /// <summary>
        /// 推进全部已经结束倒计时的权威比赛。
        /// </summary>
        /// <param name="now">当前单调时间。</param>
        private void TickMatches(double now)
        {
            cleanupMatchIds.Clear();
            foreach (KeyValuePair<long, MiniBomberServerMatch> pair in matches)
            {
                MiniBomberServerMatch match = pair.Value;
                if (!match.IsStarted && now >= match.StartTime)
                {
                    if (roomWorkers.TryStartMatch(match.MatchId))
                    {
                        match.IsStarted = true;
                    }
                }

                if (match.ResultBroadcasted && now >= match.ReturnToRoomTime)
                {
                    cleanupMatchIds.Add(match.MatchId);
                }
            }

            roomWorkers.TryTickAll();

            for (int index = 0; index < cleanupMatchIds.Count; index++)
            {
                ReturnMatchToRoom(cleanupMatchIds[index]);
            }
        }

        /// <summary>
        /// 在 Unity 主线程抽取 Worker 结果，并通过既有网络安全发送边界投递。
        /// </summary>
        private void DrainRoomWorkerOutputs()
        {
            while (roomWorkers != null && roomWorkers.TryDequeueOutput(out MiniBomberRoomWorkerOutput output))
            {
                if (!matches.TryGetValue(output.MatchId, out MiniBomberServerMatch match) || !rooms.TryGetValue(output.RoomId, out MiniBomberServerRoom room))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(output.TargetNetworkSessionId))
                {
                    if (output.Keyframe != null)
                    {
                        network.TrySend(output.TargetNetworkSessionId, output.Keyframe);
                    }

                    continue;
                }

                if (output.Events != null)
                {
                    BroadcastToRoom(room, output.Events);
                }

                if (output.Delta != null)
                {
                    BroadcastToRoom(room, output.Delta);
                }

                if (output.Keyframe != null)
                {
                    BroadcastToRoom(room, output.Keyframe);
                }

                if (output.Results != null && !match.ResultBroadcasted)
                {
                    BroadcastMatchResult(match, output.Results);
                }
            }
        }

        /// <summary>
        /// 创建权威模拟并向房间内玩家广播三秒倒计时。
        /// </summary>
        /// <param name="room">已经全员加载完成的房间。</param>
        private void BeginBattle(MiniBomberServerRoom room)
        {
            var participants = new List<MiniBomberBattleParticipant>(room.Members.Count);
            for (int index = 0; index < room.Members.Count; index++)
            {
                MiniBomberServerRoomMember member = room.Members[index];
                participants.Add(new MiniBomberBattleParticipant(member.PlayerId, member.PlayerName));
            }

            double startTime = Global.Time.UnscaledTime + (MatchCountdownMilliseconds / 1000d);
            if (!roomWorkers.TryCreateMatch(room.RoomId, room.MatchId, room.DurationSeconds, battleMap, battleRules, participants))
            {
                LogSwitch.Error($"MiniBomber 无法把比赛 {room.MatchId} 投递到 RoomWorker，房间恢复等待状态。");
                room.State = MiniBomberRoomState.MiniBomberRoomWaiting;
                room.MatchId = 0;
                room.ResetReadiness();
                room.Revision++;
                BroadcastRoom(room);
                return;
            }

            roomWorkers.TryGetAssignedWorker(room.RoomId, out int workerIndex);
            var match = new MiniBomberServerMatch
            {
                MatchId = room.MatchId,
                RoomId = room.RoomId,
                StartTime = startTime,
                WorkerIndex = workerIndex
            };
            matches.Add(match.MatchId, match);
            room.State = MiniBomberRoomState.MiniBomberRoomBattle;
            room.Revision++;
            BroadcastRoom(room);
            var notice = new MiniBomberMatchCountdownNotice
            {
                MatchId = room.MatchId,
                ServerStartTimestampMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + MatchCountdownMilliseconds,
                CountdownMilliseconds = MatchCountdownMilliseconds
            };
            BroadcastToRoom(room, notice);
            roomWorkers.TryRequestKeyframe(match.MatchId, null);
        }

        /// <summary>
        /// 广播服务器唯一比赛成绩并把房间置为结果状态。
        /// </summary>
        /// <param name="match">已经结束的比赛。</param>
        private void BroadcastMatchResult(MiniBomberServerMatch match, IReadOnlyList<MiniBomberMatchResult> results)
        {
            if (match.ResultBroadcasted || !rooms.TryGetValue(match.RoomId, out MiniBomberServerRoom room))
            {
                return;
            }

            match.ResultBroadcasted = true;
            match.ReturnToRoomTime = Global.Time.UnscaledTime + (ResultDisplayMilliseconds / 1000d);
            room.State = MiniBomberRoomState.MiniBomberRoomResult;
            room.Revision++;
            var notice = new MiniBomberMatchResultNotice
            {
                RoomId = room.RoomId,
                MatchId = match.MatchId,
                ReturnToRoomMilliseconds = ResultDisplayMilliseconds
            };
            for (int index = 0; index < results.Count; index++)
            {
                MiniBomberMatchResult result = results[index];
                notice.Results.Add(new MiniBomberMatchResultEntryDto
                {
                    Rank = result.Rank,
                    PlayerId = result.PlayerId,
                    PlayerName = result.PlayerName,
                    Score = result.Score,
                    Kills = result.Kills,
                    Deaths = result.Deaths,
                    IsOnline = result.IsOnline
                });
            }

            BroadcastToRoom(room, notice);
            BroadcastRoom(room);
        }

        /// <summary>
        /// 成绩展示结束后清理比赛并将现有成员恢复到等待房间。
        /// </summary>
        /// <param name="matchId">待清理比赛身份。</param>
        private void ReturnMatchToRoom(long matchId)
        {
            if (!matches.TryGetValue(matchId, out MiniBomberServerMatch match))
            {
                return;
            }

            matches.Remove(matchId);
            roomWorkers.TryRemoveMatch(matchId);
            if (!rooms.TryGetValue(match.RoomId, out MiniBomberServerRoom room))
            {
                return;
            }

            room.State = MiniBomberRoomState.MiniBomberRoomWaiting;
            room.MatchId = 0;
            room.ResetReadiness();
            room.Revision++;
            for (int index = 0; index < room.Members.Count; index++)
            {
                MiniBomberServerRoomMember member = room.Members[index];
                member.Score = 0;
                if (playerById.TryGetValue(member.PlayerId, out MiniBomberServerPlayerSession player))
                {
                    player.MatchId = 0;
                }
            }

            BroadcastRoom(room);
            lobbyRevision++;
            BroadcastLobbyChanged();
        }

        /// <summary>
        /// 广播战斗场景加载参数。
        /// </summary>
        /// <param name="room">进入加载阶段的房间。</param>
        private void BroadcastMatchPrepare(MiniBomberServerRoom room)
        {
            BroadcastToRoom(room, new MiniBomberMatchPrepareNotice
            {
                RoomId = room.RoomId,
                MatchId = room.MatchId,
                BattleSceneAddress = battleSceneAddress,
                MapAddress = mapAddress,
                DurationSeconds = room.DurationSeconds,
                RandomSeed = unchecked((int)room.MatchId),
                LoadingTimeoutMilliseconds = loadingTimeoutMilliseconds
            });
        }

        /// <summary>
        /// 广播房间权威快照。
        /// </summary>
        /// <param name="room">目标房间。</param>
        private void BroadcastRoom(MiniBomberServerRoom room)
        {
            BroadcastToRoom(room, new MiniBomberRoomSnapshotNotice { Room = CreateRoomSnapshot(room) });
        }

        /// <summary>
        /// 通知全部在线玩家大厅列表修订号已变化。
        /// </summary>
        private void BroadcastLobbyChanged()
        {
            var notice = new MiniBomberLobbyChangedNotice { Revision = lobbyRevision };
            foreach (KeyValuePair<string, MiniBomberServerPlayerSession> pair in playerByNetworkSession)
            {
                network.TrySend(pair.Key, notice);
            }
        }

        /// <summary>
        /// 向房间内所有在线成员发送普通消息。
        /// </summary>
        /// <typeparam name="TMessage">协议消息类型。</typeparam>
        /// <param name="room">目标房间。</param>
        /// <param name="message">待发送消息。</param>
        private void BroadcastToRoom<TMessage>(MiniBomberServerRoom room, TMessage message) where TMessage : INormalMessage
        {
            for (int index = 0; index < room.Members.Count; index++)
            {
                if (playerById.TryGetValue(room.Members[index].PlayerId, out MiniBomberServerPlayerSession player) && player.IsOnline)
                {
                    network.TrySend(player.NetworkSessionId, message);
                }
            }
        }

        /// <summary>
        /// 从权威房间创建完整 DTO。
        /// </summary>
        /// <param name="room">权威房间。</param>
        /// <returns>可发送给客户端的房间快照。</returns>
        private static MiniBomberRoomSnapshotDto CreateRoomSnapshot(MiniBomberServerRoom room)
        {
            var snapshot = new MiniBomberRoomSnapshotDto
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                OwnerPlayerId = room.OwnerPlayerId,
                DurationSeconds = room.DurationSeconds,
                State = room.State,
                Revision = room.Revision,
                MatchId = room.MatchId
            };
            for (int index = 0; index < room.Members.Count; index++)
            {
                MiniBomberServerRoomMember member = room.Members[index];
                snapshot.Members.Add(new MiniBomberRoomMemberDto
                {
                    PlayerId = member.PlayerId,
                    PlayerName = member.PlayerName,
                    IsOwner = member.PlayerId == room.OwnerPlayerId,
                    IsReady = member.IsReady,
                    IsOnline = member.IsOnline,
                    Score = member.Score
                });
            }

            return snapshot;
        }

        /// <summary>
        /// 从权威房间创建大厅摘要 DTO。
        /// </summary>
        /// <param name="room">权威房间。</param>
        /// <returns>大厅列表项。</returns>
        private MiniBomberRoomSummaryDto CreateRoomSummary(MiniBomberServerRoom room)
        {
            string ownerName = string.Empty;
            if (room.TryGetMember(room.OwnerPlayerId, out MiniBomberServerRoomMember owner))
            {
                ownerName = owner.PlayerName;
            }

            return new MiniBomberRoomSummaryDto
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                OwnerName = ownerName,
                PlayerCount = room.Members.Count,
                MaxPlayerCount = maximumPlayers,
                DurationSeconds = room.DurationSeconds,
                State = room.State,
                Revision = room.Revision
            };
        }

        /// <summary>
        /// 将在线玩家转为新房间成员。
        /// </summary>
        /// <param name="player">玩家会话。</param>
        /// <returns>新房间成员。</returns>
        private static MiniBomberServerRoomMember CreateRoomMember(MiniBomberServerPlayerSession player)
        {
            return new MiniBomberServerRoomMember
            {
                PlayerId = player.PlayerId,
                PlayerName = player.PlayerName,
                IsOnline = true
            };
        }

        /// <summary>
        /// 移除房间成员并处理空房间和房主转移。
        /// </summary>
        /// <param name="player">待移除玩家。</param>
        /// <param name="room">所在房间。</param>
        private void RemovePlayerFromRoom(MiniBomberServerPlayerSession player, MiniBomberServerRoom room)
        {
            if (!room.RemoveMember(player.PlayerId))
            {
                return;
            }

            player.RoomId = 0;
            player.MatchId = 0;
            if (room.Members.Count == 0)
            {
                rooms.Remove(room.RoomId);
            }
            else
            {
                if (room.OwnerPlayerId == player.PlayerId)
                {
                    room.OwnerPlayerId = room.Members[0].PlayerId;
                    room.Revision++;
                }

                BroadcastRoom(room);
            }

            lobbyRevision++;
            BroadcastLobbyChanged();
        }

        /// <summary>
        /// 修改房间成员在线状态。
        /// </summary>
        /// <param name="room">目标房间。</param>
        /// <param name="playerId">玩家身份。</param>
        /// <param name="online">新的在线状态。</param>
        private static void SetRoomOnline(MiniBomberServerRoom room, long playerId, bool online)
        {
            if (room.TryGetMember(playerId, out MiniBomberServerRoomMember member))
            {
                member.IsOnline = online;
                room.Revision++;
            }
        }

        /// <summary>
        /// 判断房间是否满足开局条件。
        /// </summary>
        /// <param name="room">目标房间。</param>
        /// <returns>人数足够且全员在线准备时返回 true。</returns>
        private bool CanStart(MiniBomberServerRoom room)
        {
            if (room.State != MiniBomberRoomState.MiniBomberRoomWaiting || room.Members.Count < minimumPlayers)
            {
                return false;
            }

            for (int index = 0; index < room.Members.Count; index++)
            {
                if (!room.Members[index].IsOnline || !room.Members[index].IsReady)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 判断房间成员是否全部完成场景加载。
        /// </summary>
        /// <param name="room">目标房间。</param>
        /// <returns>全员在线且就绪时返回 true。</returns>
        private static bool AreAllScenesReady(MiniBomberServerRoom room)
        {
            for (int index = 0; index < room.Members.Count; index++)
            {
                if (!room.Members[index].IsOnline || !room.Members[index].IsSceneReady)
                {
                    return false;
                }
            }

            return room.Members.Count > 0;
        }

        /// <summary>
        /// 校验网络会话与玩家身份绑定一致。
        /// </summary>
        /// <param name="session">请求网络会话。</param>
        /// <param name="playerId">请求声明的玩家身份。</param>
        /// <param name="player">通过校验的玩家会话。</param>
        /// <returns>身份与当前连接一致时返回 true。</returns>
        private bool TryAuthorize(NetworkSession session, long playerId, out MiniBomberServerPlayerSession player)
        {
            player = null;
            return session != null && playerByNetworkSession.TryGetValue(session.SessionId, out player) && player.PlayerId == playerId && player.IsOnline;
        }

        /// <summary>
        /// 校验身份并在失败时写入响应错误。
        /// </summary>
        /// <typeparam name="TResponse">RPC 响应类型。</typeparam>
        /// <param name="session">请求网络会话。</param>
        /// <param name="playerId">玩家身份。</param>
        /// <param name="response">待填写响应。</param>
        /// <returns>身份有效时返回 true。</returns>
        private bool TryAuthorize<TResponse>(NetworkSession session, long playerId, TResponse response) where TResponse : IRpcResponse
        {
            if (TryAuthorize(session, playerId, out _))
            {
                return true;
            }

            SetError(response, MiniBomberErrorCode.NotAuthenticated, "请先登录");
            return false;
        }

        /// <summary>
        /// 校验身份、返回玩家会话并在失败时写入响应错误。
        /// </summary>
        /// <typeparam name="TResponse">RPC 响应类型。</typeparam>
        /// <param name="session">请求网络会话。</param>
        /// <param name="playerId">玩家身份。</param>
        /// <param name="response">待填写响应。</param>
        /// <param name="player">通过校验的玩家会话。</param>
        /// <returns>身份有效时返回 true。</returns>
        private bool TryAuthorize<TResponse>(NetworkSession session, long playerId, TResponse response, out MiniBomberServerPlayerSession player) where TResponse : IRpcResponse
        {
            if (TryAuthorize(session, playerId, out player))
            {
                return true;
            }

            SetError(response, MiniBomberErrorCode.NotAuthenticated, "请先登录");
            return false;
        }

        /// <summary>
        /// 验证客户端协议和规则版本。
        /// </summary>
        /// <param name="version">客户端版本握手。</param>
        /// <param name="message">失败原因。</param>
        /// <returns>协议和规则版本完全一致时返回 true。</returns>
        private static bool ValidateVersion(MiniBomberVersionInfo version, out string message)
        {
            if (version == null || version.ProtocolVersion != MiniBomberConstants.ProtocolVersion || version.RuleVersion != MiniBomberConstants.RuleVersion)
            {
                message = $"版本不匹配，服务器协议/规则为 {MiniBomberConstants.ProtocolVersion}/{MiniBomberConstants.RuleVersion}";
                return false;
            }

            message = string.Empty;
            return true;
        }

        /// <summary>
        /// 创建玩家资料 DTO。
        /// </summary>
        /// <param name="player">玩家会话。</param>
        /// <returns>网络资料对象。</returns>
        private static MiniBomberPlayerProfileDto CreateProfile(MiniBomberServerPlayerSession player)
        {
            return new MiniBomberPlayerProfileDto { PlayerId = player.PlayerId, PlayerName = player.PlayerName };
        }

        /// <summary>
        /// 根据玩家当前房间和比赛状态选择客户端恢复目的地。
        /// </summary>
        /// <param name="player">玩家会话。</param>
        /// <returns>客户端目标状态。</returns>
        private MiniBomberClientDestination ResolveDestination(MiniBomberServerPlayerSession player)
        {
            if (player.MatchId > 0 && matches.ContainsKey(player.MatchId))
            {
                return MiniBomberClientDestination.MiniBomberDestinationBattle;
            }

            if (player.RoomId > 0)
            {
                return MiniBomberClientDestination.MiniBomberDestinationRoom;
            }

            return MiniBomberClientDestination.MiniBomberDestinationLobby;
        }

        /// <summary>
        /// 计算当前在线认证玩家数量。
        /// </summary>
        /// <returns>在线人数。</returns>
        private int CountOnlinePlayers()
        {
            int count = 0;
            foreach (KeyValuePair<long, MiniBomberServerPlayerSession> pair in playerById)
            {
                if (pair.Value.IsOnline)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 判断房间名称是否符合 Demo 约束。
        /// </summary>
        /// <param name="value">房间名。</param>
        /// <returns>长度为一到二十四个字符时返回 true。</returns>
        private static bool IsRoomNameValid(string value)
        {
            string trimmed = value?.Trim();
            return !string.IsNullOrEmpty(trimmed) && trimmed.Length <= 24;
        }

        /// <summary>
        /// 判断时长是否为首版固定选项。
        /// </summary>
        /// <param name="seconds">时长秒数。</param>
        /// <returns>二、五或十分钟时返回 true。</returns>
        private static bool IsDurationAllowed(int seconds)
        {
            return seconds == 120 || seconds == 300 || seconds == 600;
        }

        /// <summary>
        /// 固定时间比较两个 Base64 会话令牌。
        /// </summary>
        /// <param name="expected">服务器令牌。</param>
        /// <param name="actual">客户端令牌。</param>
        /// <returns>字节完全一致时返回 true。</returns>
        private static bool FixedTimeTokenEquals(string expected, string actual)
        {
            if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(actual))
            {
                return false;
            }

            byte[] left;
            byte[] right;
            try
            {
                left = Convert.FromBase64String(expected);
                right = Convert.FromBase64String(actual);
            }
            catch (FormatException)
            {
                return false;
            }

            if (left.Length != right.Length)
            {
                return false;
            }

            int difference = 0;
            for (int index = 0; index < left.Length; index++)
            {
                difference |= left[index] ^ right[index];
            }

            return difference == 0;
        }

        /// <summary>
        /// 将模拟事件枚举转换为协议枚举。
        /// </summary>
        /// <param name="type">模拟事件类型。</param>
        /// <returns>协议事件类型。</returns>
        private static MiniBomberBattleEventType ConvertEventType(MiniBomberSimulationEventType type)
        {
            switch (type)
            {
                case MiniBomberSimulationEventType.BombPlaced:
                    return MiniBomberBattleEventType.MiniBomberEventBombPlaced;
                case MiniBomberSimulationEventType.ExplosionStarted:
                    return MiniBomberBattleEventType.MiniBomberEventExplosionStarted;
                case MiniBomberSimulationEventType.BlockDestroyed:
                    return MiniBomberBattleEventType.MiniBomberEventBlockDestroyed;
                case MiniBomberSimulationEventType.PlayerKilled:
                    return MiniBomberBattleEventType.MiniBomberEventPlayerKilled;
                case MiniBomberSimulationEventType.PlayerRespawned:
                    return MiniBomberBattleEventType.MiniBomberEventPlayerRespawned;
                default:
                    return MiniBomberBattleEventType.MiniBomberEventNone;
            }
        }

        /// <summary>
        /// 按玩家身份查找权威显示名。
        /// </summary>
        /// <param name="players">权威玩家列表。</param>
        /// <param name="playerId">玩家身份。</param>
        /// <returns>找到的显示名；身份为零或不存在时返回空。</returns>
        private static string FindPlayerName(IReadOnlyList<MiniBomberPlayerState> players, long playerId)
        {
            for (int index = 0; index < players.Count; index++)
            {
                if (players[index].PlayerId == playerId)
                {
                    return players[index].PlayerName;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 给任意 RPC 响应写入错误码和用户消息。
        /// </summary>
        /// <typeparam name="TResponse">RPC 响应类型。</typeparam>
        /// <param name="response">待填写响应。</param>
        /// <param name="code">业务错误码。</param>
        /// <param name="message">用户消息。</param>
        private static void SetError<TResponse>(TResponse response, int code, string message) where TResponse : IRpcResponse
        {
            response.Code = code;
            response.Msg = message ?? string.Empty;
        }

        #endregion
    }
}
