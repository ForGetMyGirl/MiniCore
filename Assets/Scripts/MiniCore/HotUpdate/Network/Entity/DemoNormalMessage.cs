using MiniCore.Model;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// Demo 普通消息（由生成器映射 opcode）。
    /// </summary>
    public class DemoNormalMessage : IProtocol
    {
        public uint Opcode => 0; // 生成器会覆盖映射
        public string Content;
    }
}
