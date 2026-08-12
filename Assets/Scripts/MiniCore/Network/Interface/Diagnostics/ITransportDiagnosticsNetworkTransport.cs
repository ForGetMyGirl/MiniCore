using MiniCore.Threading;
using System;
using System.Threading;

namespace MiniCore.Model
{

    /// <summary>
    /// 允许逻辑会话按需启用底层传输收发边界诊断。
    /// 仅供压测与故障定位区分 Socket 收发、完整帧读取与收包回调，不参与正常收发控制。
    /// </summary>
    internal interface ITransportDiagnosticsNetworkTransport
    {
        /// <summary>
        /// 启用或关闭收发边界诊断，并清空上一统计周期的数据。
        /// </summary>
        /// <param name="enabled">为 true 时记录 Socket 收发、完整帧读取与回调完成数量。</param>
        void SetTransportDiagnosticsEnabled(bool enabled);

        /// <summary>
        /// 获取当前统计周期的收包边界快照。
        /// </summary>
        /// <returns>不转移缓冲区所有权的只读统计快照。</returns>
        NetworkTransportReceiveSnapshot CaptureReceiveDiagnostics();

        /// <summary>
        /// 获取当前统计周期的底层 Socket 发送操作快照。
        /// </summary>
        /// <returns>不转移缓冲区所有权的只读统计快照。</returns>
        NetworkTransportSendSnapshot CaptureSendDiagnostics();
    }
}
