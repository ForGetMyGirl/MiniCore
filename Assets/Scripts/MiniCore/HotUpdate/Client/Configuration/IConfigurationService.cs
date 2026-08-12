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
    /// 提供 JSON 与 Protobuf 配置数据加载能力的应用服务。
    /// 不同存储键可并行使用不同格式，但同一键禁止混用格式。
    /// </summary>
    public interface IConfigurationService : IAppService
    {
        /// <summary>
        /// 从 Resources 加载并反序列化 JSON 配置。
        /// </summary>
        /// <typeparam name="T">目标配置类型。</typeparam>
        /// <param name="storageKey">逻辑存储键，用于格式互斥校验。</param>
        /// <param name="resourcePath">Resources 下的 TextAsset 路径。</param>
        /// <returns>反序列化后的配置对象。</returns>
        MTask<T> LoadJsonAsync<T>(string storageKey, string resourcePath);

        /// <summary>
        /// 从 Resources 加载并反序列化 Protobuf 配置。
        /// </summary>
        /// <typeparam name="T">目标 Protobuf 消息类型。</typeparam>
        /// <param name="storageKey">逻辑存储键，用于格式互斥校验。</param>
        /// <param name="resourcePath">Resources 下的 TextAsset 路径。</param>
        /// <param name="parser">目标消息解析器。</param>
        /// <returns>反序列化后的 Protobuf 消息。</returns>
        MTask<T> LoadProtobufAsync<T>(string storageKey, string resourcePath, MessageParser<T> parser) where T : IMessage<T>;

        /// <summary>
        /// 清除一个逻辑存储键的格式占用与缓存约束。
        /// </summary>
        /// <param name="storageKey">要清除的逻辑存储键。</param>
        void Release(string storageKey);
    }
}
