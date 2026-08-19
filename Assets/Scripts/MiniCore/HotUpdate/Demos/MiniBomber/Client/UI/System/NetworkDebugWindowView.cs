using System;
using TMPro;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 网络诊断窗口 View。
    /// </summary>
    public sealed class NetworkDebugWindowView : MiniBomberWindowViewBase
    {
        #region UnityProperty Unity 引用属性

        [SerializeField] private TMP_Text DiagnosticsText; // 网络诊断文本。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 刷新网络 Tick、延迟、快照年龄与队列指标。
        /// </summary>
        /// <param name="serverTick">服务器 Tick。</param>
        /// <param name="hasRtt">是否存在往返延迟样本。</param>
        /// <param name="rttMilliseconds">往返延迟毫秒数。</param>
        /// <param name="snapshotAgeMilliseconds">快照年龄毫秒数。</param>
        /// <param name="pendingPacketCount">当前排队包数量。</param>
        /// <param name="peakPendingPacketCount">历史峰值排队包数量。</param>
        public void RefreshDiagnostics(long serverTick, bool hasRtt, int rttMilliseconds, int snapshotAgeMilliseconds, long pendingPacketCount, long peakPendingPacketCount)
        {
            if (DiagnosticsText == null) return;
            string rttText = hasRtt ? $"{rttMilliseconds} ms" : "--";
            string value = $"ServerTick: {serverTick}\nRTT: {rttText}\nSnapshotAge: {snapshotAgeMilliseconds} ms\nQueued: {pendingPacketCount}\nPeak: {peakPendingPacketCount}";
            if (!string.Equals(DiagnosticsText.text, value, StringComparison.Ordinal)) DiagnosticsText.text = value;
        }

        #endregion
    }
}
