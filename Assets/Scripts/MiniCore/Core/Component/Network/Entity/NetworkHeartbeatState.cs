using System.Threading;

namespace MiniCore.Core
{
    public sealed class NetworkHeartbeatState
    {
        public CancellationTokenSource Cts;
        public long LastPongTicks;
        public long LastPingTicks;
        public long LastPingSentTicks;
        public int LastRttMs;
        public int MinRttMs;
        public long MinRttWindowStartTicks;
        public NetworkHeartbeatMode Mode;
    }
}
