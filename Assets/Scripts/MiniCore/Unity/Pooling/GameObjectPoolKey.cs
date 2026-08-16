using System;

namespace MiniCore.Pooling
{
    /// <summary>
    /// 以资源地址、组件类型和业务分组共同标识一个 GameObject 对象池。
    /// </summary>
    internal readonly struct GameObjectPoolKey : IEquatable<GameObjectPoolKey>
    {
        #region Internal 内部成员

        /// <summary>
        /// 获取规范化后的预制体资源地址。
        /// </summary>
        internal string Address { get; }

        /// <summary>
        /// 获取预制体上需要租用的组件类型。
        /// </summary>
        internal Type ComponentType { get; }

        /// <summary>
        /// 获取按序号比较的业务分组。
        /// </summary>
        internal string Group { get; }

        /// <summary>
        /// 创建完整对象池标识。
        /// </summary>
        /// <param name="address">预制体资源地址。</param>
        /// <param name="componentType">池对象组件类型。</param>
        /// <param name="group">业务分组。</param>
        internal GameObjectPoolKey(string address, Type componentType, string group)
        {
            Address = Normalize(address, nameof(address));
            ComponentType = componentType ?? throw new ArgumentNullException(nameof(componentType));
            Group = Normalize(group, nameof(group));
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
        /// 生成与序号相等性一致的组合哈希码。
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

        #region Private 私有成员

        /// <summary>
        /// 校验并规范化对象池标识文本。
        /// </summary>
        /// <param name="value">待校验文本。</param>
        /// <param name="parameterName">参数名称。</param>
        /// <returns>去除首尾空白后的非空文本。</returns>
        private static string Normalize(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("对象池地址和分组不能为空。", parameterName);
            }

            return value.Trim();
        }

        #endregion
    }
}
