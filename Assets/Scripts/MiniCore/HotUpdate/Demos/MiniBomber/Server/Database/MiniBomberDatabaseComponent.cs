using System;
using System.IO;
using System.Net.Sockets;
using Google.Protobuf;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 服务端业务用于发现、直连和恢复可选 DatabaseServer 的普通组件。
    /// </summary>
    public sealed class MiniBomberDatabaseComponent : AComponent
    {
        #region Constant 常量

        private const string DatabaseSessionId = "MiniBomber.Database";
        private const int LoadTimeoutSeconds = 5;
        private const int SaveTimeoutSeconds = 8;

        #endregion

        #region Private 私有成员

        private readonly object connectionLock = new object(); // 保证并发业务调用共享同一次数据库连接。
        private INetworkService network; // 共享 MiniCore 网络服务。
        private IServiceDiscoveryService discovery; // Ready DatabaseServer 发现服务。
        private MSharedTask<bool> connectionTask; // 当前正在执行的共享连接任务。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取网络与服务发现依赖，并建立首次 DatabaseServer 连接。
        /// </summary>
        /// <returns>数据库业务组件初始化任务。</returns>
        public async MTask InitializeAsync()
        {
            network = Global.GetService<INetworkService>(this);
            discovery = Global.GetService<IServiceDiscoveryService>(this);
            if (!await EnsureConnectedAsync())
            {
                throw new InvalidOperationException("MiniBomber 需要数据库，但当前没有可连接的 Ready DatabaseServer。");
            }
        }

        /// <summary>
        /// 加载玩家数据；玩家首次进入时以 Revision=0 条件创建默认记录。
        /// </summary>
        /// <param name="playerId">玩家唯一标识。</param>
        /// <param name="playerName">玩家显示名称。</param>
        /// <returns>已加载或已创建的玩家数据。</returns>
        public async MTask<LoadPlayerDataResponse> LoadOrCreateAsync(long playerId, string playerName)
        {
            LoadPlayerDataResponse load = await LoadWithRecoveryAsync(playerId);
            if (load.Code != 404)
            {
                return load;
            }

            var saveRequest = new SavePlayerDataRequest
            {
                ExpectedRevision = 0,
                Player = new PlayerDataDto
                {
                    PlayerId = playerId,
                    PlayerName = playerName ?? string.Empty,
                    Payload = ByteString.Empty
                }
            };

            SavePlayerDataResponse save;
            try
            {
                if (!await EnsureConnectedAsync())
                {
                    return CreateDatabaseUnavailableResponse();
                }

                save = await SaveOnceAsync(saveRequest);
                if (save.Code == -1)
                {
                    return await ResolveUnknownSaveResultAsync(saveRequest);
                }
            }
            catch (Exception exception) when (IsRecoverableDatabaseException(exception))
            {
                return await ResolveUnknownSaveResultAsync(saveRequest);
            }

            return CreateLoadResponseFromSave(saveRequest.Player, save);
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 断开当前 DS 到 DatabaseServer 的业务会话并释放服务依赖。
        /// </summary>
        protected override void OnDispose()
        {
            network?.DisconnectSession(DatabaseSessionId);
            connectionTask = null;
            discovery = null;
            network = null;
            Global.ReleaseAll(this);
            base.OnDispose();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 确保数据库会话可用；多个并发调用共享同一个连接任务。
        /// </summary>
        /// <returns>会话已经连接时返回 true。</returns>
        private async MTask<bool> EnsureConnectedAsync()
        {
            if (IsDatabaseSessionConnected())
            {
                return true;
            }

            MSharedTask<bool> currentTask;
            lock (connectionLock)
            {
                currentTask = connectionTask;
                if (currentTask == null)
                {
                    currentTask = ConnectDatabaseAsync().Share();
                    connectionTask = currentTask;
                }
            }

            try
            {
                return await currentTask;
            }
            finally
            {
                lock (connectionLock)
                {
                    if (ReferenceEquals(connectionTask, currentTask))
                    {
                        connectionTask = null;
                    }
                }
            }
        }

        /// <summary>
        /// 从最新服务目录选择 Ready DatabaseServer 并建立内网直连。
        /// </summary>
        /// <returns>连接成功时返回 true。</returns>
        private async MTask<bool> ConnectDatabaseAsync()
        {
            network.DisconnectSession(DatabaseSessionId);
            if (!discovery.TryResolve(ServiceKind.Database, out DiscoveredServiceEndpoint endpoint))
            {
                return false;
            }

            return await network.ConnectTcpSessionAsync(
                DatabaseSessionId,
                endpoint.InnerHost,
                endpoint.InnerPort,
                TimeSpan.FromSeconds(LoadTimeoutSeconds));
        }

        /// <summary>
        /// 加载玩家数据；连接错误时重新发现服务并只重试一次。
        /// </summary>
        /// <param name="playerId">玩家唯一标识。</param>
        /// <returns>数据库加载响应。</returns>
        private async MTask<LoadPlayerDataResponse> LoadWithRecoveryAsync(long playerId)
        {
            if (!await EnsureConnectedAsync())
            {
                return CreateDatabaseUnavailableResponse();
            }

            try
            {
                LoadPlayerDataResponse first = await LoadOnceAsync(playerId);
                if (first.Code != -1)
                {
                    return first;
                }
            }
            catch (Exception exception) when (IsRecoverableDatabaseException(exception))
            {
            }

            network.DisconnectSession(DatabaseSessionId);
            if (!await EnsureConnectedAsync())
            {
                return CreateDatabaseUnavailableResponse();
            }

            try
            {
                LoadPlayerDataResponse retry = await LoadOnceAsync(playerId);
                return retry.Code == -1 ? CreateDatabaseUnavailableResponse() : retry;
            }
            catch (Exception exception) when (IsRecoverableDatabaseException(exception))
            {
                return CreateDatabaseUnavailableResponse();
            }
        }

        /// <summary>
        /// 执行一次五秒超时的玩家数据加载。
        /// </summary>
        /// <param name="playerId">玩家唯一标识。</param>
        /// <returns>数据库加载响应。</returns>
        private MTask<LoadPlayerDataResponse> LoadOnceAsync(long playerId)
        {
            return network.CallAsync<LoadPlayerDataRequest, LoadPlayerDataResponse>(
                DatabaseSessionId,
                new LoadPlayerDataRequest { PlayerId = playerId },
                LoadTimeoutSeconds);
        }

        /// <summary>
        /// 执行一次八秒超时的玩家数据保存，不在该方法内自动重发。
        /// </summary>
        /// <param name="request">带期望 Revision 的保存请求。</param>
        /// <returns>数据库保存响应。</returns>
        private MTask<SavePlayerDataResponse> SaveOnceAsync(SavePlayerDataRequest request)
        {
            return network.CallAsync<SavePlayerDataRequest, SavePlayerDataResponse>(
                DatabaseSessionId,
                request,
                SaveTimeoutSeconds);
        }

        /// <summary>
        /// 保存结果未知时重连并重新加载；确认不存在后才再次执行 Revision=0 创建。
        /// </summary>
        /// <param name="request">首次创建使用的保存请求。</param>
        /// <returns>核验后的玩家加载响应。</returns>
        private async MTask<LoadPlayerDataResponse> ResolveUnknownSaveResultAsync(SavePlayerDataRequest request)
        {
            network.DisconnectSession(DatabaseSessionId);
            if (!await EnsureConnectedAsync())
            {
                return CreateDatabaseUnavailableResponse();
            }

            LoadPlayerDataResponse verification;
            try
            {
                verification = await LoadOnceAsync(request.Player.PlayerId);
            }
            catch (Exception exception) when (IsRecoverableDatabaseException(exception))
            {
                return CreateDatabaseUnavailableResponse();
            }

            if (verification.Code == 0)
            {
                return verification;
            }

            if (verification.Code != 404)
            {
                return verification.Code == -1 ? CreateDatabaseUnavailableResponse() : verification;
            }

            try
            {
                SavePlayerDataResponse retry = await SaveOnceAsync(request);
                return retry.Code == -1
                    ? CreateDatabaseUnavailableResponse()
                    : CreateLoadResponseFromSave(request.Player, retry);
            }
            catch (Exception exception) when (IsRecoverableDatabaseException(exception))
            {
                return CreateDatabaseUnavailableResponse();
            }
        }

        /// <summary>
        /// 判断当前数据库 Session 是否存在且保持连接。
        /// </summary>
        /// <returns>会话健康时返回 true。</returns>
        private bool IsDatabaseSessionConnected()
        {
            NetworkSession session = network?.GetSession(DatabaseSessionId);
            return session != null && session.IsConnected;
        }

        /// <summary>
        /// 判断异常是否表示可通过重新发现 DatabaseServer 恢复的连接故障。
        /// </summary>
        /// <param name="exception">待判断异常。</param>
        /// <returns>属于超时或断线时返回 true。</returns>
        private static bool IsRecoverableDatabaseException(Exception exception)
        {
            if (exception is TimeoutException or IOException or SocketException or ObjectDisposedException)
            {
                return true;
            }

            if (exception is not InvalidOperationException)
            {
                return false;
            }

            string message = exception.Message ?? string.Empty;
            return message.IndexOf("not connected", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("尚未连接", StringComparison.Ordinal) >= 0
                || message.IndexOf("未连接", StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// 将保存响应转换为业务调用统一使用的加载响应。
        /// </summary>
        /// <param name="player">保存请求中的玩家数据。</param>
        /// <param name="save">数据库保存响应。</param>
        /// <returns>成功时携带玩家数据，失败时保留数据库错误码。</returns>
        private static LoadPlayerDataResponse CreateLoadResponseFromSave(PlayerDataDto player, SavePlayerDataResponse save)
        {
            if (save.Code != 0)
            {
                return new LoadPlayerDataResponse { Code = save.Code, Msg = save.Msg };
            }

            return new LoadPlayerDataResponse
            {
                Code = 0,
                Player = new PlayerDataDto
                {
                    PlayerId = player.PlayerId,
                    PlayerName = player.PlayerName,
                    Revision = save.Revision,
                    Payload = player.Payload
                }
            };
        }

        /// <summary>
        /// 创建不泄漏底层 Session 文本的数据库不可用业务响应。
        /// </summary>
        /// <returns>统一的 503 DatabaseUnavailable 响应。</returns>
        private static LoadPlayerDataResponse CreateDatabaseUnavailableResponse()
        {
            return new LoadPlayerDataResponse { Code = 503, Msg = "DatabaseUnavailable" };
        }

        #endregion
    }
}
