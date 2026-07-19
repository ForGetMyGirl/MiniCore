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

    /// <summary>
    /// 一组具有共同业务身份的 Global 组件。
    /// 销毁分组会强制释放组内全部组件，用于房间、副本和对局等明确边界。
    /// </summary>
    public sealed class ComponentGroup : IDisposable, IMTaskOwner
    {
        #region Private 私有成员

        private bool disposed; // 当前分组是否已经销毁。
        private MTaskDomain mTaskDomain; // 首次绑定异步入口时惰性创建的任务域。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取用于诊断的分组名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 获取当前分组的稳定业务身份。
        /// </summary>
        public ComponentGroupId Id { get; }

        /// <summary>
        /// 创建组件分组。
        /// </summary>
        /// <param name="name">用于日志和诊断的名称。</param>
        /// <param name="id">非默认分组业务身份。</param>
        internal ComponentGroup(string name, ComponentGroupId id)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "UnnamedGroup" : name;
            Id = id;
        }

        /// <summary>
        /// 获取当前分组中已存在的组件，并由分组持有一份引用。
        /// </summary>
        /// <typeparam name="T">组件具体类型。</typeparam>
        /// <returns>当前分组内的组件实例。</returns>
        public T Get<T>() where T : AComponent, new()
        {
            ThrowIfDisposed();
            return Global.Get<T>(this, Id);
        }

        /// <summary>
        /// 获取或创建当前分组中的无参组件，并由分组持有一份引用。
        /// </summary>
        /// <typeparam name="T">组件具体类型。</typeparam>
        /// <returns>当前分组内的组件实例。</returns>
        public T GetOrAdd<T>() where T : AComponent, new()
        {
            ThrowIfDisposed();
            return Global.GetOrAdd<T>(this, Id);
        }

        /// <summary>
        /// 获取或创建当前分组中的带参数组件，并由分组持有一份引用。
        /// </summary>
        /// <typeparam name="T">组件具体类型。</typeparam>
        /// <param name="args">仅首次创建时使用的初始化参数。</param>
        /// <returns>当前分组内的组件实例。</returns>
        public T GetOrAdd<T>(ComponentInitArgs args) where T : AComponent, new()
        {
            ThrowIfDisposed();
            return Global.GetOrAdd<T>(this, Id, args);
        }

        /// <summary>
        /// 强制销毁当前分组中的所有组件。
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            mTaskDomain?.Dispose();
            Global.DestroyGroup(Id, Name);
        }

        /// <summary>
        /// 获取当前组件分组惰性创建的 MTask 生命周期域。
        /// </summary>
        /// <returns>与当前分组一同释放的任务域。</returns>
        public MTaskDomain GetMTaskDomain()
        {
            ThrowIfDisposed();
            return mTaskDomain ??= new MTaskDomain($"ComponentGroup:{Name}:{Id}", MTaskRuntime.CurrentExecutor ?? MTaskExecutors.Unity);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 阻止销毁后的分组继续创建或获取组件。
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(Name);
            }
        }

        #endregion
    }
}
