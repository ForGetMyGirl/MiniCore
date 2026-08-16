using Google.Protobuf;
using MiniCore.Threading;

namespace MiniCore.Service
{
    /// <summary>
    /// 提供 JSON 与 Protobuf 配置的类型安全加载、共享缓存和显式释放能力。
    /// </summary>
    public interface IConfigurationService : IAppService
    {
        /// <summary>
        /// 从资源服务加载并反序列化 JSON 配置。
        /// </summary>
        /// <typeparam name="T">目标配置类型。</typeparam>
        /// <param name="storageKey">配置缓存使用的稳定逻辑键。</param>
        /// <param name="resourceAddress">配置资源地址。</param>
        /// <returns>反序列化并缓存的配置对象。</returns>
        MTask<T> LoadJsonAsync<T>(string storageKey, string resourceAddress);

        /// <summary>
        /// 从资源服务加载并反序列化 Protobuf 配置。
        /// </summary>
        /// <typeparam name="T">目标 Protobuf 消息类型。</typeparam>
        /// <param name="storageKey">配置缓存使用的稳定逻辑键。</param>
        /// <param name="resourceAddress">配置资源地址。</param>
        /// <param name="parser">目标消息解析器。</param>
        /// <returns>反序列化并缓存的 Protobuf 消息。</returns>
        MTask<T> LoadProtobufAsync<T>(string storageKey, string resourceAddress, MessageParser<T> parser)
            where T : IMessage<T>;

        /// <summary>
        /// 释放一个逻辑键的配置缓存及其资源引用。
        /// </summary>
        /// <param name="storageKey">要释放的逻辑键。</param>
        /// <returns>找到并释放缓存时返回 true。</returns>
        bool Release(string storageKey);
    }
}
