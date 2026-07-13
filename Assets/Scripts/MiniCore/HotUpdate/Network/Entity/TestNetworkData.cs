using MiniCore.Model;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// 示例网络消息，需要运行 Opcode 生成器以分配 opcode。
    /// </summary>
    public class TestNetworkData : IProtocol
    {
        /// <summary>
        /// 协议号，由 opcode 注册表在运行时映射。
        /// </summary>
        public uint Opcode => 0; // 生成器会覆盖映射并在运行时通过 Registry 使用
        /// <summary>
        /// 可选 RPC 标识，普通消息发送时通常为零。
        /// </summary>
        public long RpcId { get; set; } // 若作为 RPC 请求/响应可使用；普通消息可忽略
        /// <summary>
        /// 示例消息标识。
        /// </summary>
        public int Id;
        /// <summary>
        /// 示例消息内容。
        /// </summary>
        public string Content;
    }
}
