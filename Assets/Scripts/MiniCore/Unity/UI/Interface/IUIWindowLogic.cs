using System;
using System.Collections.Generic;
using MiniCore.Eventing;
using MiniCore.Threading;
using MiniCore.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MiniCore.UI
{

    /// <summary>
    /// WindowSession 可创建和释放的窗口逻辑统一契约。
    /// </summary>
    public interface IUIWindowLogic : IDisposable, IMTaskOwner
    {
        /// <summary>
        /// 绑定窗口上下文与实际 View。
        /// </summary>
        /// <param name="context">当前窗口上下文。</param>
        /// <param name="view">当前窗口 View。</param>
        void Bind(UIWindowContext context, AUIWindowView view);

        /// <summary>
        /// 执行窗口进入 Active 前的业务激活逻辑。
        /// </summary>
        /// <returns>业务激活完成任务。</returns>
        MTask ActivateAsync();

        /// <summary>
        /// 执行窗口离开 Active 时的业务退场逻辑。
        /// </summary>
        /// <returns>业务退场完成任务。</returns>
        MTask DeactivateAsync();
    }
}
