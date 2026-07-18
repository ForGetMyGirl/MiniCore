using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MiniCore.Core;
using MiniCore.Model;
using UnityEngine;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// 当前项目唯一的自定义启动入口。
    /// 框架会先装配编辑器配置的模块，再调用此类的 StartAsync；业务只需在这里编写额外启动行为。
    /// </summary>
    public sealed class GameStartup : AGameStartup
    {
        #region Public 公共成员

        /// <summary>
        /// 根据当前 Player 模式启动示例业务。
        /// 客户端创建测试面板；Dedicated Server 启动 KCP 监听。新项目可直接替换为自己的业务逻辑。
        /// </summary>
        /// <returns>项目启动完成任务。</returns>
        public override async Task StartAsync()
        {
            if (Application.isBatchMode)
            {
                await StartDedicatedServerAsync();
                ConfigureDedicatedServerSmokeTestIfRequested();
                return;
            }

            CreateScenePanel();
            CreateNetworkSmokeTestRunnerIfRequested();
            CreateDedicatedClientSmokeTestRunnerIfRequested();
        }

        #endregion

        #region Private 私有成员

        private const string DedicatedServerSmokeTestArgument = "-dedicatedServerSmokeTest"; // Dedicated Server 冒烟模式的命令行参数。

        /// <summary>
        /// 创建当前示例场景使用的多协议测试面板。
        /// </summary>
        private static void CreateScenePanel()
        {
            if (UnityEngine.Object.FindObjectOfType<MultiProtocolTestPanel>() != null)
            {
                return;
            }

            GameObject panelObject = new GameObject("MultiProtocolTestPanel");
            UnityEngine.Object.DontDestroyOnLoad(panelObject);
            panelObject.AddComponent<MultiProtocolTestPanel>();
        }

        /// <summary>
        /// 当客户端以网络冒烟参数启动时创建自动化验证组件。
        /// </summary>
        private static void CreateNetworkSmokeTestRunnerIfRequested()
        {
            if (!NetworkSmokeTestRunner.HasCommandLineArgument(NetworkSmokeTestRunner.RunArgument) || UnityEngine.Object.FindObjectOfType<NetworkSmokeTestRunner>() != null)
            {
                return;
            }

            GameObject testObject = new GameObject("NetworkSmokeTestRunner");
            UnityEngine.Object.DontDestroyOnLoad(testObject);
            testObject.AddComponent<NetworkSmokeTestRunner>();
        }

        /// <summary>
        /// 当客户端以 Dedicated Server 冒烟参数启动时创建跨进程 KCP 自检组件。
        /// </summary>
        private static void CreateDedicatedClientSmokeTestRunnerIfRequested()
        {
            if (!DedicatedClientSmokeTestRunner.HasCommandLineArgument(DedicatedClientSmokeTestRunner.RunArgument) || UnityEngine.Object.FindObjectOfType<DedicatedClientSmokeTestRunner>() != null)
            {
                return;
            }

            GameObject testObject = new GameObject("DedicatedClientSmokeTestRunner");
            UnityEngine.Object.DontDestroyOnLoad(testObject);
            testObject.AddComponent<DedicatedClientSmokeTestRunner>();
        }

        /// <summary>
        /// 为 Dedicated Server 启动命令行指定端口上的 KCP 监听。
        /// </summary>
        /// <returns>KCP 监听完成启动的任务。</returns>
        private async Task StartDedicatedServerAsync()
        {
            NetworkMessageComponent network = Global.Get<NetworkMessageComponent>(this);
            await network.StartKcpServerAsync("0.0.0.0", ReadServerPort()).AsTask();
        }

        /// <summary>
        /// 在 Dedicated Server 冒烟模式下订阅业务事件并输出服务端就绪日志。
        /// </summary>
        private static void ConfigureDedicatedServerSmokeTestIfRequested()
        {
            if (!NetworkSmokeTestRunner.HasCommandLineArgument(DedicatedServerSmokeTestArgument))
            {
                return;
            }

            EventCenter.AddListener<string>(HotEvent.KcpTestMessage, LogDedicatedServerSmokeEvent);
            Debug.Log($"DEDICATED_SERVER_SMOKE: READY entry:{nameof(MiniCoreStartup)} port:{ReadServerPort()}");
        }

        /// <summary>
        /// 将业务 Handler 广播的消息镜像为 Dedicated Server 冒烟日志。
        /// </summary>
        /// <param name="message">Handler 广播的业务消息文本。</param>
        private static void LogDedicatedServerSmokeEvent(string message)
        {
            Debug.Log($"DEDICATED_SERVER_SMOKE: event:{message}");
        }

        /// <summary>
        /// 从命令行读取服务端监听端口。
        /// </summary>
        /// <returns>合法端口；未指定或无效时返回 20000。</returns>
        private static int ReadServerPort()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (string.Equals(arguments[i], "-serverPort", StringComparison.OrdinalIgnoreCase) && int.TryParse(arguments[i + 1], out int port) && port > 0 && port <= 65535)
                {
                    return port;
                }
            }

            return 20000;
        }

        #endregion
    }
}
