using System;
using MiniCore.Model;
using MiniCore.Threading;

namespace MiniCore.Service
{

    /// <summary>
    /// 为需要异步准备资源、配置或平台对象的应用服务提供初始化契约。
    /// 生成的启动代码会在依赖服务完成后调用该方法。
    /// </summary>
    public interface IAsyncAppService
    {
        /// <summary>
        /// 异步初始化服务。
        /// </summary>
        /// <returns>初始化完成任务。</returns>
        MTask InitializeAsync();
    }
}
