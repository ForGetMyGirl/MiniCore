using System;
using Google.Protobuf;

namespace MiniCore.Serialization
{
    /// <summary>
    /// 使用 Google.Protobuf 生成解析器解析固定消息类型。
    /// </summary>
    /// <typeparam name="TMessage">Protobuf 消息类型。</typeparam>
    public sealed class ProtobufMessageParser<TMessage> : IMessageParser
        where TMessage : class, IMessage<TMessage>
    {
        #region Private 私有成员

        private readonly MessageParser<TMessage> parser; // protoc 为目标消息生成的共享解析器。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 创建固定消息类型的 Protobuf 解析器适配器。
        /// </summary>
        /// <param name="parser">protoc 为消息生成的解析器。</param>
        public ProtobufMessageParser(MessageParser<TMessage> parser)
        {
            this.parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        /// <summary>
        /// 当前解析器产生的消息运行时类型。
        /// </summary>
        public Type MessageType => typeof(TMessage);

        /// <summary>
        /// 将 Protobuf 字节解析为固定消息对象。
        /// </summary>
        /// <param name="data">需要解析的 Protobuf 字节。</param>
        /// <returns>解析完成的消息对象。</returns>
        public object Parse(ReadOnlyMemory<byte> data)
        {
            return parser.ParseFrom(data.Span);
        }

        #endregion
    }
}
