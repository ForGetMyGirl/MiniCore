using System;
using System.Buffers;
using Google.Protobuf;
using MiniCore.Model;

namespace MiniCore.Serialization
{
    /// <summary>
    /// 使用 Google.Protobuf 进行协议对象编解码的序列化器。
    /// </summary>
    public sealed class ProtobufSerializer : INetworkSerializer
    {
        #region Private 私有成员

        private readonly IMessageParserResolver parserResolver; // 网络服务实例提供的不可变消息解析表。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 创建仅用于序列化的 Protobuf 编解码器。
        /// </summary>
        public ProtobufSerializer()
        {
        }

        /// <summary>
        /// 创建使用指定消息解析表的 Protobuf 编解码器。
        /// </summary>
        /// <param name="parserResolver">网络服务实例持有的消息解析表。</param>
        public ProtobufSerializer(IMessageParserResolver parserResolver)
        {
            this.parserResolver = parserResolver ?? throw new ArgumentNullException(nameof(parserResolver));
        }

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
        /// 将 Protobuf 消息编码到调用方提供的数组区间，并使用池化缓冲兼容不支持偏移数组写入的 Protobuf 版本。
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

            if (offset < 0 || length < 0 || offset > buffer.Length - length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            int calculatedLength = protobufMessage.CalculateSize();
            if (length != calculatedLength)
            {
                throw new ArgumentException(
                    $"目标区间长度 {length} 与 Protobuf 精确编码长度 {calculatedLength} 不一致。",
                    nameof(length));
            }

            byte[] temporaryBuffer = ArrayPool<byte>.Shared.Rent(Math.Max(length, 1));
            try
            {
                using (var output = new CodedOutputStream(temporaryBuffer))
                {
                    protobufMessage.WriteTo(output);
                    output.Flush();
                }

                Buffer.BlockCopy(temporaryBuffer, 0, buffer, offset, length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(temporaryBuffer);
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
            if (parserResolver == null)
            {
                throw new InvalidOperationException("当前 ProtobufSerializer 未配置消息解析表，不能按运行时类型反序列化。");
            }

            return parserResolver.Parse(type, data);
        }

        #endregion
    }
}
