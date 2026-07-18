using System;
using Google.Protobuf;
using MiniCore.Model;

namespace MiniCore.Serialization
{
    /// <summary>
    /// 使用 Google.Protobuf 进行协议对象编解码的序列化器。
    /// </summary>
    public sealed class ProtobufSerializer : INetworkSerializer
    {
        /// <summary>
        /// 将 Protobuf 消息编码为字节数组。
        /// </summary>
        /// <typeparam name="T">消息类型。</typeparam>
        /// <param name="message">待编码消息。</param>
        /// <returns>Protobuf 字节。</returns>
        public byte[] Serialize<T>(T message)
        {
            if (!(message is IMessage protobufMessage))
            {
                throw new ArgumentException($"消息 {typeof(T).FullName} 未实现 Google.Protobuf.IMessage。", nameof(message));
            }

            return protobufMessage.ToByteArray();
        }

        /// <summary>
        /// 按泛型类型解析 Protobuf 字节。
        /// </summary>
        /// <typeparam name="T">目标消息类型。</typeparam>
        /// <param name="data">Protobuf 字节。</param>
        /// <returns>解析后的消息。</returns>
        public T Deserialize<T>(ReadOnlyMemory<byte> data)
        {
            return (T)Deserialize(typeof(T), data);
        }

        /// <summary>
        /// 按运行时类型解析 Protobuf 字节。
        /// </summary>
        /// <param name="type">目标消息类型。</param>
        /// <param name="data">Protobuf 字节。</param>
        /// <returns>解析后的消息。</returns>
        public object Deserialize(Type type, ReadOnlyMemory<byte> data)
        {
            return ProtobufMessageRegistry.Parse(type, data);
        }
    }
}
