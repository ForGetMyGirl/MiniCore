using System;

namespace MiniCore.Model
{
    /// <summary>
    /// 序列化接口，封装协议的序列化/反序列化。
    /// </summary>
    public interface INetworkSerializer
    {
        /// <summary>
        /// 将协议对象序列化为 UTF-8 等字节负载。
        /// </summary>
        byte[] Serialize<T>(T message);

        /// <summary>
        /// 将字节负载反序列化为指定泛型协议对象。
        /// </summary>
        T Deserialize<T>(ReadOnlyMemory<byte> data);

        /// <summary>
        /// 将字节负载反序列化为运行时指定的协议类型。
        /// </summary>
        object Deserialize(Type type, ReadOnlyMemory<byte> data);
    }
}
