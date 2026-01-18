using MiniCore.Model;

namespace MiniCore.HotUpdate
{
    public class DisconnectNotice : IProtocol
    {
        public uint Opcode => 0;
        public bool IsServerShutdown;
        public string Reason;
    }
}
