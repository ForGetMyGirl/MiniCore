using MiniCore;
using MiniCore.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MiniCore.Core
{
    /// <summary>
    /// 计时器节点
    /// </summary>
    public class Timer
    {
        public int Id { get; set; }
        public event Action TimeArrivedEvent;
        public event Action TimeEndEvent;
        private float delay;      //开始的延迟，立即开始则为0（也就是第一次执行的时间
        //private float createTime; //创建节点的时间， 实际应该执行的时间为startTime;

        private float duration;   //每次执行的时间间隔
        private int repeatTime; //重复次数

        public bool IsLifeEnd { get; set; }     //是否已经结束，当执行次数大于约定时结束

        public float CreateTime { get; set; }   //创建节点的时间， 实际应该执行的时间为startTime;
        public float StartTime => CreateTime + delay;

        public float NextExecuteTime { get; set; } //下次执行的时间

        public int ExecuteTimes { get; set; } = 0;  //已执行的次数

        public bool TimeReset { get; set; }  //需要重置时间

        public bool IsLoop { get; set; }    //是否开启无限循环
        /// <summary>
        /// 创建一个计时器节点
        /// </summary>
        /// <param name="id">计时器id</param>
        /// <param name="duration">每次执行的时间间隔</param>
        /// <param name="repeatTime">重复次数</param>
        /// <param name="delay">开始执行的延迟时间（也就是第一次执行的时间）</param>
        public Timer(int id, float duration, int repeatTime = 0, float delay = 0)
        {
            Id = id;
            this.delay = delay;
            this.duration = duration;
            this.repeatTime = repeatTime;
        }

        /// <summary>
        /// 根据当前时间生成下次执行时间（用于重置）
        /// </summary>
        /// <param name="currentTime"></param>
        public void ResetNextExecuteTime(float currentTime)
        {
            NextExecuteTime = currentTime + duration;
            TimeReset = false;
        }

        /// <summary>
        /// 执行定时器到达操作：触发事件，计算下次执行时间，
        /// </summary>
        public void Execute()
        {

            TimeArrivedEvent?.Invoke();
            //计算下次执行需要的时间
            //1、确定不是无限循环模式，2、计算是否有剩余次数
            if (!IsLoop && ++ExecuteTimes > repeatTime)
            {
                //执行结束
                IsLifeEnd = true;
                TimeEndEvent?.Invoke();
                return;
            }
            NextExecuteTime += duration;
        }

    }

    /// <summary>
    /// 定时任务管理器（*****后续改成按照完成的时间排序的方式查询，只遍历前面的节点以提高效率*****）
    /// </summary>
    public class TimerComponent : AComponent
    {
        private int timerId;         //计时器节点Id

        private Queue<Timer> addingTimerQueue;    //待增加定时器队列
        private Queue<int> removingTimerQueue;      //待删除定时器队列
        private readonly object lockObj = new object();

        private Dictionary<int, Timer> timerDic;

        public override void Awake()
        {
            addingTimerQueue = new Queue<Timer>();
            removingTimerQueue = new Queue<int>();
            timerDic = new Dictionary<int, Timer>();
        }


        /// <summary>
        /// 创建一个定时器，执行完毕会自动销毁
        /// </summary>
        /// <param name="duration">每次执行的时间间隔</param>
        /// <param name="repeatTime">重复次数</param>
        /// <param name="delay">第一次开始执行的延迟</param>
        /// <param name="timeArrived">计时器到达时执行的事件</param>
        /// <param name="timeEnd">计时器结束时执行的事件</param>
        /// <param name="isLoop">是否开启无线循环，如果开启了repeatTime将无效且不会触发timeEnd事件</param>
        /// <returns></returns>
        public int Create(float duration, int repeatTime = 0, float delay = 0, Action timeArrived = null, Action timeEnd = null, bool isLoop = false)
        {
            int id = GetTimeNodeId();
            Timer timer = new Timer(id, duration, repeatTime, delay);
            timer.IsLoop = isLoop;
            timer.TimeArrivedEvent += timeArrived;
            timer.TimeEndEvent += timeEnd;
            addingTimerQueue.Enqueue(timer);
            return id;
        }

        /// <summary>
        /// 移除一个定时器
        /// </summary>
        /// <param name="id">定时器id</param>
        public void Remove(int id)
        {
            removingTimerQueue.Enqueue(id);
        }

        private int GetTimeNodeId()
        {
            lock (lockObj)
            {
                timerId++;
            }
            return timerId;
        }

        /// <summary>
        /// 重置计时器（时间重置）
        /// </summary>
        /// <param name="id"></param>
        public void Reset(int id)
        {
            if (timerDic.TryGetValue(id, out Timer timer))
            {
                timer.TimeReset = true;
            }
        }

        protected override void Update()
        {
            AddTimer();
            RemoveTimer();
            ExecuteTimer();
        }

        /// <summary>
        /// 执行定时器
        /// </summary>
        private void ExecuteTimer()
        {
            //遍历查看有没有计时器需要触发
            foreach (var timer in timerDic.Values)
            {
                //判断该计时器是否需要重置
                if (timer.TimeReset)
                {

                    timer.ResetNextExecuteTime(Time.time);
                    continue;
                }
                //判断是否执行
                if (timer.NextExecuteTime <= Time.time) //到时间了，则执行
                {
                    timer.Execute();
                    //每次执行完毕判断当前计时器是否已经结束生命
                    if (timer.IsLifeEnd)
                    {
                        //移入待删除定时器中
                        //在下一帧执行删除操作
                        Remove(timer.Id);
                    }
                }

            }
        }

        /// <summary>
        /// 添加定时器
        /// </summary>
        private void AddTimer()
        {
            //遍历要添加的定时器
            for (int i = 0; i < addingTimerQueue.Count; i++)
            {
                Timer timer = addingTimerQueue.Dequeue();
                timer.CreateTime = Time.time;
                timer.NextExecuteTime = timer.StartTime;
                timerDic.Add(timer.Id, timer);
            }
        }

        /// <summary>
        /// 移除定时器
        /// </summary>
        private void RemoveTimer()
        {

            //遍历要删除的定时器
            for (int j = 0; j < removingTimerQueue.Count; j++)
            {
                int id = removingTimerQueue.Dequeue();
                if (timerDic.ContainsKey(id))
                {
                    timerDic.Remove(id);
                }
            }
        }
    }

}
