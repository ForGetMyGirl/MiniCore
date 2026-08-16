using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using MiniCore.Core;
using MiniCore.Eventing;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using MiniCore.Threading;
using MiniCore.Unity;
using Unity.Profiling;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace MiniCore.HotUpdate
{

    /// <summary>
    /// 将网络基准运行结果写入 Player 可导出的 JSON 与 CSV 文件。
    /// </summary>
    internal static class NetworkBenchmarkReportWriter
    {
        #region Private 私有成员

        private const string DirectoryName = "NetworkBenchmark"; // persistentDataPath 下保存基准报告的目录名。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 写入包含设备信息和全部样本的 JSON、CSV 报告。
        /// </summary>
        /// <param name="results">需要写入的全部压测样本。</param>
        /// <returns>本次报告所在目录。</returns>
        internal static string Write(IList<NetworkBenchmarkRunResult> results)
        {
            string directory = Path.Combine(Application.persistentDataPath, DirectoryName, DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(directory);
            var report = new NetworkBenchmarkReport
            {
                DeviceModel = SystemInfo.deviceModel,
                OperatingSystem = SystemInfo.operatingSystem,
                Platform = Application.platform.ToString(),
                BuildType = Debug.isDebugBuild ? "Development" : "Release",
                UnityVersion = Application.unityVersion,
                GeneratedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                Results = new List<NetworkBenchmarkRunResult>(results)
            };
            File.WriteAllText(Path.Combine(directory, "NetworkBenchmarkReport.json"), JsonUtility.ToJson(report, true), Encoding.UTF8);
            File.WriteAllText(Path.Combine(directory, "NetworkBenchmarkReport.csv"), BuildCsv(report.Results), Encoding.UTF8);
            return directory;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将全部样本转换为可以直接导入表格工具的 CSV 文本。
        /// </summary>
        /// <param name="results">需要导出的全部压测样本。</param>
        /// <returns>包含表头和全部样本行的 CSV 文本。</returns>
        private static string BuildCsv(IList<NetworkBenchmarkRunResult> results)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Transport,Scenario,TargetRateOrConcurrency,Repeat,DurationMilliseconds,SentCount,OfferedCount,RejectedCount,ReceivedCount,FailureCount,DroppedCount,DisconnectCount,ThroughputPerSecond,LatencySampleCount,P50Milliseconds,P95Milliseconds,P99Milliseconds,MaxLatencyMilliseconds,PeakQueuePacketCount,PeakQueueByteCount,QueueProcessedPacketCount,QueueRejectedPacketCount,MaxPacketProcessMilliseconds,IncomingPacketProcessP50Milliseconds,IncomingPacketProcessP95Milliseconds,IncomingPacketProcessP99Milliseconds,IncomingQueueWaitSampleCount,IncomingQueueWaitAverageMilliseconds,IncomingQueueWaitMaxMilliseconds,IncomingQueueWaitP50Milliseconds,IncomingQueueWaitP95Milliseconds,IncomingQueueWaitP99Milliseconds,ServerTransportFramedPacketCount,ServerTransportDispatchedPacketCount,ServerTransportReceiveOperationCount,ServerTransportReceiveOperationAverageMilliseconds,ServerTransportReceiveOperationMaxMilliseconds,NormalEventObservedCount,NormalEventRecognizedCount,NormalEventUnrecognizedCount,NormalEventOutOfRangeCount,NormalEventDuplicateCount,NormalEventMissingTimestampCount,ClientOutboundTimingSampleCount,ClientTransportWriteCount,ClientSocketSendOperationCount,ClientSocketSendOperationAverageMilliseconds,ClientSocketSendOperationMaxMilliseconds,ClientOutboundQueueWaitAverageMilliseconds,ClientOutboundQueueWaitMaxMilliseconds,ClientTransportSendAverageMilliseconds,ClientTransportSendMaxMilliseconds,ServerOutboundTimingSampleCount,ServerOutboundQueueWaitAverageMilliseconds,ServerOutboundQueueWaitMaxMilliseconds,ServerTransportSendAverageMilliseconds,ServerTransportSendMaxMilliseconds,MaxGcAllocatedBytesPerFrame,HitchMilliseconds,QueueRecoveryMilliseconds");
            for (int index = 0; index < results.Count; index++)
            {
                NetworkBenchmarkRunResult result = results[index];
                builder.Append(result.Transport).Append(',')
                    .Append(result.Scenario).Append(',')
                    .Append(result.TargetRateOrConcurrency).Append(',')
                    .Append(result.Repeat).Append(',')
                    .Append(result.DurationMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.SentCount).Append(',')
                    .Append(result.OfferedCount).Append(',')
                    .Append(result.RejectedCount).Append(',')
                    .Append(result.ReceivedCount).Append(',')
                    .Append(result.FailureCount).Append(',')
                    .Append(result.DroppedCount).Append(',')
                    .Append(result.DisconnectCount).Append(',')
                    .Append(result.ThroughputPerSecond.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.LatencySampleCount).Append(',')
                    .Append(result.P50Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.P95Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.P99Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.MaxLatencyMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.PeakQueuePacketCount).Append(',')
                    .Append(result.PeakQueueByteCount).Append(',')
                    .Append(result.QueueProcessedPacketCount).Append(',')
                    .Append(result.QueueRejectedPacketCount).Append(',')
                    .Append(result.MaxPacketProcessMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.IncomingPacketProcessP50Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.IncomingPacketProcessP95Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.IncomingPacketProcessP99Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.IncomingQueueWaitSampleCount).Append(',')
                    .Append(result.IncomingQueueWaitAverageMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.IncomingQueueWaitMaxMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.IncomingQueueWaitP50Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.IncomingQueueWaitP95Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.IncomingQueueWaitP99Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.ServerTransportFramedPacketCount).Append(',')
                    .Append(result.ServerTransportDispatchedPacketCount).Append(',')
                    .Append(result.ServerTransportReceiveOperationCount).Append(',')
                    .Append(result.ServerTransportReceiveOperationAverageMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.ServerTransportReceiveOperationMaxMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.NormalEventObservedCount).Append(',')
                    .Append(result.NormalEventRecognizedCount).Append(',')
                    .Append(result.NormalEventUnrecognizedCount).Append(',')
                    .Append(result.NormalEventOutOfRangeCount).Append(',')
                    .Append(result.NormalEventDuplicateCount).Append(',')
                    .Append(result.NormalEventMissingTimestampCount).Append(',')
                    .Append(result.ClientOutboundTimingSampleCount).Append(',')
                    .Append(result.ClientTransportWriteCount).Append(',')
                    .Append(result.ClientSocketSendOperationCount).Append(',')
                    .Append(result.ClientSocketSendOperationAverageMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.ClientSocketSendOperationMaxMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.ClientOutboundQueueWaitAverageMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.ClientOutboundQueueWaitMaxMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.ClientTransportSendAverageMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.ClientTransportSendMaxMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.ServerOutboundTimingSampleCount).Append(',')
                    .Append(result.ServerOutboundQueueWaitAverageMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.ServerOutboundQueueWaitMaxMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.ServerTransportSendAverageMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.ServerTransportSendMaxMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.MaxGcAllocatedBytesPerFrame).Append(',')
                    .Append(result.HitchMilliseconds).Append(',')
                    .Append(result.QueueRecoveryMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).AppendLine();
            }

            return builder.ToString();
        }

        #endregion
    }
}
