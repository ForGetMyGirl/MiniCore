using MiniCore.Model;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// Demo 普通消息（由生成器映射 opcode）。
    /// </summary>
    public class DemoNormalMessage : IProtocol
    {
        /// <summary>
        /// 协议号，由 opcode 注册表在运行时映射。
        /// </summary>
        public uint Opcode => 0; // 生成器会覆盖映射
        /// <summary>
        /// 示例消息正文。
        /// </summary>
        public string Content;
    }
}
