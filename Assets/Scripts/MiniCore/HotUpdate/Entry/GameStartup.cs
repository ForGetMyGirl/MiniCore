using MiniCore.Core;
using MiniCore.Demo.MiniBomber;
using MiniCore.Model;
using MiniCore.Threading;
using UnityEngine;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// 当前项目唯一的开发者自定义启动入口。
    /// 框架先装配项目启动配置中的服务与模块，再由此处选择运行形态和首个业务流程。
    /// </summary>
    public sealed class GameStartup : AGameStartup
    {
        #region Public 公共成员

        /// <summary>
        /// 根据当前运行形态进入网络测试、MiniBomber 客户端或 Dedicated Server 流程。
        /// </summary>
        /// <returns>选定业务入口初始化完成任务。</returns>
        public override async MTask StartAsync()
        {
            if (Application.isBatchMode)
            {
                serverStartup = Global.GetOrAdd<MiniBomberServerStartupComponent>(this);
                await serverStartup.InitializeAsync();
                return;
            }

            if (NetworkBenchmarkRunner.HasCommandLineArgument(NetworkBenchmarkRunner.RunArgument))
            {
                CreateNetworkBenchmarkRunnerIfRequested();
                return;
            }

            if (NetworkSmokeTestRunner.HasCommandLineArgument(NetworkSmokeTestRunner.RunArgument))
            {
                CreateNetworkSmokeTestRunnerIfRequested();
                return;
            }

            if (DedicatedClientSmokeTestRunner.HasCommandLineArgument(DedicatedClientSmokeTestRunner.RunArgument))
            {
                CreateDedicatedClientSmokeTestRunnerIfRequested();
                return;
            }

            clientStartup = Global.GetOrAdd<MiniBomberClientStartupComponent>(this);
            await clientStartup.InitializeAsync();
        }

        #endregion

        #region Private 私有成员

        private MiniBomberClientStartupComponent clientStartup; // 普通客户端 Demo 启动组件。
        private MiniBomberServerStartupComponent serverStartup; // Dedicated Server Demo 启动组件。

        /// <summary>
        /// 当客户端以网络冒烟参数启动时创建自动化验证组件。
        /// </summary>
        private static void CreateNetworkSmokeTestRunnerIfRequested()
        {
            if (!NetworkSmokeTestRunner.HasCommandLineArgument(NetworkSmokeTestRunner.RunArgument) ||
                UnityEngine.Object.FindObjectOfType<NetworkSmokeTestRunner>() != null)
            {
                return;
            }

            GameObject testObject = new GameObject("NetworkSmokeTestRunner");
            UnityEngine.Object.DontDestroyOnLoad(testObject);
            testObject.AddComponent<NetworkSmokeTestRunner>();
        }

        /// <summary>
        /// 当客户端以网络压测参数启动时创建无测试面板的自动化压测组件。
        /// </summary>
        private static void CreateNetworkBenchmarkRunnerIfRequested()
        {
            if (!NetworkBenchmarkRunner.HasCommandLineArgument(NetworkBenchmarkRunner.RunArgument) ||
                UnityEngine.Object.FindObjectOfType<NetworkBenchmarkRunner>() != null)
            {
                return;
            }

            GameObject testObject = new GameObject("NetworkBenchmarkRunner");
            UnityEngine.Object.DontDestroyOnLoad(testObject);
            testObject.AddComponent<NetworkBenchmarkRunner>();
        }

        /// <summary>
        /// 当客户端以 Dedicated Server 冒烟参数启动时创建跨进程 KCP 自检组件。
        /// </summary>
        private static void CreateDedicatedClientSmokeTestRunnerIfRequested()
        {
            if (!DedicatedClientSmokeTestRunner.HasCommandLineArgument(DedicatedClientSmokeTestRunner.RunArgument) ||
                UnityEngine.Object.FindObjectOfType<DedicatedClientSmokeTestRunner>() != null)
            {
                return;
            }

            GameObject testObject = new GameObject("DedicatedClientSmokeTestRunner");
            UnityEngine.Object.DontDestroyOnLoad(testObject);
            testObject.AddComponent<DedicatedClientSmokeTestRunner>();
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 释放当前 GameStartup 持有的具体业务启动组件。
        /// </summary>
        protected override void OnDispose()
        {
            clientStartup = null;
            serverStartup = null;
            Global.ReleaseAll(this);
            base.OnDispose();
        }

        #endregion
    }
}
