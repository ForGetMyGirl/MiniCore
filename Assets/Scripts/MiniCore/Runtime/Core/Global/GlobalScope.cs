using System;
using MiniCore.Model;
using MiniCore.Threading;

namespace MiniCore.Core
{
    /// <summary>
    /// 一组全局组件引用的 owner，释放时自动归还该作用域获取的全部组件。
    /// </summary>
    public sealed class GlobalScope : IDisposable, IMTaskOwner
    {
        #region Private 私有成员

        private bool disposed; // 是否已经完成作用域释放。
        private MTaskDomain mTaskDomain; // 首次绑定异步入口时惰性创建的任务域。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取作用域的诊断名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 使用诊断名称创建作用域。
        /// </summary>
        /// <param name="name">作用域名称。</param>
        internal GlobalScope(string name)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Unnamed" : name;
        }

        /// <summary>
        /// 获取已存在组件并由当前作用域持有。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <returns>已激活组件。</returns>
        public T Get<T>() where T : AComponent, new()
        {
            ThrowIfDisposed();
            return Global.Get<T>(this);
        }

        /// <summary>
        /// 获取或创建组件并由当前作用域持有。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <returns>已激活组件。</returns>
        public T GetOrAdd<T>() where T : AComponent, new()
        {
            ThrowIfDisposed();
            return Global.GetOrAdd<T>(this);
        }

        /// <summary>
        /// 获取或创建带参数组件并由当前作用域持有。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="args">首次创建参数。</param>
        /// <returns>已激活组件。</returns>
        public T GetOrAdd<T>(ComponentInitArgs args) where T : AComponent, new()
        {
            ThrowIfDisposed();
            return Global.GetOrAdd<T>(this, args);
        }

        /// <summary>
        /// 释放作用域持有的全部组件引用。
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            mTaskDomain?.Dispose();
            Global.ReleaseAll(this);
        }

        /// <summary>
        /// 获取当前 GlobalScope 惰性创建的 MTask 生命周期域。
        /// </summary>
        /// <returns>与当前 Scope 一同释放的任务域。</returns>
        public MTaskDomain GetMTaskDomain()
        {
            ThrowIfDisposed();
            return mTaskDomain ??= new MTaskDomain($"GlobalScope:{Name}", MTaskRuntime.CurrentExecutor ?? MTaskExecutors.Unity);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 阻止已释放作用域继续获取组件。
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
