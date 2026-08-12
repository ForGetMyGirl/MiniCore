using System;
using System.Threading;

namespace MiniCore.Threading
{

    /// <summary>
    /// MTask 内置执行器集合。
    /// </summary>
    public static class MTaskExecutors
    {
        #region Private 私有成员

        private static readonly MInlineExecutor InlineInstance = new MInlineExecutor(); // 无运行时环境时使用的同步执行器。
#if !UNITY_WEBGL || UNITY_EDITOR
        private static readonly IMTaskExecutor ThreadPoolInstance = new MThreadPoolExecutor(); // 复用 CLR 线程池的无亲和性执行器。
#endif
        private static IMTaskExecutor unity; // Unity 主线程执行器。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取同步执行的兜底执行器。
        /// </summary>
        public static IMTaskExecutor Inline => InlineInstance;

        /// <summary>
        /// 获取当前运行环境是否允许创建和使用托管线程。
        /// </summary>
        public static bool SupportsThreads
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return false;
#else
                return true;
#endif
            }
        }

        /// <summary>
        /// 获取复用 CLR 线程池的后台执行器。
        /// </summary>
        /// <remarks>
        /// 此执行器不会创建固定线程，也不保证两次续体运行在同一条工作线程。需要串行线程亲和性时请创建独占执行器。
        /// </remarks>
        public static IMTaskExecutor ThreadPool
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                throw new PlatformNotSupportedException("当前运行环境不支持托管线程池。");
#else
                return ThreadPoolInstance;
#endif
            }
        }

        /// <summary>
        /// 获取或设置 Unity 主线程执行器。
        /// </summary>
        public static IMTaskExecutor Unity
        {
            get => Volatile.Read(ref unity) ?? InlineInstance;
            set => Volatile.Write(ref unity, value ?? throw new ArgumentNullException(nameof(value)));
        }

        /// <summary>
        /// 尝试获取当前环境的 CLR 线程池执行器。
        /// </summary>
        /// <param name="executor">支持线程时返回共享线程池执行器，否则返回空。</param>
        /// <returns>当前环境支持线程池时返回 true。</returns>
        public static bool TryGetThreadPool(out IMTaskExecutor executor)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            executor = null;
            return false;
#else
            executor = ThreadPoolInstance;
            return true;
#endif
        }

        /// <summary>
        /// 创建由调用模块持有租约的单线程顺序执行器。
        /// </summary>
        /// <param name="name">用于线程名称和诊断输出的稳定名称。</param>
        /// <returns>已经启动并登记到 MTask 的执行器；调用方应在所属模块释放时调用 Dispose。</returns>
        /// <exception cref="PlatformNotSupportedException">当前环境不允许创建托管线程。</exception>
        public static MSingleThreadExecutor CreateSingleThread(string name)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new PlatformNotSupportedException("当前运行环境不支持单线程后台执行器。");
#else
            MSingleThreadExecutor executor = new MSingleThreadExecutor(name);
            MTaskExecutorRegistry.Register(executor);
            return executor;
#endif
        }

        /// <summary>
        /// 尝试创建由调用模块持有租约的单线程顺序执行器。
        /// </summary>
        /// <param name="name">用于线程名称和诊断输出的稳定名称。</param>
        /// <param name="executor">支持线程时返回新执行器，否则返回空。</param>
        /// <returns>成功创建执行器时返回 true。</returns>
        public static bool TryCreateSingleThread(string name, out IMTaskOwnedExecutor executor)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            executor = null;
            return false;
#else
            executor = CreateSingleThread(name);
            return true;
#endif
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 尝试取得宿主已经配置的主循环执行器。
        /// </summary>
        /// <param name="executor">已配置时返回主循环执行器，否则返回空。</param>
        /// <returns>宿主已经完成主执行器配置时返回 true。</returns>
        internal static bool TryGetUnityExecutor(out IMTaskExecutor executor)
        {
            executor = Volatile.Read(ref unity);
            return executor != null;
        }

        #endregion
    }
}
