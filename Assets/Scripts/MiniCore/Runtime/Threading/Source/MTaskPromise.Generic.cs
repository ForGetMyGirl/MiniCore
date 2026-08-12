using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// 带返回值 async MTask 的池化结果源。
    /// </summary>
    /// <typeparam name="T">异步方法返回值类型。</typeparam>
    internal sealed class MTaskPromise<T> : MTaskPromiseBase, IMTaskSource<T>
    {
        #region Private 私有成员

        private T result; // 异步方法返回值。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 获取并初始化一个带返回值 Promise。
        /// </summary>
        /// <returns>可用于新异步方法的 Promise。</returns>
        internal static MTaskPromise<T> Rent()
        {
            if (!MTaskObjectPool<MTaskPromise<T>>.TryRent(out MTaskPromise<T> promise))
            {
                promise = new MTaskPromise<T>();
            }

            promise.result = default;
            promise.InitializePromise();
            return promise;
        }

        /// <summary>
        /// 标记异步方法主体成功结束并保存返回值。
        /// </summary>
        /// <param name="value">异步方法返回值。</param>
        internal void SetResult(T value)
        {
            result = value;
            CompleteBody();
        }

        /// <summary>
        /// 标记异步方法主体异常结束。
        /// </summary>
        /// <param name="exception">状态机异常。</param>
        internal void SetException(Exception exception)
        {
            SetPromiseException(exception);
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 获取任务状态。
        /// </summary>
        /// <param name="token">结果源版本。</param>
        /// <returns>当前任务状态。</returns>
        public MTaskStatus GetStatus(short token)
        {
            return GetPromiseStatus(token);
        }

        /// <summary>
        /// 注册任务完成续体。
        /// </summary>
        /// <param name="continuation">完成续体。</param>
        /// <param name="token">结果源版本。</param>
        public void OnCompleted(Action continuation, short token)
        {
            RegisterContinuation(continuation, token);
        }

        /// <summary>
        /// 消费任务结果并归还 Promise。
        /// </summary>
        /// <param name="token">结果源版本。</param>
        /// <returns>异步方法返回值。</returns>
        public T GetResult(short token)
        {
            Exception exception = Consume(token);
            T value = result;
            result = default;
            ResetPromise();
            MTaskObjectPool<MTaskPromise<T>>.Return(this);
            if (exception != null)
            {
                throw exception;
            }

            return value;
        }

        #endregion
    }
}
