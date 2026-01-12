using MiniCore.Model;

namespace MiniCore.HotUpdate
{
    public class DemoRpcResponse : IResponse
    {
        public uint Opcode => 0; // 由生成器覆盖映射
        public long RpcId { get; set; }
        public int ErrorCode { get; set; }
        public string Message { get; set; }
        public string Echo;
    }
}

