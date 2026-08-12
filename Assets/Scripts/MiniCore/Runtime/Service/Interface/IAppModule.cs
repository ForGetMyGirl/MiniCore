using System;
using MiniCore.Model;
using MiniCore.Threading;

namespace MiniCore.Service
{

    /// <summary>
    /// 标记可由业务在运行期间按需启用的公共模块契约。
    /// 模块不会自动进入项目启动配置，调用方应通过接口获取其实例。
    /// </summary>
    public interface IAppModule
    {
    }
}
