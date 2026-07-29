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
        /// 获取 Protobuf 消息编码后的精确字节长度，供网络层预租用完整包缓冲区。
        /// </summary>
        /// <typeparam name="T">需要计算大小的消息类型。</typeparam>
        /// <param name="message">需要计算编码长度的消息。</param>
        /// <returns>Protobuf 正文的精确字节长度。</returns>
        public int GetSerializedSize<T>(T message)
        {
            if (!(message is IMessage protobufMessage))
            {
                throw new ArgumentException($"消息 {typeof(T).FullName} 未实现 Google.Protobuf.IMessage。", nameof(message));
            }

            return protobufMessage.CalculateSize();
        }

        /// <summary>
        /// 将 Protobuf 消息直接编码到调用方提供的数组区间，避免创建正文临时数组。
        /// </summary>
        /// <typeparam name="T">需要编码的消息类型。</typeparam>
        /// <param name="message">需要编码的消息。</param>
        /// <param name="buffer">已预留完整正文容量的目标数组。</param>
        /// <param name="offset">正文写入起始位置。</param>
        /// <param name="length">允许写入的正文长度。</param>
        public void SerializeInto<T>(T message, byte[] buffer, int offset, int length)
        {
            if (!(message is IMessage protobufMessage))
            {
                throw new ArgumentException($"消息 {typeof(T).FullName} 未实现 Google.Protobuf.IMessage。", nameof(message));
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0 || length < 0 || offset + length > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            using (var output = new CodedOutputStream(buffer))
            {
                protobufMessage.WriteTo(output);
                output.Flush();
            }

            if (offset != 0 && length > 0)
            {
                Buffer.BlockCopy(buffer, 0, buffer, offset, length);
            }
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
