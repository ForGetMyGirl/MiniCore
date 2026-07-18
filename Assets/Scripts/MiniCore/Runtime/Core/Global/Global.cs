using System;
using MiniCore.Model;

namespace MiniCore.Core
{
    /// <summary>
    /// 全局组件静态门面，业务代码通过此类型直接获取、持有和释放组件。
    /// </summary>
    public static class Global
    {
        #region Private 私有成员

        private static GlobalRuntime runtime; // 当前进程唯一的组件运行时。
        private static ITimeProvider timeProvider; // 当前运行时使用的时间来源。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取当前运行时使用的时间来源。
        /// </summary>
        public static ITimeProvider Time => timeProvider ?? SystemTimeProvider.Shared;

        /// <summary>
        /// 初始化全局组件运行时；重复调用保持已有实例不变。
        /// </summary>
        /// <param name="provider">可选的时间来源。</param>
        public static void Initialize(ITimeProvider provider = null)
        {
            if (runtime != null)
            {
                return;
            }

            timeProvider = provider ?? SystemTimeProvider.Shared;
            runtime = new GlobalRuntime();
        }

        /// <summary>
        /// 获取已存在组件并增加 owner 引用。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="owner">本次持有组件的 owner。</param>
        /// <returns>已激活组件。</returns>
        public static T Get<T>(object owner) where T : AComponent, new()
        {
            return EnsureRuntime().Get<T>(owner);
        }

        /// <summary>
        /// 获取或创建无参组件并增加 owner 引用。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="owner">本次持有组件的 owner。</param>
        /// <returns>已激活组件。</returns>
        public static T GetOrAdd<T>(object owner) where T : AComponent, new()
        {
            return EnsureRuntime().GetOrAdd<T>(owner);
        }

        /// <summary>
        /// 获取或创建带参数组件并增加 owner 引用。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="owner">本次持有组件的 owner。</param>
        /// <param name="args">首次创建参数。</param>
        /// <returns>已激活组件。</returns>
        public static T GetOrAdd<T>(object owner, ComponentInitArgs args) where T : AComponent, new()
        {
            return EnsureRuntime().GetOrAdd<T>(owner, args);
        }

        /// <summary>
        /// 获取或创建常驻组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <returns>常驻组件。</returns>
        public static T Pin<T>() where T : AComponent, new()
        {
            return EnsureRuntime().Pin<T>();
        }

        /// <summary>
        /// 获取或创建带参数的常驻组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="args">首次创建参数。</param>
        /// <returns>常驻组件。</returns>
        public static T Pin<T>(ComponentInitArgs args) where T : AComponent, new()
        {
            return EnsureRuntime().Pin<T>(args);
        }

        /// <summary>
        /// 解除组件的根 owner 常驻引用。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        public static void Unpin<T>() where T : AComponent, new()
        {
            EnsureRuntime().Unpin<T>();
        }

        /// <summary>
        /// 释放 owner 对指定组件的一份引用。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="owner">持有组件的 owner。</param>
        public static void Remove<T>(object owner) where T : AComponent, new()
        {
            EnsureRuntime().Remove<T>(owner);
        }

        /// <summary>
        /// 释放 owner 持有的全部组件。
        /// </summary>
        /// <param name="owner">持有组件的 owner。</param>
        public static void ReleaseAll(object owner)
        {
            EnsureRuntime().ReleaseAll(owner);
        }

        /// <summary>
        /// 忽略引用计数并立即移除组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        public static void ForceRemove<T>() where T : AComponent, new()
        {
            EnsureRuntime().ForceRemove<T>();
        }

        /// <summary>
        /// 创建一个用于批量释放组件引用的作用域。
        /// </summary>
        /// <param name="name">诊断用作用域名称。</param>
        /// <returns>新建作用域。</returns>
        public static GlobalScope CreateScope(string name)
        {
            EnsureRuntime();
            return new GlobalScope(name);
        }

        /// <summary>
        /// 调度所有激活的全局组件。
        /// </summary>
        public static void Tick()
        {
            runtime?.Tick();
        }

        /// <summary>
        /// 强制关闭全局组件运行时。
        /// </summary>
        public static void Shutdown()
        {
            if (runtime == null)
            {
                return;
            }

            runtime.Dispose();
            runtime = null;
            timeProvider = null;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 获取已初始化的运行时，首次访问时使用系统时间自动初始化。
        /// </summary>
        /// <returns>可用的组件运行时。</returns>
        private static GlobalRuntime EnsureRuntime()
        {
            if (runtime == null)
            {
                Initialize();
            }

            return runtime;
        }

        #endregion
    }
}
