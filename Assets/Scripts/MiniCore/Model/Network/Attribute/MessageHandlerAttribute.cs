using System;

namespace MiniCore.Model
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class MessageHandlerAttribute : Attribute
    {
        public uint Opcode { get; }

        public MessageHandlerAttribute(uint opcode)
        {
            Opcode = opcode;
        }
    }
}
