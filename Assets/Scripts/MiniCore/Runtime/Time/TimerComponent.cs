using System;
using System.Collections.Generic;
using MiniCore.Model;

namespace MiniCore.Core
{
    /// <summary>
    /// 由 Global Tick 驱动的计时器组件，所有回调均在组件管理线程执行。
    /// </summary>
    [MiniCoreStartupModule("计时器")]
    public sealed class TimerComponent : AComponent
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
        public override void Dispose()
        {
            tasks.Clear();
            pendingAdd.Clear();
            pendingRemove.Clear();
            base.Dispose();
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

    /// <summary>
    /// 由 TimerComponent 推进的单个计时任务。
    /// </summary>
    public sealed class TimerTask
    {
        #region Private 私有成员

        private readonly Action onComplete; // 触发回调。
        private double startTime; // 本轮计时起点。
        private double lastKnownTime; // 最近一次推进时间。
        private int firedCount; // 循环任务已触发次数。
        private bool hasStarted; // 是否已经建立起点。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取任务每轮时长，单位秒。
        /// </summary>
        public float Duration { get; }

        /// <summary>
        /// 获取本轮已过去的时长。
        /// </summary>
        public float Elapsed { get; private set; }

        /// <summary>
        /// 获取或设置任务是否循环。
        /// </summary>
        public bool IsLoop { get; private set; }

        /// <summary>
        /// 获取或设置任务是否使用非缩放时间。
        /// </summary>
        public bool IgnoreTimeScale { get; private set; }

        /// <summary>
        /// 获取任务是否暂停。
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// 获取任务是否已经停止。
        /// </summary>
        public bool IsStopped { get; private set; }

        /// <summary>
        /// 使用任务配置创建计时任务。
        /// </summary>
        /// <param name="duration">每轮时长。</param>
        /// <param name="onComplete">触发回调。</param>
        /// <param name="loop">是否循环。</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放。</param>
        internal TimerTask(float duration, Action onComplete, bool loop, bool ignoreTimeScale)
        {
            Duration = Math.Max(0f, duration);
            this.onComplete = onComplete;
            IsLoop = loop;
            IgnoreTimeScale = ignoreTimeScale;
        }

        /// <summary>
        /// 开始或恢复任务。
        /// </summary>
        public void Start()
        {
            if (IsStopped)
            {
                Elapsed = 0f;
                IsStopped = false;
                firedCount = 0;
                hasStarted = false;
            }

            IsPaused = false;
        }

        /// <summary>
        /// 暂停任务。
        /// </summary>
        public void Pause()
        {
            if (hasStarted)
            {
                Elapsed = (float)(lastKnownTime - startTime);
            }

            IsPaused = true;
        }

        /// <summary>
        /// 停止任务。
        /// </summary>
        public void Stop()
        {
            IsStopped = true;
            IsPaused = true;
        }

        /// <summary>
        /// 将任务重置为未开始状态。
        /// </summary>
        public void Restart()
        {
            Elapsed = 0f;
            IsStopped = false;
            IsPaused = false;
            firedCount = 0;
            hasStarted = false;
        }

        /// <summary>
        /// 设置循环模式。
        /// </summary>
        /// <param name="loop">是否循环。</param>
        public void SetLoop(bool loop)
        {
            IsLoop = loop;
        }

        /// <summary>
        /// 设置时间来源模式。
        /// </summary>
        /// <param name="ignoreTimeScale">是否使用非缩放时间。</param>
        public void SetIgnoreTimeScale(bool ignoreTimeScale)
        {
            IgnoreTimeScale = ignoreTimeScale;
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 使用本次 Tick 时间推进任务。
        /// </summary>
        /// <param name="currentTime">当前时间。</param>
        internal void Tick(double currentTime)
        {
            if (IsPaused || IsStopped)
            {
                return;
            }

            lastKnownTime = currentTime;
            if (!hasStarted)
            {
                startTime = currentTime - Elapsed;
                hasStarted = true;
            }

            if (Duration <= 0f)
            {
                onComplete?.Invoke();
                if (!IsLoop)
                {
                    Stop();
                }

                return;
            }

            double elapsed = currentTime - startTime;
            if (!IsLoop)
            {
                Elapsed = (float)Math.Min(elapsed, Duration);
                if (elapsed >= Duration)
                {
                    onComplete?.Invoke();
                    Stop();
                }

                return;
            }

            int expectedCount = (int)Math.Floor(elapsed / Duration);
            while (firedCount < expectedCount && !IsStopped)
            {
                firedCount++;
                onComplete?.Invoke();
            }

            Elapsed = (float)(elapsed - firedCount * Duration);
        }

        #endregion
    }
}
