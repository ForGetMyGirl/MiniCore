using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniCore.Model
{
    public static class EventCenter {

        //protected override void Init() {
        //    DontDestroyOnLoad(this);
        //}

        public static Dictionary<string, Delegate> globalEventDic = new Dictionary<string, Delegate>();

        #region 添加监听
        /// <summary>
        /// 添加无参事件监听
        /// </summary>
        /// <param name="gameEvent"></param>
        /// <param name="action"></param>
        public static void AddListener(string gameEvent, Action action) {
            CreateOrThrow(gameEvent, action);
            globalEventDic[gameEvent] = (Action)globalEventDic[gameEvent] + action;
        }

        /// <summary>
        /// 添加一个参数的事件监听
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="gameEvent"></param>
        /// <param name="action"></param>
        public static void AddListener<T>(string gameEvent, Action<T> action) {
            CreateOrThrow(gameEvent, action);
            globalEventDic[gameEvent] = (Action<T>)globalEventDic[gameEvent] + action;
        }

        public static void AddListener<T, K>(string gameEvent, Action<T, K> action) {
            CreateOrThrow(gameEvent, action);
            globalEventDic[gameEvent] = (Action<T, K>)globalEventDic[gameEvent] + action;
        }

        #endregion

        #region 移除监听
        /// <summary>
        /// 移除无参事件监听
        /// </summary>
        /// <param name="gameEvent"></param>
        /// <param name="action"></param>
        public static void RemoveListener(string gameEvent, Action action) {
            RemoveOrThrow(gameEvent, action);
            globalEventDic[gameEvent] = (Action)globalEventDic[gameEvent] - action;
            RemoveNullEvent(gameEvent);
        }

        /// <summary>
        /// 移除一个参数的事件监听
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="gameEvent"></param>
        /// <param name="action"></param>
        public static void RemoveListener<T>(string gameEvent, Action<T> action) {
            RemoveOrThrow(gameEvent, action);
            globalEventDic[gameEvent] = (Action<T>)globalEventDic[gameEvent] - action;
            RemoveNullEvent(gameEvent);
        }

        public static void RemoveListener<T, K>(string gameEvent, Action<T, K> action) {
            RemoveOrThrow(gameEvent, action);
            globalEventDic[gameEvent] = (Action<T, K>)globalEventDic[gameEvent] - action;
            RemoveNullEvent(gameEvent);
        }


        #endregion


        #region 广播事件
        /// <summary>
        /// 广播无参事件
        /// </summary>
        /// <param name="gameEvent"></param>
        public static void Broadcast(string gameEvent) {
            if (globalEventDic.TryGetValue(gameEvent, out Delegate action)) {
                Action callback = action as Action;
                if (callback != null) {
                    callback();
                } else {
                    throw new Exception($"广播消息类型错误：要广播的事件{gameEvent}与存在的事件类型{action.GetType()}不符");
                }
            }
        }

        /// <summary>
        /// 广播带有一个参数的事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="gameEvent"></param>
        /// <param name="arg"></param>
        public static void Broadcast<T>(string gameEvent, T arg) {
            if (globalEventDic.TryGetValue(gameEvent, out Delegate action)) {
                Action<T> callback = action as Action<T>;
                if (callback != null) {
                    callback(arg);
                } else {
                    throw new Exception($"广播消息类型错误：要广播的事件{gameEvent}与存在的事件类型{action.GetType()}不符");
                }
            }
        }

        public static void Broadcast<T, K>(string gameEvent, T arg1, K arg2) {
            if (globalEventDic.TryGetValue(gameEvent, out Delegate action)) {
                Action<T, K> callback = action as Action<T, K>;
                if (callback != null)
                    callback(arg1, arg2);
                else {
                    throw new Exception($"广播消息类型错误：要广播的事件{gameEvent}与存在的事件类型{action.GetType()}不符");
                }
            }
        }

        #endregion



        #region 内部方法
        /// <summary>
        /// 创建新的类型或者抛出异常
        /// </summary>
        /// <param name="gameEvent"></param>
        /// <param name="callback"></param>
        private static void CreateOrThrow(string gameEvent, Delegate callback) {
            if (globalEventDic.TryGetValue(gameEvent, out Delegate val)) {
                //如果已经有该类型的
                if (callback.GetType() != val.GetType()) {
                    //如果要添加的类型和已有类型不匹配
                    throw new Exception($"当前已有{gameEvent}事件，新添加的{callback.GetType()}委托类型与原类型{val.GetType()}不符");
                }
                //如果类型匹配
            } else {
                globalEventDic.Add(gameEvent, null);
            }
        }

        //移除或抛出异常
        private static void RemoveOrThrow(string gameEvent, Delegate callback) {
            if (!globalEventDic.TryGetValue(gameEvent, out Delegate val)) {
                throw new Exception($"移除监听失败：不存在的事件类型'{gameEvent}'");
            } else {
                if (val == null) {
                    throw new Exception($"移除监听失败：事件类型'{gameEvent}'没有对应的委托事件");
                } else if (val.GetType() != callback.GetType()) {
                    throw new Exception($"移除监听失败，要移除委托事件类型{callback.GetType()}与已存在的时间类型{val.GetType()}不符");
                }
            }
        }

        /// <summary>
        /// 移除没有委托事件的类型
        /// </summary>
        /// <param name="gameEvent"></param>
        private static void RemoveNullEvent(string gameEvent) {
            if (globalEventDic[gameEvent] == null)
                globalEventDic.Remove(gameEvent);
        }

        #endregion

    }

}