using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiniCore.Threading
{
    /// <summary>
    /// System Task 等外部异步 API 到 MTask 的集中适配入口。
    /// </summary>
    public static class MTaskExternal
    {
        #region Public 公共成员

        /// <summary>
        /// 等待外部无返回值 Task，并恢复到调用方捕获的 MTask 执行器。
        /// </summary>
        /// <param name="task">外部 Task。</param>
        /// <returns>对应的 MTask。</returns>
        public static async MTask Await(Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            await task.ConfigureAwait(false);
        }

        /// <summary>
        /// 等待外部带返回值 Task，并恢复到调用方捕获的 MTask 执行器。
        /// </summary>
        /// <typeparam name="T">外部 Task 返回值类型。</typeparam>
        /// <param name="task">外部 Task。</param>
        /// <returns>携带外部结果的 MTask。</returns>
        public static async MTask<T> Await<T>(Task<T> task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            return await task.ConfigureAwait(false);
        }

        /// <summary>
        /// 获取当前 MTask 节点的延迟创建 CancellationToken，供外部 API 边界使用。
        /// </summary>
        /// <returns>随当前结构化节点取消的 Token；没有节点时返回默认值。</returns>
        public static CancellationToken GetCancellationToken()
        {
            return MTaskRuntime.CurrentNode?.GetExternalCancellationToken() ?? default;
        }

        #endregion
    }
}
