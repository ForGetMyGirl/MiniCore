using System;
using Cysharp.Threading.Tasks;
using MiniCore.Core;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using UnityEngine;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// 连接独立 Dedicated Server 的 KCP 客户端冒烟执行器，验证客户端入口、热更新 Handler 与跨进程业务链路。
    /// </summary>
    public sealed class DedicatedClientSmokeTestRunner : MonoBehaviour
    {
        #region Private 私有成员

        private const string SmokePrefix = "[dedicated-smoke] "; // 用于关联独立服务端业务日志的固定前缀。
        private const string SessionId = "dedicated-smoke-kcp"; // Dedicated Server 冒烟客户端会话标识。
        private const string ServerHostArgument = "-serverHost"; // 覆盖服务端地址的命令行参数。
        private const string ServerPortArgument = "-serverPort"; // 覆盖服务端端口的命令行参数。
        private const string DefaultServerHost = "127.0.0.1"; // 本机 Dedicated Server 的默认地址。
        private const int DefaultServerPort = 20000; // Dedicated Server 的默认 KCP 端口。
        private const uint KcpConv = 1001; // Dedicated Server 冒烟连接标识。
        private static readonly TimeSpan StageTimeout = TimeSpan.FromSeconds(5); // 单个验证阶段的最长等待时长。

        private readonly UniTaskCompletionSource<bool> completionSource = new UniTaskCompletionSource<bool>(); // 对外暴露的单次运行完成通知。
        private INetworkService network; // 已由客户端入口初始化并注册 Handler 的网络服务。
        private GUIStyle statusStyle; // Player 屏幕状态文本样式缓存。
        private bool heldNetworkReference; // 当前对象是否持有网络组件引用。
        private bool isRunning; // 是否已经启动一次冒烟流程。
        private bool isCompleted; // 冒烟流程是否已经结束。
        private bool isPassed; // 冒烟流程是否成功。
        private string statusMessage = "等待 Dedicated Server KCP 冒烟测试启动。"; // 当前屏幕与 Console 状态文本。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 启动 Dedicated Server 客户端自检的命令行参数。
        /// </summary>
        public const string RunArgument = "-dedicatedClientSmokeTest";

        /// <summary>
        /// Dedicated Server 客户端自检完成后按结果退出 Player 的命令行参数。
        /// </summary>
        public const string QuitArgument = "-dedicatedClientSmokeQuit";

        /// <summary>
        /// 获取冒烟流程是否已经结束。
        /// </summary>
        public bool IsCompleted => isCompleted;

        /// <summary>
        /// 获取冒烟流程是否全部通过。
        /// </summary>
        public bool IsPassed => isPassed;

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
        /// 运行一次独立 Dedicated Server 的 KCP 业务验证；重复调用会返回同一轮运行结果。
        /// </summary>
        /// <returns>KCP 连接、普通消息、RPC 与断连均通过时返回 true。</returns>
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
        /// 在带有 Dedicated Server 冒烟参数的 Player 中自动启动验证。
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
        /// 执行独立 Dedicated Server 的完整 KCP 冒烟流程并将成功或失败写入统一结果。
        /// </summary>
        private async UniTaskVoid RunInternalAsync()
        {
            try
            {
                string host = ReadServerHost();
                int port = ReadServerPort();
                network = Global.GetService<INetworkService>(this);
                heldNetworkReference = true;

                ReportStage("connect", host, port);
                bool connected = await network.ConnectKcpSessionAsync(SessionId, host, port, KcpConv, StageTimeout);
                Ensure(connected, "connect", "KCP 连接探测未收到心跳响应。");

                string normalContent = $"{SmokePrefix}normal";
                ReportStage("normal-message", host, port);
                await network.SendAsync(SessionId, new DemoNormalMessage { Content = normalContent });

                string rpcPayload = $"{SmokePrefix}rpc";
                ReportStage("rpc", host, port);
                DemoRpcResponse response = await network.CallAsync<DemoRpcRequest, DemoRpcResponse>(SessionId, new DemoRpcRequest { Payload = rpcPayload });
                Ensure(response.Code == 0 && response.Msg == "RPC响应成功" && response.Echo == rpcPayload, "rpc", $"RPC 响应未返回预期 Code、Msg 或 Echo。实际 Code:{response.Code} Msg:{response.Msg} Echo:{response.Echo}");

                ReportStage("disconnect-notice", host, port);
                await network.SendAsync(SessionId, new DisconnectNotice
                {
                    IsServerShutdown = false,
                    Reason = $"{SmokePrefix}close"
                });
                await UniTask.Delay(200);

                isPassed = true;
                statusMessage = "DEDICATED_CLIENT_SMOKE: PASS protocol:KCP";
                Debug.Log(statusMessage);
            }
            catch (Exception exception)
            {
                isPassed = false;
                statusMessage = $"DEDICATED_CLIENT_SMOKE: FAIL {exception.Message}";
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
        /// 读取命令行指定的 Dedicated Server 主机地址，未指定时使用本机回环地址。
        /// </summary>
        /// <returns>用于建立 KCP 连接的服务端主机地址。</returns>
        private static string ReadServerHost()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], ServerHostArgument, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(arguments[index + 1]))
                {
                    return arguments[index + 1];
                }
            }

            return DefaultServerHost;
        }

        /// <summary>
        /// 读取命令行指定的 Dedicated Server 端口，未指定时使用默认端口。
        /// </summary>
        /// <returns>用于建立 KCP 连接的服务端端口。</returns>
        private static int ReadServerPort()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (!string.Equals(arguments[index], ServerPortArgument, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (int.TryParse(arguments[index + 1], out int port) && port > 0 && port <= 65535)
                {
                    return port;
                }

                throw new ArgumentException($"{ServerPortArgument} 必须指定 1 到 65535 之间的端口。", ServerPortArgument);
            }

            return DefaultServerPort;
        }

        /// <summary>
        /// 更新当前验证阶段，并输出包含服务端地址和会话标识的 Console 日志。
        /// </summary>
        /// <param name="stage">当前验证阶段。</param>
        /// <param name="host">当前连接的服务端地址。</param>
        /// <param name="port">当前连接的服务端端口。</param>
        private void ReportStage(string stage, string host, int port)
        {
            statusMessage = $"DEDICATED_CLIENT_SMOKE: protocol:KCP stage:{stage} sessionId:{SessionId} server:{host}:{port}";
            Debug.Log(statusMessage);
        }

        /// <summary>
        /// 将失败条件转换为包含 KCP 阶段和客户端会话标识的异常。
        /// </summary>
        /// <param name="condition">需要成立的条件。</param>
        /// <param name="stage">当前验证阶段。</param>
        /// <param name="message">失败原因。</param>
        private static void Ensure(bool condition, string stage, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"protocol:KCP stage:{stage} sessionId:{SessionId} {message}");
            }
        }

        /// <summary>
        /// 在成功、失败或对象销毁前释放客户端 KCP 会话和组件引用。
        /// </summary>
        private void CleanupNetworkState()
        {
            if (network != null)
            {
                network.DisconnectSession(SessionId);
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
