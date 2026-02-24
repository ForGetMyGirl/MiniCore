using MiniCore.Model;

namespace MiniCore.Core
{
    public struct NetworkIncomingPacket
    {
        public NetworkSession Session;
        public byte[] Buffer;
        public int Length;
    }
}
