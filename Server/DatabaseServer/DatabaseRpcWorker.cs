using System.Net;
using System.Net.Sockets;
using System.Data.Common;
using System.Text.Json;
using DatabaseServer.Data;
using DatabaseServer.Models;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniCore.Protocol.Generated;
using MiniCore.Server.Rpc;

namespace DatabaseServer;

/// <summary>
/// 监听 MiniCore Inner TCP、注册 Coordinator 并有界并发处理数据库 RPC。
/// </summary>
public sealed class DatabaseRpcWorker : BackgroundService
{
    #region Constant 常量

    private const int CoordinatorRpcTimeoutSeconds = 3;
    private static readonly int[] ReconnectDelaySeconds = { 1, 2, 4, 8, 15 };

    #endregion

    #region Private 私有成员

    private readonly IDbContextFactory<GameDbContext> dbContextFactory;
    private readonly DatabaseServerOptions options;
    private readonly ILogger<DatabaseRpcWorker> logger;
    private readonly OpcodeManifest opcodes;
    private readonly SemaphoreSlim concurrency;
    private TcpListener? listener;

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 创建 DatabaseServer Worker。
    /// </summary>
    /// <param name="dbContextFactory">每次 RPC 创建独立上下文的工厂。</param>
    /// <param name="options">监听、注册和并发配置。</param>
    /// <param name="logger">结构化日志记录器。</param>
    public DatabaseRpcWorker(
        IDbContextFactory<GameDbContext> dbContextFactory,
        IOptions<DatabaseServerOptions> options,
        ILogger<DatabaseRpcWorker> logger)
    {
        this.dbContextFactory = dbContextFactory;
        this.options = options.Value;
        this.logger = logger;
        opcodes = OpcodeManifest.Load(Path.Combine(AppContext.BaseDirectory, "OpcodeManifest.json"));
        concurrency = new SemaphoreSlim(Math.Max(1, this.options.MaximumConcurrency));
    }

    #endregion

    #region Override 重写实现

    /// <summary>
    /// 启动 RPC 监听，并持续维护 Coordinator 注册、Ready 状态与租约。
    /// </summary>
    /// <param name="stoppingToken">宿主停止令牌。</param>
    /// <returns>Worker 生命周期任务。</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(options.ReadinessFilePath) || !Path.IsPathRooted(options.ReadinessFilePath))
        {
            throw new InvalidOperationException("DatabaseServer:ReadinessFilePath 必须是服务器本机绝对路径。");
        }

        DeleteReadinessFile();
        IPAddress address = string.Equals(options.ListenHost, "0.0.0.0", StringComparison.Ordinal)
            ? IPAddress.Any
            : IPAddress.Parse(options.ListenHost);
        listener = new TcpListener(address, options.ListenPort);
        listener.Start();
        Task acceptLoop = AcceptLoopAsync(stoppingToken);
        try
        {
            await MaintainCoordinatorRegistrationAsync(stoppingToken);
        }
        finally
        {
            DeleteReadinessFile();
            listener.Stop();
            try
            {
                await acceptLoop;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (ObjectDisposedException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }
    }

    /// <summary>
    /// 停止监听并释放并发门闩。
    /// </summary>
    public override void Dispose()
    {
        listener?.Stop();
        concurrency.Dispose();
        base.Dispose();
    }

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 接受多个 DS 到 DatabaseServer 的长连接。
    /// </summary>
    /// <param name="cancellationToken">宿主停止令牌。</param>
    /// <returns>监听生命周期任务。</returns>
    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client = await listener!.AcceptTcpClientAsync(cancellationToken);
            _ = HandleConnectionSafelyAsync(client, cancellationToken);
        }
    }

    /// <summary>
    /// 隔离单个 GameCluster 连接异常，避免影响 DatabaseServer 宿主和其他连接。
    /// </summary>
    /// <param name="client">已接受的 TCP 客户端。</param>
    /// <param name="cancellationToken">宿主停止令牌。</param>
    /// <returns>连接生命周期任务。</returns>
    private async Task HandleConnectionSafelyAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            await HandleConnectionAsync(client, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException)
        {
            logger.LogWarning(exception, "一个 DatabaseServer 业务连接已经断开，不影响其他连接继续运行。");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "DatabaseServer 业务连接处理失败，当前连接将关闭，其他连接继续运行。");
        }
    }

    /// <summary>
    /// 持续连接 Coordinator；断线后按上限十五秒的退避重新注册并恢复 Ready。
    /// </summary>
    /// <param name="cancellationToken">宿主停止令牌。</param>
    /// <returns>控制面维护任务。</returns>
    private async Task MaintainCoordinatorRegistrationAsync(CancellationToken cancellationToken)
    {
        int retryIndex = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            bool reachedReady = false;
            try
            {
                await using var coordinator = new MiniCoreRpcClient(opcodes);
                await coordinator.ConnectAsync(options.CoordinatorHost, options.CoordinatorPort, cancellationToken);
                long revision = await RegisterAndReportReadyAsync(coordinator, cancellationToken);
                reachedReady = true;
                retryIndex = 0;
                logger.LogInformation(
                    "DatabaseServer {InstanceId} 已就绪，监听 {Host}:{Port}，最大并发 {Concurrency}。",
                    options.InstanceId,
                    options.ListenHost,
                    options.ListenPort,
                    options.MaximumConcurrency);

                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    ServerHeartbeatResponse heartbeat = await coordinator.CallAsync(
                        new ServerHeartbeatRequest
                        {
                            InstanceId = options.InstanceId,
                            KnownDirectoryRevision = revision
                        },
                        ServerHeartbeatResponse.Parser,
                        cancellationToken,
                        CoordinatorRpcTimeoutSeconds);
                    if (heartbeat.Code == 404)
                    {
                        throw new CoordinatorRegistrationLostException(heartbeat.Msg);
                    }

                    EnsureSuccess(heartbeat.Code, heartbeat.Msg, "续约 Coordinator");
                    revision = heartbeat.DirectoryRevision;
                    await WriteReadinessFileAsync(revision, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception) when (IsTransientCoordinatorException(exception))
            {
                DeleteReadinessFile();
                if (reachedReady)
                {
                    retryIndex = 0;
                }

                int delaySeconds = ReconnectDelaySeconds[Math.Min(retryIndex, ReconnectDelaySeconds.Length - 1)];
                retryIndex = Math.Min(retryIndex + 1, ReconnectDelaySeconds.Length - 1);
                int jitterMilliseconds = Random.Shared.Next(0, 251);
                logger.LogWarning(
                    exception,
                    "DatabaseServer 与 Coordinator 的控制连接已失效，{DelaySeconds} 秒后重新注册。",
                    delaySeconds);
                await Task.Delay(TimeSpan.FromMilliseconds(delaySeconds * 1000 + jitterMilliseconds), cancellationToken);
            }
        }
    }

    /// <summary>
    /// 在新连接上注册 DatabaseServer 并报告 Ready。
    /// </summary>
    /// <param name="coordinator">已经连接的 Coordinator RPC 客户端。</param>
    /// <param name="cancellationToken">宿主停止令牌。</param>
    /// <returns>Coordinator 当前目录修订号。</returns>
    private async Task<long> RegisterAndReportReadyAsync(
        MiniCoreRpcClient coordinator,
        CancellationToken cancellationToken)
    {
        await VerifyDatabaseConnectionAsync(cancellationToken);
        await VerifyBusinessRpcAsync(cancellationToken);
        RegisterServerResponse registration = await coordinator.CallAsync(
            new RegisterServerRequest
            {
                InstanceId = options.InstanceId,
                InnerHost = options.AdvertisedHost,
                InnerPort = options.ListenPort,
                ServiceId = 1UL << 63,
                ProtocolVersion = "1"
            },
            RegisterServerResponse.Parser,
            cancellationToken,
            CoordinatorRpcTimeoutSeconds);
        EnsureSuccess(registration.Code, registration.Msg, "注册 Coordinator");

        SetServerStateResponse ready = await coordinator.CallAsync(
            new SetServerStateRequest
            {
                InstanceId = options.InstanceId,
                State = ClusterServiceState.ClusterServiceReady
            },
            SetServerStateResponse.Parser,
            cancellationToken,
            CoordinatorRpcTimeoutSeconds);
        EnsureSuccess(ready.Code, ready.Msg, "报告 Ready");
        await WriteReadinessFileAsync(ready.DirectoryRevision, cancellationToken);
        return ready.DirectoryRevision;
    }

    /// <summary>
    /// 判断异常是否表示可恢复的 Coordinator 连接或租约故障。
    /// </summary>
    /// <param name="exception">待判断异常。</param>
    /// <returns>可以重新连接时返回 true。</returns>
    private static bool IsTransientCoordinatorException(Exception exception)
    {
        return exception is IOException
            or SocketException
            or TimeoutException
            or DbException
            or CoordinatorRegistrationLostException;
    }

    /// <summary>
    /// 使用独立 DbContext 验证游戏数据库当前可连接。
    /// </summary>
    /// <param name="cancellationToken">宿主停止令牌。</param>
    /// <returns>数据库验证完成任务。</returns>
    private async Task VerifyDatabaseConnectionAsync(CancellationToken cancellationToken)
    {
        await using GameDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (!await dbContext.Database.CanConnectAsync(cancellationToken))
        {
            throw new TimeoutException("游戏数据库当前不可连接。");
        }
    }

    /// <summary>
    /// 通过回环 TCP 完成一次 Ping/Pong，验证业务监听与帧处理循环已经就绪。
    /// </summary>
    /// <param name="cancellationToken">宿主停止令牌。</param>
    /// <returns>RPC 自检完成任务。</returns>
    private async Task VerifyBusinessRpcAsync(CancellationToken cancellationToken)
    {
        string host = string.Equals(options.ListenHost, "0.0.0.0", StringComparison.Ordinal)
            || string.Equals(options.ListenHost, "::", StringComparison.Ordinal)
                ? "127.0.0.1"
                : options.ListenHost;
        using var client = new TcpClient();
        await client.ConnectAsync(host, options.ListenPort, cancellationToken);
        await using NetworkStream stream = client.GetStream();
        const long rpcId = 1;
        await MiniCoreRpcFrameCodec.WriteAsync(stream, 1, rpcId, ReadOnlyMemory<byte>.Empty, cancellationToken);
        MiniCoreRpcFrame frame = await MiniCoreRpcFrameCodec.ReadAsync(stream, cancellationToken)
            ?? throw new EndOfStreamException("DatabaseServer RPC 自检未收到 Pong。");
        if (frame.Opcode != 2 || frame.RpcId != rpcId)
        {
            throw new InvalidDataException("DatabaseServer RPC 自检返回了无效 Pong。");
        }
    }

    /// <summary>
    /// 原子刷新仅供本机部署器读取的深度就绪文件。
    /// </summary>
    /// <param name="directoryRevision">Coordinator 当前目录修订号。</param>
    /// <param name="cancellationToken">宿主停止令牌。</param>
    /// <returns>状态写入完成任务。</returns>
    private async Task WriteReadinessFileAsync(long directoryRevision, CancellationToken cancellationToken)
    {
        string path = options.ReadinessFilePath;
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string temporaryPath = path + ".tmp";
        string json = JsonSerializer.Serialize(new
        {
            instanceId = options.InstanceId,
            databaseReady = true,
            coordinatorRegistered = true,
            rpcReady = true,
            directoryRevision,
            updatedAtUtc = DateTimeOffset.UtcNow
        });
        await File.WriteAllTextAsync(temporaryPath, json, new System.Text.UTF8Encoding(false), cancellationToken);
        File.Move(temporaryPath, path, true);
    }

    /// <summary>
    /// 在启动、失联或停止时删除可能误导部署器的旧就绪文件。
    /// </summary>
    private void DeleteReadinessFile()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(options.ReadinessFilePath) && File.Exists(options.ReadinessFilePath))
            {
                File.Delete(options.ReadinessFilePath);
            }

            string temporaryPath = options.ReadinessFilePath + ".tmp";
            if (!string.IsNullOrWhiteSpace(options.ReadinessFilePath) && File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "清理 DatabaseServer 就绪文件失败，部署器仍会通过时间戳拒绝陈旧状态。");
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.LogWarning(exception, "清理 DatabaseServer 就绪文件权限不足，部署器仍会通过时间戳拒绝陈旧状态。");
        }
    }

    /// <summary>
    /// 按单连接顺序读取 MiniCore 帧，并把数据库操作交给全局有界并发门闩。
    /// </summary>
    /// <param name="client">已接受的 TCP 客户端。</param>
    /// <param name="cancellationToken">宿主停止令牌。</param>
    /// <returns>连接生命周期任务。</returns>
    private async Task HandleConnectionAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        await using (NetworkStream stream = client.GetStream())
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                MiniCoreRpcFrame? frame = await MiniCoreRpcFrameCodec.ReadAsync(stream, cancellationToken);
                if (frame == null)
                {
                    return;
                }

                if (frame.Opcode == 1)
                {
                    await MiniCoreRpcFrameCodec.WriteAsync(stream, 2, frame.RpcId, ReadOnlyMemory<byte>.Empty, cancellationToken);
                    continue;
                }

                if (frame.Opcode == opcodes.Get<LoadPlayerDataRequest>())
                {
                    await DispatchLoadAsync(stream, frame, cancellationToken);
                }
                else if (frame.Opcode == opcodes.Get<SavePlayerDataRequest>())
                {
                    await DispatchSaveAsync(stream, frame, cancellationToken);
                }
                else
                {
                    logger.LogWarning(
                        "DatabaseServer 收到未注册 Opcode {Opcode}，RpcId {RpcId}；当前读取 Opcode {LoadOpcode}，保存 Opcode {SaveOpcode}。",
                        frame.Opcode,
                        frame.RpcId,
                        opcodes.Get<LoadPlayerDataRequest>(),
                        opcodes.Get<SavePlayerDataRequest>());
                }
            }
        }
    }

    /// <summary>
    /// 使用独立只读 DbContext 处理一次玩家数据加载。
    /// </summary>
    /// <param name="stream">当前连接流。</param>
    /// <param name="frame">已经解析的请求帧。</param>
    /// <param name="cancellationToken">宿主停止令牌。</param>
    /// <returns>响应写入完成任务。</returns>
    private async Task DispatchLoadAsync(NetworkStream stream, MiniCoreRpcFrame frame, CancellationToken cancellationToken)
    {
        if (!concurrency.Wait(0))
        {
            await WriteResponseAsync(stream, frame.RpcId, new LoadPlayerDataResponse { Code = 429, Msg = "Overloaded" }, cancellationToken);
            return;
        }

        try
        {
            LoadPlayerDataRequest request = LoadPlayerDataRequest.Parser.ParseFrom(frame.Payload);
            await using GameDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            PlayerDataEntity? entity = await db.Players.AsNoTracking().SingleOrDefaultAsync(item => item.PlayerId == request.PlayerId, cancellationToken);
            var response = new LoadPlayerDataResponse();
            if (entity == null)
            {
                response.Code = 404;
                response.Msg = "PlayerNotFound";
            }
            else
            {
                response.Code = 0;
                response.Player = new PlayerDataDto
                {
                    PlayerId = entity.PlayerId,
                    PlayerName = entity.PlayerName,
                    Revision = entity.Revision,
                    Payload = ByteString.CopyFrom(entity.Payload)
                };
            }

            await WriteResponseAsync(stream, frame.RpcId, response, cancellationToken);
        }
        finally
        {
            concurrency.Release();
        }
    }

    /// <summary>
    /// 使用独立 DbContext 和 Revision 并发令牌处理一次玩家数据保存。
    /// </summary>
    /// <param name="stream">当前连接流。</param>
    /// <param name="frame">已经解析的请求帧。</param>
    /// <param name="cancellationToken">宿主停止令牌。</param>
    /// <returns>响应写入完成任务。</returns>
    private async Task DispatchSaveAsync(NetworkStream stream, MiniCoreRpcFrame frame, CancellationToken cancellationToken)
    {
        if (!concurrency.Wait(0))
        {
            await WriteResponseAsync(stream, frame.RpcId, new SavePlayerDataResponse { Code = 429, Msg = "Overloaded" }, cancellationToken);
            return;
        }

        try
        {
            SavePlayerDataRequest request = SavePlayerDataRequest.Parser.ParseFrom(frame.Payload);
            if (request.Player == null || request.Player.PlayerId <= 0)
            {
                await WriteResponseAsync(stream, frame.RpcId, new SavePlayerDataResponse { Code = 400, Msg = "InvalidPlayer" }, cancellationToken);
                return;
            }

            await using GameDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            PlayerDataEntity? entity = await db.Players.SingleOrDefaultAsync(item => item.PlayerId == request.Player.PlayerId, cancellationToken);
            if (entity == null)
            {
                if (request.ExpectedRevision != 0)
                {
                    await WriteResponseAsync(stream, frame.RpcId, new SavePlayerDataResponse { Code = 409, Msg = "RevisionConflict" }, cancellationToken);
                    return;
                }

                entity = new PlayerDataEntity { PlayerId = request.Player.PlayerId, Revision = 1 };
                db.Players.Add(entity);
            }
            else
            {
                if (entity.Revision != request.ExpectedRevision)
                {
                    await WriteResponseAsync(stream, frame.RpcId, new SavePlayerDataResponse { Code = 409, Msg = "RevisionConflict", Revision = entity.Revision }, cancellationToken);
                    return;
                }

                entity.Revision++;
            }

            entity.PlayerName = request.Player.PlayerName;
            entity.Payload = request.Player.Payload.ToByteArray();
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await WriteResponseAsync(stream, frame.RpcId, new SavePlayerDataResponse { Code = 0, Revision = entity.Revision }, cancellationToken);
            }
            catch (DbUpdateException)
            {
                await WriteResponseAsync(stream, frame.RpcId, new SavePlayerDataResponse { Code = 409, Msg = "RevisionConflict" }, cancellationToken);
            }
        }
        finally
        {
            concurrency.Release();
        }
    }

    /// <summary>
    /// 使用响应类型的稳定 Opcode 和原 RpcId 写回 Protobuf 帧。
    /// </summary>
    /// <typeparam name="TResponse">具体 RPC 响应类型。</typeparam>
    /// <param name="stream">当前连接流。</param>
    /// <param name="rpcId">请求帧关联标识。</param>
    /// <param name="response">待序列化响应。</param>
    /// <param name="cancellationToken">宿主停止令牌。</param>
    /// <returns>帧写入任务。</returns>
    private Task WriteResponseAsync<TResponse>(NetworkStream stream, long rpcId, TResponse response, CancellationToken cancellationToken)
        where TResponse : IMessage<TResponse>
    {
        return MiniCoreRpcFrameCodec.WriteAsync(stream, opcodes.Get<TResponse>(), rpcId, response.ToByteArray(), cancellationToken).AsTask();
    }

    /// <summary>
    /// 将控制面非零结果转换为宿主启动或运行异常。
    /// </summary>
    /// <param name="code">控制面结果码。</param>
    /// <param name="message">控制面错误消息。</param>
    /// <param name="operation">当前操作名称。</param>
    private static void EnsureSuccess(int code, string message, string operation)
    {
        if (code != 0)
        {
            throw new InvalidOperationException($"{operation}失败：{message}（{code}）。");
        }
    }

    /// <summary>
    /// 表示 Coordinator 已遗忘当前实例，需要重新注册而不是终止宿主。
    /// </summary>
    private sealed class CoordinatorRegistrationLostException : Exception
    {
        /// <summary>
        /// 创建注册丢失异常。
        /// </summary>
        /// <param name="message">Coordinator 返回的错误信息。</param>
        public CoordinatorRegistrationLostException(string message)
            : base(string.IsNullOrWhiteSpace(message) ? "Coordinator 中不存在当前 DatabaseServer 注册。" : message)
        {
        }
    }

    #endregion
}
