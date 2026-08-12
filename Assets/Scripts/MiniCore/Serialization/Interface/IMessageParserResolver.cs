using System;

namespace MiniCore.Serialization
{
    /// <summary>
    /// 根据消息运行时类型选择已注册解析器。
    /// </summary>
    public interface IMessageParserResolver
    {
        #region Public 公共成员

        /// <summary>
        /// 使用目标类型对应的解析器解析消息字节。
        /// </summary>
        /// <param name="messageType">目标消息运行时类型。</param>
        /// <param name="data">需要解析的消息字节。</param>
        /// <returns>解析完成的消息对象。</returns>
        object Parse(Type messageType, ReadOnlyMemory<byte> data);

        #endregion
    }
}
