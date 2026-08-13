using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Threading;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 普通客户端的账号、大厅、房间、战斗表现与首场景装配入口。
    /// </summary>
    public sealed class MiniBomberClientStartupComponent : MiniBomberStartupComponentBase
    {
        #region Private 私有成员

        private const int ClientTargetFrameRate = 60; // Demo 客户端目标显示帧率。

        private AccountSessionComponent accountSession; // 客户端账号会话组件。
        private LobbyComponent lobby; // 客户端大厅状态组件。
        private RoomComponent room; // 客户端房间状态组件。
        private BattleClientComponent battle; // 客户端战斗状态组件。
        private MiniBomberClientFlowComponent clientFlow; // 客户端高层流程组件。
        private MiniBomberBattlePresentationComponent battlePresentation; // BattleScene 表现桥接组件。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 创建 MiniBomber 客户端组件，并在热更新启动完成后进入登录场景。
        /// </summary>
        /// <returns>客户端组件与登录流程初始化完成任务。</returns>
        public async MTask InitializeAsync()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = ClientTargetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            await LoadConfigurationAsync();
            accountSession = Global.GetOrAdd<AccountSessionComponent>(this);
            await accountSession.InitializeAsync(RuntimeConfig);
            lobby = Global.GetOrAdd<LobbyComponent>(this);
            room = Global.GetOrAdd<RoomComponent>(this);
            battle = Global.GetOrAdd<BattleClientComponent>(this);
            battlePresentation = Global.GetOrAdd<MiniBomberBattlePresentationComponent>(this);
            battlePresentation.Configure(RuntimeConfig, RuleConfig, MapDefinition);
            clientFlow = Global.GetOrAdd<MiniBomberClientFlowComponent>(this);
            await clientFlow.InitializeAsync(RuntimeConfig);
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 清除业务组件引用并释放客户端启动组件持有的全部租约。
        /// </summary>
        protected override void OnDispose()
        {
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            accountSession = null;
            lobby = null;
            room = null;
            battle = null;
            clientFlow = null;
            battlePresentation = null;
            base.OnDispose();
        }

        #endregion
    }
}
