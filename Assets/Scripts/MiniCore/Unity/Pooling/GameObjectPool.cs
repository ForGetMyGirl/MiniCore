using System;
using System.Collections.Generic;
using MiniCore.Service;
using MiniCore.Threading;
using UnityEngine;

namespace MiniCore.Pooling
{
    /// <summary>
    /// 管理一个固定资源地址、组件类型和业务分组的 GameObject 实例集合。
    /// </summary>
    internal sealed class GameObjectPool : IDisposable
    {
        #region Private 私有成员

        private readonly GameObjectPoolKey key; // 当前池的完整复合标识。
        private readonly Transform retainedRoot; // 归还对象统一停放的父节点。
        private readonly IResourceService resourceService; // 负责创建和释放实例的资源服务。
        private readonly Stack<IPoolObject> retained; // 当前可直接租用的对象。
        private readonly HashSet<IPoolObject> rented = new HashSet<IPoolObject>(); // 当前已经租出的对象。
        private int instanceSeed; // 当前池实例名称序号。
        private bool disposed; // 当前池是否已经释放。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 获取当前池允许保留的最大实例数量。
        /// </summary>
        internal int MaximumRetained { get; }

        /// <summary>
        /// 创建一个复合标识唯一的 GameObject 对象池。
        /// </summary>
        /// <param name="key">资源地址、组件类型和业务分组。</param>
        /// <param name="retainedRoot">归还对象的父节点。</param>
        /// <param name="resourceService">资源实例生命周期服务。</param>
        /// <param name="maximumRetained">最大缓存实例数量。</param>
        internal GameObjectPool(
            GameObjectPoolKey key,
            Transform retainedRoot,
            IResourceService resourceService,
            int maximumRetained)
        {
            if (maximumRetained < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumRetained));
            }

            this.key = key;
            this.retainedRoot = retainedRoot ?? throw new ArgumentNullException(nameof(retainedRoot));
            this.resourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
            MaximumRetained = maximumRetained;
            retained = new Stack<IPoolObject>(Math.Min(maximumRetained, 16));
        }

        /// <summary>
        /// 租用缓存实例；缓存为空时通过资源服务异步实例化。
        /// </summary>
        /// <typeparam name="T">预制体上实现池契约的组件类型。</typeparam>
        /// <param name="parent">租用期间使用的父节点。</param>
        /// <returns>已执行初始化的池对象。</returns>
        internal async MTask<T> RentAsync<T>(Transform parent) where T : MonoBehaviour, IPoolObject
        {
            ThrowIfDisposed();
            IPoolObject value = retained.Count > 0 ? retained.Pop() : await CreateAsync(retainedRoot);
            if (!(value is T typedValue))
            {
                ReleaseInstance(value);
                throw new InvalidOperationException($"对象池 {key.Address} 的组件不是 {typeof(T).FullName}。");
            }

            try
            {
                typedValue.transform.SetParent(parent, false);
                typedValue.gameObject.SetActive(true);
                typedValue.Init();
                rented.Add(value);
                return typedValue;
            }
            catch (Exception rentException)
            {
                Exception cleanupException = ReleaseFailedRent(value);
                if (cleanupException != null)
                {
                    throw new AggregateException("池对象初始化失败，且清理实例时也发生异常。", rentException, cleanupException);
                }

                throw;
            }
        }

        /// <summary>
        /// 归还一个由当前池租出的对象。
        /// </summary>
        /// <param name="value">调用方不再使用的池对象。</param>
        /// <returns>成功识别并回收当前池对象时返回 true。</returns>
        internal bool Return(IPoolObject value)
        {
            ThrowIfDisposed();
            if (value == null || !rented.Contains(value))
            {
                return false;
            }

            value.Clear();
            if (value is MonoBehaviour behaviour)
            {
                behaviour.transform.SetParent(retainedRoot, false);
                behaviour.gameObject.SetActive(false);
            }

            rented.Remove(value);
            if (retained.Count >= MaximumRetained)
            {
                ReleaseInstance(value);
            }
            else
            {
                retained.Push(value);
            }

            return true;
        }

        /// <summary>
        /// 异步预热到指定缓存数量。
        /// </summary>
        /// <param name="count">希望保留的实例数量。</param>
        /// <returns>全部预热对象完成创建并停放后的任务。</returns>
        internal async MTask PrewarmAsync(int count)
        {
            ThrowIfDisposed();
            if (count < 0 || count > MaximumRetained)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            while (retained.Count < count)
            {
                IPoolObject value = await CreateAsync(retainedRoot);
                value.Clear();
                if (value is MonoBehaviour behaviour)
                {
                    behaviour.gameObject.SetActive(false);
                }

                retained.Push(value);
            }
        }

        /// <summary>
        /// 销毁当前缓存对象，已经租出的对象保持有效并可继续归还。
        /// </summary>
        internal void ClearRetained()
        {
            ThrowIfDisposed();
            while (retained.Count > 0)
            {
                ReleaseInstance(retained.Pop());
            }
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 销毁缓存和租出的全部实例，并禁止继续使用当前池。
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            while (retained.Count > 0)
            {
                ReleaseInstance(retained.Pop());
            }

            foreach (IPoolObject value in rented)
            {
                ReleaseInstance(value);
            }

            rented.Clear();
            disposed = true;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 通过资源服务创建并校验预制体池组件。
        /// </summary>
        /// <param name="parent">创建时使用的父节点。</param>
        /// <returns>预制体上的目标池组件。</returns>
        private async MTask<IPoolObject> CreateAsync(Transform parent)
        {
            GameObject instance = await resourceService.InstantiateAsync(key.Address, parent);
            if (instance == null)
            {
                throw new InvalidOperationException($"资源服务未能实例化对象池地址：{key.Address}");
            }

            instance.name = $"{key.ComponentType.Name}_{key.Group}_{++instanceSeed}";
            IPoolObject value = instance.GetComponent(key.ComponentType) as IPoolObject;
            if (value != null)
            {
                return value;
            }

            resourceService.ReleaseInstance(instance);
            throw new InvalidOperationException(
                $"预制体 {key.Address} 未挂载实现 IPoolObject 的组件 {key.ComponentType.FullName}。");
        }

        /// <summary>
        /// 清理并释放未能完成初始化的租用实例。
        /// </summary>
        /// <param name="value">尚未成功交给调用方的池对象。</param>
        /// <returns>清理或释放阶段发生的异常；全部成功时返回空。</returns>
        private Exception ReleaseFailedRent(IPoolObject value)
        {
            Exception result = null;
            try
            {
                value.Clear();
            }
            catch (Exception exception)
            {
                result = exception;
            }

            try
            {
                ReleaseInstance(value);
            }
            catch (Exception exception)
            {
                result = result == null ? exception : new AggregateException(result, exception);
            }

            return result;
        }

        /// <summary>
        /// 通过资源服务释放池对象对应的 GameObject 与资源引用。
        /// </summary>
        /// <param name="value">需要释放的池对象。</param>
        private void ReleaseInstance(IPoolObject value)
        {
            if (value is MonoBehaviour behaviour && behaviour != null)
            {
                resourceService.ReleaseInstance(behaviour.gameObject);
            }
        }

        /// <summary>
        /// 已释放后禁止继续使用对象池。
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(GameObjectPool));
            }
        }

        #endregion
    }
}
