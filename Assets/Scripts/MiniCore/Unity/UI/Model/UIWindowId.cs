using System;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.UI
{

    /// <summary>
    /// 稳定标识一个窗口定义的 128 位身份。
    /// </summary>
    [Serializable]
    public struct UIWindowId : IEquatable<UIWindowId>
    {
        #region Private 私有成员

        private ulong high; // 身份高 64 位。
        private ulong low; // 身份低 64 位。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取身份高 64 位。
        /// </summary>
        public ulong High => high;

        /// <summary>
        /// 获取身份低 64 位。
        /// </summary>
        public ulong Low => low;

        /// <summary>
        /// 判断当前身份是否尚未初始化。
        /// </summary>
        public bool IsEmpty => high == 0UL && low == 0UL;

        /// <summary>
        /// 使用高低 64 位创建窗口身份。
        /// </summary>
        /// <param name="highValue">身份高 64 位。</param>
        /// <param name="lowValue">身份低 64 位。</param>
        public UIWindowId(ulong highValue, ulong lowValue)
        {
            high = highValue;
            low = lowValue;
        }

        /// <summary>
        /// 将 Guid 转换为窗口身份。
        /// </summary>
        /// <param name="value">稳定 Guid。</param>
        /// <returns>对应的 128 位窗口身份。</returns>
        public static UIWindowId FromGuid(Guid value)
        {
            byte[] bytes = value.ToByteArray();
            return new UIWindowId(BitConverter.ToUInt64(bytes, 0), BitConverter.ToUInt64(bytes, 8));
        }

        /// <summary>
        /// 将窗口身份还原为 Guid。
        /// </summary>
        /// <returns>对应 Guid。</returns>
        public Guid ToGuid()
        {
            byte[] bytes = new byte[16];
            Array.Copy(BitConverter.GetBytes(high), 0, bytes, 0, 8);
            Array.Copy(BitConverter.GetBytes(low), 0, bytes, 8, 8);
            return new Guid(bytes);
        }

        /// <summary>
        /// 判断两个窗口身份是否相同。
        /// </summary>
        /// <param name="other">待比较身份。</param>
        /// <returns>高低位均相同时返回 true。</returns>
        public bool Equals(UIWindowId other)
        {
            return high == other.high && low == other.low;
        }

        /// <summary>
        /// 判断目标对象是否为同一窗口身份。
        /// </summary>
        /// <param name="obj">待比较对象。</param>
        /// <returns>对象表示相同身份时返回 true。</returns>
        public override bool Equals(object obj)
        {
            return obj is UIWindowId other && Equals(other);
        }

        /// <summary>
        /// 获取窗口身份哈希值。
        /// </summary>
        /// <returns>组合后的哈希值。</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (high.GetHashCode() * 397) ^ low.GetHashCode();
            }
        }

        /// <summary>
        /// 输出用于日志和诊断的稳定身份文本。
        /// </summary>
        /// <returns>Guid 格式文本。</returns>
        public override string ToString()
        {
            return ToGuid().ToString("N");
        }

        /// <summary>
        /// 判断两个窗口身份是否相同。
        /// </summary>
        public static bool operator ==(UIWindowId left, UIWindowId right) => left.Equals(right);

        /// <summary>
        /// 判断两个窗口身份是否不同。
        /// </summary>
        public static bool operator !=(UIWindowId left, UIWindowId right) => !left.Equals(right);

        #endregion
    }
}
