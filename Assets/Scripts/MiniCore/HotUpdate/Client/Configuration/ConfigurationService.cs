using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
        UniTask<T> LoadJsonAsync<T>(string storageKey, string resourcePath);

        /// <summary>
        /// 从 Resources 加载并反序列化 Protobuf 配置。
        /// </summary>
        /// <typeparam name="T">目标 Protobuf 消息类型。</typeparam>
        /// <param name="storageKey">逻辑存储键，用于格式互斥校验。</param>
        /// <param name="resourcePath">Resources 下的 TextAsset 路径。</param>
        /// <param name="parser">目标消息解析器。</param>
        /// <returns>反序列化后的 Protobuf 消息。</returns>
        UniTask<T> LoadProtobufAsync<T>(string storageKey, string resourcePath, MessageParser<T> parser) where T : IMessage<T>;

        /// <summary>
        /// 清除一个逻辑存储键的格式占用与缓存约束。
        /// </summary>
        /// <param name="storageKey">要清除的逻辑存储键。</param>
        void Release(string storageKey);
    }

    /// <summary>
    /// 默认配置服务实现，使用 Resources TextAsset 作为基础载体。
    /// 项目可替换为基于资源服务、YooAsset 或远端配置的实现。
    /// </summary>
    [AppService("配置", typeof(IConfigurationService), Description = "加载并缓存 JSON 与 Protobuf 格式的项目配置数据。")]
    public sealed class ConfigurationService : AAppService, IConfigurationService
    {
        #region Private 私有成员

        private readonly Dictionary<string, ConfigurationFormat> formats = new Dictionary<string, ConfigurationFormat>(StringComparer.Ordinal); // 存储键到已锁定格式的映射。

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 加载 JSON 配置并锁定该逻辑存储键为 JSON 格式。
        /// </summary>
        /// <typeparam name="T">目标配置类型。</typeparam>
        /// <param name="storageKey">逻辑存储键。</param>
        /// <param name="resourcePath">Resources 文本资源路径。</param>
        /// <returns>反序列化后的配置对象。</returns>
        public UniTask<T> LoadJsonAsync<T>(string storageKey, string resourcePath)
        {
            EnsureFormat(storageKey, ConfigurationFormat.Json);
            TextAsset asset = LoadAsset(resourcePath);
            return UniTask.FromResult(JsonConvert.DeserializeObject<T>(asset.text));
        }

        /// <summary>
        /// 加载 Protobuf 配置并锁定该逻辑存储键为 Protobuf 格式。
        /// </summary>
        /// <typeparam name="T">目标 Protobuf 消息类型。</typeparam>
        /// <param name="storageKey">逻辑存储键。</param>
        /// <param name="resourcePath">Resources 二进制文本资源路径。</param>
        /// <param name="parser">目标消息解析器。</param>
        /// <returns>反序列化后的 Protobuf 消息。</returns>
        public UniTask<T> LoadProtobufAsync<T>(string storageKey, string resourcePath, MessageParser<T> parser) where T : IMessage<T>
        {
            if (parser == null)
            {
                throw new ArgumentNullException(nameof(parser));
            }

            EnsureFormat(storageKey, ConfigurationFormat.Protobuf);
            TextAsset asset = LoadAsset(resourcePath);
            return UniTask.FromResult(parser.ParseFrom(asset.bytes));
        }

        /// <summary>
        /// 解除一个逻辑键的格式锁定，使其后续可被重新加载。
        /// </summary>
        /// <param name="storageKey">要释放的逻辑存储键。</param>
        public void Release(string storageKey)
        {
            if (!string.IsNullOrWhiteSpace(storageKey))
            {
                formats.Remove(storageKey);
            }
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 清理全部运行期格式锁定。
        /// </summary>
        public override void Dispose()
        {
            formats.Clear();
            base.Dispose();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 验证逻辑键合法性，并保证同一键不会混用 JSON 与 Protobuf。
        /// </summary>
        /// <param name="storageKey">逻辑存储键。</param>
        /// <param name="format">本次请求的序列化格式。</param>
        private void EnsureFormat(string storageKey, ConfigurationFormat format)
        {
            if (string.IsNullOrWhiteSpace(storageKey))
            {
                throw new ArgumentException("配置存储键不能为空。", nameof(storageKey));
            }

            if (formats.TryGetValue(storageKey, out ConfigurationFormat current) && current != format)
            {
                throw new InvalidOperationException($"配置存储键 {storageKey} 已使用 {current}，不能再使用 {format}。");
            }

            formats[storageKey] = format;
        }

        /// <summary>
        /// 加载 Resources 中的 TextAsset，并在资源缺失时给出明确诊断。
        /// </summary>
        /// <param name="resourcePath">Resources 相对路径。</param>
        /// <returns>已加载文本资源。</returns>
        private static TextAsset LoadAsset(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                throw new ArgumentException("配置资源路径不能为空。", nameof(resourcePath));
            }

            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            return asset ?? throw new InvalidOperationException($"未找到配置资源：{resourcePath}。");
        }

        #endregion
    }
}
