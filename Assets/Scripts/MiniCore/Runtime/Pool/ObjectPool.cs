using System;
using System.Collections.Generic;

namespace MiniCore.Pooling
{
    /// <summary>
    /// 可配置创建、租用、归还、预热、清空和最大保留量的通用对象池。
    /// 实例不提供内部锁；跨线程使用时应由所属模块在外部串行化访问。
    /// </summary>
    /// <typeparam name="T">被复用的对象类型。</typeparam>
    public sealed class ObjectPool<T> : IDisposable where T : class
    {
        #region Private 私有成员

        private readonly Stack<T> retained; // 当前保留的可租用对象。
        private readonly Func<T> factory; // 缓存不足时的创建工厂。
        private readonly Action<T> onRent; // 每次租用后的可选初始化回调。
        private readonly Action<T> onReturn; // 每次接受归还前的可选清理回调。
        private readonly Action<T> onDestroy; // 超限或清空时的可选销毁回调。
        private bool disposed; // 对象池是否已经释放。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取允许保留的最大对象数量。
        /// </summary>
        public int MaximumRetained { get; }

        /// <summary>
        /// 获取当前可直接租用的保留对象数量。
        /// </summary>
        public int RetainedCount => retained.Count;

        /// <summary>
        /// 创建通用对象池。
        /// </summary>
        /// <param name="factory">缓存不足时创建新对象的工厂。</param>
        /// <param name="maximumRetained">允许保留的最大对象数量。</param>
        /// <param name="onRent">对象交给调用方前的初始化回调。</param>
        /// <param name="onReturn">对象进入缓存前的清理回调。</param>
        /// <param name="onDestroy">对象超限或清空时的销毁回调。</param>
        public ObjectPool(
            Func<T> factory,
            int maximumRetained = 64,
            Action<T> onRent = null,
            Action<T> onReturn = null,
            Action<T> onDestroy = null)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            if (maximumRetained < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumRetained));
            }

            MaximumRetained = maximumRetained;
            this.onRent = onRent;
            this.onReturn = onReturn;
            this.onDestroy = onDestroy;
            retained = new Stack<T>(Math.Min(maximumRetained, 16));
        }

        /// <summary>
        /// 从缓存取得对象；缓存为空时通过工厂创建。
        /// </summary>
        /// <returns>已经执行租用初始化的对象。</returns>
        public T Rent()
        {
            ThrowIfDisposed();
            T value = retained.Count > 0 ? retained.Pop() : factory();
            if (value == null)
            {
                throw new InvalidOperationException("对象池工厂返回了空对象。");
            }

            try
            {
                onRent?.Invoke(value);
                return value;
            }
            catch (Exception rentException)
            {
                try
                {
                    RecoverFailedRent(value);
                }
                catch (Exception recoveryException)
                {
                    throw new AggregateException(
                        "对象租用初始化失败，且对象池无法安全回收该实例。",
                        rentException,
                        recoveryException);
                }

                throw;
            }
        }

        /// <summary>
        /// 归还对象；超过最大保留量时直接调用销毁回调。
        /// </summary>
        /// <param name="value">调用方不再持有的对象。</param>
        /// <returns>对象进入缓存时返回 true，因超限被销毁时返回 false。</returns>
        public bool Return(T value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            ThrowIfDisposed();
            onReturn?.Invoke(value);
            if (retained.Count >= MaximumRetained)
            {
                onDestroy?.Invoke(value);
                return false;
            }

            retained.Push(value);
            return true;
        }

        /// <summary>
        /// 预先创建对象直到缓存达到目标数量或最大保留量。
        /// </summary>
        /// <param name="count">希望预热到的缓存数量。</param>
        public void Prewarm(int count)
        {
            ThrowIfDisposed();
            if (count < 0 || count > MaximumRetained)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            while (retained.Count < count)
            {
                T value = factory();
                if (value == null)
                {
                    throw new InvalidOperationException("对象池工厂返回了空对象。");
                }

                onReturn?.Invoke(value);
                retained.Push(value);
            }
        }

        /// <summary>
        /// 销毁当前保留的全部对象，但允许对象池继续使用。
        /// </summary>
        public void Clear()
        {
            while (retained.Count > 0)
            {
                onDestroy?.Invoke(retained.Pop());
            }
        }

        /// <summary>
        /// 清空缓存并禁止后续租用和归还。
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            Clear();
            disposed = true;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 在租用初始化失败后执行归还清理；清理失败时销毁对象并向调用方报告异常。
        /// </summary>
        /// <param name="value">尚未交给调用方的失败租用对象。</param>
        private void RecoverFailedRent(T value)
        {
            try
            {
                onReturn?.Invoke(value);
            }
            catch
            {
                onDestroy?.Invoke(value);
                throw;
            }

            if (retained.Count < MaximumRetained)
            {
                retained.Push(value);
                return;
            }

            onDestroy?.Invoke(value);
        }

        /// <summary>
        /// 已释放后禁止继续操作对象池。
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ObjectPool<T>));
            }
        }

        #endregion
    }
}
