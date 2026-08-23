using System;

namespace MiniCore.Model
{
    /// <summary>
    /// 表示 Coordinator 目录中的稳定服务标识，框架不解释其业务语义。
    /// </summary>
    public readonly struct ServiceId : IEquatable<ServiceId>
    {
        #region Public 公共成员

        /// <summary>
        /// 获取无效空标识。
        /// </summary>
        public static ServiceId Unspecified => new ServiceId(0UL);

        /// <summary>
        /// 获取稳定无符号值。
        /// </summary>
        public ulong Value { get; }

        /// <summary>
        /// 创建服务标识。
        /// </summary>
        /// <param name="value">非零且稳定的服务值。</param>
        public ServiceId(ulong value)
        {
            Value = value;
        }

        /// <summary>
        /// 比较两个服务标识。
        /// </summary>
        /// <param name="other">另一个标识。</param>
        /// <returns>值相同时返回 true。</returns>
        public bool Equals(ServiceId other)
        {
            return Value == other.Value;
        }

        /// <summary>
        /// 比较对象是否为相同服务标识。
        /// </summary>
        /// <param name="obj">待比较对象。</param>
        /// <returns>类型和值均相同时返回 true。</returns>
        public override bool Equals(object obj)
        {
            return obj is ServiceId other && Equals(other);
        }

        /// <summary>
        /// 获取稳定标识哈希码。
        /// </summary>
        /// <returns>哈希码。</returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <summary>
        /// 返回便于日志查看的十六进制值。
        /// </summary>
        /// <returns>服务标识文本。</returns>
        public override string ToString()
        {
            return "0x" + Value.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
