using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using MiniCore.Model;
using UnityEngine;

namespace MiniCore.Core
{
    public class TimerComponent : AComponent
    {
        private readonly List<TimerTask> tasks = new List<TimerTask>();
        private readonly List<TimerTask> pendingAdd = new List<TimerTask>();
        private readonly List<TimerTask> pendingRemove = new List<TimerTask>();
        private readonly object syncRoot = new object();

        private const int TickIntervalMs = 10;
        private Timer timer;
        private Stopwatch stopwatch;
        private double lastUnscaledTime;
        private double scaledTime;
        private SynchronizationContext unityContext;
        private bool disposed;

        public override void Awake()
        {
            base.Awake();
            unityContext = SynchronizationContext.Current;
            stopwatch = Stopwatch.StartNew();
            lastUnscaledTime = 0d;
            scaledTime = 0d;
            timer = new Timer(TimerTick, null, 0, TickIntervalMs);
        }

        public TimerTask CreateTimer(float duration, Action onComplete, bool loop = false, bool ignoreTimeScale = true, bool autoStart = true)
        {
            TimerTask task = new TimerTask(duration, onComplete, loop, ignoreTimeScale, DispatchToMainThread);
            if (!autoStart)
            {
                task.Pause();
            }
            lock (syncRoot)
            {
                pendingAdd.Add(task);
            }
            return task;
        }

        public void RemoveTimer(TimerTask task)
        {
            if (task == null) return;
            task.Stop();
            lock (syncRoot)
            {
                pendingRemove.Add(task);
            }
        }

        public void PauseAll()
        {
            lock (syncRoot)
            {
                for (int i = 0; i < tasks.Count; i++)
                {
                    tasks[i].Pause();
                }
            }
        }

        public void PauseTimer(TimerTask task)
        {
            if (task == null) return;
            lock (syncRoot)
            {
                UpdateTimesLocked();
                float currentTime = task.IgnoreTimeScale ? (float)lastUnscaledTime : (float)scaledTime;
                task.SyncTime(currentTime);
                task.Pause();
            }
        }

        public void ResumeAll()
        {
            lock (syncRoot)
            {
                for (int i = 0; i < tasks.Count; i++)
                {
                    tasks[i].Start();
                }
            }
        }

        public override void Dispose()
        {
            disposed = true;
            timer?.Dispose();
            timer = null;
            lock (syncRoot)
            {
                tasks.Clear();
                pendingAdd.Clear();
                pendingRemove.Clear();
            }
            base.Dispose();
        }

        private void TimerTick(object state)
        {
            if (disposed) return;

            lock (syncRoot)
            {
                UpdateTimesLocked();

                if (pendingAdd.Count > 0)
                {
                    tasks.AddRange(pendingAdd);
                    pendingAdd.Clear();
                }

                if (tasks.Count == 0) return;

                float unscaledNow = (float)lastUnscaledTime;
                float scaledNow = (float)scaledTime;
                for (int i = 0; i < tasks.Count; i++)
                {
                    TimerTask task = tasks[i];
                    if (task.IsStopped)
                    {
                        pendingRemove.Add(task);
                        continue;
                    }

                    float currentTime = task.IgnoreTimeScale ? unscaledNow : scaledNow;
                    task.Tick(currentTime);

                    if (task.IsStopped)
                    {
                        pendingRemove.Add(task);
                    }
                }

                if (pendingRemove.Count > 0)
                {
                    for (int i = 0; i < pendingRemove.Count; i++)
                    {
                        tasks.Remove(pendingRemove[i]);
                    }
                    pendingRemove.Clear();
                }
            }
        }

        private void UpdateTimesLocked()
        {
            double unscaledNow = stopwatch.Elapsed.TotalSeconds;
            double delta = unscaledNow - lastUnscaledTime;
            if (delta < 0d) delta = 0d;
            float timeScale = Time.timeScale;
            scaledTime += delta * timeScale;
            lastUnscaledTime = unscaledNow;
        }

        private void DispatchToMainThread(Action action)
        {
            if (action == null) return;
            if (unityContext != null)
            {
                unityContext.Post(_ => action(), null);
            }
            else
            {
                action();
            }
        }
    }

    public class TimerTask
    {
        private readonly Action onComplete;
        private readonly Action<Action> dispatcher;
        private double startTime;
        private double lastKnownTime;
        private int firedCount;
        private bool hasStarted;

        public float Duration { get; private set; }
        public float Elapsed { get; private set; }
        public bool IsLoop { get; private set; }
        public bool IgnoreTimeScale { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsStopped { get; private set; }

        internal TimerTask(float duration, Action onComplete, bool loop, bool ignoreTimeScale, Action<Action> dispatcher)
        {
            Duration = Mathf.Max(0f, duration);
            this.onComplete = onComplete;
            this.dispatcher = dispatcher;
            IsLoop = loop;
            IgnoreTimeScale = ignoreTimeScale;
        }

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

        public void Pause()
        {
            if (!IsPaused && hasStarted)
            {
                Elapsed = (float)(lastKnownTime - startTime);
            }
            IsPaused = true;
        }

        public void Stop()
        {
            IsStopped = true;
            IsPaused = true;
        }

        public void Restart()
        {
            Elapsed = 0f;
            IsStopped = false;
            IsPaused = false;
            firedCount = 0;
            hasStarted = false;
        }

        public void SetLoop(bool loop)
        {
            IsLoop = loop;
        }

        public void SetIgnoreTimeScale(bool ignoreTimeScale)
        {
            IgnoreTimeScale = ignoreTimeScale;
        }

        internal void SyncTime(float currentTime)
        {
            lastKnownTime = currentTime;
            if (!hasStarted)
            {
                startTime = currentTime - Elapsed;
                hasStarted = true;
            }
        }

        internal void Tick(float currentTime)
        {
            if (IsPaused || IsStopped) return;

            if (Duration <= 0f)
            {
                InvokeComplete();
                if (!IsLoop)
                {
                    Stop();
                }
                return;
            }

            SyncTime(currentTime);

            double elapsed = currentTime - startTime;
            if (!IsLoop)
            {
                if (elapsed >= Duration)
                {
                    InvokeComplete();
                    Elapsed = Duration;
                    Stop();
                }
                else
                {
                    Elapsed = (float)elapsed;
                }
                return;
            }

            int expectedCount = (int)Math.Floor(elapsed / Duration);
            if (expectedCount <= firedCount)
            {
                Elapsed = (float)(elapsed - firedCount * Duration);
                return;
            }

            int toFire = expectedCount - firedCount;
            for (int i = 0; i < toFire; i++)
            {
                InvokeComplete();
                if (IsStopped) return;
            }
            firedCount = expectedCount;
            Elapsed = (float)(elapsed - firedCount * Duration);
        }

        private void InvokeComplete()
        {
            if (onComplete == null) return;
            if (dispatcher != null)
            {
                dispatcher(onComplete);
            }
            else
            {
                onComplete();
            }
        }
    }
}
