using System;
using Cysharp.Threading.Tasks;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using UnityEngine;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// 在 Editor 测试和 Player 中复用的本机网络冒烟执行器，验证 HotUpdate Handler、Protobuf 与三种传输的完整业务链路。
    /// </summary>
    public sealed class NetworkSmokeTestRunner : MonoBehaviour
    {
        #region Private 私有成员

        private const string SmokePrefix = "[network-smoke] "; // 用于关联测试业务消息与事件日志的固定前缀。
        private const string TcpSessionId = "network-smoke-tcp"; // TCP 客户端会话标识。
        private const string KcpSessionId = "network-smoke-kcp"; // KCP 客户端会话标识。
        private const string UdpSessionId = "network-smoke-udp"; // UDP 客户端会话标识。
        private const int TcpPort = 25001; // 本机 TCP 冒烟监听端口。
        private const int KcpPort = 25002; // 本机 KCP 冒烟监听端口。
        private const int UdpPort = 25003; // 本机 UDP 冒烟监听端口。
        private const uint KcpConv = 1001; // 本机 KCP 冒烟连接标识。
        private static readonly TimeSpan StageTimeout = TimeSpan.FromSeconds(5); // 单个验证阶段的最长等待时长。

        private readonly UniTaskCompletionSource<bool> completionSource = new UniTaskCompletionSource<bool>(); // 对外暴露的单次运行完成通知。
        private INetworkService network; // 已由客户端入口初始化并注册 Handler 的网络服务。
        private GUIStyle statusStyle; // Player 屏幕状态文本样式缓存。
        private bool heldNetworkReference; // 当前对象是否持有网络组件引用。
        private bool subscribedToNetworkEvents; // 是否已订阅业务网络事件。
        private bool subscribedToSessionEvents; // 是否已订阅服务端会话事件。
        private bool isRunning; // 是否已经启动一次冒烟流程。
        private bool isCompleted; // 冒烟流程是否已经结束。
        private bool isPassed; // 冒烟流程是否成功。
        private int serverSessionCreatedCount; // 已观察到的服务端会话创建数量。
        private int serverSessionClosedCount; // 已观察到的服务端会话关闭数量。
        private string expectedNormalContent; // 当前等待的普通消息内容。
        private string lastNetworkEvent; // 最近收到的业务网络事件文本。
        private string statusMessage = "等待网络冒烟测试启动。"; // 当前屏幕与 Console 状态文本。
        private string failureMessage = string.Empty; // 失败时输出的可定位摘要。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 启动 Player 网络自检的命令行参数。
        /// </summary>
        public const string RunArgument = "-networkSmokeTest";

        /// <summary>
        /// 冒烟完成后按结果退出 Player 的命令行参数。
        /// </summary>
        public const string QuitArgument = "-networkSmokeQuit";

        /// <summary>
        /// 获取冒烟流程是否已经结束。
        /// </summary>
        public bool IsCompleted => isCompleted;

        /// <summary>
        /// 获取冒烟流程是否全部通过。
        /// </summary>
        public bool IsPassed => isPassed;

        /// <summary>
        /// 获取失败时包含协议、阶段、会话与异常信息的诊断摘要。
        /// </summary>
        public string FailureMessage => failureMessage;

        /// <summary>
        /// 获取当前显示给操作者的测试进度或最终结果。
        /// </summary>
        public string StatusMessage => statusMessage;

        /// <summary>
        /// 检查当前进程是否带有指定命令行参数。
        /// </summary>
        /// <param name="argument">要精确匹配的参数。</param>
        /// <returns>存在匹配参数时返回 true。</returns>
        public static bool HasCommandLineArgument(string argument)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                return false;
            }

            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length; index++)
            {
                if (string.Equals(arguments[index], argument, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 运行一次 TCP、KCP、UDP 本机回环业务验证；重复调用会返回同一轮运行结果。
        /// </summary>
        /// <returns>三种协议均通过时返回 true，否则返回 false。</returns>
        public UniTask<bool> RunAsync()
        {
            if (!isRunning)
            {
                isRunning = true;
                RunInternalAsync().Forget();
            }

            return completionSource.Task;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 在带有冒烟参数的 Player 中自动启动验证。
        /// </summary>
        private void Start()
        {
            if (HasCommandLineArgument(RunArgument))
            {
                RunAsync().Forget();
            }
        }

        /// <summary>
        /// 在 Player 中显示当前验证状态，方便不读取日志时人工确认结果。
        /// </summary>
        private void OnGUI()
        {
            if (!HasCommandLineArgument(RunArgument))
            {
                return;
            }

            if (statusStyle == null)
            {
                statusStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24,
                    wordWrap = true
                };
            }

            Color previousColor = GUI.contentColor;
            GUI.contentColor = isCompleted && !isPassed ? Color.red : isCompleted ? Color.green : Color.yellow;
            GUI.Box(new Rect(20f, 20f, 920f, 110f), GUIContent.none);
            GUI.Label(new Rect(36f, 36f, 888f, 78f), statusMessage, statusStyle);
            GUI.contentColor = previousColor;
        }

        /// <summary>
        /// 执行完整冒烟流程并将成功或失败写入统一结果。
        /// </summary>
        private async UniTaskVoid RunInternalAsync()
        {
            try
            {
                network = Global.GetService<INetworkService>(this);
                heldNetworkReference = true;
                network.OnServerSessionCreated += HandleServerSessionCreated;
                network.OnServerSessionClosed += HandleServerSessionClosed;
                subscribedToSessionEvents = true;
                EventCenter.AddListener<string>(HotEvent.KcpTestMessage, HandleNetworkEvent);
                subscribedToNetworkEvents = true;

                await RunTcpAsync();
                await RunKcpAsync();
                await RunUdpAsync();

                isPassed = true;
                statusMessage = "NETWORK_SMOKE: PASS (TCP / KCP / UDP)";
                Debug.Log(statusMessage);
            }
            catch (Exception exception)
            {
                isPassed = false;
                failureMessage = exception.Message;
                statusMessage = $"NETWORK_SMOKE: FAIL {failureMessage}";
                Debug.LogError(statusMessage);
            }
            finally
            {
                CleanupNetworkState();
                isCompleted = true;
                completionSource.TrySetResult(isPassed);

                if (HasCommandLineArgument(QuitArgument))
                {
                    Application.Quit(isPassed ? 0 : 1);
                }
            }
        }

        /// <summary>
        /// 启动并验证 TCP 本机回环。
        /// </summary>
        /// <returns>TCP 验证完成任务。</returns>
        private UniTask RunTcpAsync()
        {
            return RunTransportAsync(
                "TCP",
                TcpSessionId,
                () => network.StartTcpServerAsync("127.0.0.1", TcpPort),
                () => network.ConnectTcpSessionAsync(TcpSessionId, "127.0.0.1", TcpPort, StageTimeout),
                network.StopTcpServer);
        }

        /// <summary>
        /// 启动并验证 KCP 本机回环。
        /// </summary>
        /// <returns>KCP 验证完成任务。</returns>
        private UniTask RunKcpAsync()
        {
            return RunTransportAsync(
                "KCP",
                KcpSessionId,
                () => network.StartKcpServerAsync("127.0.0.1", KcpPort, new KcpServerConfig { Interval = 10, SessionTimeoutMs = 30000 }),
                () => network.ConnectKcpSessionAsync(KcpSessionId, "127.0.0.1", KcpPort, KcpConv, StageTimeout),
                network.StopKcpServer);
        }

        /// <summary>
        /// 启动并验证 UDP 本机回环。
        /// </summary>
        /// <returns>UDP 验证完成任务。</returns>
        private UniTask RunUdpAsync()
        {
            return RunTransportAsync(
                "UDP",
                UdpSessionId,
                () => network.StartUdpServerAsync("127.0.0.1", UdpPort, new UdpServerConfig()),
                () => network.ConnectUdpSessionAsync(UdpSessionId, "127.0.0.1", UdpPort, StageTimeout),
                network.StopUdpServer);
        }

        /// <summary>
        /// 运行单种传输的连接、普通消息、RPC 和服务端关闭验证。
        /// </summary>
        /// <param name="protocol">当前传输协议名称。</param>
        /// <param name="sessionId">当前客户端会话标识。</param>
        /// <param name="startServer">启动本地服务端的方法。</param>
        /// <param name="connectClient">建立并探测客户端会话的方法。</param>
        /// <param name="stopServer">停止本地服务端的方法。</param>
        /// <returns>当前传输验证完成任务。</returns>
        private async UniTask RunTransportAsync(string protocol, string sessionId, Func<UniTask> startServer, Func<UniTask<bool>> connectClient, Action stopServer)
        {
            int createdBeforeConnect = serverSessionCreatedCount;
            ReportStage(protocol, "start-server", sessionId);
            await startServer();

            ReportStage(protocol, "connect", sessionId);
            bool connected = await connectClient();
            Ensure(connected, protocol, "connect", sessionId, "连接探测未收到心跳响应。");
            await WaitForConditionAsync(() => serverSessionCreatedCount > createdBeforeConnect, protocol, "server-session-created", sessionId);

            expectedNormalContent = $"{SmokePrefix}{protocol}-normal";
            lastNetworkEvent = null;
            ReportStage(protocol, "normal-message", sessionId);
            await network.SendAsync(sessionId, new DemoNormalMessage { Content = expectedNormalContent });
            await WaitForConditionAsync(IsExpectedNormalMessageReceived, protocol, "normal-message", sessionId);

            ReportStage(protocol, "rpc", sessionId);
            string rpcPayload = $"{SmokePrefix}{protocol}-rpc";
            DemoRpcResponse response = await network.CallAsync<DemoRpcRequest, DemoRpcResponse>(sessionId, new DemoRpcRequest { Payload = rpcPayload });
            Ensure(response.Code == 0 && response.Msg == "RPC响应成功" && response.Echo == rpcPayload, protocol, "rpc", sessionId, $"RPC 响应未返回预期 Code、Msg 或 Echo。实际 Code:{response.Code} Msg:{response.Msg} Echo:{response.Echo}");

            int closedBeforeDisconnect = serverSessionClosedCount;
            ReportStage(protocol, "disconnect-notice", sessionId);
            await network.SendAsync(sessionId, new DisconnectNotice
            {
                IsServerShutdown = false,
                Reason = $"{SmokePrefix}{protocol}-close"
            });
            await WaitForConditionAsync(() => serverSessionClosedCount > closedBeforeDisconnect, protocol, "server-session-closed", sessionId);

            network.DisconnectSession(sessionId);
            await WaitForConditionAsync(() => network.GetSession(sessionId) == null, protocol, "client-session-removed", sessionId);
            stopServer();
            ReportStage(protocol, "passed", sessionId);
        }

        /// <summary>
        /// 等待指定条件成立，并在超时时给出协议与阶段上下文。
        /// </summary>
        /// <param name="condition">需要等待的条件。</param>
        /// <param name="protocol">当前传输协议名称。</param>
        /// <param name="stage">当前验证阶段。</param>
        /// <param name="sessionId">当前客户端会话标识。</param>
        /// <returns>条件成立后的完成任务。</returns>
        private async UniTask WaitForConditionAsync(Func<bool> condition, string protocol, string stage, string sessionId)
        {
            DateTime deadline = DateTime.UtcNow + StageTimeout;
            while (!condition())
            {
                if (DateTime.UtcNow >= deadline)
                {
                    throw new TimeoutException($"protocol:{protocol} stage:{stage} sessionId:{sessionId} 超时 {StageTimeout.TotalSeconds:0} 秒。");
                }

                await UniTask.Yield(PlayerLoopTiming.Update);
            }
        }

        /// <summary>
        /// 判断普通消息 Handler 广播的文本是否包含当前期待的业务内容。
        /// </summary>
        /// <returns>收到当前普通消息时返回 true。</returns>
        private bool IsExpectedNormalMessageReceived()
        {
            return !string.IsNullOrEmpty(expectedNormalContent) && !string.IsNullOrEmpty(lastNetworkEvent) && lastNetworkEvent.IndexOf(expectedNormalContent, StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// 记录服务端新建会话事件。
        /// </summary>
        /// <param name="session">刚创建的服务端逻辑会话。</param>
        private void HandleServerSessionCreated(NetworkSession session)
        {
            if (session != null)
            {
                serverSessionCreatedCount++;
            }
        }

        /// <summary>
        /// 记录服务端会话关闭事件。
        /// </summary>
        /// <param name="sessionId">已经关闭的服务端会话标识。</param>
        private void HandleServerSessionClosed(string sessionId)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                serverSessionClosedCount++;
            }
        }

        /// <summary>
        /// 保存业务 Handler 广播的最近一条网络事件文本。
        /// </summary>
        /// <param name="message">业务 Handler 广播的事件内容。</param>
        private void HandleNetworkEvent(string message)
        {
            lastNetworkEvent = message;
        }

        /// <summary>
        /// 更新当前协议阶段，并输出可搜索的 Console 日志。
        /// </summary>
        /// <param name="protocol">当前传输协议名称。</param>
        /// <param name="stage">当前验证阶段。</param>
        /// <param name="sessionId">当前客户端会话标识。</param>
        private void ReportStage(string protocol, string stage, string sessionId)
        {
            statusMessage = $"NETWORK_SMOKE: protocol:{protocol} stage:{stage} sessionId:{sessionId}";
            Debug.Log(statusMessage);
        }

        /// <summary>
        /// 将失败条件转换为包含协议、阶段和会话标识的异常。
        /// </summary>
        /// <param name="condition">需要成立的条件。</param>
        /// <param name="protocol">当前传输协议名称。</param>
        /// <param name="stage">当前验证阶段。</param>
        /// <param name="sessionId">当前客户端会话标识。</param>
        /// <param name="message">失败原因。</param>
        private static void Ensure(bool condition, string protocol, string stage, string sessionId, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"protocol:{protocol} stage:{stage} sessionId:{sessionId} {message}");
            }
        }

        /// <summary>
        /// 在成功、失败或对象销毁前解除订阅、清理客户端会话并停止本机监听。
        /// </summary>
        private void CleanupNetworkState()
        {
            if (network != null)
            {
                if (subscribedToSessionEvents)
                {
                    network.OnServerSessionCreated -= HandleServerSessionCreated;
                    network.OnServerSessionClosed -= HandleServerSessionClosed;
                    subscribedToSessionEvents = false;
                }

                if (subscribedToNetworkEvents)
                {
                    EventCenter.RemoveListener<string>(HotEvent.KcpTestMessage, HandleNetworkEvent);
                    subscribedToNetworkEvents = false;
                }

                network.DisconnectSession(TcpSessionId);
                network.DisconnectSession(KcpSessionId);
                network.DisconnectSession(UdpSessionId);
                network.StopTcpServer();
                network.StopKcpServer();
                network.StopUdpServer();
            }

            if (heldNetworkReference)
            {
                heldNetworkReference = false;
                Global.ReleaseAll(this);
            }

            network = null;
        }

        #endregion
    }
}
