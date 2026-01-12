using MiniCore.Model;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// 示例网络消息，需要运行 Opcode 生成器以分配 opcode。
    /// </summary>
    public class TestNetworkData : IProtocol
    {
        public uint Opcode => 0; // 生成器会覆盖映射并在运行时通过 Registry 使用
        public long RpcId { get; set; } // 若作为 RPC 请求/响应可使用；普通消息可忽略
        public int Id;
        public string Content;
    }
}
