using System;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.UI
{

    /// <summary>
    /// 唯一标识某个活动或缓存窗口实例。
    /// </summary>
    [Serializable]
    public struct UIWindowInstanceId : IEquatable<UIWindowInstanceId>
    {
        #region Public 公共成员

        /// <summary>
        /// 获取窗口定义身份。
        /// </summary>
        public UIWindowId WindowId { get; }

        /// <summary>
        /// 获取业务实例键。
        /// </summary>
        public UIWindowInstanceKey InstanceKey { get; }

        /// <summary>
        /// 获取本次实例代次。
        /// </summary>
        public uint Generation { get; }

        /// <summary>
        /// 创建窗口实例身份。
        /// </summary>
        /// <param name="windowId">窗口定义身份。</param>
        /// <param name="instanceKey">业务实例键。</param>
        /// <param name="generation">实例代次。</param>
        public UIWindowInstanceId(UIWindowId windowId, UIWindowInstanceKey instanceKey, uint generation)
        {
            WindowId = windowId;
            InstanceKey = instanceKey;
            Generation = generation;
        }

        /// <summary>
        /// 判断两个窗口实例身份是否相同。
        /// </summary>
        /// <param name="other">待比较实例身份。</param>
        /// <returns>定义、业务键和代次均相同时返回 true。</returns>
        public bool Equals(UIWindowInstanceId other)
        {
            return WindowId.Equals(other.WindowId) && InstanceKey.Equals(other.InstanceKey) && Generation == other.Generation;
        }

        /// <summary>
        /// 判断目标对象是否表示同一窗口实例。
        /// </summary>
        /// <param name="obj">待比较对象。</param>
        /// <returns>对象表示同一实例时返回 true。</returns>
        public override bool Equals(object obj)
        {
            return obj is UIWindowInstanceId other && Equals(other);
        }

        /// <summary>
        /// 获取实例身份哈希值。
        /// </summary>
        /// <returns>组合哈希值。</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = WindowId.GetHashCode();
                hash = (hash * 397) ^ InstanceKey.GetHashCode();
                return (hash * 397) ^ (int)Generation;
            }
        }

        /// <summary>
        /// 判断两个实例身份是否相同。
        /// </summary>
        public static bool operator ==(UIWindowInstanceId left, UIWindowInstanceId right) => left.Equals(right);

        /// <summary>
        /// 判断两个实例身份是否不同。
        /// </summary>
        public static bool operator !=(UIWindowInstanceId left, UIWindowInstanceId right) => !left.Equals(right);

        #endregion
    }
}
