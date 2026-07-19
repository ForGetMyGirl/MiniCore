namespace MiniCore.Core
{
    /// <summary>
    /// 单个会话的心跳计时与 RTT 统计状态。
    /// </summary>
    public sealed class NetworkHeartbeatState
    {
        internal int Stopped; // 会话心跳是否已被停止，使用 Volatile 读写。
        /// <summary>
        /// 最近一次收到 Pong 的 Unix 毫秒时间戳。
        /// </summary>
        public long LastPongTicks;
        /// <summary>
        /// 最近一次收到 Ping 的 Unix 毫秒时间戳。
        /// </summary>
        public long LastPingTicks;
        /// <summary>
        /// 最近一次发出 Ping 的 Unix 毫秒时间戳。
        /// </summary>
        public long LastPingSentTicks;
        /// <summary>
        /// 最近一次计算出的心跳往返耗时（毫秒）。
        /// </summary>
        public int LastRttMs;
        /// <summary>
        /// 当前统计窗口内的最小往返耗时（毫秒）。
        /// </summary>
        public int MinRttMs;
        /// <summary>
        /// 最小 RTT 统计窗口的起始 Unix 毫秒时间戳。
        /// </summary>
        public long MinRttWindowStartTicks;
        /// <summary>
        /// 当前会话的心跳角色。
        /// </summary>
        public NetworkHeartbeatMode Mode;
    }
}
