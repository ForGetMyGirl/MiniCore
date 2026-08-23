namespace MiniCore.Server
{
    /// <summary>
    /// 允许业务层停止接收新工作并向框架报告安全关闭阻塞项。
    /// </summary>
    public interface IDedicatedServerDrainParticipant
    {
        /// <summary>
        /// 同步切换为不再接收新业务工作的状态。
        /// </summary>
        void BeginDrain();

        /// <summary>
        /// 捕获当前活动工作和阻塞原因快照。
        /// </summary>
        /// <returns>不可变 Drain 状态。</returns>
        DedicatedServerDrainStatus CaptureDrainStatus();
    }
}
