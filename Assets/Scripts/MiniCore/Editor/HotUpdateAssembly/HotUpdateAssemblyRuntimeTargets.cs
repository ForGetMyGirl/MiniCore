using System;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 指定热更新程序集会进入客户端、Dedicated Server 或两类运行包。
    /// </summary>
    [Flags]
    public enum HotUpdateAssemblyRuntimeTargets
    {
        None = 0,
        Client = 1 << 0,
        DedicatedServer = 1 << 1,
        All = Client | DedicatedServer
    }
}
