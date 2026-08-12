using System;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.UI
{

    /// <summary>
    /// 业务持有的不可变窗口操作句柄，不暴露具体 Unity View。
    /// 使用引用类型可避免复杂值类型跨热更新异步泛型边界时依赖 HybridCLR adjustor thunk。
    /// </summary>
    public sealed class UIWindowHandle : IEquatable<UIWindowHandle>
    {
        #region Public 公共成员

        /// <summary>
        /// 获取窗口实例身份。
        /// </summary>
        public UIWindowInstanceId InstanceId { get; }

        /// <summary>
        /// 判断句柄是否包含有效窗口定义。
        /// </summary>
        public bool IsValid => !InstanceId.WindowId.IsEmpty;

        /// <summary>
        /// 创建窗口句柄。
        /// </summary>
        /// <param name="instanceId">窗口实例身份。</param>
        public UIWindowHandle(UIWindowInstanceId instanceId)
        {
            InstanceId = instanceId;
        }

        /// <summary>
        /// 判断两个句柄是否指向同一代窗口实例。
        /// </summary>
        /// <param name="other">待比较句柄。</param>
        /// <returns>实例身份相同时返回 true。</returns>
        public bool Equals(UIWindowHandle other)
        {
            return !ReferenceEquals(other, null) && InstanceId.Equals(other.InstanceId);
        }

        /// <summary>
        /// 判断目标对象是否为相同句柄。
        /// </summary>
        /// <param name="obj">待比较对象。</param>
        /// <returns>对象为相同句柄时返回 true。</returns>
        public override bool Equals(object obj) => Equals(obj as UIWindowHandle);

        /// <summary>
        /// 获取句柄哈希值。
        /// </summary>
        /// <returns>实例身份哈希值。</returns>
        public override int GetHashCode() => InstanceId.GetHashCode();

        /// <summary>
        /// 判断两个窗口句柄是否指向同一代实例。
        /// </summary>
        /// <param name="left">左侧句柄。</param>
        /// <param name="right">右侧句柄。</param>
        /// <returns>实例身份相同时返回 true。</returns>
        public static bool operator ==(UIWindowHandle left, UIWindowHandle right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            return !ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right);
        }

        /// <summary>
        /// 判断两个窗口句柄是否指向不同实例。
        /// </summary>
        /// <param name="left">左侧句柄。</param>
        /// <param name="right">右侧句柄。</param>
        /// <returns>实例身份不同时返回 true。</returns>
        public static bool operator !=(UIWindowHandle left, UIWindowHandle right) => !(left == right);

        #endregion
    }
}
