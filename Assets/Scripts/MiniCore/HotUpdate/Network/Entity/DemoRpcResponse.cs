using MiniCore.Model;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// 用于演示 RPC 调用的响应协议。
    /// </summary>
    public class DemoRpcResponse : IResponse
    {
        /// <summary>
        /// 协议号，由 opcode 注册表在运行时映射。
        /// </summary>
        public uint Opcode => 0; // 由生成器覆盖映射
        /// <summary>
        /// 与请求匹配的 RPC 标识。
        /// </summary>
        public long RpcId { get; set; }
        /// <summary>
        /// 业务错误码，零表示成功。
        /// </summary>
        public int ErrorCode { get; set; }
        /// <summary>
        /// 业务执行结果描述。
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// 回显的请求文本。
        /// </summary>
        public string Echo;
    }
}
