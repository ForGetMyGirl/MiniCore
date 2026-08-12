using System;
using System.Collections.Generic;
using MiniCore.Core;
using MiniCore.Model;

namespace MiniCore.Service
{

    /// <summary>
    /// 由 TimerService 推进的单个计时任务。
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
