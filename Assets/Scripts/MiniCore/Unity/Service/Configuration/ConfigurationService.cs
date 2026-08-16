using System;
using System.Collections.Generic;
using Google.Protobuf;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Threading;
using Newtonsoft.Json;
using UnityEngine;

namespace MiniCore.Service
{
    /// <summary>
    /// 通过资源服务加载并缓存 JSON 与 Protobuf 配置的默认实现。
    /// </summary>
    [AppService(
        "配置",
        typeof(IConfigurationService),
        Description = "通过资源服务加载、校验、缓存并显式释放 JSON 与 Protobuf 配置。",
        RequiresServices = new[] { typeof(IResourceService) })]
    public sealed class ConfigurationService : AAppService, IConfigurationService
    {
        #region Private 私有成员

        private readonly Dictionary<string, ConfigurationEntry> entries = new Dictionary<string, ConfigurationEntry>(StringComparer.Ordinal); // 逻辑键到配置缓存条目的映射。
        private IResourceService resourceService; // 配置文本和二进制资源的加载服务。

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 加载 JSON 配置；同一逻辑键的并发调用共享一次加载与反序列化。
        /// </summary>
        /// <typeparam name="T">目标配置类型。</typeparam>
        /// <param name="storageKey">配置缓存使用的稳定逻辑键。</param>
        /// <param name="resourceAddress">配置资源地址。</param>
        /// <returns>反序列化并缓存的配置对象。</returns>
        public async MTask<T> LoadJsonAsync<T>(string storageKey, string resourceAddress)
        {
            string normalizedKey = NormalizeRequired(storageKey, nameof(storageKey));
            string normalizedAddress = NormalizeRequired(resourceAddress, nameof(resourceAddress));
            ConfigurationEntry entry = GetOrCreateEntry(normalizedKey, normalizedAddress, typeof(T), ConfigurationFormat.Json);
            if (entry.Loading == null)
            {
                entry.Loading = LoadJsonEntryAsync<T>(entry).Share();
            }

            return (T)await entry.Loading;
        }

        /// <summary>
        /// 加载 Protobuf 配置；同一逻辑键的并发调用共享一次加载与反序列化。
        /// </summary>
        /// <typeparam name="T">目标 Protobuf 消息类型。</typeparam>
        /// <param name="storageKey">配置缓存使用的稳定逻辑键。</param>
        /// <param name="resourceAddress">配置资源地址。</param>
        /// <param name="parser">目标消息解析器。</param>
        /// <returns>反序列化并缓存的 Protobuf 消息。</returns>
        public async MTask<T> LoadProtobufAsync<T>(string storageKey, string resourceAddress, MessageParser<T> parser)
            where T : IMessage<T>
        {
            if (parser == null)
            {
                throw new ArgumentNullException(nameof(parser));
            }

            string normalizedKey = NormalizeRequired(storageKey, nameof(storageKey));
            string normalizedAddress = NormalizeRequired(resourceAddress, nameof(resourceAddress));
            ConfigurationEntry entry = GetOrCreateEntry(normalizedKey, normalizedAddress, typeof(T), ConfigurationFormat.Protobuf);
            if (entry.Loading == null)
            {
                entry.Loading = LoadProtobufEntryAsync(entry, parser).Share();
            }

            return (T)await entry.Loading;
        }

        /// <summary>
        /// 释放一个逻辑键的配置缓存及其资源引用。
        /// </summary>
        /// <param name="storageKey">要释放的逻辑键。</param>
        /// <returns>找到并释放缓存时返回 true。</returns>
        public bool Release(string storageKey)
        {
            string normalizedKey = NormalizeRequired(storageKey, nameof(storageKey));
            if (!entries.TryGetValue(normalizedKey, out ConfigurationEntry entry))
            {
                return false;
            }

            entries.Remove(normalizedKey);
            entry.Released = true;
            ReleaseResource(entry);
            return true;
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 获取项目启动时选定的资源服务 Provider。
        /// </summary>
        public override void Awake()
        {
            resourceService = Global.GetService<IResourceService>(this);
        }

        /// <summary>
        /// 释放全部配置缓存、资源引用和服务租约。
        /// </summary>
        protected override void OnDispose()
        {
            foreach (ConfigurationEntry entry in entries.Values)
            {
                entry.Released = true;
                ReleaseResource(entry);
            }

            entries.Clear();
            resourceService = null;
            Global.ReleaseAll(this);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 获取已有缓存条目或创建一个锁定地址、类型与格式的新条目。
        /// </summary>
        /// <param name="key">已规范化逻辑键。</param>
        /// <param name="address">已规范化资源地址。</param>
        /// <param name="valueType">目标配置类型。</param>
        /// <param name="format">配置序列化格式。</param>
        /// <returns>配置缓存条目。</returns>
        private ConfigurationEntry GetOrCreateEntry(string key, string address, Type valueType, ConfigurationFormat format)
        {
            if (!entries.TryGetValue(key, out ConfigurationEntry entry))
            {
                entry = new ConfigurationEntry(key, address, valueType, format);
                entries.Add(key, entry);
                return entry;
            }

            if (entry.Format != format || entry.ValueType != valueType || !string.Equals(entry.ResourceAddress, address, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"配置键 {key} 已绑定 {entry.Format}/{entry.ValueType.FullName}/{entry.ResourceAddress}，不能改用 {format}/{valueType.FullName}/{address}。");
            }

            return entry;
        }

        /// <summary>
        /// 加载并反序列化 JSON 配置条目。
        /// </summary>
        /// <typeparam name="T">目标配置类型。</typeparam>
        /// <param name="entry">待加载缓存条目。</param>
        /// <returns>反序列化后的配置对象。</returns>
        private async MTask<object> LoadJsonEntryAsync<T>(ConfigurationEntry entry)
        {
            try
            {
                TextAsset asset = await ResourceService.PreloadAssetAsync<TextAsset>(entry.ResourceAddress);
                entry.ResourceAcquired = true;
                ThrowIfReleased(entry);
                T value = JsonConvert.DeserializeObject<T>(asset.text);
                if (ReferenceEquals(value, null))
                {
                    throw new InvalidOperationException($"JSON 配置反序列化结果为空：{entry.ResourceAddress}。");
                }

                entry.Value = value;
                return value;
            }
            catch
            {
                RemoveFailedEntry(entry);
                throw;
            }
        }

        /// <summary>
        /// 加载并反序列化 Protobuf 配置条目。
        /// </summary>
        /// <typeparam name="T">目标 Protobuf 消息类型。</typeparam>
        /// <param name="entry">待加载缓存条目。</param>
        /// <param name="parser">目标消息解析器。</param>
        /// <returns>反序列化后的配置对象。</returns>
        private async MTask<object> LoadProtobufEntryAsync<T>(ConfigurationEntry entry, MessageParser<T> parser)
            where T : IMessage<T>
        {
            try
            {
                TextAsset asset = await ResourceService.PreloadAssetAsync<TextAsset>(entry.ResourceAddress);
                entry.ResourceAcquired = true;
                ThrowIfReleased(entry);
                T value = parser.ParseFrom(asset.bytes);
                entry.Value = value;
                return value;
            }
            catch
            {
                RemoveFailedEntry(entry);
                throw;
            }
        }

        /// <summary>
        /// 移除加载失败的缓存条目并归还可能已经取得的资源引用。
        /// </summary>
        /// <param name="entry">加载失败的缓存条目。</param>
        private void RemoveFailedEntry(ConfigurationEntry entry)
        {
            if (entries.TryGetValue(entry.Key, out ConfigurationEntry current) && ReferenceEquals(current, entry))
            {
                entries.Remove(entry.Key);
            }

            ReleaseResource(entry);
        }

        /// <summary>
        /// 释放配置条目持有的一份资源引用。
        /// </summary>
        /// <param name="entry">待释放配置条目。</param>
        private void ReleaseResource(ConfigurationEntry entry)
        {
            if (!entry.ResourceAcquired || resourceService == null)
            {
                return;
            }

            entry.ResourceAcquired = false;
            resourceService.ReleaseAsset(entry.ResourceAddress);
            entry.Value = null;
        }

        /// <summary>
        /// 在异步资源加载期间缓存已经被释放时终止反序列化。
        /// </summary>
        /// <param name="entry">刚刚取得资源引用的缓存条目。</param>
        private static void ThrowIfReleased(ConfigurationEntry entry)
        {
            if (entry.Released)
            {
                throw new ObjectDisposedException(entry.Key, "配置在异步加载完成前已被释放。");
            }
        }

        /// <summary>
        /// 校验必填文本并移除首尾空白。
        /// </summary>
        /// <param name="value">待校验文本。</param>
        /// <param name="parameterName">参数名称。</param>
        /// <returns>规范化后的非空文本。</returns>
        private static string NormalizeRequired(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("值不能为空。", parameterName);
            }

            return value.Trim();
        }

        /// <summary>
        /// 获取已经初始化的资源服务。
        /// </summary>
        /// <returns>资源服务实例。</returns>
        private IResourceService ResourceService => resourceService
            ?? throw new InvalidOperationException("配置服务尚未初始化。");

        /// <summary>
        /// 保存一个逻辑键的固定类型、格式、资源地址和共享加载结果。
        /// </summary>
        private sealed class ConfigurationEntry
        {
            #region Internal 内部成员

            internal readonly string Key; // 配置逻辑键。
            internal readonly string ResourceAddress; // 配置资源地址。
            internal readonly Type ValueType; // 反序列化目标类型。
            internal readonly ConfigurationFormat Format; // 配置序列化格式。
            internal MSharedTask<object> Loading; // 并发调用共享的加载任务。
            internal object Value; // 真实反序列化缓存。
            internal bool ResourceAcquired; // 是否持有一份资源引用。
            internal bool Released; // 是否已从服务缓存中移除并禁止继续完成加载。

            /// <summary>
            /// 创建未加载的配置缓存条目。
            /// </summary>
            /// <param name="key">配置逻辑键。</param>
            /// <param name="resourceAddress">配置资源地址。</param>
            /// <param name="valueType">反序列化目标类型。</param>
            /// <param name="format">配置格式。</param>
            internal ConfigurationEntry(string key, string resourceAddress, Type valueType, ConfigurationFormat format)
            {
                Key = key;
                ResourceAddress = resourceAddress;
                ValueType = valueType;
                Format = format;
            }

            #endregion
        }

        #endregion
    }
}
