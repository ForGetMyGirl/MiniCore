using System;
using System.Text;
using Newtonsoft.Json;

namespace MiniCore.Model
{
    /// <summary>
    /// Newtonsoft.Json based serializer to support auto-properties.
    /// </summary>
    public class NewtonsoftJsonSerializer : INetworkSerializer
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        public byte[] Serialize<T>(T message)
        {
            string json = JsonConvert.SerializeObject(message, Settings);
            return Encoding.UTF8.GetBytes(json);
        }

        public T Deserialize<T>(ReadOnlyMemory<byte> data)
        {
            string json = Encoding.UTF8.GetString(data.Span);
            return JsonConvert.DeserializeObject<T>(json, Settings);
        }

        public object Deserialize(Type type, ReadOnlyMemory<byte> data)
        {
            string json = Encoding.UTF8.GetString(data.Span);
            return JsonConvert.DeserializeObject(json, type, Settings);
        }
    }
}
