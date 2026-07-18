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
        /// <summary>
        /// 将协议对象序列化为 UTF-8 JSON 字节。
        /// </summary>
        /// <param name="message">执行该方法所需的 message 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public byte[] Serialize<T>(T message)
        {
            string json = JsonUtility.ToJson(message);
            return Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// 将 UTF-8 JSON 字节反序列化为泛型对象。
        /// </summary>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public T Deserialize<T>(ReadOnlyMemory<byte> data)
        {
            string json = Encoding.UTF8.GetString(data.Span);
            return JsonUtility.FromJson<T>(json);
        }

        /// <summary>
        /// 将 UTF-8 JSON 字节反序列化为运行时指定类型。
        /// </summary>
        /// <param name="type">执行该方法所需的 type 参数。</param>
        /// <param name="data">执行该方法所需的 data 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public object Deserialize(Type type, ReadOnlyMemory<byte> data)
        {
            string json = Encoding.UTF8.GetString(data.Span);
            return JsonUtility.FromJson(json, type);
        }
    }
}
