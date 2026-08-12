using System;
using System.Collections.Generic;
using System.Threading;
using MiniCore.Threading;

namespace MiniCore.Service
{

    /// <summary>
    /// 提供客户端偏好设置读取、写入与变更通知的服务契约。
    /// </summary>
    public interface ISettingsService : IAppService
    {
        /// <summary>
        /// 当前设置发生保存或替换后触发。
        /// </summary>
        event Action<ClientSettings> Changed;

        /// <summary>
        /// 获取当前客户端设置快照。
        /// </summary>
        ClientSettings Current { get; }

        /// <summary>
        /// 异步加载设置。
        /// </summary>
        /// <returns>加载完成任务。</returns>
        MTask LoadAsync();

        /// <summary>
        /// 替换设置并持久化。
        /// </summary>
        /// <param name="settings">待保存设置。</param>
        /// <returns>保存完成任务。</returns>
        MTask SaveAsync(ClientSettings settings);
    }
}
