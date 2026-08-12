using System;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.UI
{

    /// <summary>
    /// 提供强类型窗口打开、导航、预加载、关闭与聚焦能力。
    /// </summary>
    public interface IUIService : IAppService
    {
        /// <summary>
        /// 按编辑器生成的稳定路由名称打开窗口，供数据驱动流程使用。
        /// </summary>
        /// <param name="routeName">窗口 Authoring 中的稳定 RouteName。</param>
        /// <returns>活动窗口句柄。</returns>
        MTask<UIWindowHandle> OpenAsync(string routeName);

        /// <summary>
        /// 打开不带业务参数的窗口。
        /// </summary>
        /// <typeparam name="TRoute">生成的窗口路由。</typeparam>
        /// <returns>活动窗口句柄。</returns>
        MTask<UIWindowHandle> OpenAsync<TRoute>() where TRoute : IUIWindowRoute;

        /// <summary>
        /// 使用强类型参数打开窗口。
        /// </summary>
        /// <typeparam name="TRoute">生成的窗口路由。</typeparam>
        /// <param name="args">只允许用于该路由的参数。</param>
        /// <returns>活动窗口句柄。</returns>
        MTask<UIWindowHandle> OpenAsync<TRoute>(IUIWindowArgs<TRoute> args) where TRoute : IUIWindowRoute;

        /// <summary>
        /// 将目标全屏窗口导航到其导航组顶部。
        /// </summary>
        /// <typeparam name="TRoute">生成的窗口路由。</typeparam>
        /// <returns>导航完成任务。</returns>
        MTask NavigateAsync<TRoute>() where TRoute : IUIWindowRoute;

        /// <summary>
        /// 按稳定路由名称切换 Screen 导航组顶部窗口。
        /// </summary>
        /// <param name="routeName">窗口 Authoring 中的稳定 RouteName。</param>
        /// <returns>导航完成任务。</returns>
        MTask NavigateAsync(string routeName);

        /// <summary>
        /// 关闭指定导航组当前的 Screen 窗口，使应用进入没有全屏窗口的流程状态。
        /// </summary>
        /// <param name="navigationGroup">窗口 Authoring 中的导航组名称。</param>
        /// <returns>当前 Screen 关闭完成任务；导航组为空时立即完成。</returns>
        MTask CloseNavigationAsync(string navigationGroup);

        /// <summary>
        /// 预加载目标窗口资源和可配置数量的 View。
        /// </summary>
        /// <typeparam name="TRoute">生成的窗口路由。</typeparam>
        /// <param name="count">希望准备的缓存实例数。</param>
        /// <returns>预加载完成任务。</returns>
        MTask PrefetchAsync<TRoute>(int count = 1) where TRoute : IUIWindowRoute;

        /// <summary>
        /// 关闭句柄指向的当前代窗口实例。
        /// </summary>
        /// <param name="handle">待关闭句柄。</param>
        /// <returns>关闭完成任务。</returns>
        MTask CloseAsync(UIWindowHandle handle);

        /// <summary>
        /// 将句柄对应窗口移动到所在层最前方并恢复输入焦点。
        /// </summary>
        /// <param name="handle">目标窗口句柄。</param>
        /// <returns>句柄仍然有效并成功聚焦时返回 true。</returns>
        bool Focus(UIWindowHandle handle);

        /// <summary>
        /// 打开会返回业务结果的窗口并等待其关闭结果。
        /// </summary>
        /// <typeparam name="TRoute">生成的窗口路由。</typeparam>
        /// <typeparam name="TArgs">强类型打开参数。</typeparam>
        /// <typeparam name="TResult">关闭结果类型。</typeparam>
        /// <param name="args">窗口打开参数。</param>
        /// <returns>窗口提交的关闭结果。</returns>
        MTask<TResult> ShowAsync<TRoute, TArgs, TResult>(TArgs args)
            where TRoute : IUIWindowRoute
            where TArgs : IUIWindowArgs<TRoute>;
    }
}
