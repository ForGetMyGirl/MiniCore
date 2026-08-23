using System;

namespace MiniCore.Server
{
    /// <summary>
    /// 描述业务层当前能否安全关闭以及仍然存在的阻塞项。
    /// </summary>
    public sealed class DedicatedServerDrainStatus
    {
        #region Public 公共成员

        /// <summary>
        /// 获取已经没有活动业务工作。
        /// </summary>
        public bool IsDrained { get; }

        /// <summary>
        /// 获取活动业务工作总数。
        /// </summary>
        public int ActiveWorkCount { get; }

        /// <summary>
        /// 获取业务提供的可读阻塞原因。
        /// </summary>
        public string[] Blockers { get; }

        /// <summary>
        /// 创建不可变 Drain 状态。
        /// </summary>
        /// <param name="isDrained">是否已经排空。</param>
        /// <param name="activeWorkCount">活动工作数。</param>
        /// <param name="blockers">阻塞原因。</param>
        public DedicatedServerDrainStatus(bool isDrained, int activeWorkCount, string[] blockers)
        {
            IsDrained = isDrained;
            ActiveWorkCount = Math.Max(0, activeWorkCount);
            Blockers = blockers ?? Array.Empty<string>();
        }

        /// <summary>
        /// 创建没有业务阻塞项的已排空状态。
        /// </summary>
        /// <returns>共享语义的已排空状态。</returns>
        public static DedicatedServerDrainStatus Drained()
        {
            return new DedicatedServerDrainStatus(true, 0, Array.Empty<string>());
        }

        #endregion
    }
}
