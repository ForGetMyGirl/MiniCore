using System;

namespace MiniCore.Serialization
{
    /// <summary>
    /// 将一段序列化字节解析为固定运行时消息类型。
    /// </summary>
    public interface IMessageParser
    {
        #region Public 公共成员

        /// <summary>
        /// 当前解析器产生的消息运行时类型。
        /// </summary>
        Type MessageType { get; }

        /// <summary>
        /// 将字节负载解析为消息对象。
        /// </summary>
        /// <param name="data">需要解析的消息字节。</param>
        /// <returns>解析完成的消息对象。</returns>
        object Parse(ReadOnlyMemory<byte> data);

        #endregion
    }
}
