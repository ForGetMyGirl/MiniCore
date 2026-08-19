using MiniCore.Core;
using MiniCore.UI;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 短时消息提示 Presenter。
    /// </summary>
    public sealed class MessageToastWindowPresenter : AUIWindowPresenter<MessageToastWindowView>
    {
        #region Protected 受保护成员

        /// <summary>
        /// 显示当前流程最近提示。
        /// </summary>
        protected override void OnBind()
        {
            MiniBomberClientFlowComponent flow = Global.Get<MiniBomberClientFlowComponent>(this);
            MiniBomberClientFlowModel model = flow.Model;
            View.ShowMessage(MiniBomberClientFlowNoticeFormatter.Format(model.Notice, model.ReconnectAttempt, model.NextRetryMilliseconds, model.Detail));
        }

        #endregion
    }
}
