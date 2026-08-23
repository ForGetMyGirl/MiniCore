using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Server;
using MiniCore.Threading;
using System.Collections.Generic;
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
            ServerRoleMask roles = context.ActiveRoles;
            if (roles.Intersects((ulong)MiniBomberServerRole.Match))
            {
                matchServer = Global.GetOrAdd<MiniBomberMatchServerComponent>(this);
            }

            if (!roles.Intersects((ulong)(MiniBomberServerRole.Lobby | MiniBomberServerRole.Game)))
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

        /// <summary>
        /// 通知当前启用的 MiniBomber Role 不再接收新的业务工作。
        /// </summary>
        public void BeginDrain()
        {
            serverRuntime?.BeginDrain();
            matchServer?.BeginDrain();
        }

        /// <summary>
        /// 汇总当前玩家、房间、比赛和匹配队列数量。
        /// </summary>
        /// <returns>用于自动发布人工门禁的 Drain 快照。</returns>
        public DedicatedServerDrainStatus CaptureDrainStatus()
        {
            int players = serverRuntime?.OnlinePlayerCount ?? 0;
            int rooms = serverRuntime?.RoomCount ?? 0;
            int matches = serverRuntime?.MatchCount ?? 0;
            int queuedPlayers = matchServer?.WaitingCount ?? 0;
            int activeCount = players + rooms + matches + queuedPlayers;
            if (activeCount == 0)
            {
                return DedicatedServerDrainStatus.Drained();
            }

            var blockers = new List<string>(4);
            if (players > 0)
            {
                blockers.Add($"在线玩家：{players}");
            }

            if (rooms > 0)
            {
                blockers.Add($"活动房间：{rooms}");
            }

            if (matches > 0)
            {
                blockers.Add($"活动比赛：{matches}");
            }

            if (queuedPlayers > 0)
            {
                blockers.Add($"匹配队列玩家：{queuedPlayers}");
            }

            return new DedicatedServerDrainStatus(false, activeCount, blockers.ToArray());
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
