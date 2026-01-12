using System;
using System.Text;
using UnityEngine;

namespace MiniCore.Model
{
    /// <summary>
    /// 基于 UnityEngine.JsonUtility 的简易序列化器（需消息为可序列化的 class/struct）。
    /// 可替换为 MessagePack/ProtoBuf 等更强方案。
    /// </summary>
    public class UnityJsonSerializer : INetworkSerializer
    {
        public byte[] Serialize<T>(T message)
        {
            string json = JsonUtility.ToJson(message);
            return Encoding.UTF8.GetBytes(json);
        }

        public T Deserialize<T>(ReadOnlyMemory<byte> data)
        {
            string json = Encoding.UTF8.GetString(data.ToArray());
            return JsonUtility.FromJson<T>(json);
        }

        public object Deserialize(Type type, ReadOnlyMemory<byte> data)
        {
            string json = Encoding.UTF8.GetString(data.ToArray());
            return JsonUtility.FromJson(json, type);
        }
    }
}
