using System;

namespace MiniCore.Core
{
    /// <summary>
    /// 以资源地址、组件类型和业务分组共同标识一个 GameObject 对象池。
    /// </summary>
    internal readonly struct GameObjectPoolKey : IEquatable<GameObjectPoolKey>
    {
        #region Internal 内部成员

        /// <summary>
        /// 获取预制体资源地址。
        /// </summary>
        internal string Address { get; }

        /// <summary>
        /// 获取预制体上需要租用的池对象组件类型。
        /// </summary>
        internal Type ComponentType { get; }

        /// <summary>
        /// 获取业务分组名称。
        /// </summary>
        internal string Group { get; }

        /// <summary>
        /// 创建完整对象池标识。
        /// </summary>
        /// <param name="address">预制体资源地址。</param>
        /// <param name="componentType">池对象组件类型。</param>
        /// <param name="group">业务分组名称。</param>
        internal GameObjectPoolKey(string address, Type componentType, string group)
        {
            Address = string.IsNullOrWhiteSpace(address)
                ? throw new ArgumentException("对象池资源地址不能为空。", nameof(address))
                : address;
            ComponentType = componentType ?? throw new ArgumentNullException(nameof(componentType));
            Group = string.IsNullOrWhiteSpace(group)
                ? throw new ArgumentException("对象池分组不能为空。", nameof(group))
                : group;
        }

        /// <summary>
        /// 比较资源地址、组件类型和分组是否全部一致。
        /// </summary>
        /// <param name="other">另一个对象池标识。</param>
        /// <returns>三个组成部分全部相同时返回 true。</returns>
        public bool Equals(GameObjectPoolKey other)
        {
            return ComponentType == other.ComponentType
                && string.Equals(Address, other.Address, StringComparison.Ordinal)
                && string.Equals(Group, other.Group, StringComparison.Ordinal);
        }

        /// <summary>
        /// 比较任意对象是否为相同对象池标识。
        /// </summary>
        /// <param name="obj">待比较对象。</param>
        /// <returns>对象池标识相同时返回 true。</returns>
        public override bool Equals(object obj)
        {
            return obj is GameObjectPoolKey other && Equals(other);
        }

        /// <summary>
        /// 生成与相等性一致的哈希码。
        /// </summary>
        /// <returns>组合哈希码。</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Address);
                hash = hash * 31 + ComponentType.GetHashCode();
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Group);
                return hash;
            }
        }

        #endregion
    }
}
