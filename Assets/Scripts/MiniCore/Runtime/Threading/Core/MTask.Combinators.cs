using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiniCore.Threading
{
    /// <summary>
    /// MTask 常用组合操作。
    /// </summary>
    public readonly partial struct MTask
    {
        #region Public 公共成员

        /// <summary>
        /// 等待两个已经启动的任务全部完成。
        /// </summary>
        /// <param name="first">第一个任务。</param>
        /// <param name="second">第二个任务。</param>
        /// <returns>两个任务均完成后的任务。</returns>
        public static async MTask WhenAll(MTask first, MTask second)
        {
            await first;
            await second;
        }

        /// <summary>
        /// 等待任务数组中的所有任务完成。
        /// </summary>
        /// <param name="tasks">已经启动的任务数组。</param>
        /// <returns>全部任务完成后的任务。</returns>
        public static async MTask WhenAll(params MTask[] tasks)
        {
            if (tasks == null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }

            for (int i = 0; i < tasks.Length; i++)
            {
                await tasks[i];
            }
        }

        /// <summary>
        /// 等待两个任务中的任意一个完成，并持续监督另一个任务直至结束。
        /// </summary>
        /// <param name="first">第一个任务。</param>
        /// <param name="second">第二个任务。</param>
        /// <returns>首先完成的任务索引，0 表示 first，1 表示 second。</returns>
        public static MTask<int> WhenAny(MTask first, MTask second)
        {
            MTaskCompletionSource<int> completion = new MTaskCompletionSource<int>();
            ObserveWhenAny(first, 0, completion).Forget();
            ObserveWhenAny(second, 1, completion).Forget();
            return completion.Task;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 消费 WhenAny 的一个候选任务并尝试提交胜出索引。
        /// </summary>
        /// <param name="task">候选任务。</param>
        /// <param name="index">候选索引。</param>
        /// <param name="completion">共享完成源。</param>
        /// <returns>候选任务监督过程。</returns>
        private static async MTask ObserveWhenAny(MTask task, int index, MTaskCompletionSource<int> completion)
        {
            try
            {
                await task;
                completion.TrySetResult(index);
            }
            catch (OperationCanceledException)
            {
                completion.TrySetCanceled();
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
        }

        #endregion
    }
}
