using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MiniCore.Core;
using MiniCore.Model;
using Newtonsoft.Json;
using UnityEngine;

namespace MiniCore.Service
{
    /// <summary>
    /// 将运行指标、业务事件和异常批量写入项目存储根目录的本地 NDJSON 文件服务。
    /// 写入失败会被隔离，不能影响游戏主流程。
    /// </summary>
    [AppService("本地运行数据记录", typeof(ITelemetryService), Description = "将运行指标、业务事件与异常批量写入本地 NDJSON 文件。", RequiresServices = new[] { typeof(IStoragePathService) })]
    public sealed class LocalTelemetryFileService : AAppService, ITelemetryService
    {
        #region Private 私有成员

        private const int FlushBatchSize = 64; // 单次落盘最多写入的记录数。
        private const long MaxFileBytes = 2L * 1024L * 1024L; // 单个滚动文件上限。
        private const double FlushIntervalSeconds = 2d; // 空闲状态的最长批量等待时间。
        private readonly ConcurrentQueue<string> pendingRecords = new ConcurrentQueue<string>(); // 等待批量落盘的 NDJSON 记录。
        private readonly Dictionary<string, long> counters = new Dictionary<string, long>(); // 当前会话计数器快照。
        private readonly Dictionary<string, double> gauges = new Dictionary<string, double>(); // 当前会话 Gauge 快照。
        private IStoragePathService storagePathService; // 本地持久化路径服务。
        private string rootPath; // 本地运行数据目录。
        private string currentPath; // 当前写入文件。
        private double nextFlushTime; // 下一次自动批量落盘时间。
        private int fileSequence; // 同日滚动文件序号。

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 创建本地运行数据目录并安排首轮批量落盘。
        /// </summary>
        public override void Awake()
        {
            storagePathService = Global.GetService<IStoragePathService>(this);
            rootPath = storagePathService.GetDirectory("Telemetry");
            nextFlushTime = Global.Time.UnscaledTime + FlushIntervalSeconds;
        }

        /// <summary>
        /// 定期批量落盘，避免每个指标事件执行同步 I/O。
        /// </summary>
        protected override void Update()
        {
            if (pendingRecords.Count >= FlushBatchSize || Global.Time.UnscaledTime >= nextFlushTime)
            {
                Flush();
            }
        }

        /// <summary>
        /// 退出时尝试写完尚未落盘的记录。
        /// </summary>
        protected override void OnDispose()
        {
            Flush();
            storagePathService = null;
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 累加会话计数器并记录指标事件。
        /// </summary>
        /// <param name="name">稳定指标名称。</param>
        /// <param name="value">累加值。</param>
        public void Increment(string name, long value = 1)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            counters.TryGetValue(name, out long current);
            counters[name] = current + value;
            Enqueue("counter", name, value.ToString(CultureInfo.InvariantCulture), null);
        }

        /// <summary>
        /// 更新会话 Gauge 并记录指标事件。
        /// </summary>
        /// <param name="name">稳定指标名称。</param>
        /// <param name="value">当前数值。</param>
        public void Gauge(string name, double value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            gauges[name] = value;
            Enqueue("gauge", name, value.ToString("R", CultureInfo.InvariantCulture), null);
        }

        /// <summary>
        /// 记录业务结构化事件。
        /// </summary>
        /// <param name="name">稳定事件名称。</param>
        /// <param name="fields">可选字段。</param>
        public void Track(string name, IReadOnlyDictionary<string, string> fields = null)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                Enqueue("event", name, null, fields);
            }
        }

        /// <summary>
        /// 记录异常类型、消息与上下文，而不将异常继续抛给调用方。
        /// </summary>
        /// <param name="exception">待记录异常。</param>
        /// <param name="context">可选错误上下文。</param>
        public void TrackException(Exception exception, string context = null)
        {
            if (exception == null)
            {
                return;
            }

            Dictionary<string, string> fields = new Dictionary<string, string>
            {
                ["type"] = exception.GetType().FullName,
                ["message"] = exception.Message,
                ["context"] = context ?? string.Empty
            };
            Enqueue("exception", "exception", null, fields);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将一条遥测记录序列化为独立 JSON 行并加入批处理队列。
        /// </summary>
        /// <param name="kind">记录类别。</param>
        /// <param name="name">记录名称。</param>
        /// <param name="value">可选数值文本。</param>
        /// <param name="fields">可选字段。</param>
        private void Enqueue(string kind, string name, string value, IReadOnlyDictionary<string, string> fields)
        {
            pendingRecords.Enqueue(JsonConvert.SerializeObject(new TelemetryRecord
            {
                TimeUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                Kind = kind,
                Name = name,
                Value = value,
                Fields = fields == null ? null : new Dictionary<string, string>(fields)
            }));
        }

        /// <summary>
        /// 以有限批次追加记录；文件达到上限时创建新滚动文件。
        /// </summary>
        private void Flush()
        {
            nextFlushTime = Global.Time.UnscaledTime + FlushIntervalSeconds;
            if (pendingRecords.IsEmpty)
            {
                return;
            }

            try
            {
                EnsureWritablePath();
                int written = 0;
                using StreamWriter writer = new StreamWriter(currentPath, true);
                while (written < FlushBatchSize && pendingRecords.TryDequeue(out string record))
                {
                    writer.WriteLine(record);
                    written++;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"MiniCore telemetry flush failed: {exception.Message}");
            }
        }

        /// <summary>
        /// 选择尚未超过大小限制的当前按日滚动文件。
        /// </summary>
        private void EnsureWritablePath()
        {
            if (!string.IsNullOrEmpty(currentPath) && File.Exists(currentPath) && new FileInfo(currentPath).Length < MaxFileBytes)
            {
                return;
            }

            string day = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            currentPath = Path.Combine(rootPath, $"telemetry-{day}-{fileSequence++:D3}.ndjson");
        }

        /// <summary>
        /// 本地 NDJSON 文件的单条序列化数据。
        /// </summary>
        private sealed class TelemetryRecord
        {
            /// <summary>
            /// 获取或设置 UTC 时间戳。
            /// </summary>
            public string TimeUtc { get; set; }

            /// <summary>
            /// 获取或设置记录类别。
            /// </summary>
            public string Kind { get; set; }

            /// <summary>
            /// 获取或设置稳定记录名称。
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// 获取或设置可选指标值。
            /// </summary>
            public string Value { get; set; }

            /// <summary>
            /// 获取或设置结构化字段。
            /// </summary>
            public Dictionary<string, string> Fields { get; set; }
        }

        #endregion
    }
}
