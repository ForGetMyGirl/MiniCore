using MiniCore.Threading;
using MiniCore.Unity;

namespace MiniCore.Model
{
    /// <summary>
    /// 拥有独立打开周期 MTask 域的 UI View 基类。
    /// </summary>
    public abstract class AUIBase : AMTaskBehaviour
    {
        #region Private 私有成员

        private MTaskDomain activationDomain; // 当前窗口打开周期的任务域。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取当前窗口打开周期使用的任务域。
        /// </summary>
        /// <returns>窗口激活域；尚未打开时返回对象生命周期域。</returns>
        public override MTaskDomain GetMTaskDomain()
        {
            return activationDomain ?? base.GetMTaskDomain();
        }

        /// <summary>
        /// 创建新的窗口激活域并执行派生 View 的打开逻辑。
        /// </summary>
        /// <returns>窗口打开任务。</returns>
        public MTask OpenAsync()
        {
            activationDomain?.Dispose();
            activationDomain = new MTaskDomain($"{GetType().FullName}.Activation", MTaskExecutors.Unity);
            return OnOpenAsync();
        }

        /// <summary>
        /// 执行派生 View 的关闭逻辑并取消本次打开周期任务。
        /// </summary>
        /// <returns>窗口关闭任务。</returns>
        public MTask CloseAsync()
        {
            return CloseActivationAsync();
        }

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 执行派生 View 的异步打开逻辑。
        /// </summary>
        /// <returns>打开逻辑任务。</returns>
        protected virtual MTask OnOpenAsync()
        {
            return MTask.CompletedTask;
        }

        /// <summary>
        /// 执行派生 View 的异步关闭逻辑。
        /// </summary>
        /// <returns>关闭逻辑任务。</returns>
        protected virtual MTask OnCloseAsync()
        {
            return MTask.CompletedTask;
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 销毁 View 时取消当前打开周期和对象生命周期任务。
        /// </summary>
        protected override void OnDestroy()
        {
            activationDomain?.Dispose();
            activationDomain = null;
            base.OnDestroy();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 等待关闭逻辑完成后终止当前窗口激活域。
        /// </summary>
        /// <returns>完整关闭流程任务。</returns>
        private async MTask CloseActivationAsync()
        {
            try
            {
                await OnCloseAsync();
            }
            finally
            {
                activationDomain?.Dispose();
                activationDomain = null;
            }
        }

        #endregion
    }
}
