using System;
using MiniCore.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace MiniCore.UI
{

    /// <summary>
    /// 将 ShowAsync 结果桥接到单次 MTask 等待者。
    /// </summary>
    /// <typeparam name="TResult">业务结果类型。</typeparam>
    internal sealed class UIWindowResultChannel<TResult> : IUIWindowResultChannel
    {
        #region Private 私有成员

        private readonly MTaskCompletionSource<TResult> completion = new MTaskCompletionSource<TResult>(); // 业务结果完成源。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取等待业务结果的任务。
        /// </summary>
        public MTask<TResult> Task => completion.Task;

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 校验类型并完成结果任务。
        /// </summary>
        /// <param name="value">Presenter 提交的结果。</param>
        public void SetResult(object value)
        {
            if (value is TResult result)
            {
                completion.TrySetResult(result);
                return;
            }

            if (value == null && !typeof(TResult).IsValueType)
            {
                completion.TrySetResult(default);
                return;
            }

            completion.TrySetException(new InvalidOperationException($"窗口结果类型不匹配，期望 {typeof(TResult).FullName}，实际 {value?.GetType().FullName ?? "<null>"}。"));
        }

        /// <summary>
        /// 以明确异常结束未提交结果的窗口等待。
        /// </summary>
        public void CloseWithoutResult()
        {
            completion.TrySetException(new InvalidOperationException("窗口关闭前未提交 ShowAsync 结果。"));
        }

        #endregion
    }
}
