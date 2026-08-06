using System;
using System.Collections.Generic;
using MiniCore.Core;
using MiniCore.Demo.MiniBomber.Unity;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 将客户端权威快照映射到 BattleScene 表现对象，并以三十赫兹发送统一输入。
    /// </summary>
    public sealed class MiniBomberBattlePresentationComponent : AComponent
    {
        #region Private 私有成员

        private readonly Dictionary<long, BomberPlayerView> playerViews = new Dictionary<long, BomberPlayerView>(4); // 玩家身份到表现。
        private readonly Dictionary<long, BomberBombView> bombViews = new Dictionary<long, BomberBombView>(16); // 炸弹身份到表现。
        private readonly HashSet<long> snapshotIds = new HashSet<long>(); // 单次快照实体身份复用集合。
        private readonly List<long> removedIds = new List<long>(16); // 待销毁实体身份复用列表。
        private AccountSessionComponent account; // 当前账号会话。
        private BattleClientComponent battle; // 客户端战斗状态。
        private BomberBattleSceneBinding sceneBinding; // 当前 BattleScene 引用入口。
        private Vector2 latestMove; // 最新平台无关移动输入。
        private bool pendingBomb; // 尚未随三十赫兹输入发送的炸弹按键边沿。
        private long clientTick; // 客户端输入 Tick。
        private double nextInputSendTime; // 下一次输入发送单调时间。
        private BomberPlayerView localPlayerView; // 本地玩家预测显示对象。
        private MiniCore.UI.IUIService uiService; // 诊断窗口所属 UI 服务。
        private MiniCore.UI.UIWindowHandle debugWindowHandle; // 当前网络诊断窗口句柄。
        private double inputSendInterval = 1d / 30d; // 可配置输入发送间隔。
        private float movementSpeedCellsPerSecond = 3.5f; // 仅用于客户端显示预测的移速。
        private long lastPresentedEventId; // 已转换为世界表现的最后事件编号。
        private bool debugToggleInProgress; // 是否正在切换诊断窗口。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 取得客户端状态组件并监听 BattleScene Binding。
        /// </summary>
        public override void Awake()
        {
            account = Global.Get<AccountSessionComponent>(this);
            battle = Global.Get<BattleClientComponent>(this);
            uiService = Global.GetService<MiniCore.UI.IUIService>(this);
            battle.SnapshotChanged += ApplySnapshot;
            battle.EventsChanged += ApplyEvents;
            BomberBattleSceneBinding.Available += BindScene;
            BomberBattleSceneBinding existing = UnityEngine.Object.FindObjectOfType<BomberBattleSceneBinding>();
            if (existing != null)
            {
                BindScene(existing);
            }
        }

        /// <summary>
        /// 从共享资产写入客户端输入频率和显示预测速度。
        /// </summary>
        /// <param name="runtimeConfig">运行时网络频率配置。</param>
        /// <param name="ruleConfig">权威移动规则。</param>
        /// <param name="mapDefinition">用于将毫米速度换算为每秒格数的地图。</param>
        public void Configure(MiniBomberRuntimeConfig runtimeConfig, MiniBomberRuleConfig ruleConfig, BomberMapDefinition mapDefinition)
        {
            int inputRate = runtimeConfig != null ? runtimeConfig.InputSendRate : 30;
            inputSendInterval = 1d / Mathf.Max(1, inputRate);
            int cellSize = mapDefinition != null ? mapDefinition.CellSizeMillimeters : 1000;
            int movementSpeed = ruleConfig != null ? ruleConfig.MovementSpeedMillimetersPerSecond : 3500;
            movementSpeedCellsPerSecond = movementSpeed / (float)Mathf.Max(1, cellSize);
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 按三十赫兹发送最新输入；Unity 渲染帧率不会改变协议输入频率。
        /// </summary>
        protected override void Update()
        {
            if (sceneBinding == null || battle.Snapshot == null || Global.Time.UnscaledTime < nextInputSendTime)
            {
                return;
            }

            nextInputSendTime = Global.Time.UnscaledTime + inputSendInterval;
            battle.SendInput(
                battle.Snapshot.MatchId,
                ++clientTick,
                Mathf.RoundToInt(latestMove.x * 1000f),
                Mathf.RoundToInt(latestMove.y * 1000f),
                pendingBomb);
            pendingBomb = false;
        }

        /// <summary>
        /// 解除场景、输入和战斗状态事件并清理表现索引。
        /// </summary>
        protected override void OnDispose()
        {
            BomberBattleSceneBinding.Available -= BindScene;
            if (battle != null)
            {
                battle.SnapshotChanged -= ApplySnapshot;
                battle.EventsChanged -= ApplyEvents;
            }

            UnbindInput();
            playerViews.Clear();
            bombViews.Clear();
            snapshotIds.Clear();
            removedIds.Clear();
            sceneBinding = null;
            localPlayerView = null;
            uiService = null;
            debugWindowHandle = null;
            account = null;
            battle = null;
            Global.ReleaseAll(this);
            base.OnDispose();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 绑定新加载的 BattleScene 表现入口。
        /// </summary>
        /// <param name="binding">场景 Binding。</param>
        private void BindScene(BomberBattleSceneBinding binding)
        {
            if (binding == null || ReferenceEquals(sceneBinding, binding))
            {
                return;
            }

            UnbindInput();
            playerViews.Clear();
            bombViews.Clear();
            localPlayerView = null;
            lastPresentedEventId = 0;
            clientTick = 0;
            pendingBomb = false;
            sceneBinding = binding;
            if (sceneBinding.Input != null)
            {
                sceneBinding.Input.FrameReady += HandleInputFrame;
                sceneBinding.Input.DebugPressed += ToggleNetworkDebug;
            }

            if (sceneBinding.MapView != null && sceneBinding.MapView.Definition != null && sceneBinding.CameraController != null)
            {
                sceneBinding.CameraController.SetMapBounds(sceneBinding.MapView.Definition.Width, sceneBinding.MapView.Definition.Height);
            }

            ApplySnapshot();
        }

        /// <summary>
        /// 解除旧 BattleScene 输入源。
        /// </summary>
        private void UnbindInput()
        {
            if (sceneBinding != null && sceneBinding.Input != null)
            {
                sceneBinding.Input.FrameReady -= HandleInputFrame;
                sceneBinding.Input.DebugPressed -= ToggleNetworkDebug;
            }
        }

        /// <summary>
        /// 缓存最新输入并保留炸弹按键边沿直到下一次网络发送。
        /// </summary>
        /// <param name="frame">平台无关输入帧。</param>
        private void HandleInputFrame(BomberInputFrame frame)
        {
            latestMove = frame.Move;
            pendingBomb |= frame.PlaceBomb;
            localPlayerView?.Predict(frame.Move, movementSpeedCellsPerSecond, Time.deltaTime);
        }

        /// <summary>
        /// 将最新服务器快照同步到玩家、炸弹、地图和相机表现。
        /// </summary>
        private void ApplySnapshot()
        {
            MiniBomberBattleSnapshot snapshot = battle?.Snapshot;
            if (sceneBinding == null || snapshot == null)
            {
                return;
            }

            int cellSize = sceneBinding.MapView != null && sceneBinding.MapView.Definition != null
                ? sceneBinding.MapView.Definition.CellSizeMillimeters
                : 1000;
            snapshotIds.Clear();
            for (int index = 0; index < snapshot.Players.Count; index++)
            {
                MiniBomberBattlePlayerDto player = snapshot.Players[index];
                snapshotIds.Add(player.PlayerId);
                if (!playerViews.TryGetValue(player.PlayerId, out BomberPlayerView view))
                {
                    view = sceneBinding.CreatePlayer();
                    view.Initialize(player.PlayerId, cellSize);
                    playerViews.Add(player.PlayerId, view);
                    if (player.PlayerId == account.PlayerId && sceneBinding.CameraController != null)
                    {
                        localPlayerView = view;
                        sceneBinding.CameraController.SetTarget(view.transform);
                    }
                }

                view.ApplyState(
                    player.PositionXMillimeters,
                    player.PositionZMillimeters,
                    player.FacingX,
                    player.FacingZ,
                    player.IsAlive,
                    player.IsAlive && snapshot.ServerTick < player.InvulnerableUntilTick);
            }

            CollectRemoved(playerViews, snapshotIds, removedIds);
            for (int index = 0; index < removedIds.Count; index++)
            {
                long id = removedIds[index];
                sceneBinding.DestroyView(playerViews[id]);
                playerViews.Remove(id);
            }

            snapshotIds.Clear();
            for (int index = 0; index < snapshot.Bombs.Count; index++)
            {
                MiniBomberBattleBombDto bomb = snapshot.Bombs[index];
                snapshotIds.Add(bomb.BombId);
                if (!bombViews.TryGetValue(bomb.BombId, out BomberBombView view))
                {
                    view = sceneBinding.CreateBomb();
                    view.Initialize(bomb.BombId, bomb.CellX, bomb.CellZ);
                    bombViews.Add(bomb.BombId, view);
                }
            }

            CollectRemoved(bombViews, snapshotIds, removedIds);
            for (int index = 0; index < removedIds.Count; index++)
            {
                long id = removedIds[index];
                sceneBinding.DestroyView(bombViews[id]);
                bombViews.Remove(id);
            }

            sceneBinding.MapView?.ApplyDestroyedBreakables(snapshot.DestroyedBreakableCells?.ToByteArray());
        }

        /// <summary>
        /// 将服务器即时爆炸事件转换为短时表现。
        /// </summary>
        private void ApplyEvents()
        {
            if (sceneBinding == null || battle == null)
            {
                return;
            }

            IReadOnlyList<MiniBomberBattleEventDto> events = battle.RecentEvents;
            if (events.Count == 0)
            {
                return;
            }

            for (int index = 0; index < events.Count; index++)
            {
                MiniBomberBattleEventDto item = events[index];
                if (item.EventId <= lastPresentedEventId)
                {
                    continue;
                }

                lastPresentedEventId = item.EventId;
                if (item.Type == MiniBomberBattleEventType.MiniBomberEventExplosionStarted)
                {
                    sceneBinding.CreateExplosion().Play(item.CellX, item.CellZ);
                }
                else if (item.Type == MiniBomberBattleEventType.MiniBomberEventBlockDestroyed)
                {
                    sceneBinding.MapView?.HideBreakable(item.CellX, item.CellZ);
                }
            }
        }

        /// <summary>
        /// 从输入事件启动网络诊断窗口切换任务。
        /// </summary>
        private void ToggleNetworkDebug()
        {
            if (!debugToggleInProgress)
            {
                ToggleNetworkDebugAsync().Forget();
            }
        }

        /// <summary>
        /// 使用精确句柄打开或关闭 Debug Layer 的网络诊断窗口。
        /// </summary>
        /// <returns>诊断窗口切换完成任务。</returns>
        private async MTask ToggleNetworkDebugAsync()
        {
            debugToggleInProgress = true;
            try
            {
                if (debugWindowHandle != null && debugWindowHandle.IsValid)
                {
                    await uiService.CloseAsync(debugWindowHandle);
                    debugWindowHandle = null;
                    return;
                }

                try
                {
                    debugWindowHandle = await uiService.OpenAsync(MiniBomberConstants.NetworkDebugWindowRoute);
                }
                catch (InvalidOperationException exception)
                {
                    LogSwitch.Warning($"MiniBomber 网络诊断窗口尚未生成：{exception.Message}");
                }
            }
            finally
            {
                debugToggleInProgress = false;
            }
        }

        /// <summary>
        /// 收集字典中不再存在于当前快照的实体身份。
        /// </summary>
        /// <typeparam name="TView">表现组件类型。</typeparam>
        /// <param name="views">现有表现字典。</param>
        /// <param name="currentIds">当前快照身份集合。</param>
        /// <param name="output">待移除身份复用列表。</param>
        private static void CollectRemoved<TView>(Dictionary<long, TView> views, HashSet<long> currentIds, List<long> output) where TView : Component
        {
            output.Clear();
            foreach (KeyValuePair<long, TView> pair in views)
            {
                if (!currentIds.Contains(pair.Key))
                {
                    output.Add(pair.Key);
                }
            }
        }

        #endregion
    }
}
