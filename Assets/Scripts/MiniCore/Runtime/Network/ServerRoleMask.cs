using System;

namespace MiniCore.Model
{
    /// <summary>
    /// 表示框架只按位判断、但不解释业务含义的 Dedicated Server Role 集合。
    /// </summary>
    public readonly struct ServerRoleMask : IEquatable<ServerRoleMask>
    {
        #region Public 公共成员

        /// <summary>
        /// 框架保留的 Coordinator 位值。
        /// </summary>
        public const ulong CoordinatorValue = 1UL;

        /// <summary>
        /// 获取空 Role 集合。
        /// </summary>
        public static ServerRoleMask None => new ServerRoleMask(0UL);

        /// <summary>
        /// 获取只包含 Coordinator 的 Role 集合。
        /// </summary>
        public static ServerRoleMask Coordinator => new ServerRoleMask(CoordinatorValue);

        /// <summary>
        /// 获取不透明位集合值。
        /// </summary>
        public ulong Value { get; }

        /// <summary>
        /// 获取是否未启用任何 Role。
        /// </summary>
        public bool IsEmpty => Value == 0UL;

        /// <summary>
        /// 创建不透明 Role 集合。
        /// </summary>
        /// <param name="value">由 Role Catalog 校验过的非零位集合。</param>
        public ServerRoleMask(ulong value)
        {
            Value = value;
        }

        /// <summary>
        /// 判断集合是否包含指定单个或组合 Role。
        /// </summary>
        /// <param name="requiredMask">需要同时存在的位。</param>
        /// <returns>所有指定位均存在时返回 true。</returns>
        public bool Contains(ulong requiredMask)
        {
            return requiredMask != 0UL && (Value & requiredMask) == requiredMask;
        }

        /// <summary>
        /// 判断集合是否与指定 Role 存在交集。
        /// </summary>
        /// <param name="candidateMask">候选位集合。</param>
        /// <returns>至少一个位相同时返回 true。</returns>
        public bool Intersects(ulong candidateMask)
        {
            return candidateMask != 0UL && (Value & candidateMask) != 0UL;
        }

        /// <summary>
        /// 比较两个 Role 集合值。
        /// </summary>
        /// <param name="other">另一个集合。</param>
        /// <returns>位值相同时返回 true。</returns>
        public bool Equals(ServerRoleMask other)
        {
            return Value == other.Value;
        }

        /// <summary>
        /// 比较对象是否为相同 Role 集合。
        /// </summary>
        /// <param name="obj">待比较对象。</param>
        /// <returns>类型和值均相同时返回 true。</returns>
        public override bool Equals(object obj)
        {
            return obj is ServerRoleMask other && Equals(other);
        }

        /// <summary>
        /// 获取 Role 位集合的哈希码。
        /// </summary>
        /// <returns>位集合哈希码。</returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <summary>
        /// 返回便于日志查看的十六进制位集合。
        /// </summary>
        /// <returns>十六进制 Role Mask。</returns>
        public override string ToString()
        {
            return "0x" + Value.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
