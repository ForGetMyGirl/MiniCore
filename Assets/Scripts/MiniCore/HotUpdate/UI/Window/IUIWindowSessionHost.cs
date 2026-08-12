using System;
using MiniCore.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace MiniCore.UI
{
    /// <summary>
    /// UIService 向窗口会话提供的资源、缓存和完成回调。
    /// </summary>
    internal interface IUIWindowSessionHost
    {
        /// <summary>
        /// 获取当前持久化 UI Root。
        /// </summary>
        ApplicationUIRoot Root { get; }

        /// <summary>
        /// 获取项目 UI Profile。
        /// </summary>
        UIProjectProfile Profile { get; }

        /// <summary>
        /// 获取窗口服务公共接口。
        /// </summary>
        IUIService Service { get; }

        /// <summary>
        /// 获取缓存 View 或加载一个新实例。
        /// </summary>
        /// <param name="definition">窗口定义。</param>
        /// <returns>准备进入 Staging 的 View。</returns>
        MTask<AUIWindowView> AcquireViewAsync(UIWindowDefinition definition);

        /// <summary>
        /// 归还或销毁一个关闭后的 View。
        /// </summary>
        /// <param name="definition">窗口定义。</param>
        /// <param name="view">待回收 View。</param>
        void ReleaseView(UIWindowDefinition definition, AUIWindowView view);

        /// <summary>
        /// 通知服务会话已进入唯一终态。
        /// </summary>
        /// <param name="session">完成的窗口会话。</param>
        void CompleteSession(UIWindowSession session);
    }
}
