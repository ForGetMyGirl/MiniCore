using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// MTask 生命周期持有者，由组件、模块和 Unity 对象实现。
    /// </summary>
    public interface IMTaskOwner
    {
        /// <summary>
        /// 获取当前对象的任务生命周期域。
        /// </summary>
        /// <returns>当前对象使用的任务域。</returns>
        MTaskDomain GetMTaskDomain();
    }
}
