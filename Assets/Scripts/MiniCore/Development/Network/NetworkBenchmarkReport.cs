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
    /// 表示 JSON 报告需要附带的运行设备信息与样本集合。
    /// </summary>
    [Serializable]
    internal sealed class NetworkBenchmarkReport
    {
        #region Public 公共成员

        /// <summary>
        /// 运行 Player 的设备型号。
        /// </summary>
        public string DeviceModel;
        /// <summary>
        /// 运行 Player 的操作系统信息。
        /// </summary>
        public string OperatingSystem;
        /// <summary>
        /// 运行 Player 的 Unity 平台。
        /// </summary>
        public string Platform;
        /// <summary>
        /// Development 或 Release 构建标识。
        /// </summary>
        public string BuildType;
        /// <summary>
        /// 运行 Player 的 Unity 版本。
        /// </summary>
        public string UnityVersion;
        /// <summary>
        /// 报告生成时刻的 UTC ISO 8601 文本。
        /// </summary>
        public string GeneratedUtc;
        /// <summary>
        /// 本次运行产生的全部压测样本。
        /// </summary>
        public List<NetworkBenchmarkRunResult> Results;

        #endregion
    }
}
