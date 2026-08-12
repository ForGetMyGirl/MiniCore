using System;
using System.Collections.Generic;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Eventing
{

    /// <summary>
    /// 描述一次性事件等待的可选限制。
    /// 默认值表示不设置超时，等待会在事件频道销毁或当前任务取消时结束。
    /// </summary>
    public readonly struct EventWaitOptions
    {
        #region Public 公共成员

        /// <summary>
        /// 获取等待的最大时长；零表示不设置超时。
        /// </summary>
        public TimeSpan Timeout { get; }

        /// <summary>
        /// 判断当前等待是否配置了超时。
        /// </summary>
        public bool HasTimeout => Timeout > TimeSpan.Zero;

        /// <summary>
        /// 使用指定超时创建等待选项。
        /// </summary>
        /// <param name="timeout">大于零的最长等待时长。</param>
        public EventWaitOptions(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), "事件等待超时必须大于零。");
            }

            Timeout = timeout;
        }

        #endregion
    }
}
