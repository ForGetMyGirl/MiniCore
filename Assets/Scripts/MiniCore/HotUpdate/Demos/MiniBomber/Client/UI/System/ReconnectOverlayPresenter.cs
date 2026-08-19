using MiniCore.Core;
using MiniCore.UI;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 断线重连遮罩 Presenter。
    /// </summary>
    public sealed class ReconnectOverlayPresenter : AUIWindowPresenter<ReconnectOverlayView>
    {
        #region Private 私有成员

        private MiniBomberClientFlowComponent flow; // 客户端流程组件。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 绑定重连状态变化事件。
        /// </summary>
        protected override void OnBind()
        {
            flow = Global.Get<MiniBomberClientFlowComponent>(this);
            flow.Changed += Render;
            Bindings.Add(() => flow.Changed -= Render);
            Render();
        }

        /// <summary>
        /// 清空流程引用。
        /// </summary>
        protected override void OnDispose()
        {
            flow = null;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 渲染重连状态和尝试次数。
        /// </summary>
        private void Render()
        {
            MiniBomberClientFlowModel model = flow.Model;
            View.ShowStatus(MiniBomberClientFlowNoticeFormatter.Format(model.Notice, model.ReconnectAttempt, model.NextRetryMilliseconds, model.Detail));
        }

        #endregion
    }
}
