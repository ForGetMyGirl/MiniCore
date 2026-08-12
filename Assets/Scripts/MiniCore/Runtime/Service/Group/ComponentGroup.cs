using System;
using MiniCore.Core;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Model
{

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
