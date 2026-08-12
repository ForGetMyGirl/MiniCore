using MiniCore.Threading;
using UnityEngine;

namespace MiniCore.UI
{
    /// <summary>
    /// 窗口入场和退场动画的可插拔驱动契约。
    /// </summary>
    public interface IUITransitionDriver
    {
        /// <summary>
        /// 记录原始状态并切换到入场起始状态。
        /// </summary>
        void ResetToEnterState();

        /// <summary>
        /// 播放窗口入场动画。
        /// </summary>
        /// <returns>动画完成任务。</returns>
        MTask PlayEnterAsync();

        /// <summary>
        /// 播放窗口退场动画。
        /// </summary>
        /// <returns>动画完成任务。</returns>
        MTask PlayExitAsync();

        /// <summary>
        /// 按指定方式中断当前动画。
        /// </summary>
        /// <param name="mode">动画收敛方式。</param>
        void Interrupt(UITransitionInterruptMode mode);

        /// <summary>
        /// 立即应用指定阶段的结束状态。
        /// </summary>
        /// <param name="phase">需要完成的阶段。</param>
        void CompleteImmediately(UITransitionPhase phase);

        /// <summary>
        /// 恢复首次播放前记录的 Transform 与透明度。
        /// </summary>
        void RestoreOriginalState();
    }
}
