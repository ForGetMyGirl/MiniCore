using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{
    /// <summary>
    /// MTask async 状态机 Runner 的非泛型接口。
    /// </summary>
    internal interface IMTaskStateMachineRunner
    {
        /// <summary>
        /// 获取 awaiter 应注册的复用续体。
        /// </summary>
        Action Continuation { get; }

        /// <summary>
        /// 清理状态机并归还对应类型的对象池。
        /// </summary>
        void Return();
    }
}
