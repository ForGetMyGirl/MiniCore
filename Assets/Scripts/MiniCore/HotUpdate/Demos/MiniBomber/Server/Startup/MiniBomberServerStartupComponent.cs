using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Server;
using MiniCore.Threading;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber Dedicated Server 的 Role 驱动业务装配入口。
    /// </summary>
    public sealed class MiniBomberServerStartupComponent : MiniBomberStartupComponentBase
    {
        #region Public 公共成员

        /// <summary>
        /// 根据当前 Role 加载 MiniBomber 共享配置并创建业务组件。
        /// </summary>
        /// <returns>服务器运行时和监听初始化完成任务。</returns>
        /// <param name="context">固定宿主提供的 Role 和部署配置。</param>
        public async MTask InitializeAsync(DedicatedServerApplicationContext context)
        {
            if (context == null)
            {
                throw new System.ArgumentNullException(nameof(context));
            }

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            DedicatedServerRole roles = context.ActiveRoles;
            if ((roles & DedicatedServerRole.Match) != 0)
            {
                matchServer = Global.GetOrAdd<MiniBomberMatchServerComponent>(this);
            }

            if ((roles & (DedicatedServerRole.Lobby | DedicatedServerRole.Game)) == 0)
            {
                return;
            }

            await LoadConfigurationAsync();
            serverRuntime = Global.GetOrAdd<MiniBomberServerRuntimeComponent>(this);
            await serverRuntime.InitializeAsync(
                RuntimeConfig,
                RuleConfig,
                MapDefinition,
                context.RuntimeConfig.ParsePersistenceMode());
        }

        #endregion

        #region Private 私有成员

        private MiniBomberServerRuntimeComponent serverRuntime; // Dedicated Server 权威业务运行时。
        private MiniBomberMatchServerComponent matchServer; // Match Role 的匹配队列业务组件。
        #endregion

        #region Override 重写实现

        /// <summary>
        /// 释放服务端启动组件持有的业务运行时。
        /// </summary>
        protected override void OnDispose()
        {
            serverRuntime = null;
            matchServer = null;
            base.OnDispose();
        }

        #endregion
    }
}
