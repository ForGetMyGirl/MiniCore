using MiniCore.Model;

namespace MiniCore.HotUpdate
{
    public class DemoRpcRequest : IRequest
    {
        public uint Opcode => 0; // 由生成器覆盖映射
        public long RpcId { get; set; }
        public string Payload;
    }
}

