using System;
using System.Collections.Generic;
using MiniCore.Threading;
using Google.Protobuf;
using MiniCore.Model;
using Newtonsoft.Json;
using UnityEngine;

namespace MiniCore.Service
{
    /// <summary>
    /// 配置数据的序列化格式。相同逻辑存储键在一次运行期只能使用其中一种格式。
    /// </summary>
    public enum ConfigurationFormat
    {
        /// <summary>
        /// JSON 文本配置。
        /// </summary>
        Json,

        /// <summary>
        /// Protobuf 二进制配置。
        /// </summary>
        Protobuf
    }
}
