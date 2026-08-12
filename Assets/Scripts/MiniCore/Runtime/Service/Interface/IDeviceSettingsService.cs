using System;
using System.Collections.Generic;
using System.Threading;
using MiniCore.Threading;

namespace MiniCore.Service
{

    /// <summary>
    /// 将客户端设置应用到当前 Unity 运行环境的服务契约。
    /// Dedicated Server 可不绑定此服务，或绑定安全的空实现。
    /// </summary>
    public interface IDeviceSettingsService : IAppService
    {
        /// <summary>
        /// 将设置应用到当前运行环境。
        /// </summary>
        /// <param name="settings">要应用的设置。</param>
        void Apply(ClientSettings settings);
    }
}
