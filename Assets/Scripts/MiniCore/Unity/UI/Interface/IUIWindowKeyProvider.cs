using System;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.UI
{

    /// <summary>
    /// 为 SingletonPerKey 窗口提供稳定业务实例键。
    /// </summary>
    public interface IUIWindowKeyProvider
    {
        /// <summary>
        /// 获取当前打开参数对应的业务实例键。
        /// </summary>
        UIWindowInstanceKey InstanceKey { get; }
    }
}
