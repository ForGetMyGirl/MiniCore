using System;
using System.Collections.Generic;
using Google.Protobuf;

namespace MiniCore.Model
{
    /// <summary>
    /// 由生成代码填充的 Protobuf 消息解析器注册表。
    /// </summary>
    public static partial class ProtobufMessageRegistry
    {
        #region Private 私有成员

        private static readonly Dictionary<Type, MessageParser> parsers = new Dictionary<Type, MessageParser>(); // 消息类型到 Parser 的映射。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 按消息运行时类型解析 Protobuf 字节。
        /// </summary>
        /// <param name="messageType">目标消息类型。</param>
        /// <param name="data">消息字节。</param>
        /// <returns>反序列化后的消息对象。</returns>
        public static object Parse(Type messageType, ReadOnlyMemory<byte> data)
        {
            if (messageType == null)
            {
                throw new ArgumentNullException(nameof(messageType));
            }

            if (!parsers.TryGetValue(messageType, out MessageParser parser))
            {
                throw new InvalidOperationException($"未注册 Protobuf 消息 Parser：{messageType.FullName}");
            }

            return parser.ParseFrom(data.Span);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 注册生成消息的解析器。
        /// </summary>
        /// <typeparam name="T">消息类型。</typeparam>
        /// <param name="parser">生成消息的 Parser。</param>
        private static void Register<T>(MessageParser<T> parser) where T : IMessage<T>
        {
            parsers[typeof(T)] = parser;
        }

        /// <summary>
        /// 由生成分部文件填充所有协议 Parser。
        /// </summary>
        static partial void RegisterGenerated();

        /// <summary>
        /// 初始化生成 Parser 映射。
        /// </summary>
        static ProtobufMessageRegistry()
        {
            RegisterGenerated();
        }

        #endregion
    }
}
