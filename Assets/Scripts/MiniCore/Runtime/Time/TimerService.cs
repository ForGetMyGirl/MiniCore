using System;
using System.Collections.Generic;
using MiniCore.Core;
using MiniCore.Model;

namespace MiniCore.Service
{
    /// <summary>
    /// 由 Global Tick 驱动的计时器服务，所有回调均在组件管理线程执行。
    /// </summary>
    [AppService("计时器", typeof(ITimerService), Description = "创建、暂停、恢复和移除由 Global Tick 驱动的计时任务。")]
    public sealed class TimerService : AAppService, ITimerService
    {
        #region Private 私有成员

        private readonly List<TimerTask> tasks = new List<TimerTask>(); // 当前存活任务。
        private readonly List<TimerTask> pendingAdd = new List<TimerTask>(); // Tick 期间新增任务。
        private readonly List<TimerTask> pendingRemove = new List<TimerTask>(); // Tick 期间待移除任务。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 创建一个由 Global Tick 驱动的计时任务。
        /// </summary>
        /// <param name="duration">每次触发间隔，单位秒。</param>
        /// <param name="onComplete">触发回调。</param>
        /// <param name="loop">是否循环触发。</param>
        /// <param name="ignoreTimeScale">是否使用非缩放时间。</param>
        /// <param name="autoStart">是否立即开始。</param>
        /// <returns>新建任务。</returns>
        public TimerTask CreateTimer(float duration, Action onComplete, bool loop = false, bool ignoreTimeScale = true, bool autoStart = true)
        {
            TimerTask task = new TimerTask(duration, onComplete, loop, ignoreTimeScale);
            if (!autoStart)
            {
                task.Pause();
            }

            pendingAdd.Add(task);
            return task;
        }

        /// <summary>
        /// 停止并在本次 Tick 后移除指定任务。
        /// </summary>
        /// <param name="task">要移除的任务。</param>
        public void RemoveTimer(TimerTask task)
        {
            if (task == null)
            {
                return;
            }

            task.Stop();
            pendingRemove.Add(task);
        }

        /// <summary>
        /// 暂停全部已创建任务。
        /// </summary>
        public void PauseAll()
        {
            for (int i = 0; i < tasks.Count; i++)
            {
                tasks[i].Pause();
            }
        }

        /// <summary>
        /// 暂停指定任务。
        /// </summary>
        /// <param name="task">要暂停的任务。</param>
        public void PauseTimer(TimerTask task)
        {
            task?.Pause();
        }

        /// <summary>
        /// 恢复全部任务。
        /// </summary>
        public void ResumeAll()
        {
            for (int i = 0; i < tasks.Count; i++)
            {
                tasks[i].Start();
            }
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 清空计时任务并释放组件资源。
        /// </summary>
        protected override void OnDispose()
        {
            tasks.Clear();
            pendingAdd.Clear();
            pendingRemove.Clear();
        }

        /// <summary>
        /// 在 Global Tick 中推进所有计时任务。
        /// </summary>
        protected override void Update()
        {
            if (pendingAdd.Count > 0)
            {
                tasks.AddRange(pendingAdd);
                pendingAdd.Clear();
            }

            double unscaledTime = Global.Time.UnscaledTime;
            double scaledTime = Global.Time.ScaledTime;
            for (int i = 0; i < tasks.Count; i++)
            {
                TimerTask task = tasks[i];
                task.Tick(task.IgnoreTimeScale ? unscaledTime : scaledTime);
                if (task.IsStopped)
                {
                    pendingRemove.Add(task);
                }
            }

            if (pendingRemove.Count == 0)
            {
                return;
            }

            for (int i = 0; i < pendingRemove.Count; i++)
            {
                tasks.Remove(pendingRemove[i]);
            }

            pendingRemove.Clear();
        }

        #endregion
    }
}
