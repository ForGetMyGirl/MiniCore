using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MiniCore.Threading
{

    /// <summary>
    /// 为只标记 MTaskOwnerAttribute 的对象提供弱关联任务域。
    /// </summary>
    internal sealed class MTaskExternalOwner : IMTaskOwner, IDisposable
    {
        #region Private 私有成员

        private readonly MTaskDomain domain; // 与外部对象生命周期绑定的任务域。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 使用对象类型名称和当前执行器创建外部 Owner。
        /// </summary>
        /// <param name="name">Owner 诊断名称。</param>
        /// <param name="executor">Owner 默认执行器。</param>
        internal MTaskExternalOwner(string name, IMTaskExecutor executor)
        {
            domain = new MTaskDomain(name, executor);
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 获取外部 Owner 的任务生命周期域。
        /// </summary>
        /// <returns>外部 Owner 任务域。</returns>
        public MTaskDomain GetMTaskDomain()
        {
            return domain;
        }

        /// <summary>
        /// 取消外部 Owner 名下全部任务。
        /// </summary>
        public void Dispose()
        {
            domain.Dispose();
        }

        #endregion
    }
}
