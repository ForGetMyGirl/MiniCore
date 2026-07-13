using System;
using System.Text;
using Newtonsoft.Json;

namespace MiniCore.Model
{
    /// <summary>
    /// 基于 Newtonsoft.Json 的网络序列化器，支持自动属性。
    /// </summary>
    public class NewtonsoftJsonSerializer : INetworkSerializer
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings // 网络 JSON 序列化配置。
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>
        /// 将协议对象序列化为 UTF-8 JSON 字节。
        /// </summary>
        /// <param name="message">执行该方法所需的 message 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public byte[] Serialize<T>(T message)
        {
            string json = JsonConvert.SerializeObject(message, Settings);
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
            return JsonConvert.DeserializeObject<T>(json, Settings);
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
            return JsonConvert.DeserializeObject(json, type, Settings);
        }
    }
}
