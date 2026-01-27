using Cysharp.Threading.Tasks;
using MiniCore.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;

namespace MiniCore.Core
{
    /// <summary>
    /// Network message hub: handles RPC, normal messages, heartbeat. Shared serializer, optional multi-session.
    /// </summary>
    public class NetworkMessageComponent : AComponent
    {
        private NetworkSessionComponent sessionComponent;
        private INetworkSerializer serializer;
        private long rpcIdGenerator = 1;

        private readonly Dictionary<long, PendingRpc> pendingRpcs = new Dictionary<long, PendingRpc>();
        private readonly Dictionary<uint, HandlerInfo> handlers = new Dictionary<uint, HandlerInfo>();
        private readonly Dictionary<uint, RpcHandlerInfo> rpcHandlers = new Dictionary<uint, RpcHandlerInfo>();
        private readonly Dictionary<string, HeartbeatState> heartbeatStates = new Dictionary<string, HeartbeatState>();
        private readonly Dictionary<Type, uint> opcodeCache = new Dictionary<Type, uint>();
        private readonly ConcurrentQueue<IncomingPacket> incomingPackets = new ConcurrentQueue<IncomingPacket>();
        private int processingQueue;


        private bool clientSendEnabled = true;
        private class PendingRpc
        {
            public Type ResponseType;
            public UniTaskCompletionSource<object> Tcs;
        }

        private class HandlerInfo
        {
            public Type MessageType;
            public Func<NetworkSession, object, UniTask> Invoker;
        }

        private class RpcHandlerInfo
        {
            public Type RequestType;
            public Type ResponseType;
            public Func<NetworkSession, object, IResponse, UniTask> Invoker;
        }

        private enum HeartbeatMode
        {
            Client,
            Server
        }

        private class HeartbeatState
        {
            public CancellationTokenSource Cts;
            public long LastPongTicks;
            public long LastPingTicks;
            public long LastPingSentTicks;
            public int LastRttMs;
            public int MinRttMs;
            public long MinRttWindowStartTicks;
            public HeartbeatMode Mode;
        }

        private struct IncomingPacket
        {
            public NetworkSession Session;
            public byte[] Buffer;
            public int Length;
        }

        public string DefaultSessionId { get; set; } = "default";
        public uint PingOpcode { get; set; } = 1;
        public uint PongOpcode { get; set; } = 2;
        public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan HeartbeatTimeout { get; set; } = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan DefaultProbeTimeout = TimeSpan.FromSeconds(2);


        public override void Awake()
        {
            base.Awake();
            sessionComponent = Global.Com.Get<NetworkSessionComponent>();
            serializer = null;
            AutoRegisterHandlersFromAssembly("HotUpdate");
        }

        public void SetSerializer(INetworkSerializer customSerializer)
        {
            serializer = customSerializer;
        }

        public async UniTask InitializeDefaultSessionAsync(string host, int port, CancellationToken token = default)
        {
            await InitializeSessionAsync(DefaultSessionId, host, port, token);
        }
        /*
                public async UniTask InitializeDefaultKcpSessionAsync(string host, int port, uint conv, KcpTransportConfig config = null, CancellationToken token = default)
                {
                    await InitializeKcpSessionAsync(DefaultSessionId, host, port, conv, config, token);
                }*/

        public UniTask<bool> ConnectDefaultKcpSessionAsync(string host, int port, uint conv, TimeSpan probeTimeout = default, KcpTransportConfig config = null, CancellationToken token = default)
        {
            return ConnectKcpSessionAsync(DefaultSessionId, host, port, conv, probeTimeout, config, token);
        }

        public async UniTask<bool> ConnectKcpSessionAsync(string sessionId, string host, int port, uint conv, TimeSpan probeTimeout = default, KcpTransportConfig config = null, CancellationToken token = default)
        {
            try
            {
                await InitializeKcpSessionAsync(sessionId, host, port, conv, config, token);
            }
            catch (Exception ex)
            {
                LogSwitch.Warning($"Kcp session init failed: {ex.Message}");
                return false;
            }

            if (probeTimeout <= TimeSpan.Zero)
            {
                probeTimeout = DefaultProbeTimeout;
            }

            bool ok = await ProbeSessionAsync(sessionId, probeTimeout, token);
            if (!ok)
            {
                sessionComponent?.RemoveSession(sessionId);
            }
            return ok;
        }

        public async UniTask InitializeSessionAsync(string sessionId, string host, int port, CancellationToken token = default)
        {
            await sessionComponent.CreateTcpSessionAsync(sessionId, host, port, token);
            BindSessionReceiver(sessionId);
        }

        public async UniTask InitializeKcpSessionAsync(string sessionId, string host, int port, uint conv, KcpTransportConfig config = null, CancellationToken token = default)
        {
            await sessionComponent.CreateKcpSessionAsync(sessionId, host, port, conv, config, token);
            BindSessionReceiver(sessionId);
        }

        public void BindSessionReceiver(string sessionId)
        {
            BindSessionReceiverInternal(sessionId, HeartbeatMode.Client);
        }

        public void BindServerSessionReceiver(string sessionId)
        {
            BindSessionReceiverInternal(sessionId, HeartbeatMode.Server);
        }

        protected override void Update()
        {
            if (incomingPackets.IsEmpty)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref processingQueue, 1, 0) != 0)
            {
                return;
            }

            ProcessQueueAsync().Forget();
        }

        public async UniTask<bool> ProbeSessionAsync(string sessionId, TimeSpan timeout, CancellationToken token = default)
        {
            if (!heartbeatStates.TryGetValue(sessionId, out var state))
            {
                return false;
            }

            if (!TryGetSession(sessionId, out var session))
            {
                return false;
            }
            long lastPong = state.LastPongTicks;
            var start = DateTimeOffset.UtcNow;
            var nextPing = start;
            while (!token.IsCancellationRequested && DateTimeOffset.UtcNow - start < timeout)
            {
                if (DateTimeOffset.UtcNow >= nextPing)
                {
                    try
                    {
                        await SendPingAsync(session, token);
                    }
                    catch
                    {
                        return false;
                    }
                    nextPing = DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(200);
                }
                if (heartbeatStates.TryGetValue(sessionId, out var updated) && updated.LastPongTicks != lastPong)
                {
                    return true;
                }
                await UniTask.Delay(50, cancellationToken: token);
            }
            return false;
        }

        public UniTask<TResponse> CallAsync<TRequest, TResponse>(TRequest request, CancellationToken token = default)
            where TRequest : IRequest
            where TResponse : IResponse
        {
            return CallAsync<TRequest, TResponse>(DefaultSessionId, request, token);
        }

        public async UniTask<TResponse> CallAsync<TRequest, TResponse>(string sessionId, TRequest request, CancellationToken token = default)
            where TRequest : IRequest
            where TResponse : IResponse
        {
            if (!clientSendEnabled)
            {
                return CreateLocalErrorResponse<TResponse>("Client disconnected.");
            }
            if (!TryGetSession(sessionId, out var session))
            {
                return CreateLocalErrorResponse<TResponse>($"Session {sessionId} not found.");
            }

            long rpcId = rpcIdGenerator++;
            request.RpcId = rpcId;

            var tcs = new UniTaskCompletionSource<object>();
            pendingRpcs[rpcId] = new PendingRpc { ResponseType = typeof(TResponse), Tcs = tcs };

            uint opcode = ResolveOpcode(request.GetType(), request.Opcode);
            string sendTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            LogSwitch.Info($"[{sendTime}] [{GetLogSide(session.SessionId)}] 发送RPC opcode:{opcode} rpcId:{rpcId} type:{request.GetType().FullName}");
            byte[] payload = GetSerializer().Serialize(request);
            if (LogSwitch.EnablePayloadLog)
            {
                string payloadText = Encoding.UTF8.GetString(payload);
                LogSwitch.Info($"[{sendTime}] 发送RPC内容: {payloadText}");
            }
            byte[] body = BuildPacket(opcode, rpcId, payload);
            await session.SendAsync(new ArraySegment<byte>(body), token);

            object result = await tcs.Task;
            return (TResponse)result;
        }

        public UniTask SendAsync<TMessage>(TMessage message, CancellationToken token = default) where TMessage : IProtocol
        {
            return SendAsync(DefaultSessionId, message, token);
        }

        public async UniTask SendAsync<TMessage>(string sessionId, TMessage message, CancellationToken token = default) where TMessage : IProtocol
        {
            if (!clientSendEnabled)
            {
                LogSwitch.Warning("Client disconnected, send skipped.");
                return;
            }
            if (!TryGetSession(sessionId, out var session))
            {
                LogSwitch.Warning($"Session {sessionId} not found, send skipped.");
                return;
            }
            if (!session.IsConnected)
            {
                LogSwitch.Warning($"Session {sessionId} not connected, send skipped.");
                return;
            }
            uint opcode = ResolveOpcode(message.GetType(), message.Opcode);
            string sendTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            LogSwitch.Info($"[{sendTime}] [{GetLogSide(session.SessionId)}] 发送普通消息 opcode:{opcode} rpcId:0 type:{message.GetType().FullName}");
            byte[] payload = GetSerializer().Serialize(message);
            if (LogSwitch.EnablePayloadLog)
            {
                string payloadText = Encoding.UTF8.GetString(payload);
                LogSwitch.Info($"[{sendTime}] 发送普通消息内容: {payloadText}");
            }
            byte[] body = BuildPacket(opcode, 0, payload);
            await session.SendAsync(new ArraySegment<byte>(body), token);
        }

        /// <summary>
        /// Scan assemblies for AMHandler<> and ARpcHandler<,> to auto-bind opcodes.
        /// </summary>
        public void AutoRegisterHandlersFromAssembly(string assemblyName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var asm = Array.Find(assemblies, a => a.FullName.Contains(assemblyName));
            if (asm == null)
            {
                LogSwitch.Warning($"未找到包含 {assemblyName} 的程序集，跳过自动注册。");
                return;
            }

            int normalCount = 0;
            int rpcCount = 0;

            foreach (var type in asm.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                if (!OpcodeRegistry.TryGetOpcodeByHandler(type, out uint opcode))
                {
                    continue;
                }

                if (!OpcodeRegistry.TryGetHandlerInfo(opcode, out _, out string requestTypeName, out string responseTypeName, out bool isRpc))
                {
                    LogSwitch.Warning($"Opcode 注册信息缺失: {type.FullName} (opcode:{opcode})，请重新生成映射。");
                    continue;
                }

                Type requestType = ResolveType(assemblies, requestTypeName);
                Type responseType = string.IsNullOrEmpty(responseTypeName) ? null : ResolveType(assemblies, responseTypeName);
                if (requestType == null)
                {
                    LogSwitch.Warning($"未找到请求类型 {requestTypeName}，handler: {type.FullName}");
                    continue;
                }
                if (isRpc && responseType == null)
                {
                    LogSwitch.Warning($"未找到响应类型 {responseTypeName}，RPC handler: {type.FullName}");
                    continue;
                }

                object handlerInstance = Activator.CreateInstance(type);
                var handleMethod = type.GetMethod("HandleAsync", BindingFlags.Instance | BindingFlags.Public);
                if (handleMethod == null)
                {
                    LogSwitch.Warning($"未找到 HandleAsync 方法: {type.FullName}");
                    continue;
                }

                if (isRpc)
                {
                    if (rpcHandlers.ContainsKey(opcode))
                    {
                        LogSwitch.Error($"RPC opcode 冲突:{opcode}，已跳过 {type.FullName}");
                        continue;
                    }

                    rpcHandlers[opcode] = new RpcHandlerInfo
                    {
                        RequestType = requestType,
                        ResponseType = responseType,
                        Invoker = CreateRpcInvoker(handlerInstance, handleMethod)
                    };
                    rpcCount++;
                }
                else
                {
                    if (handlers.ContainsKey(opcode))
                    {
                        LogSwitch.Error($"普通消息 opcode 冲突:{opcode}，{type.FullName} 与 {handlers[opcode].MessageType.FullName} 冲突，已跳过");
                        continue;
                    }

                    handlers[opcode] = new HandlerInfo
                    {
                        MessageType = requestType,
                        Invoker = (session, msg) => (UniTask)handleMethod.Invoke(handlerInstance, new object[] { session, msg })
                    };
                    normalCount++;
                }
            }

            LogSwitch.Info($"自动注册完成: 普通消息 {normalCount} 个，RPC {rpcCount} 个，程序集: {asm.FullName}");
        }

        private Type ResolveType(Assembly[] assemblies, string fullName)
        {
            foreach (var asm in assemblies)
            {
                var t = asm.GetType(fullName);
                if (t != null)
                {
                    return t;
                }
            }
            return Type.GetType(fullName);
        }

        private byte[] BuildPacket(uint opcode, long rpcId, byte[] payload)
        {
            // header: opcode(4 bytes, big-endian) + rpcId(8 bytes, big-endian)
            int length = 4 + 8 + (payload?.Length ?? 0);
            byte[] buffer = new byte[length];

            WriteUInt32BE(buffer, 0, opcode);
            WriteInt64BE(buffer, 4, rpcId);

            if (payload != null && payload.Length > 0)
            {
                Buffer.BlockCopy(payload, 0, buffer, 12, payload.Length);
            }
            return buffer;
        }

        private async UniTask HandleIncoming(NetworkSession session, ReadOnlyMemory<byte> data)
        {
            if (data.Length < 12)
            {
                LogSwitch.Warning("包长度无效，头部不足。");
                return;
            }

            uint opcode = ReadUInt32BE(data, 0);
            long rpcId = ReadInt64BE(data, 4);

            int payloadLength = data.Length - 12;
            ReadOnlyMemory<byte> payload = payloadLength > 0 ? data.Slice(12, payloadLength) : ReadOnlyMemory<byte>.Empty;

            string recvTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            LogSwitch.Info($"[{recvTime}] [{GetLogSide(session.SessionId)}] 收到消息 opcode:{opcode} rpcId:{rpcId} len:{payloadLength}");

            // Heartbeat handling
            if (opcode == PingOpcode)
            {
                if (TryGetHeartbeatMode(session.SessionId, out var mode) && mode == HeartbeatMode.Server)
                {
                    TouchPing(session.SessionId);
                    await SendPongAsync(session, rpcId);
                }
                return;
            }
            if (opcode == PongOpcode)
            {
                if (TryGetHeartbeatMode(session.SessionId, out var mode) && mode == HeartbeatMode.Client)
                {
                    TouchPong(session.SessionId);
                }
                return;
            }

            if (rpcId != 0 && pendingRpcs.TryGetValue(rpcId, out PendingRpc pending))
            {
                try
                {
                    object resp = GetSerializer().Deserialize(pending.ResponseType, payload);
                    pendingRpcs.Remove(rpcId);
                    pending.Tcs.TrySetResult(resp);
                }
                catch (Exception ex)
                {
                    LogSwitch.Error($"反序列化响应失败 opcode:{opcode} rpcId:{rpcId} err:{ex.Message}");
                    pending.Tcs.TrySetException(ex);
                }
                return;
            }

            if (rpcId != 0 && rpcHandlers.TryGetValue(opcode, out RpcHandlerInfo rpcInfo))
            {
                object req = GetSerializer().Deserialize(rpcInfo.RequestType, payload);
                if (!(Activator.CreateInstance(rpcInfo.ResponseType) is IResponse response))
                {
                    LogSwitch.Error($"RPC响应实例创建失败，类型:{rpcInfo.ResponseType?.FullName}");
                    return;
                }

                response.RpcId = rpcId;
                try
                {
                    await rpcInfo.Invoker(session, req, response);
                }
                catch (Exception ex)
                {
                    LogSwitch.Error($"RPC处理器执行异常，opcode:{opcode} 会话:{session.SessionId} 错误:{ex}");
                    return;
                }

                uint respOpcode = ResolveOpcode(response.GetType(), response.Opcode);
                string sendTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                LogSwitch.Info($"[{sendTime}] [{GetLogSide(session.SessionId)}] 发送RPC响应 opcode:{respOpcode} rpcId:{rpcId} type:{response.GetType().FullName}");
                try
                {
                    byte[] respPayload = GetSerializer().Serialize(response);
                    if (LogSwitch.EnablePayloadLog)
                    {
                        string payloadText = Encoding.UTF8.GetString(respPayload);
                        LogSwitch.Info($"[{sendTime}] 发送RPC响应内容: {payloadText}");
                    }
                    byte[] packet = BuildPacket(respOpcode, rpcId, respPayload);
                    await session.SendAsync(new ArraySegment<byte>(packet));
                }
                catch (Exception ex)
                {
                    LogSwitch.Error($"RPC响应发送异常，opcode:{respOpcode} 会话:{session.SessionId} 错误:{ex}");
                }
                return;
            }

            if (handlers.TryGetValue(opcode, out HandlerInfo info))
            {
                object msg = GetSerializer().Deserialize(info.MessageType, payload);
                await info.Invoker(session, msg);
            }
            else
            {
                LogSwitch.Warning($"未找到 opcode:{opcode} 的处理器");
            }
        }

        private void StartHeartbeat(NetworkSession session, HeartbeatMode mode)
        {
            StopHeartbeat(session.SessionId);
            var cts = new CancellationTokenSource();
            heartbeatStates[session.SessionId] = new HeartbeatState
            {
                Cts = cts,
                LastPongTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                LastPingTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Mode = mode
            };
            if (mode == HeartbeatMode.Client)
            {
                _ = HeartbeatLoopClient(session, cts.Token);
            }
            else
            {
                _ = HeartbeatLoopServer(session, cts.Token);
            }
        }

        private void StopHeartbeat(string sessionId)
        {
            if (heartbeatStates.TryGetValue(sessionId, out var state))
            {
                state.Cts.Cancel();
                heartbeatStates.Remove(sessionId);
            }
        }

        private async UniTask HeartbeatLoopClient(NetworkSession session, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && session.IsConnected)
                {
                    await UniTask.Delay(HeartbeatInterval, cancellationToken: token);
                    await SendPingAsync(session, token);
                    if (IsHeartbeatTimeout(session.SessionId))
                    {
                        string side = GetLogSide(session.SessionId);
                        string text = $"{side}心跳超时，主动断开，会话:{session.SessionId}";
                        LogSwitch.Warning(text);
                        session.Transport?.Disconnect();
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                LogSwitch.Warning($"心跳循环异常: {ex.Message}");
            }
        }

        private async UniTask HeartbeatLoopServer(NetworkSession session, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && session.IsConnected)
                {
                    await UniTask.Delay(HeartbeatInterval, cancellationToken: token);
                    if (IsPingTimeout(session.SessionId))
                    {
                        string side = GetLogSide(session.SessionId);
                        string text = $"{side}心跳超时，踢出连接，会话:{session.SessionId}";
                        LogSwitch.Warning(text);
                        session.Transport?.Disconnect();
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                LogSwitch.Warning($"服务端心跳循环异常: {ex.Message}");
            }
        }

        private async UniTask SendPingAsync(NetworkSession session, CancellationToken token)
        {
            if (heartbeatStates.TryGetValue(session.SessionId, out var state))
            {
                state.LastPingSentTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            byte[] body = BuildPacket(PingOpcode, 0, null);
            await session.SendAsync(new ArraySegment<byte>(body), token);
        }

        private async UniTask SendPongAsync(NetworkSession session, long rpcId)
        {
            byte[] body = BuildPacket(PongOpcode, rpcId, null);
            await session.SendAsync(new ArraySegment<byte>(body));
        }

        private void TouchPong(string sessionId)
        {
            if (heartbeatStates.TryGetValue(sessionId, out var state))
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                state.LastPongTicks = now;
                if (state.LastPingSentTicks > 0)
                {
                    int rtt = (int)Math.Max(0, now - state.LastPingSentTicks);
                    state.LastRttMs = rtt;
                    if (state.MinRttWindowStartTicks == 0)
                    {
                        state.MinRttWindowStartTicks = now;
                        state.MinRttMs = rtt;
                    }
                    else if (now - state.MinRttWindowStartTicks > 10000)
                    {
                        state.MinRttWindowStartTicks = now;
                        state.MinRttMs = rtt;
                    }
                    else if (state.MinRttMs == 0 || rtt < state.MinRttMs)
                    {
                        state.MinRttMs = rtt;
                    }
                }
            }
        }

        private void TouchPing(string sessionId)
        {
            if (heartbeatStates.TryGetValue(sessionId, out var state))
            {
                state.LastPingTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }

        public bool TryGetLastPingMs(string sessionId, out int pingMs)
        {
            pingMs = 0;
            if (!heartbeatStates.TryGetValue(sessionId, out var state))
            {
                return false;
            }
            pingMs = state.LastRttMs;
            return pingMs > 0;
        }

        public bool TryGetMinPingMs(string sessionId, out int pingMs)
        {
            pingMs = 0;
            if (!heartbeatStates.TryGetValue(sessionId, out var state))
            {
                return false;
            }
            pingMs = state.MinRttMs;
            return pingMs > 0;
        }

        public bool TryGetTransportRttMs(string sessionId, out int rttMs)
        {
            rttMs = 0;
            if (sessionComponent == null)
            {
                return false;
            }

            var session = sessionComponent.GetSession(sessionId);
            if (session?.Transport is KcpTransport kcpTransport)
            {
                return kcpTransport.TryGetSmoothedRttMs(out rttMs);
            }

            return false;
        }

        private bool IsHeartbeatTimeout(string sessionId)
        {
            if (!heartbeatStates.TryGetValue(sessionId, out var state))
            {
                return false;
            }
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return now - state.LastPongTicks > HeartbeatTimeout.TotalMilliseconds;
        }

        private bool IsPingTimeout(string sessionId)
        {
            if (!heartbeatStates.TryGetValue(sessionId, out var state))
            {
                return false;
            }
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return now - state.LastPingTicks > HeartbeatTimeout.TotalMilliseconds;
        }

        private void WriteInt64BE(byte[] buffer, int offset, long value)
        {
            buffer[offset] = (byte)((value >> 56) & 0xFF);
            buffer[offset + 1] = (byte)((value >> 48) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 40) & 0xFF);
            buffer[offset + 3] = (byte)((value >> 32) & 0xFF);
            buffer[offset + 4] = (byte)((value >> 24) & 0xFF);
            buffer[offset + 5] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 6] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 7] = (byte)(value & 0xFF);
        }

        private void WriteUInt32BE(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)((value >> 24) & 0xFF);
            buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 3] = (byte)(value & 0xFF);
        }

        private uint ReadUInt32BE(ReadOnlySpan<byte> buffer, int offset)
        {
            uint v = 0;
            for (int i = 0; i < 4; i++)
            {
                v = (v << 8) | buffer[offset + i];
            }
            return v;
        }

        private long ReadInt64BE(ReadOnlySpan<byte> buffer, int offset)
        {
            long v = 0;
            for (int i = 0; i < 8; i++)
            {
                v = (v << 8) | buffer[offset + i];
            }
            return v;
        }

        private uint ReadUInt32BE(ReadOnlyMemory<byte> buffer, int offset)
        {
            return ReadUInt32BE(buffer.Span, offset);
        }

        private long ReadInt64BE(ReadOnlyMemory<byte> buffer, int offset)
        {
            return ReadInt64BE(buffer.Span, offset);
        }

        private bool TryGetSession(string sessionId, out NetworkSession session)
        {
            session = sessionComponent?.GetSession(sessionId);
            if (session != null)
            {
                return true;
            }

            LogSwitch.Warning($"Session {sessionId} not found.");
            return false;
        }

        private TResponse CreateLocalErrorResponse<TResponse>(string message) where TResponse : IResponse
        {
            if (Activator.CreateInstance(typeof(TResponse)) is TResponse response)
            {
                response.ErrorCode = -1;
                response.Message = message;
                return response;
            }

            throw new InvalidOperationException($"Response type {typeof(TResponse).FullName} cannot be created.");
        }

        private INetworkSerializer GetSerializer()
        {
            if (serializer == null)
            {
                serializer = new UnityJsonSerializer();
            }
            return serializer;
        }

        private uint ResolveOpcode(Type msgType, uint fallback)
        {
            if (opcodeCache.TryGetValue(msgType, out uint cached))
            {
                return cached;
            }
            if (OpcodeRegistry.TryGetOpcodeByMessage(msgType, out uint mapped))
            {
                opcodeCache[msgType] = mapped;
                return mapped;
            }
            if (fallback != 0)
            {
                opcodeCache[msgType] = fallback;
                return fallback;
            }
            throw new InvalidOperationException($"Opcode mapping missing for {msgType.FullName}, please regenerate mapping.");
        }

        private Func<NetworkSession, object, IResponse, UniTask> CreateRpcInvoker(object handlerInstance, MethodInfo handleMethod)
        {
            return async (session, msg, response) =>
            {
                var resultObj = handleMethod.Invoke(handlerInstance, new object[] { session, msg, response });
                if (resultObj is UniTask task)
                {
                    await task;
                }
                else
                {
                    throw new InvalidOperationException($"RPC处理器返回值必须是 UniTask: {handleMethod.DeclaringType?.FullName}.{handleMethod.Name}");
                }
            };
        }

        private bool TryGetHeartbeatMode(string sessionId, out HeartbeatMode mode)
        {
            if (heartbeatStates.TryGetValue(sessionId, out var state))
            {
                mode = state.Mode;
                return true;
            }
            mode = HeartbeatMode.Client;
            return false;
        }

        private void BindSessionReceiverInternal(string sessionId, HeartbeatMode mode)
        {
            var session = sessionComponent.GetSession(sessionId);
            if (session?.Transport == null)
            {
                throw new InvalidOperationException($"Session {sessionId} not found or not connected.");
            }

            session.Transport.OnDataReceived += data => EnqueueIncoming(session, data);
            session.Transport.OnDisconnected += () =>
            {
                StopHeartbeat(session.SessionId);
                if (mode == HeartbeatMode.Client)
                {
                    clientSendEnabled = false;
                }
                string side = GetLogSide(session.SessionId);
                string text = $"{side}连接已断开，会话:{session.SessionId}";
                LogSwitch.Warning(text);
            };
            StartHeartbeat(session, mode);
        }

        private UniTask EnqueueIncoming(NetworkSession session, ReadOnlyMemory<byte> data)
        {
            if (data.IsEmpty)
            {
                return UniTask.CompletedTask;
            }

            int length = data.Length;
            byte[] buffer = ByteBufferPool.Shared.Rent(length);
            data.Span.CopyTo(buffer);
            incomingPackets.Enqueue(new IncomingPacket
            {
                Session = session,
                Buffer = buffer,
                Length = length
            });
            return UniTask.CompletedTask;
        }

        private async UniTaskVoid ProcessQueueAsync()
        {
            try
            {
                while (incomingPackets.TryDequeue(out var packet))
                {
                    try
                    {
                        await UniTask.SwitchToMainThread();
                        await HandleIncoming(packet.Session, new ReadOnlyMemory<byte>(packet.Buffer, 0, packet.Length));
                    }
                    finally
                    {
                        ByteBufferPool.Shared.Return(packet.Buffer);
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref processingQueue, 0);
            }
        }

        private string GetLogSide(string sessionId)
        {
            if (TryGetHeartbeatMode(sessionId, out var mode))
            {
                return mode == HeartbeatMode.Server ? "服务端" : "客户端";
            }

            return "未知端";
        }
    }
}
