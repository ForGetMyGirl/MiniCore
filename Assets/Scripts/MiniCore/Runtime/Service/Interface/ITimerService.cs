using System;
using System.Collections.Generic;
using System.Threading;
using MiniCore.Threading;

namespace MiniCore.Service
{
    /// <summary>
    /// 提供由 Global Tick 驱动的计时任务创建与控制能力。
    /// </summary>
    public interface ITimerService : IAppService
    {
        /// <summary>
        /// 创建计时任务。
        /// </summary>
        /// <param name="duration">触发间隔秒数。</param>
        /// <param name="onComplete">到期回调。</param>
        /// <param name="loop">是否循环触发。</param>
        /// <param name="ignoreTimeScale">是否使用非缩放时间。</param>
        /// <param name="autoStart">是否立即开始。</param>
        /// <returns>可暂停、继续或移除的计时任务。</returns>
        TimerTask CreateTimer(float duration, Action onComplete, bool loop = false, bool ignoreTimeScale = true, bool autoStart = true);

        /// <summary>
        /// 停止并移除指定计时任务。
        /// </summary>
        /// <param name="task">待移除任务。</param>
        void RemoveTimer(TimerTask task);

        /// <summary>
        /// 暂停全部计时任务。
        /// </summary>
        void PauseAll();

        /// <summary>
        /// 恢复全部计时任务。
        /// </summary>
        void ResumeAll();
    }
}
