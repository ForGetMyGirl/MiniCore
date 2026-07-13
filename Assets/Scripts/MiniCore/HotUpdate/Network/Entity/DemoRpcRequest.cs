using MiniCore.Model;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// 用于演示 RPC 调用的请求协议。
    /// </summary>
    public class DemoRpcRequest : IRequest
    {
        /// <summary>
        /// 协议号，由 opcode 注册表在运行时映射。
        /// </summary>
        public uint Opcode => 0; // 由生成器覆盖映射
        /// <summary>
        /// 用于匹配响应的 RPC 标识。
        /// </summary>
        public long RpcId { get; set; }
        /// <summary>
        /// 请求携带的示例文本。
        /// </summary>
        public string Payload;
    }
}
