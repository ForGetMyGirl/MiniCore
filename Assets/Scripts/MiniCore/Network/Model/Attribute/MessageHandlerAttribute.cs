using System;

namespace MiniCore.Model
{
    /// <summary>
    /// 为消息处理器显式声明 opcode 的特性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class MessageHandlerAttribute : Attribute
    {
        /// <summary>
        /// 处理器对应的协议号。
        /// </summary>
        public uint Opcode { get; }

        /// <summary>
        /// 使用指定协议号创建处理器标记。
        /// </summary>
        /// <param name="opcode">执行该方法所需的 opcode 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public MessageHandlerAttribute(uint opcode)
        {
            Opcode = opcode;
        }
    }
}
