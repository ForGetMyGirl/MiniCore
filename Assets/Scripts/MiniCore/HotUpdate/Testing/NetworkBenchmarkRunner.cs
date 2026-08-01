using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using MiniCore.Core;
using MiniCore.Eventing;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using MiniCore.Threading;
using MiniCore.Unity;
using Unity.Profiling;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// 仅在显式命令行参数存在时运行的 Android/Player 网络基准执行器。
    /// 它复用真实的 NetworkService、Protobuf、HotUpdate Handler 和 TCP/KCP/UDP 本机回环，并将结果导出为 JSON 与 CSV。
    /// </summary>
    public sealed class NetworkBenchmarkRunner : AMTaskBehaviour
    {
        #region Private 私有成员

        private const string TcpSessionId = "network-benchmark-tcp"; // TCP 客户端会话标识。
        private const string KcpSessionId = "network-benchmark-kcp"; // KCP 客户端会话标识。
        private const string UdpSessionId = "network-benchmark-udp"; // UDP 客户端会话标识。
        private const int TcpPort = 25101; // 本机 TCP 基准监听端口。
        private const int KcpPort = 25102; // 本机 KCP 基准监听端口。
        private const int UdpPort = 25103; // 本机 UDP 基准监听端口。
        private const uint KcpConv = 1101; // KCP 基准连接标识。
        private const int WarmupSeconds = 10; // 每个正常消息负载档开始前的预热秒数。
        private const int MeasurementSeconds = 60; // 每个正常消息或 RPC 压测样本的稳定测量秒数。
        private const int RepeatCount = 3; // 每项负载在每种传输下的重复运行次数。
        private const int RpcConcurrency = 64; // RPC 压测保持的最大并发请求数。
        private const int RpcQuickMeasurementSeconds = 15; // RPC 快速回归单轮的测量时长。
        private const int RpcLaunchBurstPerSchedulerTick = 8; // 每个调度间隔最多发起的 RPC 数量，保证高吞吐传输仍可维持 64 个在途请求。
        private const int RpcLaunchIntervalMilliseconds = 1; // RPC 补发之间至少等待一毫秒，确保不会在同一次 MTask Drain 内反复启动。
        private const int RpcReliableQueueHighWatermark = 16; // RPC 补发时可靠队列允许占用的最大包数，始终为心跳和响应留出半数槽位。
        private const double RpcProgressLogIntervalSeconds = 5d; // RPC 阶段输出进度日志的最短间隔。
        private const int MainThreadHitchMilliseconds = 2000; // 主线程积压诊断的人为停顿时间。
        private const int MediumNormalMessageRate = 1000; // 正常消息中负载档的目标发送速率。
        private const double MaximumMediumNormalMessageP99Milliseconds = 50d; // 正常消息中负载档允许的端到端 P99 上限。
        private const double WarmupDrainTimeoutSeconds = 30d; // 预热停止后等待已接受预热消息抵达 Handler 的最长时间，避免污染正式样本。
        private const int MaxNormalMessagesPerRun = 300000; // 5000 条每秒、60 秒时单轮正常消息的最大样本数。
        private const int MaxRpcRequestsPerRun = 500000; // 单轮 RPC 饱和压测允许记录的最大延迟样本数。
        private const int NormalSequenceDigits = 6; // 正常消息序号使用的固定十进制宽度。
        private const int MaxSendBurstPerFrame = 64; // 单帧追赶发送频率时的最大普通消息突发数。
        private const double NormalDrainTimeoutSeconds = 2d; // 正常消息停止发送后等待队列排空的最长时间。
        private const double RpcDrainTimeoutSeconds = 12d; // 停止发起 RPC 后等待剩余请求完成或超时的最长时间。
        private const string NormalPrefix = "MCBENCH-N|"; // 正常消息基准标记，供 Handler 事件回调识别。
        private const string WarmupPrefix = "MCBENCH-W|"; // 预热消息标记，不计入正式结果。
        private const string RpcPrefix = "MCBENCH-R|"; // RPC 消息基准标记，避免被正常消息统计误识别。
        private const string FixedPayload = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz"; // 固定长度的业务正文，保持每条正常消息负载大小一致。
        private static readonly int[] NormalMessageRates = { 100, 1000, 5000 }; // 需要依次执行的正常消息发送速率。
        private static readonly int[] RemainingNormalMessageRates = { 1000, 5000 }; // 尚未冻结的高频普通消息发送速率。
        private static readonly int[] HighNormalMessageRate = { 5000 }; // 仅用于已冻结中低负载后复查单个高负载条目的发送速率。
        private static readonly int[] MediumNormalMessageRateOnly = { MediumNormalMessageRate }; // 仅用于三协议中负载尾延迟定位的发送速率。
        private static readonly TimeSpan TransportTimeout = TimeSpan.FromSeconds(5); // 建连与服务器会话出现的超时。

        private readonly MTaskCompletionSource<bool> completionSource = new MTaskCompletionSource<bool>(); // 对外公开的单次压测完成通知。
        private readonly NetworkBenchmarkLatencyCollector normalLatency = new NetworkBenchmarkLatencyCollector(MaxNormalMessagesPerRun); // 正常消息延迟样本收集器。
        private readonly NetworkBenchmarkLatencyCollector rpcLatency = new NetworkBenchmarkLatencyCollector(MaxRpcRequestsPerRun); // RPC 延迟样本收集器。
        private readonly long[] normalSentTicks = new long[MaxNormalMessagesPerRun]; // 按正常消息序号保存发送开始 tick。
        private readonly byte[] normalReceivedFlags = new byte[MaxNormalMessagesPerRun]; // 标记正常消息是否已收到首个业务回调，忽略 UDP 重复包。
        private readonly List<NetworkBenchmarkRunResult> results = new List<NetworkBenchmarkRunResult>(); // 本次 Player 运行产生的全部结果。

        private INetworkService network; // 当前基准使用的真实网络服务。
        private EventSubscription networkMessageSubscription; // 观察真实普通 Handler 业务事件的订阅 token。
        private bool heldNetworkReference; // 当前 Runner 是否持有网络服务引用。
        private bool subscribedToNetworkEvents; // 是否已订阅业务消息事件。
        private bool previousEnableLog; // 压测开始前的普通日志开关状态。
        private bool previousEnablePayloadLog; // 压测开始前的正文日志开关状态。
        private bool logSwitchCaptured; // 是否已经保存并关闭压测期间的日志开关。
        private bool isRunning; // 是否已经启动过基准流程。
        private bool isCompleted; // 基准流程是否已经结束。
        private bool isPassed; // 所有传输和负载均完成，且 RPC 样本未出现可靠性质量失败。
        private string failureMessage = string.Empty; // 首个导致流程终止的失败信息。
        private string qualityFailureMessage = string.Empty; // 任一压测样本完成但存在拒绝、丢失、断线或恢复超限时的质量失败摘要。
        private string statusMessage = "等待网络压测启动。"; // 展示给操作者的当前进度。
        private int serverSessionCreatedCount; // 已观察到的本机服务端会话创建数量。
        private int normalSentCount; // 当前正常消息样本成功发送数量。
        private int normalOfferedCount; // 当前正常消息样本尝试提交到出站队列的数量。
        private int normalRejectedCount; // 当前正常消息样本被有界出站队列拒绝的数量。
        private int normalReceivedCount; // 当前正常消息样本收到首个业务回调的数量。
        private int normalFailureCount; // 当前正常消息样本的发送失败数量。
        private int normalEventObservedCount; // 当前样本观察到的全部 DemoMessageReceivedEvent 数量。
        private int normalEventRecognizedCount; // 当前样本被识别为 MCBENCH-N 普通消息的事件数量。
        private int normalEventUnrecognizedCount; // 当前样本不包含 MCBENCH-N 序号的业务事件数量。
        private int normalEventOutOfRangeCount; // 当前样本序号尚未登记或超出已发送范围的普通消息事件数量。
        private int normalEventDuplicateCount; // 当前样本已被计入过一次的普通消息事件数量。
        private int normalEventMissingTimestampCount; // 当前样本序号已登记但发送时间戳尚未写入的普通消息事件数量。
        private int warmupAcceptedCount; // 当前预热阶段成功进入出站队列的消息数量。
        private int warmupReceivedCount; // 当前预热阶段已到达 DemoNormalHandler 事件观察点的消息数量。
        private int disconnectCount; // 当前样本观察到的服务端逻辑会话断开次数。
        private int rpcSentCount; // 当前 RPC 样本已发起请求数量。
        private int rpcReceivedCount; // 当前 RPC 样本已成功收到响应数量。
        private int rpcFailureCount; // 当前 RPC 样本的异常或错误响应数量。
        private int rpcOutstandingCount; // 当前尚未完成的 RPC 请求数量。
        private bool collectingFrameMetrics; // 是否在当前样本期间读取每帧 GC 分配采样。
        private long maxGcAllocatedBytesPerFrame; // 当前样本观察到的最大 GC Allocated In Frame。
        private ProfilerRecorder gcAllocatedRecorder; // Unity Profiler 的 GC Allocated In Frame 采样器。
        private bool gcAllocatedRecorderRunning; // GC 采样器是否已成功启动。
        private GUIStyle statusStyle; // Player 状态文字样式缓存。
        private BenchmarkRunProfile runProfile; // 本次启动选择的完整、RPC、普通消息专项或中负载诊断执行范围。

        /// <summary>
        /// 表示本次真机压测需要覆盖的场景范围。
        /// </summary>
        private enum BenchmarkRunProfile
        {
            Full,
            RpcOnly,
            RpcQuick,
            RemainingNormalQuick,
            TcpNormalQuick,
            UdpNormalQuick,
            MediumNormalDiagnostic
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 启动完整网络压测流程的命令行参数。
        /// </summary>
        public const string RunArgument = "-networkBenchmark";

        /// <summary>
        /// 在压测流程结束后以通过或失败退出 Player 的命令行参数。
        /// </summary>
        public const string QuitArgument = "-networkBenchmarkQuit";

        /// <summary>
        /// 只运行三种传输的完整 RPC 样本，不运行普通消息与主线程停顿诊断的命令行参数。
        /// 必须与 RunArgument 一起传入。
        /// </summary>
        public const string RpcOnlyArgument = "-networkBenchmarkRpcOnly";

        /// <summary>
        /// 对三种传输各运行一轮十五秒 RPC 的快速回归命令行参数。
        /// 必须与 RunArgument 一起传入，用于修改 RPC 或可靠出站队列后的先行验证。
        /// </summary>
        public const string RpcQuickArgument = "-networkBenchmarkRpcQuick";

        /// <summary>
        /// 只运行 TCP/UDP 尚未冻结的 1000/5000 条每秒普通消息各一轮，并包含对应主线程停顿诊断的快速回归命令行参数。
        /// 必须与 RunArgument 一起传入；它不运行 KCP、RPC 和已冻结的 100 条每秒样本。
        /// </summary>
        public const string RemainingNormalQuickArgument = "-networkBenchmarkRemainingNormalQuick";

        /// <summary>
        /// 只运行 TCP 的 1000/5000 条每秒普通消息各一轮的快速回归命令行参数。
        /// 必须与 RunArgument 一起传入；用于仅改动 TCP 写出路径后验证吞吐，不运行 UDP、KCP、RPC 或主线程停顿诊断。
        /// </summary>
        public const string TcpNormalQuickArgument = "-networkBenchmarkTcpNormalQuick";

        /// <summary>
        /// 只运行 UDP 的 5000 条每秒普通消息一轮的快速回归命令行参数。
        /// 必须与 RunArgument 一起传入；用于仅改动 UDP 传输路径后验证尚未通过的高负载条目，不运行已冻结的 UDP 1000/s、TCP、KCP、RPC 或主线程停顿诊断。
        /// </summary>
        public const string UdpNormalQuickArgument = "-networkBenchmarkUdpNormalQuick";

        /// <summary>
        /// 对 TCP、KCP、UDP 各运行一轮 1000 条每秒普通消息的分段诊断命令行参数。
        /// 必须与 RunArgument 一起传入；不运行其他普通消息档位、RPC 或主线程停顿。
        /// </summary>
        public const string MediumNormalDiagnosticArgument = "-networkBenchmarkMediumNormalDiagnostic";

        /// <summary>
        /// 获取压测流程是否已经完成。
        /// </summary>
        public bool IsCompleted => isCompleted;

        /// <summary>
        /// 获取压测流程是否全部完成且未出现阻断性错误。
        /// </summary>
        public bool IsPassed => isPassed;

        /// <summary>
        /// 获取流程失败时包含传输、阶段和异常的摘要。
        /// </summary>
        public string FailureMessage => failureMessage;

        /// <summary>
        /// 获取当前显示给操作者的进度或最终结果。
        /// </summary>
        public string StatusMessage => statusMessage;

        /// <summary>
        /// 判断当前进程是否带有指定命令行参数。
        /// </summary>
        /// <param name="argument">需要精确匹配的参数。</param>
        /// <returns>存在不区分大小写的匹配项时返回 true。</returns>
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
        /// 启动一次完整的 TCP、KCP、UDP 基准；重复调用会返回同一轮结果。
        /// </summary>
        /// <returns>全部样本完成且已导出报告时返回 true。</returns>
        public MTask<bool> RunAsync()
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
        /// 在显式基准参数存在时自动开始压测。
        /// </summary>
        private void Start()
        {
            if (HasCommandLineArgument(RunArgument))
            {
                RunAsync().Forget();
            }
        }

        /// <summary>
        /// 在采样期间记录当前帧的 GC Allocated In Frame 峰值。
        /// </summary>
        private void Update()
        {
            if (!collectingFrameMetrics || !gcAllocatedRecorderRunning)
            {
                return;
            }

            long allocatedBytes = gcAllocatedRecorder.LastValue;
            if (allocatedBytes > maxGcAllocatedBytesPerFrame)
            {
                maxGcAllocatedBytesPerFrame = allocatedBytes;
            }
        }

        /// <summary>
        /// 在 Player 中展示当前压测阶段，避免操作者必须读取日志判断状态。
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
                    fontSize = 22,
                    wordWrap = true
                };
            }

            Color previousColor = GUI.contentColor;
            GUI.contentColor = isCompleted && !isPassed ? Color.red : isCompleted ? Color.green : Color.yellow;
            GUI.Box(new Rect(20f, 20f, 1040f, 120f), GUIContent.none);
            GUI.Label(new Rect(36f, 36f, 1008f, 88f), statusMessage, statusStyle);
            GUI.contentColor = previousColor;
        }

        /// <summary>
        /// 根据命令行选择执行完整压测、RPC 回归、普通消息专项或中负载分段诊断，并导出最终报告。
        /// </summary>
        /// <returns>完整压测流程的异步任务。</returns>
        private async MTask RunInternalAsync()
        {
            try
            {
                runProfile = ResolveRunProfile();
                previousEnableLog = LogSwitch.EnableLog;
                previousEnablePayloadLog = LogSwitch.EnablePayloadLog;
                logSwitchCaptured = true;
                LogSwitch.EnableLog = false;
                LogSwitch.EnablePayloadLog = false;
                network = Global.GetService<INetworkService>(this);
                heldNetworkReference = true;
                network.OnServerSessionCreated += HandleServerSessionCreated;
                network.OnServerSessionClosed += HandleServerSessionClosed;
                IApplicationEventBus eventBus = Global.GetOrAddModule<IApplicationEventBus>(this);
                networkMessageSubscription = eventBus.Subscribe<DemoMessageReceivedEvent>(HandleNetworkMessage);
                subscribedToNetworkEvents = true;

                if (runProfile != BenchmarkRunProfile.UdpNormalQuick)
                {
                    await RunTcpAsync();
                }

                if (runProfile != BenchmarkRunProfile.RemainingNormalQuick
                    && runProfile != BenchmarkRunProfile.TcpNormalQuick
                    && runProfile != BenchmarkRunProfile.UdpNormalQuick)
                {
                    await RunKcpAsync();
                }

                if (runProfile != BenchmarkRunProfile.TcpNormalQuick)
                {
                    await RunUdpAsync();
                }

                string reportDirectory = NetworkBenchmarkReportWriter.Write(results);
                if (string.IsNullOrEmpty(qualityFailureMessage))
                {
                    isPassed = true;
                    statusMessage = $"NETWORK_BENCHMARK: PASS results:{results.Count} directory:{reportDirectory}";
                    Debug.Log(statusMessage);
                }
                else
                {
                    isPassed = false;
                    failureMessage = qualityFailureMessage;
                    statusMessage = $"NETWORK_BENCHMARK: FAIL {failureMessage} directory:{reportDirectory}";
                    Debug.LogError(statusMessage);
                }
            }
            catch (Exception exception)
            {
                failureMessage = exception.Message;
                statusMessage = $"NETWORK_BENCHMARK: FAIL {failureMessage}";
                Debug.LogError(statusMessage);
            }
            finally
            {
                StopFrameMetrics();
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
        /// 执行 TCP 本机回环的全部基准样本。
        /// </summary>
        /// <returns>TCP 基准完成任务。</returns>
        private MTask RunTcpAsync()
        {
            return RunTransportAsync(
                "TCP",
                TcpSessionId,
                () => network.StartTcpServerAsync("127.0.0.1", TcpPort),
                () => network.ConnectTcpSessionAsync(TcpSessionId, "127.0.0.1", TcpPort, TransportTimeout),
                network.StopTcpServer);
        }

        /// <summary>
        /// 执行 KCP 本机回环的全部基准样本。
        /// </summary>
        /// <returns>KCP 基准完成任务。</returns>
        private MTask RunKcpAsync()
        {
            return RunTransportAsync(
                "KCP",
                KcpSessionId,
                () => network.StartKcpServerAsync("127.0.0.1", KcpPort, new KcpServerConfig { Interval = 10, SessionTimeoutMs = 30000 }),
                () => network.ConnectKcpSessionAsync(KcpSessionId, "127.0.0.1", KcpPort, KcpConv, TransportTimeout),
                network.StopKcpServer);
        }

        /// <summary>
        /// 执行 UDP 本机回环的全部基准样本。
        /// </summary>
        /// <returns>UDP 基准完成任务。</returns>
        private MTask RunUdpAsync()
        {
            return RunTransportAsync(
                "UDP",
                UdpSessionId,
                () => network.StartUdpServerAsync("127.0.0.1", UdpPort, new UdpServerConfig()),
                () => network.ConnectUdpSessionAsync(UdpSessionId, "127.0.0.1", UdpPort, TransportTimeout),
                network.StopUdpServer);
        }

        /// <summary>
        /// 建立一种本机传输并按当前范围执行对应压测样本。
        /// </summary>
        /// <param name="transport">报告使用的传输名称。</param>
        /// <param name="sessionId">客户端逻辑会话标识。</param>
        /// <param name="startServer">启动本机服务端的操作。</param>
        /// <param name="connectClient">建立客户端并完成探测的操作。</param>
        /// <param name="stopServer">停止本机服务端的操作。</param>
        /// <returns>当前传输的全部样本完成任务。</returns>
        private async MTask RunTransportAsync(string transport, string sessionId, Func<MTask> startServer, Func<MTask<bool>> connectClient, Action stopServer)
        {
            int serverCountBefore = serverSessionCreatedCount;
            ReportStage(transport, "start-server");
            await startServer();

            try
            {
                ReportStage(transport, "connect");
                bool connected = await connectClient();
                Ensure(connected, transport, "connect", "连接探测未收到心跳响应。");
                await WaitForConditionAsync(() => serverSessionCreatedCount > serverCountBefore, transport, "server-session-created", TransportTimeout);

                if (runProfile == BenchmarkRunProfile.Full
                    || runProfile == BenchmarkRunProfile.RemainingNormalQuick
                    || runProfile == BenchmarkRunProfile.TcpNormalQuick
                    || runProfile == BenchmarkRunProfile.UdpNormalQuick
                    || runProfile == BenchmarkRunProfile.MediumNormalDiagnostic)
                {
                    int[] normalRates = runProfile == BenchmarkRunProfile.Full
                        ? NormalMessageRates
                        : runProfile == BenchmarkRunProfile.UdpNormalQuick
                            ? HighNormalMessageRate
                            : runProfile == BenchmarkRunProfile.MediumNormalDiagnostic
                                ? MediumNormalMessageRateOnly
                                : RemainingNormalMessageRates;
                    int normalRepeatCount = runProfile == BenchmarkRunProfile.Full ? RepeatCount : 1;
                    for (int rateIndex = 0; rateIndex < normalRates.Length; rateIndex++)
                    {
                        int rate = normalRates[rateIndex];
                        for (int repeat = 1; repeat <= normalRepeatCount; repeat++)
                        {
                            await RunNormalBenchmarkAsync(transport, sessionId, rate, repeat);
                        }
                    }
                }

                if (runProfile != BenchmarkRunProfile.RemainingNormalQuick
                    && runProfile != BenchmarkRunProfile.TcpNormalQuick
                    && runProfile != BenchmarkRunProfile.UdpNormalQuick
                    && runProfile != BenchmarkRunProfile.MediumNormalDiagnostic)
                {
                    int rpcRepeatCount = runProfile == BenchmarkRunProfile.RpcQuick ? 1 : RepeatCount;
                    int rpcMeasurementSeconds = runProfile == BenchmarkRunProfile.RpcQuick ? RpcQuickMeasurementSeconds : MeasurementSeconds;
                    for (int repeat = 1; repeat <= rpcRepeatCount; repeat++)
                    {
                        await RunRpcBenchmarkAsync(transport, sessionId, repeat, rpcMeasurementSeconds);
                    }
                }

                if (runProfile == BenchmarkRunProfile.Full || runProfile == BenchmarkRunProfile.RemainingNormalQuick)
                {
                    await RunMainThreadHitchBenchmarkAsync(transport, sessionId);
                }
            }
            finally
            {
                network.DisconnectSession(sessionId);
                stopServer();
            }
        }

        /// <summary>
        /// 预热后运行一轮固定发送速率的正常消息负载。
        /// </summary>
        /// <param name="transport">报告使用的传输名称。</param>
        /// <param name="sessionId">客户端逻辑会话标识。</param>
        /// <param name="rate">目标每秒发送消息数。</param>
        /// <param name="repeat">当前速率的重复序号。</param>
        /// <returns>正常消息样本完成任务。</returns>
        private async MTask RunNormalBenchmarkAsync(string transport, string sessionId, int rate, int repeat)
        {
            ResetWarmupSample();
            ReportStage(transport, $"normal-warmup-{rate}-r{repeat}");
            await SendNormalMessagesForDurationAsync(sessionId, rate, WarmupSeconds, false);
            ReportStage(transport, $"normal-warmup-drain-{rate}-r{repeat}");
            await WaitForWarmupMessagesAsync(transport, rate, repeat);

            ResetNormalSample();
            ResetTimingMetrics(sessionId);
            StartFrameMetrics();
            long startedTicks = Stopwatch.GetTimestamp();
            ReportStage(transport, $"normal-{rate}-r{repeat}");
            await SendNormalMessagesForDurationAsync(sessionId, rate, MeasurementSeconds, true);
            await WaitForNormalMessagesAsync(NormalDrainTimeoutSeconds);
            NetworkBenchmarkRunResult result = CreateResult(
                transport,
                "NormalMessage",
                rate,
                repeat,
                sessionId,
                startedTicks,
                normalSentCount,
                normalReceivedCount,
                normalFailureCount,
                normalLatency.CalculateSummary());
            StopFrameMetrics();
            results.Add(result);
            RecordQualityFailure(transport, result);
        }

        /// <summary>
        /// 运行一轮最多 64 个并发请求的饱和 RPC 负载。
        /// </summary>
        /// <param name="transport">报告使用的传输名称。</param>
        /// <param name="sessionId">客户端逻辑会话标识。</param>
        /// <param name="repeat">当前 RPC 样本的重复序号。</param>
        /// <param name="measurementSeconds">当前 RPC 样本的正式测量时长。</param>
        /// <returns>RPC 样本完成任务。</returns>
        private async MTask RunRpcBenchmarkAsync(string transport, string sessionId, int repeat, int measurementSeconds)
        {
            ResetRpcSample();
            ResetTimingMetrics(sessionId);
            StartFrameMetrics();
            long startedTicks = Stopwatch.GetTimestamp();
            ReportStage(transport, $"rpc-r{repeat}");
            double deadline = Time.realtimeSinceStartupAsDouble + measurementSeconds;
            double nextProgressLogTime = Time.realtimeSinceStartupAsDouble + RpcProgressLogIntervalSeconds;
            int sequence = 0;

            while (Time.realtimeSinceStartupAsDouble < deadline && sequence < MaxRpcRequestsPerRun)
            {
                int launchCount = 0;
                while (rpcOutstandingCount < RpcConcurrency && sequence < MaxRpcRequestsPerRun && launchCount < RpcLaunchBurstPerSchedulerTick && CanLaunchRpc(sessionId))
                {
                    rpcOutstandingCount++;
                    rpcSentCount++;
                    RunRpcCallAsync(sessionId, sequence, Stopwatch.GetTimestamp()).Forget();
                    sequence++;
                    launchCount++;
                }

                ReportRpcProgressIfDue(transport, repeat, ref nextProgressLogTime);
                await MTask.Delay(RpcLaunchIntervalMilliseconds);
            }

            await WaitForConditionAsync(() => rpcOutstandingCount == 0, transport, "rpc-drained", TimeSpan.FromSeconds(RpcDrainTimeoutSeconds));
            NetworkBenchmarkRunResult result = CreateResult(
                transport,
                "Rpc",
                RpcConcurrency,
                repeat,
                sessionId,
                startedTicks,
                rpcSentCount,
                rpcReceivedCount,
                rpcFailureCount,
                rpcLatency.CalculateSummary());
            StopFrameMetrics();
            results.Add(result);
            RecordQualityFailure(transport, result);
        }

        /// <summary>
        /// 在中等正常消息负载后阻塞主线程两秒，记录积压峰值和恢复时间。
        /// </summary>
        /// <param name="transport">报告使用的传输名称。</param>
        /// <param name="sessionId">客户端逻辑会话标识。</param>
        /// <returns>主线程停顿样本完成任务。</returns>
        private async MTask RunMainThreadHitchBenchmarkAsync(string transport, string sessionId)
        {
            ResetNormalSample();
            ResetTimingMetrics(sessionId);
            StartFrameMetrics();
            long startedTicks = Stopwatch.GetTimestamp();
            ReportStage(transport, "main-thread-hitch-send");
            await SendNormalMessagesForDurationAsync(sessionId, 1000, 3, true);

            ReportStage(transport, "main-thread-hitch-sleep");
            Thread.Sleep(MainThreadHitchMilliseconds);
            long recoveryStartedTicks = Stopwatch.GetTimestamp();
            await WaitForNormalMessagesAsync(NormalDrainTimeoutSeconds);
            long recoveryTicks = Stopwatch.GetTimestamp() - recoveryStartedTicks;
            NetworkBenchmarkRunResult result = CreateResult(
                transport,
                "MainThreadHitch",
                1000,
                1,
                sessionId,
                startedTicks,
                normalSentCount,
                normalReceivedCount,
                normalFailureCount,
                normalLatency.CalculateSummary());
            result.HitchMilliseconds = MainThreadHitchMilliseconds;
            result.QueueRecoveryMilliseconds = recoveryTicks * 1000d / Stopwatch.Frequency;
            StopFrameMetrics();
            results.Add(result);
            RecordQualityFailure(transport, result);
        }

        /// <summary>
        /// 按目标速率发送正常消息；预热消息与正式消息使用不同标记，避免互相污染统计。
        /// </summary>
        /// <param name="sessionId">客户端逻辑会话标识。</param>
        /// <param name="rate">目标每秒发送消息数。</param>
        /// <param name="durationSeconds">本阶段持续时间。</param>
        /// <param name="record">是否记录本阶段的正常消息延迟。</param>
        /// <returns>发送循环结束任务。</returns>
        private async MTask SendNormalMessagesForDurationAsync(string sessionId, int rate, int durationSeconds, bool record)
        {
            double intervalSeconds = 1d / rate;
            double deadline = Time.realtimeSinceStartupAsDouble + durationSeconds;
            double nextSendTime = Time.realtimeSinceStartupAsDouble;
            int warmupSequence = 0;

            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                int burstCount = 0;
                while (Time.realtimeSinceStartupAsDouble >= nextSendTime && burstCount < MaxSendBurstPerFrame)
                {
                    if (record)
                    {
                        if (normalSentCount >= MaxNormalMessagesPerRun)
                        {
                            return;
                        }

                        int sequence = normalSentCount;
                        normalOfferedCount++;
                        try
                        {
                            NetworkSendResult sendResult = network.TrySend(sessionId, new DemoNormalMessage { Content = BuildNormalContent(NormalPrefix, sequence) });
                            if (sendResult == NetworkSendResult.Accepted)
                            {
                                normalSentTicks[sequence] = Stopwatch.GetTimestamp();
                                normalSentCount++;
                            }
                            else if (sendResult == NetworkSendResult.QueueFull)
                            {
                                normalRejectedCount++;
                            }
                            else
                            {
                                normalFailureCount++;
                            }
                        }
                        catch (Exception)
                        {
                            normalFailureCount++;
                        }
                    }
                    else
                    {
                        try
                        {
                            NetworkSendResult sendResult = network.TrySend(sessionId, new DemoNormalMessage { Content = BuildNormalContent(WarmupPrefix, warmupSequence) });
                            if (sendResult == NetworkSendResult.Accepted)
                            {
                                warmupAcceptedCount++;
                            }
                            warmupSequence = (warmupSequence + 1) % MaxNormalMessagesPerRun;
                        }
                        catch (Exception)
                        {
                        }
                    }

                    nextSendTime += intervalSeconds;
                    burstCount++;
                }

                await MTask.Yield();
            }
        }

        /// <summary>
        /// 执行一个 RPC 请求并将成功、失败和延迟记录到当前样本。
        /// </summary>
        /// <param name="sessionId">客户端逻辑会话标识。</param>
        /// <param name="sequence">当前 RPC 请求序号。</param>
        /// <param name="startedTicks">请求发起时的 Stopwatch tick。</param>
        /// <returns>单条 RPC 完成任务。</returns>
        private async MTask RunRpcCallAsync(string sessionId, int sequence, long startedTicks)
        {
            try
            {
                DemoRpcResponse response = await network.CallAsync<DemoRpcRequest, DemoRpcResponse>(sessionId, new DemoRpcRequest
                {
                    Payload = BuildNormalContent(RpcPrefix, sequence)
                });
                if (response == null || response.Code != 0)
                {
                    rpcFailureCount++;
                    return;
                }

                rpcReceivedCount++;
                rpcLatency.Add(Stopwatch.GetTimestamp() - startedTicks);
            }
            catch (Exception)
            {
                rpcFailureCount++;
            }
            finally
            {
                rpcOutstandingCount--;
            }
        }

        /// <summary>
        /// 在 RPC 样本执行期间定期输出已发起、已收到、失败与待完成数量，便于真机观察是否仍有进度。
        /// </summary>
        /// <param name="transport">当前传输名称。</param>
        /// <param name="repeat">当前 RPC 样本重复序号。</param>
        /// <param name="nextProgressLogTime">下次允许输出进度的时间点；输出后会推进该时间点。</param>
        private void ReportRpcProgressIfDue(string transport, int repeat, ref double nextProgressLogTime)
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (now < nextProgressLogTime)
            {
                return;
            }

            nextProgressLogTime = now + RpcProgressLogIntervalSeconds;
            statusMessage = $"NETWORK_BENCHMARK: transport:{transport} stage:rpc-r{repeat} sent:{rpcSentCount} received:{rpcReceivedCount} failed:{rpcFailureCount} outstanding:{rpcOutstandingCount} results:{results.Count}";
            Debug.Log(statusMessage);
        }

        /// <summary>
        /// 判断当前客户端会话是否仍保留足够的可靠出站队列空间，可继续启动一批 RPC。
        /// </summary>
        /// <param name="sessionId">需要检查的客户端逻辑会话标识。</param>
        /// <returns>会话已连接且可靠队列低于调度高水位时返回 true。</returns>
        private bool CanLaunchRpc(string sessionId)
        {
            NetworkSession session = network.GetSession(sessionId);
            return session != null && session.IsConnected && session.GetOutboundQueueSnapshot().ReliablePacketCount < RpcReliableQueueHighWatermark;
        }

        /// <summary>
        /// 根据命令行参数解析本次压测的场景范围；TCP、UDP 专项和剩余普通消息快速回归优先于 RPC 范围。
        /// </summary>
        /// <returns>当前启动应执行的压测范围。</returns>
        private static BenchmarkRunProfile ResolveRunProfile()
        {
            if (HasCommandLineArgument(TcpNormalQuickArgument))
            {
                return BenchmarkRunProfile.TcpNormalQuick;
            }

            if (HasCommandLineArgument(UdpNormalQuickArgument))
            {
                return BenchmarkRunProfile.UdpNormalQuick;
            }

            if (HasCommandLineArgument(MediumNormalDiagnosticArgument))
            {
                return BenchmarkRunProfile.MediumNormalDiagnostic;
            }

            if (HasCommandLineArgument(RemainingNormalQuickArgument))
            {
                return BenchmarkRunProfile.RemainingNormalQuick;
            }

            if (HasCommandLineArgument(RpcQuickArgument))
            {
                return BenchmarkRunProfile.RpcQuick;
            }

            return HasCommandLineArgument(RpcOnlyArgument) ? BenchmarkRunProfile.RpcOnly : BenchmarkRunProfile.Full;
        }

        /// <summary>
        /// 根据当前消息计数和队列/帧诊断数据创建一条可导出的结果。
        /// </summary>
        /// <param name="transport">传输名称。</param>
        /// <param name="scenario">场景名称。</param>
        /// <param name="targetRateOrConcurrency">正常消息目标速率或 RPC 并发度。</param>
        /// <param name="repeat">当前场景重复序号。</param>
        /// <param name="startedTicks">样本开始时的 Stopwatch tick。</param>
        /// <param name="sent">已成功发起或发送的消息数量。</param>
        /// <param name="received">已收到响应或业务回调的消息数量。</param>
        /// <param name="failures">发送、响应或处理失败数量。</param>
        /// <param name="latency">当前样本的延迟百分位汇总。</param>
        /// <returns>可序列化的一条压测结果。</returns>
        private NetworkBenchmarkRunResult CreateResult(
            string transport,
            string scenario,
            int targetRateOrConcurrency,
            int repeat,
            string sessionId,
            long startedTicks,
            int sent,
            int received,
            int failures,
            NetworkBenchmarkLatencySummary latency)
        {
            double elapsedMilliseconds = (Stopwatch.GetTimestamp() - startedTicks) * 1000d / Stopwatch.Frequency;
            NetworkIncomingQueueSnapshot queue = network.GetIncomingQueueSnapshot();
            var result = new NetworkBenchmarkRunResult
            {
                Transport = transport,
                Scenario = scenario,
                TargetRateOrConcurrency = targetRateOrConcurrency,
                Repeat = repeat,
                DurationMilliseconds = elapsedMilliseconds,
                SentCount = sent,
                OfferedCount = scenario == "NormalMessage" || scenario == "MainThreadHitch" ? normalOfferedCount : sent,
                RejectedCount = scenario == "NormalMessage" || scenario == "MainThreadHitch" ? normalRejectedCount : 0,
                ReceivedCount = received,
                FailureCount = failures,
                DroppedCount = Math.Max(0, sent - received - failures),
                DisconnectCount = disconnectCount,
                ThroughputPerSecond = elapsedMilliseconds <= 0d ? 0d : received * 1000d / elapsedMilliseconds,
                LatencySampleCount = latency.SampleCount,
                P50Milliseconds = latency.P50Milliseconds,
                P95Milliseconds = latency.P95Milliseconds,
                P99Milliseconds = latency.P99Milliseconds,
                MaxLatencyMilliseconds = latency.MaxMilliseconds,
                PeakQueuePacketCount = queue.PeakPendingPacketCount,
                PeakQueueByteCount = queue.PeakPendingByteCount,
                QueueProcessedPacketCount = queue.ProcessedPacketCount,
                QueueRejectedPacketCount = queue.RejectedPacketCount,
                MaxPacketProcessMilliseconds = queue.MaxPacketProcessMilliseconds,
                IncomingPacketProcessP50Milliseconds = queue.PacketProcessP50Milliseconds,
                IncomingPacketProcessP95Milliseconds = queue.PacketProcessP95Milliseconds,
                IncomingPacketProcessP99Milliseconds = queue.PacketProcessP99Milliseconds,
                IncomingQueueWaitSampleCount = queue.QueueWaitSampleCount,
                IncomingQueueWaitAverageMilliseconds = queue.AverageQueueWaitMilliseconds,
                IncomingQueueWaitMaxMilliseconds = queue.MaxQueueWaitMilliseconds,
                IncomingQueueWaitP50Milliseconds = queue.QueueWaitP50Milliseconds,
                IncomingQueueWaitP95Milliseconds = queue.QueueWaitP95Milliseconds,
                IncomingQueueWaitP99Milliseconds = queue.QueueWaitP99Milliseconds,
                NormalEventObservedCount = normalEventObservedCount,
                NormalEventRecognizedCount = normalEventRecognizedCount,
                NormalEventUnrecognizedCount = normalEventUnrecognizedCount,
                NormalEventOutOfRangeCount = normalEventOutOfRangeCount,
                NormalEventDuplicateCount = normalEventDuplicateCount,
                NormalEventMissingTimestampCount = normalEventMissingTimestampCount,
                MaxGcAllocatedBytesPerFrame = maxGcAllocatedBytesPerFrame
            };
            ApplyOutboundTimingMetrics(result, sessionId);
            return result;
        }

        /// <summary>
        /// 为当前样本启用并清空入站、客户端出站和服务端出站的分段耗时诊断。
        /// </summary>
        /// <param name="sessionId">当前客户端逻辑会话标识。</param>
        private void ResetTimingMetrics(string sessionId)
        {
            network.SetIncomingQueueTimingMetricsEnabled(true);
            network.ResetIncomingQueueMetrics();

            NetworkSession clientSession = network.GetSession(sessionId);
            clientSession?.SetOutboundTimingMetricsEnabled(true);
            clientSession?.SetTransportDiagnosticsEnabled(true);

            List<NetworkSession> serverSessions = network.GetServerSessionsSnapshot();
            for (int index = 0; index < serverSessions.Count; index++)
            {
                serverSessions[index].SetOutboundTimingMetricsEnabled(true);
                serverSessions[index].SetTransportDiagnosticsEnabled(true);
            }
        }

        /// <summary>
        /// 将当前客户端与全部本机服务端会话的出站分段耗时写入压测结果。
        /// </summary>
        /// <param name="result">需要补充诊断字段的当前压测结果。</param>
        /// <param name="sessionId">当前客户端逻辑会话标识。</param>
        private void ApplyOutboundTimingMetrics(NetworkBenchmarkRunResult result, string sessionId)
        {
            NetworkSession clientSession = network.GetSession(sessionId);
            if (clientSession != null)
            {
                NetworkOutboundQueueSnapshot client = clientSession.GetOutboundQueueSnapshot();
                NetworkTransportSendSnapshot clientTransport = clientSession.GetTransportSendSnapshot();
                result.ClientOutboundTimingSampleCount = client.TimingSampleCount;
                result.ClientTransportWriteCount = client.TransportWriteCount;
                result.ClientOutboundQueueWaitAverageMilliseconds = client.AverageQueueWaitMilliseconds;
                result.ClientOutboundQueueWaitMaxMilliseconds = client.MaxQueueWaitMilliseconds;
                result.ClientTransportSendAverageMilliseconds = client.AverageTransportSendMilliseconds;
                result.ClientTransportSendMaxMilliseconds = client.MaxTransportSendMilliseconds;
                result.ClientSocketSendOperationCount = clientTransport.SendOperationCount;
                result.ClientSocketSendOperationAverageMilliseconds = clientTransport.AverageSendOperationMilliseconds;
                result.ClientSocketSendOperationMaxMilliseconds = clientTransport.MaxSendOperationMilliseconds;
            }

            long serverSampleCount = 0;
            double serverQueueWaitTotalMilliseconds = 0d;
            double serverTransportSendTotalMilliseconds = 0d;
            double serverQueueWaitMaxMilliseconds = 0d;
            double serverTransportSendMaxMilliseconds = 0d;
            long serverFramedPacketCount = 0;
            long serverDispatchedPacketCount = 0;
            long serverReceiveOperationCount = 0;
            double serverReceiveOperationTotalMilliseconds = 0d;
            double serverReceiveOperationMaxMilliseconds = 0d;
            List<NetworkSession> serverSessions = network.GetServerSessionsSnapshot();
            for (int index = 0; index < serverSessions.Count; index++)
            {
                NetworkSession serverSession = serverSessions[index];
                NetworkOutboundQueueSnapshot server = serverSession.GetOutboundQueueSnapshot();
                NetworkTransportReceiveSnapshot receive = serverSession.GetTransportReceiveSnapshot();
                long samples = server.TimingSampleCount;
                serverSampleCount += samples;
                serverFramedPacketCount += receive.FramedPacketCount;
                serverDispatchedPacketCount += receive.DispatchedPacketCount;
                serverReceiveOperationCount += receive.ReceiveOperationCount;
                serverReceiveOperationTotalMilliseconds += receive.AverageReceiveOperationMilliseconds * receive.ReceiveOperationCount;
                serverReceiveOperationMaxMilliseconds = Math.Max(
                    serverReceiveOperationMaxMilliseconds,
                    receive.MaxReceiveOperationMilliseconds);
                serverQueueWaitTotalMilliseconds += server.AverageQueueWaitMilliseconds * samples;
                serverTransportSendTotalMilliseconds += server.AverageTransportSendMilliseconds * samples;
                serverQueueWaitMaxMilliseconds = Math.Max(serverQueueWaitMaxMilliseconds, server.MaxQueueWaitMilliseconds);
                serverTransportSendMaxMilliseconds = Math.Max(serverTransportSendMaxMilliseconds, server.MaxTransportSendMilliseconds);
            }

            result.ServerOutboundTimingSampleCount = serverSampleCount;
            result.ServerOutboundQueueWaitAverageMilliseconds = serverSampleCount == 0 ? 0d : serverQueueWaitTotalMilliseconds / serverSampleCount;
            result.ServerOutboundQueueWaitMaxMilliseconds = serverQueueWaitMaxMilliseconds;
            result.ServerTransportSendAverageMilliseconds = serverSampleCount == 0 ? 0d : serverTransportSendTotalMilliseconds / serverSampleCount;
            result.ServerTransportSendMaxMilliseconds = serverTransportSendMaxMilliseconds;
            result.ServerTransportFramedPacketCount = serverFramedPacketCount;
            result.ServerTransportDispatchedPacketCount = serverDispatchedPacketCount;
            result.ServerTransportReceiveOperationCount = serverReceiveOperationCount;
            result.ServerTransportReceiveOperationAverageMilliseconds = serverReceiveOperationCount == 0
                ? 0d
                : serverReceiveOperationTotalMilliseconds / serverReceiveOperationCount;
            result.ServerTransportReceiveOperationMaxMilliseconds = serverReceiveOperationMaxMilliseconds;
        }

        /// <summary>
        /// 清空当前正常消息样本的计数和预分配标记数组。
        /// </summary>
        private void ResetNormalSample()
        {
            normalSentCount = 0;
            normalOfferedCount = 0;
            normalRejectedCount = 0;
            normalReceivedCount = 0;
            normalFailureCount = 0;
            normalEventObservedCount = 0;
            normalEventRecognizedCount = 0;
            normalEventUnrecognizedCount = 0;
            normalEventOutOfRangeCount = 0;
            normalEventDuplicateCount = 0;
            normalEventMissingTimestampCount = 0;
            disconnectCount = 0;
            normalLatency.Reset();
            Array.Clear(normalSentTicks, 0, normalSentTicks.Length);
            Array.Clear(normalReceivedFlags, 0, normalReceivedFlags.Length);
        }

        /// <summary>
        /// 清空当前正常消息预热阶段的接受与业务到达计数。
        /// </summary>
        private void ResetWarmupSample()
        {
            warmupAcceptedCount = 0;
            warmupReceivedCount = 0;
        }

        /// <summary>
        /// 清空当前 RPC 样本的计数与延迟统计。
        /// </summary>
        private void ResetRpcSample()
        {
            rpcSentCount = 0;
            rpcReceivedCount = 0;
            rpcFailureCount = 0;
            rpcOutstandingCount = 0;
            disconnectCount = 0;
            rpcLatency.Reset();
        }

        /// <summary>
        /// 记录一轮压测的质量失败，但不中断后续传输采样，使导出的报告保留完整诊断证据。
        /// </summary>
        /// <param name="transport">当前报告使用的传输名称。</param>
        /// <param name="result">已汇总的当前压测样本。</param>
        private void RecordQualityFailure(string transport, NetworkBenchmarkRunResult result)
        {
            bool hitchRecoveryExceeded = result.Scenario == "MainThreadHitch" && result.QueueRecoveryMilliseconds > 50d;
            bool mediumLatencyExceeded = result.Scenario == "NormalMessage"
                && result.TargetRateOrConcurrency == MediumNormalMessageRate
                && result.P99Milliseconds > MaximumMediumNormalMessageP99Milliseconds;
            if (!string.IsNullOrEmpty(qualityFailureMessage)
                || (result.FailureCount == 0
                    && result.DroppedCount == 0
                    && result.RejectedCount == 0
                    && result.QueueRejectedPacketCount == 0
                    && result.DisconnectCount == 0
                    && !hitchRecoveryExceeded
                    && !mediumLatencyExceeded))
            {
                return;
            }

            qualityFailureMessage = $"transport:{transport} scenario:{result.Scenario} repeat:{result.Repeat} sent:{result.SentCount} received:{result.ReceivedCount} failed:{result.FailureCount} dropped:{result.DroppedCount} rejected:{result.RejectedCount} incomingRejected:{result.QueueRejectedPacketCount} disconnect:{result.DisconnectCount} p99:{result.P99Milliseconds:F3}ms recovery:{result.QueueRecoveryMilliseconds:F3}ms";
        }

        /// <summary>
        /// 启动每帧 GC Allocated In Frame 采样，并清空当前样本的峰值。
        /// </summary>
        private void StartFrameMetrics()
        {
            StopFrameMetrics();
            maxGcAllocatedBytesPerFrame = 0;
            gcAllocatedRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            gcAllocatedRecorderRunning = gcAllocatedRecorder.Valid;
            collectingFrameMetrics = true;
        }

        /// <summary>
        /// 停止并释放当前每帧 GC 采样器。
        /// </summary>
        private void StopFrameMetrics()
        {
            collectingFrameMetrics = false;
            if (gcAllocatedRecorderRunning)
            {
                gcAllocatedRecorder.Dispose();
                gcAllocatedRecorderRunning = false;
            }
        }

        /// <summary>
        /// 等待当前正常消息样本全部收到业务 Handler 通知，或在超时后继续输出丢包统计。
        /// </summary>
        /// <param name="timeoutSeconds">等待排空的最长时间。</param>
        /// <returns>等待结束任务。</returns>
        private async MTask WaitForNormalMessagesAsync(double timeoutSeconds)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + timeoutSeconds;
            while (normalReceivedCount < normalSentCount && Time.realtimeSinceStartupAsDouble < deadline)
            {
                await MTask.Yield();
            }

            await WaitForQueueToDrainAsync(Math.Max(0d, deadline - Time.realtimeSinceStartupAsDouble));
        }

        /// <summary>
        /// 等待当前预热阶段所有已接受的消息抵达真实 Handler 观察点，避免仍在 TCP 或应用链路中的预热流量进入正式样本。
        /// </summary>
        /// <param name="transport">当前报告使用的传输名称。</param>
        /// <param name="rate">当前正式样本的目标每秒发送消息数。</param>
        /// <param name="repeat">当前速率的重复序号。</param>
        /// <returns>全部预热消息完成业务处理时完成的任务。</returns>
        private async MTask WaitForWarmupMessagesAsync(string transport, int rate, int repeat)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + WarmupDrainTimeoutSeconds;
            while (warmupReceivedCount < warmupAcceptedCount && Time.realtimeSinceStartupAsDouble < deadline)
            {
                await MTask.Yield();
            }

            if (warmupReceivedCount < warmupAcceptedCount)
            {
                throw new TimeoutException($"transport:{transport} stage:normal-warmup-drain-{rate}-r{repeat} accepted:{warmupAcceptedCount} received:{warmupReceivedCount} timeout:{WarmupDrainTimeoutSeconds:0.###}s");
            }

            await WaitForQueueToDrainAsync(NormalDrainTimeoutSeconds);
        }

        /// <summary>
        /// 等待 NetworkService 的当前收包队列归零，超时后保留快照供报告诊断。
        /// </summary>
        /// <param name="timeoutSeconds">等待队列排空的最长时间。</param>
        /// <returns>等待结束任务。</returns>
        private async MTask WaitForQueueToDrainAsync(double timeoutSeconds)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + timeoutSeconds;
            while (network.GetIncomingQueueSnapshot().PendingPacketCount > 0 && Time.realtimeSinceStartupAsDouble < deadline)
            {
                await MTask.Yield();
            }
        }

        /// <summary>
        /// 等待一个条件在限定时间内成立，超时时附带传输与阶段信息抛出异常。
        /// </summary>
        /// <param name="condition">需要满足的条件。</param>
        /// <param name="transport">当前传输名称。</param>
        /// <param name="stage">当前阶段名称。</param>
        /// <param name="timeout">最长等待时间。</param>
        /// <returns>条件满足后的完成任务。</returns>
        private async MTask WaitForConditionAsync(Func<bool> condition, string transport, string stage, TimeSpan timeout)
        {
            DateTime deadline = DateTime.UtcNow + timeout;
            while (!condition())
            {
                if (DateTime.UtcNow >= deadline)
                {
                    throw new TimeoutException($"transport:{transport} stage:{stage} timeout:{timeout.TotalSeconds:0.###}s");
                }

                await MTask.Yield();
            }
        }

        /// <summary>
        /// 处理真实 DemoNormalHandler 发布的业务事件，并提取当前正常消息的固定宽度序号。
        /// </summary>
        /// <param name="@event">业务 Handler 已处理普通消息后发布的事件。</param>
        private void HandleNetworkMessage(DemoMessageReceivedEvent @event)
        {
            normalEventObservedCount++;
            if (@event == null)
            {
                normalEventUnrecognizedCount++;
                return;
            }

            if (TryReadBenchmarkSequence(@event.Message, WarmupPrefix, out _))
            {
                warmupReceivedCount++;
                normalEventUnrecognizedCount++;
                return;
            }

            if (!TryReadBenchmarkSequence(@event.Message, NormalPrefix, out int sequence))
            {
                normalEventUnrecognizedCount++;
                return;
            }

            normalEventRecognizedCount++;
            if (sequence < 0 || sequence >= normalSentCount)
            {
                normalEventOutOfRangeCount++;
                return;
            }

            if (normalReceivedFlags[sequence] != 0)
            {
                normalEventDuplicateCount++;
                return;
            }

            long sentTicks = normalSentTicks[sequence];
            if (sentTicks <= 0)
            {
                normalEventMissingTimestampCount++;
                return;
            }

            normalReceivedFlags[sequence] = 1;
            normalReceivedCount++;
            normalLatency.Add(Stopwatch.GetTimestamp() - sentTicks);
        }

        /// <summary>
        /// 从 Handler 格式化后的文本中查找指定基准标记并读取固定宽度序号。
        /// </summary>
        /// <param name="message">DemoNormalHandler 生成的业务事件文本。</param>
        /// <param name="prefix">需要匹配的正式或预热消息基准标记。</param>
        /// <param name="sequence">解析成功时得到的零基序号。</param>
        /// <returns>文本包含有效指定基准标记时返回 true。</returns>
        private static bool TryReadBenchmarkSequence(string message, string prefix, out int sequence)
        {
            sequence = 0;
            if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(prefix))
            {
                return false;
            }

            int start = message.IndexOf(prefix, StringComparison.Ordinal);
            if (start < 0)
            {
                return false;
            }

            int digitStart = start + prefix.Length;
            if (digitStart + NormalSequenceDigits >= message.Length)
            {
                return false;
            }

            for (int index = 0; index < NormalSequenceDigits; index++)
            {
                char character = message[digitStart + index];
                if (character < '0' || character > '9')
                {
                    return false;
                }

                sequence = sequence * 10 + character - '0';
            }

            return message[digitStart + NormalSequenceDigits] == '|';
        }

        /// <summary>
        /// 创建带固定宽度序号和固定业务正文的基准协议内容。
        /// </summary>
        /// <param name="prefix">区分正常、预热或 RPC 消息的固定前缀。</param>
        /// <param name="sequence">需要编码的非负序号。</param>
        /// <returns>长度稳定、可在正常消息事件中识别的协议正文。</returns>
        private static string BuildNormalContent(string prefix, int sequence)
        {
            return string.Concat(prefix, sequence.ToString($"D{NormalSequenceDigits}", CultureInfo.InvariantCulture), "|", FixedPayload);
        }

        /// <summary>
        /// 记录当前传输与阶段，便于从 Player 日志定位长时间压测进度。
        /// </summary>
        /// <param name="transport">当前传输名称。</param>
        /// <param name="stage">当前阶段名称。</param>
        private void ReportStage(string transport, string stage)
        {
            statusMessage = $"NETWORK_BENCHMARK: transport:{transport} stage:{stage} results:{results.Count}";
            Debug.Log(statusMessage);
        }

        /// <summary>
        /// 当本机服务端创建逻辑会话时递增计数，以确认连接已进入业务层。
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
        /// 记录当前样本期间观察到的服务端逻辑会话断开次数。
        /// </summary>
        /// <param name="sessionId">已断开的服务端逻辑会话标识。</param>
        private void HandleServerSessionClosed(string sessionId)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                disconnectCount++;
            }
        }

        /// <summary>
        /// 将失败条件转换为带有传输和阶段上下文的异常。
        /// </summary>
        /// <param name="condition">需要为 true 的条件。</param>
        /// <param name="transport">当前传输名称。</param>
        /// <param name="stage">当前阶段名称。</param>
        /// <param name="message">失败原因。</param>
        private static void Ensure(bool condition, string transport, string stage, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"transport:{transport} stage:{stage} {message}");
            }
        }

        /// <summary>
        /// 在完成、失败或对象销毁前解除事件订阅并停止所有本机监听。
        /// </summary>
        private void CleanupNetworkState()
        {
            if (network != null)
            {
                network.OnServerSessionCreated -= HandleServerSessionCreated;
                network.OnServerSessionClosed -= HandleServerSessionClosed;
                if (subscribedToNetworkEvents)
                {
                    networkMessageSubscription.Dispose();
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

            if (logSwitchCaptured)
            {
                LogSwitch.EnableLog = previousEnableLog;
                LogSwitch.EnablePayloadLog = previousEnablePayloadLog;
                logSwitchCaptured = false;
            }

            network = null;
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 在对象销毁时停止采样并释放网络引用，避免中断的压测留下会话或订阅。
        /// </summary>
        protected override void OnDestroy()
        {
            StopFrameMetrics();
            CleanupNetworkState();
            base.OnDestroy();
        }

        #endregion
    }

    /// <summary>
    /// 表示一条可导出的网络基准运行结果。
    /// </summary>
    [Serializable]
    public sealed class NetworkBenchmarkRunResult
    {
        #region Public 公共成员

        /// <summary>传输名称。</summary>
        public string Transport;
        /// <summary>压测场景名称。</summary>
        public string Scenario;
        /// <summary>正常消息目标速率或 RPC 并发度。</summary>
        public int TargetRateOrConcurrency;
        /// <summary>同一场景的重复运行序号。</summary>
        public int Repeat;
        /// <summary>样本总持续时间，单位为毫秒。</summary>
        public double DurationMilliseconds;
        /// <summary>已发起发送或请求的消息数量。</summary>
        public int SentCount;
        /// <summary>普通消息场景尝试提交到出站队列的总数量。</summary>
        public int OfferedCount;
        /// <summary>普通消息场景被有界出站队列拒绝的总数量。</summary>
        public int RejectedCount;
        /// <summary>已收到业务回调或 RPC 响应的消息数量。</summary>
        public int ReceivedCount;
        /// <summary>发送、响应或处理失败数量。</summary>
        public int FailureCount;
        /// <summary>发送后未收到成功或失败结果的消息数量。</summary>
        public int DroppedCount;
        /// <summary>样本期间观察到的服务端逻辑会话断开次数。</summary>
        public int DisconnectCount;
        /// <summary>按已接收消息计算的每秒吞吐量。</summary>
        public double ThroughputPerSecond;
        /// <summary>参与延迟百分位计算的样本数量。</summary>
        public int LatencySampleCount;
        /// <summary>P50 端到端延迟，单位为毫秒。</summary>
        public double P50Milliseconds;
        /// <summary>P95 端到端延迟，单位为毫秒。</summary>
        public double P95Milliseconds;
        /// <summary>P99 端到端延迟，单位为毫秒。</summary>
        public double P99Milliseconds;
        /// <summary>最大端到端延迟，单位为毫秒。</summary>
        public double MaxLatencyMilliseconds;
        /// <summary>收包队列积压包数量峰值。</summary>
        public long PeakQueuePacketCount;
        /// <summary>收包队列积压字节数峰值。</summary>
        public long PeakQueueByteCount;
        /// <summary>当前样本由主线程处理的收包总数。</summary>
        public long QueueProcessedPacketCount;
        /// <summary>
        /// 当前样本入站固定队列因容量不足拒绝的数据包总数。
        /// RPC 样本出现该值时，不能作为可靠性通过结果。
        /// </summary>
        public long QueueRejectedPacketCount;
        /// <summary>
        /// 单包主线程处理最大耗时，单位为毫秒。
        /// </summary>
        public double MaxPacketProcessMilliseconds;
        /// <summary>
        /// 主线程处理单个收包的 P50 耗时，单位为毫秒。
        /// </summary>
        public double IncomingPacketProcessP50Milliseconds;
        /// <summary>
        /// 主线程处理单个收包的 P95 耗时，单位为毫秒。
        /// </summary>
        public double IncomingPacketProcessP95Milliseconds;
        /// <summary>
        /// 主线程处理单个收包的 P99 耗时，单位为毫秒。
        /// </summary>
        public double IncomingPacketProcessP99Milliseconds;
        /// <summary>
        /// 入站队列等待耗时的有效样本数量；包含本机客户端与服务端两个方向。
        /// </summary>
        public long IncomingQueueWaitSampleCount;
        /// <summary>
        /// 网络线程入队到主线程开始处理的平均等待时间，单位为毫秒；包含本机客户端与服务端两个方向。
        /// </summary>
        public double IncomingQueueWaitAverageMilliseconds;
        /// <summary>
        /// 网络线程入队到主线程开始处理的最大等待时间，单位为毫秒；包含本机客户端与服务端两个方向。
        /// </summary>
        public double IncomingQueueWaitMaxMilliseconds;
        /// <summary>
        /// 网络线程入队到主线程开始处理的 P50 等待时间，单位为毫秒。
        /// </summary>
        public double IncomingQueueWaitP50Milliseconds;
        /// <summary>
        /// 网络线程入队到主线程开始处理的 P95 等待时间，单位为毫秒。
        /// </summary>
        public double IncomingQueueWaitP95Milliseconds;
        /// <summary>
        /// 网络线程入队到主线程开始处理的 P99 等待时间，单位为毫秒。
        /// </summary>
        public double IncomingQueueWaitP99Milliseconds;
        /// <summary>本机服务端 TCP 已完成完整长度帧读取的包数量；非 TCP 传输为零。</summary>
        public long ServerTransportFramedPacketCount;
        /// <summary>本机服务端 TCP 已完成收包回调派发的包数量；非 TCP 传输为零。</summary>
        public long ServerTransportDispatchedPacketCount;
        /// <summary>
        /// 本机服务端 TCP 底层 Socket 接收操作完成次数；一个完整长度帧通常至少对应包头和正文两次接收操作。
        /// 非 TCP 传输为零。
        /// </summary>
        public long ServerTransportReceiveOperationCount;
        /// <summary>
        /// 本机服务端 TCP 单次底层 Socket 接收操作平均等待时间，单位为毫秒；非 TCP 传输为零。
        /// </summary>
        public double ServerTransportReceiveOperationAverageMilliseconds;
        /// <summary>
        /// 本机服务端 TCP 单次底层 Socket 接收操作最大等待时间，单位为毫秒；非 TCP 传输为零。
        /// </summary>
        public double ServerTransportReceiveOperationMaxMilliseconds;
        /// <summary>样本期间压测订阅者观察到的全部业务事件数量。</summary>
        public int NormalEventObservedCount;
        /// <summary>样本期间被识别为 MCBENCH-N 普通消息的业务事件数量。</summary>
        public int NormalEventRecognizedCount;
        /// <summary>样本期间未包含 MCBENCH-N 固定序号的业务事件数量。</summary>
        public int NormalEventUnrecognizedCount;
        /// <summary>样本期间序号未登记或超过已发送范围的普通消息事件数量。</summary>
        public int NormalEventOutOfRangeCount;
        /// <summary>样本期间已被记入接收结果的重复普通消息事件数量。</summary>
        public int NormalEventDuplicateCount;
        /// <summary>样本期间序号已登记但发送时间尚未写入的普通消息事件数量。</summary>
        public int NormalEventMissingTimestampCount;
        /// <summary>
        /// 客户端会话出站分段耗时的有效样本数量。
        /// </summary>
        public long ClientOutboundTimingSampleCount;
        /// <summary>
        /// 客户端实际调用底层传输写入的次数；TCP 普通消息批量时小于已发送包数量。
        /// </summary>
        public long ClientTransportWriteCount;
        /// <summary>
        /// 客户端底层 Socket 发送操作完成次数；一个传输写入在部分写入时可能对应多次 Socket 操作。
        /// </summary>
        public long ClientSocketSendOperationCount;
        /// <summary>
        /// 客户端单次底层 Socket 发送操作平均等待时间，单位为毫秒。
        /// </summary>
        public double ClientSocketSendOperationAverageMilliseconds;
        /// <summary>
        /// 客户端单次底层 Socket 发送操作最大等待时间，单位为毫秒。
        /// </summary>
        public double ClientSocketSendOperationMaxMilliseconds;
        /// <summary>
        /// 客户端包进入出站队列到开始调用传输发送的平均等待时间，单位为毫秒。
        /// </summary>
        public double ClientOutboundQueueWaitAverageMilliseconds;
        /// <summary>
        /// 客户端包进入出站队列到开始调用传输发送的最大等待时间，单位为毫秒。
        /// </summary>
        public double ClientOutboundQueueWaitMaxMilliseconds;
        /// <summary>
        /// 客户端调用底层传输发送到完成的平均等待时间，单位为毫秒。
        /// </summary>
        public double ClientTransportSendAverageMilliseconds;
        /// <summary>
        /// 客户端调用底层传输发送到完成的最大等待时间，单位为毫秒。
        /// </summary>
        public double ClientTransportSendMaxMilliseconds;
        /// <summary>
        /// 本机服务端全部会话出站分段耗时的有效样本数量。
        /// </summary>
        public long ServerOutboundTimingSampleCount;
        /// <summary>
        /// 本机服务端包进入出站队列到开始调用传输发送的平均等待时间，单位为毫秒。
        /// </summary>
        public double ServerOutboundQueueWaitAverageMilliseconds;
        /// <summary>
        /// 本机服务端包进入出站队列到开始调用传输发送的最大等待时间，单位为毫秒。
        /// </summary>
        public double ServerOutboundQueueWaitMaxMilliseconds;
        /// <summary>
        /// 本机服务端调用底层传输发送到完成的平均等待时间，单位为毫秒。
        /// </summary>
        public double ServerTransportSendAverageMilliseconds;
        /// <summary>
        /// 本机服务端调用底层传输发送到完成的最大等待时间，单位为毫秒。
        /// </summary>
        public double ServerTransportSendMaxMilliseconds;
        /// <summary>样本期间 GC Allocated In Frame 的最大字节数。</summary>
        public long MaxGcAllocatedBytesPerFrame;
        /// <summary>主线程停顿场景的人为停顿时长；其他场景为零。</summary>
        public int HitchMilliseconds;
        /// <summary>主线程停顿后等待消息与队列恢复的耗时；其他场景为零。</summary>
        public double QueueRecoveryMilliseconds;

        #endregion
    }

    /// <summary>
    /// 将网络基准运行结果写入 Player 可导出的 JSON 与 CSV 文件。
    /// </summary>
    internal static class NetworkBenchmarkReportWriter
    {
        #region Private 私有成员

        private const string DirectoryName = "NetworkBenchmark"; // persistentDataPath 下保存基准报告的目录名。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 写入包含设备信息和全部样本的 JSON、CSV 报告。
        /// </summary>
        /// <param name="results">需要写入的全部压测样本。</param>
        /// <returns>本次报告所在目录。</returns>
        internal static string Write(IList<NetworkBenchmarkRunResult> results)
        {
            string directory = Path.Combine(Application.persistentDataPath, DirectoryName, DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(directory);
            var report = new NetworkBenchmarkReport
            {
                DeviceModel = SystemInfo.deviceModel,
                OperatingSystem = SystemInfo.operatingSystem,
                Platform = Application.platform.ToString(),
                BuildType = Debug.isDebugBuild ? "Development" : "Release",
                UnityVersion = Application.unityVersion,
                GeneratedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                Results = new List<NetworkBenchmarkRunResult>(results)
            };
            File.WriteAllText(Path.Combine(directory, "NetworkBenchmarkReport.json"), JsonUtility.ToJson(report, true), Encoding.UTF8);
            File.WriteAllText(Path.Combine(directory, "NetworkBenchmarkReport.csv"), BuildCsv(report.Results), Encoding.UTF8);
            return directory;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将全部样本转换为可以直接导入表格工具的 CSV 文本。
        /// </summary>
        /// <param name="results">需要导出的全部压测样本。</param>
        /// <returns>包含表头和全部样本行的 CSV 文本。</returns>
        private static string BuildCsv(IList<NetworkBenchmarkRunResult> results)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Transport,Scenario,TargetRateOrConcurrency,Repeat,DurationMilliseconds,SentCount,OfferedCount,RejectedCount,ReceivedCount,FailureCount,DroppedCount,DisconnectCount,ThroughputPerSecond,LatencySampleCount,P50Milliseconds,P95Milliseconds,P99Milliseconds,MaxLatencyMilliseconds,PeakQueuePacketCount,PeakQueueByteCount,QueueProcessedPacketCount,QueueRejectedPacketCount,MaxPacketProcessMilliseconds,IncomingPacketProcessP50Milliseconds,IncomingPacketProcessP95Milliseconds,IncomingPacketProcessP99Milliseconds,IncomingQueueWaitSampleCount,IncomingQueueWaitAverageMilliseconds,IncomingQueueWaitMaxMilliseconds,IncomingQueueWaitP50Milliseconds,IncomingQueueWaitP95Milliseconds,IncomingQueueWaitP99Milliseconds,ServerTransportFramedPacketCount,ServerTransportDispatchedPacketCount,ServerTransportReceiveOperationCount,ServerTransportReceiveOperationAverageMilliseconds,ServerTransportReceiveOperationMaxMilliseconds,NormalEventObservedCount,NormalEventRecognizedCount,NormalEventUnrecognizedCount,NormalEventOutOfRangeCount,NormalEventDuplicateCount,NormalEventMissingTimestampCount,ClientOutboundTimingSampleCount,ClientTransportWriteCount,ClientSocketSendOperationCount,ClientSocketSendOperationAverageMilliseconds,ClientSocketSendOperationMaxMilliseconds,ClientOutboundQueueWaitAverageMilliseconds,ClientOutboundQueueWaitMaxMilliseconds,ClientTransportSendAverageMilliseconds,ClientTransportSendMaxMilliseconds,ServerOutboundTimingSampleCount,ServerOutboundQueueWaitAverageMilliseconds,ServerOutboundQueueWaitMaxMilliseconds,ServerTransportSendAverageMilliseconds,ServerTransportSendMaxMilliseconds,MaxGcAllocatedBytesPerFrame,HitchMilliseconds,QueueRecoveryMilliseconds");
            for (int index = 0; index < results.Count; index++)
            {
                NetworkBenchmarkRunResult result = results[index];
                builder.Append(result.Transport).Append(',')
                    .Append(result.Scenario).Append(',')
                    .Append(result.TargetRateOrConcurrency).Append(',')
                    .Append(result.Repeat).Append(',')
                    .Append(result.DurationMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.SentCount).Append(',')
                    .Append(result.OfferedCount).Append(',')
                    .Append(result.RejectedCount).Append(',')
                    .Append(result.ReceivedCount).Append(',')
                    .Append(result.FailureCount).Append(',')
                    .Append(result.DroppedCount).Append(',')
                    .Append(result.DisconnectCount).Append(',')
                    .Append(result.ThroughputPerSecond.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.LatencySampleCount).Append(',')
                    .Append(result.P50Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.P95Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.P99Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.MaxLatencyMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.PeakQueuePacketCount).Append(',')
                    .Append(result.PeakQueueByteCount).Append(',')
                    .Append(result.QueueProcessedPacketCount).Append(',')
                    .Append(result.QueueRejectedPacketCount).Append(',')
                    .Append(result.MaxPacketProcessMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.IncomingPacketProcessP50Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.IncomingPacketProcessP95Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.IncomingPacketProcessP99Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.IncomingQueueWaitSampleCount).Append(',')
                    .Append(result.IncomingQueueWaitAverageMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.IncomingQueueWaitMaxMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.IncomingQueueWaitP50Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.IncomingQueueWaitP95Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.IncomingQueueWaitP99Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.ServerTransportFramedPacketCount).Append(',')
                    .Append(result.ServerTransportDispatchedPacketCount).Append(',')
                    .Append(result.ServerTransportReceiveOperationCount).Append(',')
                    .Append(result.ServerTransportReceiveOperationAverageMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.ServerTransportReceiveOperationMaxMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.NormalEventObservedCount).Append(',')
                    .Append(result.NormalEventRecognizedCount).Append(',')
                    .Append(result.NormalEventUnrecognizedCount).Append(',')
                    .Append(result.NormalEventOutOfRangeCount).Append(',')
                    .Append(result.NormalEventDuplicateCount).Append(',')
                    .Append(result.NormalEventMissingTimestampCount).Append(',')
                    .Append(result.ClientOutboundTimingSampleCount).Append(',')
                    .Append(result.ClientTransportWriteCount).Append(',')
                    .Append(result.ClientSocketSendOperationCount).Append(',')
                    .Append(result.ClientSocketSendOperationAverageMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.ClientSocketSendOperationMaxMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.ClientOutboundQueueWaitAverageMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.ClientOutboundQueueWaitMaxMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.ClientTransportSendAverageMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.ClientTransportSendMaxMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.ServerOutboundTimingSampleCount).Append(',')
                    .Append(result.ServerOutboundQueueWaitAverageMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.ServerOutboundQueueWaitMaxMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.ServerTransportSendAverageMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.ServerTransportSendMaxMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.MaxGcAllocatedBytesPerFrame).Append(',')
                    .Append(result.HitchMilliseconds).Append(',')
                    .Append(result.QueueRecoveryMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).AppendLine();
            }

            return builder.ToString();
        }

        #endregion
    }

    /// <summary>
    /// 表示 JSON 报告需要附带的运行设备信息与样本集合。
    /// </summary>
    [Serializable]
    internal sealed class NetworkBenchmarkReport
    {
        #region Public 公共成员

        /// <summary>运行 Player 的设备型号。</summary>
        public string DeviceModel;
        /// <summary>运行 Player 的操作系统信息。</summary>
        public string OperatingSystem;
        /// <summary>运行 Player 的 Unity 平台。</summary>
        public string Platform;
        /// <summary>Development 或 Release 构建标识。</summary>
        public string BuildType;
        /// <summary>运行 Player 的 Unity 版本。</summary>
        public string UnityVersion;
        /// <summary>报告生成时刻的 UTC ISO 8601 文本。</summary>
        public string GeneratedUtc;
        /// <summary>本次运行产生的全部压测样本。</summary>
        public List<NetworkBenchmarkRunResult> Results;

        #endregion
    }
}
