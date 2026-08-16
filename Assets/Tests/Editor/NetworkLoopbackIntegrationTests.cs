using System.Collections;
using MiniCore.Core;
using MiniCore.Eventing;
using MiniCore.Service;
using MiniCore.HotUpdate;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniCore.EditorTests
{
    /// <summary>
    /// 使用真实 HotUpdate Handler、Protobuf 和 TCP/KCP/UDP 传输验证重构后的网络回环链路。
    /// </summary>
    public sealed class NetworkLoopbackIntegrationTests
    {
        #region Private 私有成员

        private const float TestTimeoutSeconds = 30f; // 整体三协议回环测试的最长执行时间。

        private GameObject runnerObject; // 承载网络冒烟执行器的临时对象。
        private NetworkSmokeTestRunner runner; // 被测的共享冒烟执行器。
        private MTaskMainThreadExecutor mainThreadExecutor; // 模拟 Player 主线程的 MTask 续体调度器。
        private bool previousEnableLog; // 用例开始前的普通日志开关状态。
        private bool previousEnablePayloadLog; // 用例开始前的正文日志开关状态。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 初始化独立的全局组件运行时并注册实际生成的 HotUpdate Handler。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            Global.Shutdown();
            MTaskRuntime.Shutdown();
            mainThreadExecutor = new MTaskMainThreadExecutor("NetworkLoopbackIntegrationTests");
            MTaskExecutors.Unity = mainThreadExecutor;
            MTaskRuntime.Initialize(mainThreadExecutor);
            Global.Initialize();
            previousEnableLog = LogSwitch.EnableLog;
            previousEnablePayloadLog = LogSwitch.EnablePayloadLog;
            LogSwitch.EnableLog = false;
            LogSwitch.EnablePayloadLog = false;

            Global.RegisterAppModule<IApplicationEventBus, ApplicationEventBusModule>();
            NetworkService network = Global.RegisterAppService<INetworkService, NetworkService>();
            var protocolBuilder = new NetworkProtocolBuilder();
            BusinessClientProtocolRegistration.Register(protocolBuilder);
            HotUpdateHandlerRegistration.Register(protocolBuilder);
            network.ConfigureProtocol(protocolBuilder.Build());

            runnerObject = new GameObject("NetworkLoopbackIntegrationTests");
            runner = runnerObject.AddComponent<NetworkSmokeTestRunner>();
        }

        /// <summary>
        /// 验证三种传输均能完成连接、普通消息、RPC 和服务端业务断开。
        /// </summary>
        /// <returns>供 Unity EditMode Test Runner 驱动的协程。</returns>
        [UnityTest]
        public IEnumerator NetworkLoopback_AllTransports_UseActualHandlersAndProtobuf()
        {
            runner.RunAsync();
            float deadline = Time.realtimeSinceStartup + TestTimeoutSeconds;
            while (!runner.IsCompleted && Time.realtimeSinceStartup < deadline)
            {
                mainThreadExecutor.Drain();
                Global.Tick();
                mainThreadExecutor.Drain();
                yield return null;
            }

            Assert.IsTrue(runner.IsCompleted, "网络冒烟测试未在限定时间内结束。");
            Assert.IsTrue(runner.IsPassed, runner.FailureMessage);
        }

        /// <summary>
        /// 销毁测试对象并关闭 Global，避免会话、事件订阅或静态组件影响其他 EditMode 用例。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (runnerObject != null)
            {
                Object.DestroyImmediate(runnerObject);
            }

            runnerObject = null;
            runner = null;
            MTaskRuntime.BeginFastShutdown();
            Global.Shutdown();
            MTaskRuntime.CancelApplicationTasks();
            mainThreadExecutor?.Drain();
            MTaskRuntime.Shutdown();
            mainThreadExecutor = null;
            LogSwitch.EnableLog = previousEnableLog;
            LogSwitch.EnablePayloadLog = previousEnablePayloadLog;
        }

        #endregion
    }
}
