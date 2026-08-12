using System;
using System.Text;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using MiniCore.Threading;
using MiniCore.UI;
using UnityEngine;

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
            View.MessageText.text = flow.Message;
        }

        #endregion
    }
}
