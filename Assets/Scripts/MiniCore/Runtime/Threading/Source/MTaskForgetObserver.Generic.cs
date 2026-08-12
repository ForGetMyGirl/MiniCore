using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// 监督一个被 Forget 的带返回值 MTask。
    /// </summary>
    /// <typeparam name="T">任务返回值类型。</typeparam>
    internal sealed class MTaskForgetObserver<T>
    {
        #region Private 私有成员

        private readonly Action continuation; // 任务完成后的稳定回调。
        private MTask<T> task; // 当前观察的任务。
        private string ownerName; // 任务 Owner 诊断名称。

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 创建观察器并缓存完成回调。
        /// </summary>
        private MTaskForgetObserver()
        {
            continuation = Complete;
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 开始监督一个带返回值 MTask。
        /// </summary>
        /// <param name="value">不再由业务 await 的任务。</param>
        internal static void Observe(MTask<T> value)
        {
            if (!MTaskObjectPool<MTaskForgetObserver<T>>.TryRent(out MTaskForgetObserver<T> observer))
            {
                observer = new MTaskForgetObserver<T>();
            }

            observer.task = value;
            observer.ownerName = MTaskRuntime.CurrentOwner?.GetType().FullName ?? MTaskRuntime.CurrentNode?.Owner?.GetType().FullName ?? "Application";
            MTaskAwaiter<T> awaiter = value.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                observer.Complete();
            }
            else
            {
                awaiter.UnsafeOnCompleted(observer.continuation);
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 消费后台任务结果并上报未处理异常。
        /// </summary>
        private void Complete()
        {
            try
            {
                task.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                MTaskSupervisor.Report(exception, ownerName);
            }
            finally
            {
                task = default;
                ownerName = null;
                MTaskObjectPool<MTaskForgetObserver<T>>.Return(this);
            }
        }

        #endregion
    }
}
