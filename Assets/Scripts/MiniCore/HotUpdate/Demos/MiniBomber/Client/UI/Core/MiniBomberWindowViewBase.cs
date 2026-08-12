using MiniCore.Threading;
using MiniCore.UI;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 被动窗口 View 的无额外异步生命周期基类。
    /// </summary>
    public abstract class MiniBomberWindowViewBase : AUIWindowView
    {
        #region Protected 受保护成员

        /// <summary>
        /// 窗口打开时不执行额外异步行为。
        /// </summary>
        /// <returns>已完成任务。</returns>
        protected override MTask OnOpenAsync()
        {
            return MTask.CompletedTask;
        }

        /// <summary>
        /// 窗口关闭时不执行额外异步行为。
        /// </summary>
        /// <returns>已完成任务。</returns>
        protected override MTask OnCloseAsync()
        {
            return MTask.CompletedTask;
        }

        #endregion
    }
}
