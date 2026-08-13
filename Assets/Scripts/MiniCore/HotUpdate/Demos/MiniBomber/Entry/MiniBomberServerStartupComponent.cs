using System;
using MiniCore.Core;
using MiniCore.Eventing;
using MiniCore.HotUpdate;
using MiniCore.Threading;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber Dedicated Server 的权威运行时、KCP/WebSocket 监听与跨进程冒烟装配入口。
    /// </summary>
    public sealed class MiniBomberServerStartupComponent : MiniBomberStartupComponentBase
    {
        #region Public 公共成员

        /// <summary>
        /// 加载 MiniBomber 共享配置并在命令行指定端口上启动 KCP 与 WebSocket 权威服务器。
        /// </summary>
        /// <returns>服务器运行时和监听初始化完成任务。</returns>
        public async MTask InitializeAsync()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            await LoadConfigurationAsync();
            serverRuntime = Global.GetOrAdd<MiniBomberServerRuntimeComponent>(this);
            int serverPort = ReadServerPort(RuntimeConfig.ServerPort);
            await serverRuntime.InitializeAsync(RuntimeConfig, RuleConfig, MapDefinition, "0.0.0.0", serverPort);
            ConfigureSmokeTestIfRequested(serverPort);
        }

        #endregion

        #region Private 私有成员

        private const string DedicatedServerSmokeTestArgument = "-dedicatedServerSmokeTest"; // Dedicated Server 冒烟模式参数。
        private EventSubscription dedicatedServerSmokeSubscription; // Dedicated Server 冒烟业务事件订阅。
        private MiniBomberServerRuntimeComponent serverRuntime; // Dedicated Server 权威业务运行时。

        /// <summary>
        /// 在 Dedicated Server 冒烟模式下订阅业务事件并输出服务端就绪日志。
        /// </summary>
        /// <param name="serverPort">当前 KCP 与 WebSocket 监听使用的数值端口。</param>
        private void ConfigureSmokeTestIfRequested(int serverPort)
        {
            if (!NetworkSmokeTestRunner.HasCommandLineArgument(DedicatedServerSmokeTestArgument))
            {
                return;
            }

            IApplicationEventBus eventBus = Global.GetOrAddModule<IApplicationEventBus>(this);
            dedicatedServerSmokeSubscription = eventBus.Subscribe<DemoMessageReceivedEvent>(LogSmokeEvent);
            Debug.Log($"DEDICATED_SERVER_SMOKE: READY entry:{nameof(MiniCoreStartup)} port:{serverPort}");
        }

        /// <summary>
        /// 将业务 Handler 广播的消息镜像为 Dedicated Server 冒烟日志。
        /// </summary>
        /// <param name="@event">Handler 广播的强类型业务事件。</param>
        private static void LogSmokeEvent(DemoMessageReceivedEvent @event)
        {
            Debug.Log($"DEDICATED_SERVER_SMOKE: event:{@event.Message}");
        }

        /// <summary>
        /// 从命令行读取服务端监听端口。
        /// </summary>
        /// <param name="defaultPort">运行时配置指定的默认监听端口。</param>
        /// <returns>合法命令行端口；未指定或无效时返回配置端口。</returns>
        private static int ReadServerPort(int defaultPort)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], "-serverPort", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(arguments[index + 1], out int port) &&
                    port > 0 && port <= 65535)
                {
                    return port;
                }
            }

            return defaultPort;
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 解除冒烟订阅并释放服务端启动组件持有的业务运行时。
        /// </summary>
        protected override void OnDispose()
        {
            dedicatedServerSmokeSubscription.Dispose();
            serverRuntime = null;
            base.OnDispose();
        }

        #endregion
    }
}
