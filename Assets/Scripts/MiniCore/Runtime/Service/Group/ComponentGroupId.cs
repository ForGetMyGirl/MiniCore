using System;
using MiniCore.Core;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// Global 组件分组的稳定身份。
    /// 默认值表示传统全局组，同一组件类型在不同非默认组中可同时存在多个实例。
    /// </summary>
    public readonly struct ComponentGroupId : IEquatable<ComponentGroupId>
    {
        #region Public 公共成员

        /// <summary>
        /// 获取默认全局组身份。
        /// </summary>
        public static ComponentGroupId Default => default;

        /// <summary>
        /// 获取分组业务标识。
        /// </summary>
        public long Value { get; }

        /// <summary>
        /// 判断当前是否为默认全局组。
        /// </summary>
        public bool IsDefault => Value == 0;

        /// <summary>
        /// 使用业务标识创建组件分组身份。
        /// </summary>
        /// <param name="value">非零的业务标识。</param>
        public ComponentGroupId(long value)
        {
            if (value == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "组件分组标识不能为零。");
            }

            Value = value;
        }

        /// <summary>
        /// 判断两个分组身份是否相同。
        /// </summary>
        /// <param name="other">待比较的分组身份。</param>
        /// <returns>业务标识相同时返回 true。</returns>
        public bool Equals(ComponentGroupId other)
        {
            return Value == other.Value;
        }

        /// <summary>
        /// 判断当前对象是否与指定对象表示同一分组。
        /// </summary>
        /// <param name="obj">待比较对象。</param>
        /// <returns>对象为相同分组身份时返回 true。</returns>
        public override bool Equals(object obj)
        {
            return obj is ComponentGroupId other && Equals(other);
        }

        /// <summary>
        /// 获取分组身份哈希值。
        /// </summary>
        /// <returns>业务标识对应的哈希值。</returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <summary>
        /// 输出用于诊断的分组标识文本。
        /// </summary>
        /// <returns>当前业务标识文本。</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        /// <summary>
        /// 判断两个分组身份是否相同。
        /// </summary>
        /// <param name="left">左侧分组身份。</param>
        /// <param name="right">右侧分组身份。</param>
        /// <returns>业务标识相同时返回 true。</returns>
        public static bool operator ==(ComponentGroupId left, ComponentGroupId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 判断两个分组身份是否不同。
        /// </summary>
        /// <param name="left">左侧分组身份。</param>
        /// <param name="right">右侧分组身份。</param>
        /// <returns>业务标识不同时返回 true。</returns>
        public static bool operator !=(ComponentGroupId left, ComponentGroupId right)
        {
            return !left.Equals(right);
        }

        #endregion
    }
}
