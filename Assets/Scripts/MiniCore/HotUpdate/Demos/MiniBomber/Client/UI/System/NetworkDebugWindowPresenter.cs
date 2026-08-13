using System;
using System.Text;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using MiniCore.Threading;
using MiniCore.UI;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 网络诊断窗口 Presenter。
    /// </summary>
    public sealed class NetworkDebugWindowPresenter : AUIWindowPresenter<NetworkDebugWindowView>
    {
        #region Private 私有成员

        private INetworkService network; // 网络队列诊断服务。
        private BattleClientComponent battle; // 服务器 Tick 来源。
        private bool isActive; // 诊断窗口是否仍在活动。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 渲染当前网络队列和服务器 Tick 诊断值。
        /// </summary>
        protected override void OnBind()
        {
            network = Global.GetService<INetworkService>(this);
            battle = Global.Get<BattleClientComponent>(this);
            battle.SnapshotChanged += Render;
            Bindings.Add(() => battle.SnapshotChanged -= Render);
            isActive = true;
            Render();
            RefreshLoopAsync().Forget();
        }

        /// <summary>
        /// 清空网络和战斗状态引用。
        /// </summary>
        protected override void OnDispose()
        {
            isActive = false;
            network = null;
            battle = null;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 刷新服务器 Tick 与当前入站队列诊断。
        /// </summary>
        private void Render()
        {
            NetworkIncomingQueueSnapshot queue = network.GetIncomingQueueSnapshot();
            int rtt = 0;
            bool hasRtt = network.TryGetLastPingMs(MiniBomberConstants.DefaultSessionId, out rtt);
            int snapshotAge = battle.LastSnapshotReceiveTime <= 0d
                ? 0
                : Mathf.Max(0, Mathf.RoundToInt((float)((Global.Time.UnscaledTime - battle.LastSnapshotReceiveTime) * 1000d)));
            string rttText = hasRtt ? $"{rtt} ms" : "--";
            View.DiagnosticsText.text = $"ServerTick: {battle.Snapshot?.ServerTick ?? 0}\nRTT: {rttText}\nSnapshotAge: {snapshotAge} ms\nQueued: {queue.PendingPacketCount}\nPeak: {queue.PeakPendingPacketCount}";
        }

        /// <summary>
        /// 每二百五十毫秒刷新 RTT、快照新鲜度和队列状态。
        /// </summary>
        /// <returns>窗口关闭后退出的诊断任务。</returns>
        private async MTask RefreshLoopAsync()
        {
            while (isActive)
            {
                await MTask.Delay(250);
                if (isActive)
                {
                    Render();
                }
            }
        }

        #endregion
    }
}
