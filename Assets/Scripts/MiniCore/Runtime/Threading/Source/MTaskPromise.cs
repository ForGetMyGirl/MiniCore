using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// 无返回值 async MTask 的池化结果源。
    /// </summary>
    internal sealed class MTaskPromise : MTaskPromiseBase, IMTaskSource
    {
        #region Private 私有成员

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 获取并初始化一个无返回值 Promise。
        /// </summary>
        /// <returns>可用于新异步方法的 Promise。</returns>
        internal static MTaskPromise Rent()
        {
            if (!MTaskObjectPool<MTaskPromise>.TryRent(out MTaskPromise promise))
            {
                promise = new MTaskPromise();
            }

            promise.InitializePromise();
            return promise;
        }

        /// <summary>
        /// 标记异步方法主体成功结束。
        /// </summary>
        internal void SetResult()
        {
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
        public void GetResult(short token)
        {
            Exception exception = Consume(token);
            ResetPromise();
            MTaskObjectPool<MTaskPromise>.Return(this);
            if (exception != null)
            {
                throw exception;
            }
        }

        #endregion
    }
}
