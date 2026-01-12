using System;

namespace MiniCore.Model
{
    /// <summary>
    /// 序列化接口，封装协议的序列化/反序列化。
    /// </summary>
    public interface INetworkSerializer
    {
        byte[] Serialize<T>(T message);

        T Deserialize<T>(ReadOnlyMemory<byte> data);

        object Deserialize(Type type, ReadOnlyMemory<byte> data);
    }
}
