using System;
using System.Collections.Generic;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Eventing
{

    /// <summary>
    /// 表示一次事件订阅。
    /// 订阅 token 为值类型，调用方应保存它并在自身生命周期结束时调用 <see cref="Dispose"/>。
    /// </summary>
    public readonly struct EventSubscription : IDisposable
    {
        #region Private 私有成员

        private readonly IEventSubscriptionOwner owner; // 实际持有订阅槽位的频道。
        private readonly int slotId; // 订阅在频道中的槽位编号。
        private readonly uint generation; // 槽位复用保护版本。
        private readonly byte kind; // 同步或异步订阅类别。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建一个绑定频道槽位的订阅 token。
        /// </summary>
        /// <param name="owner">订阅所在频道。</param>
        /// <param name="slotId">订阅槽位编号。</param>
        /// <param name="generation">槽位当前版本。</param>
        /// <param name="kind">订阅类别。</param>
        internal EventSubscription(IEventSubscriptionOwner owner, int slotId, uint generation, byte kind)
        {
            this.owner = owner;
            this.slotId = slotId;
            this.generation = generation;
            this.kind = kind;
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 解除本次订阅。
        /// 重复调用或频道已经销毁时保持安全且无副作用。
        /// </summary>
        public void Dispose()
        {
            owner?.RemoveSubscription(slotId, generation, kind);
        }

        #endregion
    }
}
