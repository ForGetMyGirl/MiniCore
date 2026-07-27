using MiniCore.Protocol.Generated;

namespace MiniCore.EditorTests
{
    /// <summary>
    /// JSON 与 Protobuf 序列化基准共用的协议输入，确保两种实现处理完全相同的字段和值。
    /// </summary>
    internal static class NetworkSerializationBenchmarkPayload
    {
        #region Internal 内部成员

        internal const int MessageId = 1001; // 固定的非默认协议标识。
        internal const string MediumContent = "MiniCore network serialization benchmark payload. This fixed content represents a medium-sized protocol message and remains unchanged between measurements."; // 固定的中等负载业务文本。

        /// <summary>
        /// 创建供网络序列化基准复用的中等载荷协议消息。
        /// </summary>
        /// <returns>字段和值固定的协议消息。</returns>
        internal static TestNetworkData CreateMediumMessage()
        {
            return new TestNetworkData
            {
                Id = MessageId,
                Content = MediumContent
            };
        }

        #endregion
    }
}
