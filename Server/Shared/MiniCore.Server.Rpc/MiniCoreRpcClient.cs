using System.Net.Sockets;
using Google.Protobuf;

namespace MiniCore.Server.Rpc;

/// <summary>
/// 为 .NET 服务提供与 Unity MiniCore 兼容的长连接 RPC 客户端。
/// </summary>
public sealed class MiniCoreRpcClient : IAsyncDisposable
{
    #region Constant 常量

    private const uint PingOpcode = 1;
    private const uint PongOpcode = 2;
    private const int HeartbeatIntervalSeconds = 2;
    private const int HeartbeatTimeoutSeconds = 10;

    #endregion

    #region Private 私有成员

    private readonly OpcodeManifest opcodes;
    private readonly SemaphoreSlim writeGate = new(1, 1);
    private readonly object pendingLock = new();
    private readonly Dictionary<long, PendingRpc> pendingRpcs = new();
    private TcpClient? client;
    private NetworkStream? stream;
    private CancellationTokenSource? connectionCancellation;
    private Task? receiveLoop;
    private Task? heartbeatLoop;
    private long nextRpcId;
    private long lastPongTimestamp;
    private int connectionFailed;
    private int disposed;

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 创建使用指定 Opcode Manifest 的 RPC 客户端。
    /// </summary>
    /// <param name="opcodes">与 Unity 项目共享的稳定 Opcode 清单。</param>
    public MiniCoreRpcClient(OpcodeManifest opcodes)
    {
        this.opcodes = opcodes;
    }

    /// <summary>
    /// 连接 MiniCore TCP 服务端并启动唯一的接收循环与心跳循环。
    /// </summary>
    /// <param name="host">目标内网主机。</param>
    /// <param name="port">目标 Inner TCP 端口。</param>
    /// <param name="cancellationToken">连接取消令牌。</param>
    /// <returns>连接完成任务。</returns>
    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        if (client != null)
        {
            throw new InvalidOperationException("同一个 RPC 客户端实例只能建立一次连接。");
        }

        TcpClient newClient = new();
        try
        {
            await newClient.ConnectAsync(host, port, cancellationToken);
            client = newClient;
            stream = newClient.GetStream();
            connectionCancellation = new CancellationTokenSource();
            Volatile.Write(ref lastPongTimestamp, Environment.TickCount64);
            receiveLoop = ReceiveLoopAsync(connectionCancellation.Token);
            heartbeatLoop = HeartbeatLoopAsync(connectionCancellation.Token);
        }
        catch
        {
            newClient.Dispose();
            throw;
        }
    }

    /// <summary>
    /// 发送 RPC，并在指定时间内等待具有相同 RpcId 和响应 Opcode 的结果。
    /// </summary>
    /// <typeparam name="TRequest">Protobuf 请求类型。</typeparam>
    /// <typeparam name="TResponse">Protobuf 响应类型。</typeparam>
    /// <param name="request">待发送请求。</param>
    /// <param name="parser">响应 Protobuf Parser。</param>
    /// <param name="cancellationToken">调用取消令牌。</param>
    /// <param name="timeoutSeconds">本次 RPC 超时秒数，默认十秒。</param>
    /// <returns>反序列化后的响应。</returns>
    public async Task<TResponse> CallAsync<TRequest, TResponse>(
        TRequest request,
        MessageParser<TResponse> parser,
        CancellationToken cancellationToken,
        int timeoutSeconds = 10)
        where TRequest : IMessage<TRequest>
        where TResponse : IMessage<TResponse>
    {
        ThrowIfDisposed();
        if (timeoutSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), timeoutSeconds, "RPC 超时秒数必须大于零。");
        }

        EnsureConnected();
        long rpcId = Interlocked.Increment(ref nextRpcId);
        uint requestOpcode = opcodes.Get<TRequest>();
        uint responseOpcode = opcodes.Get<TResponse>();
        PendingRpc pendingRpc = new(responseOpcode);
        lock (pendingLock)
        {
            pendingRpcs.Add(rpcId, pendingRpc);
        }

        try
        {
            await WriteFrameAsync(requestOpcode, rpcId, request.ToByteArray(), cancellationToken);
            MiniCoreRpcFrame frame = await pendingRpc.Completion.Task.WaitAsync(
                TimeSpan.FromSeconds(timeoutSeconds),
                cancellationToken);
            return parser.ParseFrom(frame.Payload);
        }
        finally
        {
            lock (pendingLock)
            {
                pendingRpcs.Remove(rpcId);
            }
        }
    }

    /// <summary>
    /// 关闭 TCP 连接、后台循环与全部待处理 RPC。
    /// </summary>
    /// <returns>异步释放完成结果。</returns>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        FailConnection(new ObjectDisposedException(nameof(MiniCoreRpcClient)));
        Task[] loops = GetBackgroundLoops();
        try
        {
            await Task.WhenAll(loops);
        }
        catch
        {
            // 连接失败原因已经分发给 Pending RPC，释放阶段不重复抛出。
        }

        connectionCancellation?.Dispose();
        stream?.Dispose();
        client?.Dispose();
        writeGate.Dispose();
    }

    #endregion

    #region Private 私有方法

    /// <summary>
    /// 持续读取连接中的唯一入站数据流，并按 RpcId 分发响应。
    /// </summary>
    /// <param name="cancellationToken">连接生命周期取消令牌。</param>
    /// <returns>接收循环任务。</returns>
    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            NetworkStream currentStream = stream ?? throw new InvalidOperationException("RPC 客户端尚未连接。");
            while (!cancellationToken.IsCancellationRequested)
            {
                MiniCoreRpcFrame frame = await MiniCoreRpcFrameCodec.ReadAsync(currentStream, cancellationToken)
                    ?? throw new EndOfStreamException("RPC 连接已由远端关闭。");
                if (frame.Opcode == PingOpcode)
                {
                    await WriteFrameAsync(PongOpcode, frame.RpcId, ReadOnlyMemory<byte>.Empty, cancellationToken);
                    continue;
                }

                if (frame.Opcode == PongOpcode)
                {
                    Volatile.Write(ref lastPongTimestamp, Environment.TickCount64);
                    continue;
                }

                CompletePendingRpc(frame);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            FailConnection(exception);
        }
    }

    /// <summary>
    /// 每两秒发送一次 Ping，并在十秒没有收到 Pong 时关闭失效连接。
    /// </summary>
    /// <param name="cancellationToken">连接生命周期取消令牌。</param>
    /// <returns>心跳循环任务。</returns>
    private async Task HeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(HeartbeatIntervalSeconds), cancellationToken);
                long elapsedMilliseconds = Environment.TickCount64 - Volatile.Read(ref lastPongTimestamp);
                if (elapsedMilliseconds >= TimeSpan.FromSeconds(HeartbeatTimeoutSeconds).TotalMilliseconds)
                {
                    throw new TimeoutException($"连续 {HeartbeatTimeoutSeconds} 秒未收到 RPC Pong，连接已失效。");
                }

                await WriteFrameAsync(
                    PingOpcode,
                    Environment.TickCount64,
                    ReadOnlyMemory<byte>.Empty,
                    cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            FailConnection(exception);
        }
    }

    /// <summary>
    /// 通过连接唯一串行写入口发送一帧数据。
    /// </summary>
    /// <param name="opcode">消息 Opcode。</param>
    /// <param name="rpcId">RPC 或心跳标识。</param>
    /// <param name="payload">消息负载。</param>
    /// <param name="cancellationToken">写入取消令牌。</param>
    /// <returns>写入完成任务。</returns>
    private async Task WriteFrameAsync(
        uint opcode,
        long rpcId,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        await writeGate.WaitAsync(cancellationToken);
        try
        {
            EnsureConnected();
            NetworkStream currentStream = stream ?? throw new InvalidOperationException("RPC 客户端尚未连接。");
            await MiniCoreRpcFrameCodec.WriteAsync(currentStream, opcode, rpcId, payload, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException)
        {
            FailConnection(exception);
            throw;
        }
        finally
        {
            writeGate.Release();
        }
    }

    /// <summary>
    /// 将响应交给对应的待处理 RPC；已经超时的迟到响应会被忽略。
    /// </summary>
    /// <param name="frame">收到的 RPC 响应帧。</param>
    private void CompletePendingRpc(MiniCoreRpcFrame frame)
    {
        PendingRpc? pendingRpc;
        lock (pendingLock)
        {
            if (!pendingRpcs.Remove(frame.RpcId, out pendingRpc))
            {
                return;
            }
        }

        if (pendingRpc.ResponseOpcode != frame.Opcode)
        {
            pendingRpc.Completion.TrySetException(
                new InvalidDataException(
                    $"RpcId={frame.RpcId} 的响应 Opcode 不匹配，期望 {pendingRpc.ResponseOpcode}，实际 {frame.Opcode}。"));
            return;
        }

        pendingRpc.Completion.TrySetResult(frame);
    }

    /// <summary>
    /// 原子地标记连接失败，并一次性结束全部待处理 RPC。
    /// </summary>
    /// <param name="exception">导致连接失效的异常。</param>
    private void FailConnection(Exception exception)
    {
        if (Interlocked.Exchange(ref connectionFailed, 1) != 0)
        {
            return;
        }

        connectionCancellation?.Cancel();
        stream?.Close();
        client?.Close();

        PendingRpc[] pendingSnapshot;
        lock (pendingLock)
        {
            pendingSnapshot = pendingRpcs.Values.ToArray();
            pendingRpcs.Clear();
        }

        foreach (PendingRpc pendingRpc in pendingSnapshot)
        {
            pendingRpc.Completion.TrySetException(exception);
        }
    }

    /// <summary>
    /// 获取已经启动的后台循环，便于释放阶段等待结束。
    /// </summary>
    /// <returns>后台循环任务数组。</returns>
    private Task[] GetBackgroundLoops()
    {
        if (receiveLoop != null && heartbeatLoop != null)
        {
            return new[] { receiveLoop, heartbeatLoop };
        }

        if (receiveLoop != null)
        {
            return new[] { receiveLoop };
        }

        return heartbeatLoop != null ? new[] { heartbeatLoop } : Array.Empty<Task>();
    }

    /// <summary>
    /// 确保客户端已连接且尚未被判定失效。
    /// </summary>
    private void EnsureConnected()
    {
        if (client == null || stream == null || Volatile.Read(ref connectionFailed) != 0)
        {
            throw new IOException("RPC 客户端当前未连接。");
        }
    }

    /// <summary>
    /// 确保客户端尚未释放。
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(MiniCoreRpcClient));
        }
    }

    #endregion

    #region Private 私有类型

    /// <summary>
    /// 保存单次 RPC 的响应 Opcode 与异步完成源。
    /// </summary>
    private sealed class PendingRpc
    {
        /// <summary>
        /// 创建待处理 RPC。
        /// </summary>
        /// <param name="responseOpcode">期望的响应 Opcode。</param>
        public PendingRpc(uint responseOpcode)
        {
            ResponseOpcode = responseOpcode;
            Completion = new TaskCompletionSource<MiniCoreRpcFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        /// <summary>
        /// 获取期望的响应 Opcode。
        /// </summary>
        public uint ResponseOpcode { get; }

        /// <summary>
        /// 获取响应异步完成源。
        /// </summary>
        public TaskCompletionSource<MiniCoreRpcFrame> Completion { get; }
    }

    #endregion
}
