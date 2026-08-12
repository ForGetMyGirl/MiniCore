using System;
using System.Collections.Generic;
using System.Threading;
using MiniCore.Threading;

namespace MiniCore.Service
{

    /// <summary>
    /// 提供结构化指标、事件和异常记录的系统服务契约。
    /// </summary>
    public interface ITelemetryService : IAppService
    {
        /// <summary>
        /// 累加一个计数指标。
        /// </summary>
        /// <param name="name">稳定指标名称。</param>
        /// <param name="value">累加值。</param>
        void Increment(string name, long value = 1);

        /// <summary>
        /// 更新一个瞬时数值指标。
        /// </summary>
        /// <param name="name">稳定指标名称。</param>
        /// <param name="value">当前数值。</param>
        void Gauge(string name, double value);

        /// <summary>
        /// 记录结构化业务事件。
        /// </summary>
        /// <param name="name">稳定事件名称。</param>
        /// <param name="fields">可选结构化字段。</param>
        void Track(string name, IReadOnlyDictionary<string, string> fields = null);

        /// <summary>
        /// 记录异常事件。
        /// </summary>
        /// <param name="exception">待记录异常。</param>
        /// <param name="context">可选错误上下文。</param>
        void TrackException(Exception exception, string context = null);
    }
}
