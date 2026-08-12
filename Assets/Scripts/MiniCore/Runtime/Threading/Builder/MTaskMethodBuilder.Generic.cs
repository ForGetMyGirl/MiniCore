using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// 带返回值 MTask 的自定义异步方法构建器。
    /// </summary>
    /// <typeparam name="T">异步方法返回值类型。</typeparam>
    public struct MTaskMethodBuilder<T>
    {
        #region Private 私有成员

        private MTaskPromise<T> promise; // 状态机共享的池化结果源。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取当前异步方法返回的 MTask。
        /// </summary>
        public MTask<T> Task => new MTask<T>(promise, promise.Version);

        /// <summary>
        /// 创建异步方法构建器并建立结构化任务节点。
        /// </summary>
        /// <returns>初始化后的构建器。</returns>
        public static MTaskMethodBuilder<T> Create()
        {
            return new MTaskMethodBuilder<T>
            {
                promise = MTaskPromise<T>.Rent()
            };
        }

        /// <summary>
        /// 启动编译器生成的异步状态机。
        /// </summary>
        /// <typeparam name="TStateMachine">状态机类型。</typeparam>
        /// <param name="stateMachine">状态机实例。</param>
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            promise.Start(ref stateMachine);
        }

        /// <summary>
        /// 通知构建器异步方法已经成功结束。
        /// </summary>
        /// <param name="result">异步方法返回值。</param>
        public void SetResult(T result)
        {
            promise.SetResult(result);
        }

        /// <summary>
        /// 通知构建器异步方法因异常结束。
        /// </summary>
        /// <param name="exception">状态机抛出的异常。</param>
        public void SetException(Exception exception)
        {
            promise.SetException(exception);
        }

        /// <summary>
        /// 注册实现安全完成通知的 awaiter。
        /// </summary>
        /// <typeparam name="TAwaiter">awaiter 类型。</typeparam>
        /// <typeparam name="TStateMachine">状态机类型。</typeparam>
        /// <param name="awaiter">当前等待器。</param>
        /// <param name="stateMachine">当前状态机。</param>
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(promise.GetStateMachineContinuation(ref stateMachine));
        }

        /// <summary>
        /// 注册不捕获 ExecutionContext 的 awaiter。
        /// </summary>
        /// <typeparam name="TAwaiter">awaiter 类型。</typeparam>
        /// <typeparam name="TStateMachine">状态机类型。</typeparam>
        /// <param name="awaiter">当前等待器。</param>
        /// <param name="stateMachine">当前状态机。</param>
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(promise.GetStateMachineContinuation(ref stateMachine));
        }

        /// <summary>
        /// 兼容 IAsyncStateMachine 的显式设置入口。
        /// </summary>
        /// <param name="stateMachine">编译器生成的状态机。</param>
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }

        #endregion
    }
}
