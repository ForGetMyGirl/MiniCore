using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Threading;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace MiniCore.Service
{
    /// <summary>
    /// HTTP 服务的启动配置参数。
    /// </summary>
    public sealed class HttpServiceInitArgs : ComponentInitArgs
    {
        /// <summary>
        /// 获取或设置单次请求默认超时秒数。
        /// </summary>
        public int DefaultTimeoutSeconds { get; set; } = 15;

        /// <summary>
        /// 获取或设置幂等请求最大重试次数。
        /// </summary>
        public int MaxRetryCount { get; set; } = 2;

        /// <summary>
        /// 获取或设置首次重试的退避毫秒数。
        /// </summary>
        public int RetryBackoffMilliseconds { get; set; } = 250;
    }
}
