using MiniCore.Model;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// 通知对端即将或已经断开会话的普通协议。
    /// </summary>
    public class DisconnectNotice : IProtocol
    {
        /// <summary>
        /// 协议号，由 opcode 注册表在运行时映射。
        /// </summary>
        public uint Opcode => 0;
        /// <summary>
        /// 是否由服务端关闭导致断开。
        /// </summary>
        public bool IsServerShutdown;
        /// <summary>
        /// 断开原因说明。
        /// </summary>
        public string Reason;
    }
}
