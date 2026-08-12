using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{

    /// <summary>
    /// 标记需要由编译后处理器注入 MTask 生命周期的类型。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class MTaskOwnerAttribute : Attribute
    {
    }
}
