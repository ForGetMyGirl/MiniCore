using System;
using System.Collections.Generic;
using MiniCore.Core;
using MiniCore.Service;
using MiniCore.Threading;
using UnityEngine;

namespace MiniCore.Pooling
{
    /// <summary>
    /// 通过 AppModule 生命周期按需管理全部 GameObject 复合对象池。
    /// </summary>
    [AppModule(typeof(IGameObjectPool), Description = "按资源地址、组件类型和业务分组复用 GameObject 实例。")]
    public sealed class GameObjectPoolModule : AAppModule, IGameObjectPool
    {
        #region Private 私有成员

        private const int DefaultMaximumRetained = 64; // Rent 首次创建池时使用的默认最大保留量。
        private readonly Dictionary<GameObjectPoolKey, GameObjectPool> pools = new Dictionary<GameObjectPoolKey, GameObjectPool>(); // 复合标识到对象池的映射。
        private readonly Dictionary<IPoolObject, GameObjectPool> owners = new Dictionary<IPoolObject, GameObjectPool>(); // 租出实例到所属池的映射。
        private IResourceService resourceService; // 创建与释放池实例的资源服务。
        private GameObject rootObject; // 当前模块全部缓存对象的跨场景根节点。

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 从指定复合对象池租用实例。
        /// </summary>
        /// <typeparam name="T">预制体上实现池契约的组件类型。</typeparam>
        /// <param name="address">预制体资源地址。</param>
        /// <param name="parent">租用期间使用的父节点。</param>
        /// <param name="group">业务分组。</param>
        /// <returns>已初始化并激活的组件实例。</returns>
        public async MTask<T> RentAsync<T>(string address, Transform parent = null, string group = "Default")
            where T : MonoBehaviour, IPoolObject
        {
            GameObjectPool pool = GetOrCreatePool<T>(address, group, DefaultMaximumRetained, false);
            T value = await pool.RentAsync<T>(parent);
            owners.Add(value, pool);
            return value;
        }

        /// <summary>
        /// 将实例归还到创建它的复合对象池。
        /// </summary>
        /// <param name="instance">不再使用的池对象。</param>
        /// <returns>实例属于当前模块且成功归还时返回 true。</returns>
        public bool Return(IPoolObject instance)
        {
            if (instance == null || !owners.TryGetValue(instance, out GameObjectPool pool) || !pool.Return(instance))
            {
                return false;
            }

            owners.Remove(instance);
            return true;
        }

        /// <summary>
        /// 将指定复合对象池预热到目标缓存数量。
        /// </summary>
        /// <typeparam name="T">预制体上实现池契约的组件类型。</typeparam>
        /// <param name="address">预制体资源地址。</param>
        /// <param name="count">希望保留的缓存数量。</param>
        /// <param name="group">业务分组。</param>
        /// <param name="maximumRetained">对象池允许保留的最大数量。</param>
        /// <returns>全部预热实例完成创建后的任务。</returns>
        public MTask PrewarmAsync<T>(string address, int count, string group = "Default", int maximumRetained = 64)
            where T : MonoBehaviour, IPoolObject
        {
            return GetOrCreatePool<T>(address, group, maximumRetained, true).PrewarmAsync(count);
        }

        /// <summary>
        /// 清空指定复合对象池当前缓存的实例。
        /// </summary>
        /// <typeparam name="T">池对象组件类型。</typeparam>
        /// <param name="address">预制体资源地址。</param>
        /// <param name="group">业务分组。</param>
        /// <returns>找到目标对象池时返回 true。</returns>
        public bool Clear<T>(string address, string group = "Default")
            where T : MonoBehaviour, IPoolObject
        {
            var key = new GameObjectPoolKey(address, typeof(T), group);
            if (!pools.TryGetValue(key, out GameObjectPool pool))
            {
                return false;
            }

            pool.ClearRetained();
            return true;
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 获取资源服务并创建当前模块唯一的跨场景池根节点。
        /// </summary>
        public override void Awake()
        {
            resourceService = Global.GetService<IResourceService>(this);
            rootObject = new GameObject("MiniCore.GameObjectPools");
            UnityEngine.Object.DontDestroyOnLoad(rootObject);
        }

        /// <summary>
        /// 销毁缓存、租出实例和池根节点，并释放资源服务租约。
        /// </summary>
        protected override void OnDispose()
        {
            foreach (GameObjectPool pool in pools.Values)
            {
                pool.Dispose();
            }

            pools.Clear();
            owners.Clear();
            if (rootObject != null)
            {
                UnityEngine.Object.Destroy(rootObject);
                rootObject = null;
            }

            resourceService = null;
            Global.ReleaseAll(this);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 获取或创建指定复合标识的对象池，并校验最大保留量保持一致。
        /// </summary>
        /// <typeparam name="T">池对象组件类型。</typeparam>
        /// <param name="address">预制体资源地址。</param>
        /// <param name="group">业务分组。</param>
        /// <param name="maximumRetained">最大缓存数量。</param>
        /// <param name="validateExistingMaximum">已有池是否必须与调用方显式容量一致。</param>
        /// <returns>唯一对象池实例。</returns>
        private GameObjectPool GetOrCreatePool<T>(string address, string group, int maximumRetained, bool validateExistingMaximum)
            where T : MonoBehaviour, IPoolObject
        {
            var key = new GameObjectPoolKey(address, typeof(T), group);
            if (pools.TryGetValue(key, out GameObjectPool existing))
            {
                if (validateExistingMaximum && existing.MaximumRetained != maximumRetained)
                {
                    throw new InvalidOperationException(
                        $"对象池 {key.Address}/{key.ComponentType.FullName}/{key.Group} 已使用最大保留量 {existing.MaximumRetained} 创建，不能改为 {maximumRetained}。");
                }

                return existing;
            }

            GameObject poolRoot = new GameObject($"{typeof(T).Name}_{key.Group}_Pool");
            poolRoot.transform.SetParent(rootObject.transform, false);
            var pool = new GameObjectPool(key, poolRoot.transform, ResourceService, maximumRetained);
            pools.Add(key, pool);
            return pool;
        }

        /// <summary>
        /// 获取已经初始化的资源服务。
        /// </summary>
        /// <returns>资源服务实例。</returns>
        private IResourceService ResourceService => resourceService
            ?? throw new InvalidOperationException("GameObject 对象池模块尚未初始化。");

        #endregion
    }
}
