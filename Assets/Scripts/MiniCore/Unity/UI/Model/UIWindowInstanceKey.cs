using System;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.UI
{

    /// <summary>
    /// 表示 SingletonPerKey 或 Multiple 窗口的业务实例键。
    /// </summary>
    [Serializable]
    public struct UIWindowInstanceKey : IEquatable<UIWindowInstanceKey>
    {
        #region Private 私有成员

        private long numericValue; // 数字业务键。
        private string textValue; // 文本业务键。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取空实例键。
        /// </summary>
        public static UIWindowInstanceKey Empty => default;

        /// <summary>
        /// 判断当前键是否为空。
        /// </summary>
        public bool IsEmpty => numericValue == 0L && string.IsNullOrEmpty(textValue);

        /// <summary>
        /// 使用数字创建实例键。
        /// </summary>
        /// <param name="value">非零业务数字。</param>
        public UIWindowInstanceKey(long value)
        {
            numericValue = value;
            textValue = null;
        }

        /// <summary>
        /// 使用文本创建实例键。
        /// </summary>
        /// <param name="value">非空稳定业务文本。</param>
        public UIWindowInstanceKey(string value)
        {
            numericValue = 0L;
            textValue = value;
        }

        /// <summary>
        /// 判断两个实例键是否相同。
        /// </summary>
        /// <param name="other">待比较实例键。</param>
        /// <returns>数字和文本均相同时返回 true。</returns>
        public bool Equals(UIWindowInstanceKey other)
        {
            return numericValue == other.numericValue && string.Equals(textValue, other.textValue, StringComparison.Ordinal);
        }

        /// <summary>
        /// 判断目标对象是否表示同一实例键。
        /// </summary>
        /// <param name="obj">待比较对象。</param>
        /// <returns>对象表示相同实例键时返回 true。</returns>
        public override bool Equals(object obj)
        {
            return obj is UIWindowInstanceKey other && Equals(other);
        }

        /// <summary>
        /// 获取实例键哈希值。
        /// </summary>
        /// <returns>稳定哈希值。</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (numericValue.GetHashCode() * 397) ^ (textValue != null ? StringComparer.Ordinal.GetHashCode(textValue) : 0);
            }
        }

        /// <summary>
        /// 输出实例键诊断文本。
        /// </summary>
        /// <returns>数字或文本内容。</returns>
        public override string ToString()
        {
            return textValue ?? numericValue.ToString();
        }

        /// <summary>
        /// 判断两个实例键是否相同。
        /// </summary>
        public static bool operator ==(UIWindowInstanceKey left, UIWindowInstanceKey right) => left.Equals(right);

        /// <summary>
        /// 判断两个实例键是否不同。
        /// </summary>
        public static bool operator !=(UIWindowInstanceKey left, UIWindowInstanceKey right) => !left.Equals(right);

        #endregion
    }
}
