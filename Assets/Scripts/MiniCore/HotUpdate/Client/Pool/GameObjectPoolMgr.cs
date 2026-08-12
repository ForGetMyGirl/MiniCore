using System;
using System.Collections.Generic;
using MiniCore.Model;
using MiniCore.Threading;
using UnityEngine;

namespace MiniCore.Core
{
    /// <summary>
    /// 按资源地址、组件类型和业务分组管理 GameObject 对象池。
    /// 同一脚本挂在不同预制体上时会进入不同对象池。
    /// </summary>
    [MTaskOwner]
    public sealed class GameObjectPoolMgr : MonoSingleton<GameObjectPoolMgr>
    {
        #region Private 私有成员

        private const string DefaultGroupName = "DefaultGroup"; // 未显式指定时使用的业务分组。
        private const int DefaultMaximumRetained = 64; // 每个复合池默认最大缓存数量。
        private readonly Dictionary<GameObjectPoolKey, GameObjectPool> pools
            = new Dictionary<GameObjectPoolKey, GameObjectPool>(); // 复合标识到对象池的映射。
        private readonly Dictionary<IPoolObject, GameObjectPool> owners
            = new Dictionary<IPoolObject, GameObjectPool>(); // 已创建对象到所属池的稳定映射。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 初始化跨场景对象池管理器。
        /// </summary>
        protected override void Init()
        {
            base.Init();
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 按资源地址、组件类型和分组租用对象。
        /// </summary>
        /// <typeparam name="T">预制体上实现池契约的组件类型。</typeparam>
        /// <param name="path">预制体资源地址。</param>
        /// <param name="group">业务分组；相同地址和类型可按组隔离。</param>
        /// <param name="parent">租用期间的父节点。</param>
        /// <returns>已经初始化并激活的池对象。</returns>
        public async MTask<T> GeneratePoolObject<T>(
            string path,
            string group = DefaultGroupName,
            Transform parent = null)
            where T : MonoBehaviour, IPoolObject
        {
            GameObjectPool pool = GetOrCreatePool<T>(path, group, DefaultMaximumRetained);
            T value = await pool.RentAsync<T>(parent);
            owners[value] = pool;
            return value;
        }

        /// <summary>
        /// 将对象归还到创建它的复合对象池。
        /// </summary>
        /// <typeparam name="T">池对象组件类型。</typeparam>
        /// <param name="obj">不再使用的池对象。</param>
        /// <returns>对象属于本管理器且成功归还时返回 true。</returns>
        public bool CollectPoolObject<T>(T obj) where T : MonoBehaviour, IPoolObject
        {
            if (obj == null || !owners.TryGetValue(obj, out GameObjectPool pool))
            {
                return false;
            }

            if (!pool.Return(obj, out bool destroyed))
            {
                return false;
            }

            owners.Remove(obj);

            return true;
        }

        /// <summary>
        /// 预热指定资源地址、组件类型和分组对应的对象池。
        /// </summary>
        /// <typeparam name="T">预制体上实现池契约的组件类型。</typeparam>
        /// <param name="path">预制体资源地址。</param>
        /// <param name="count">希望缓存的对象数量。</param>
        /// <param name="group">业务分组。</param>
        /// <param name="maximumRetained">当前池最大缓存数量。</param>
        /// <returns>全部预热实例完成创建后的任务。</returns>
        public MTask PrewarmAsync<T>(
            string path,
            int count,
            string group = DefaultGroupName,
            int maximumRetained = DefaultMaximumRetained)
            where T : MonoBehaviour, IPoolObject
        {
            return GetOrCreatePool<T>(path, group, maximumRetained).PrewarmAsync(count);
        }

        /// <summary>
        /// 清空指定复合对象池当前缓存的对象，已经租出的对象仍可归还。
        /// </summary>
        /// <typeparam name="T">池对象组件类型。</typeparam>
        /// <param name="path">预制体资源地址。</param>
        /// <param name="group">业务分组。</param>
        /// <returns>找到目标对象池时返回 true。</returns>
        public bool ClearPool<T>(string path, string group = DefaultGroupName)
            where T : MonoBehaviour, IPoolObject
        {
            var key = new GameObjectPoolKey(path, typeof(T), group);
            if (!pools.TryGetValue(key, out GameObjectPool pool))
            {
                return false;
            }

            pool.ClearRetained();
            return true;
        }

        /// <summary>
        /// 销毁全部对象池及其缓存和租出实例。
        /// </summary>
        public void ReleaseAllPools()
        {
            foreach (GameObjectPool pool in pools.Values)
            {
                pool.Dispose();
            }

            pools.Clear();
            owners.Clear();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 获取或创建指定复合标识的对象池。
        /// </summary>
        /// <typeparam name="T">池对象组件类型。</typeparam>
        /// <param name="path">预制体资源地址。</param>
        /// <param name="group">业务分组。</param>
        /// <param name="maximumRetained">最大缓存数量。</param>
        /// <returns>唯一对象池实例。</returns>
        private GameObjectPool GetOrCreatePool<T>(string path, string group, int maximumRetained)
            where T : MonoBehaviour, IPoolObject
        {
            var key = new GameObjectPoolKey(path, typeof(T), group);
            if (pools.TryGetValue(key, out GameObjectPool pool))
            {
                return pool;
            }

            var rootObject = new GameObject($"{typeof(T).Name}_{group}_Pool");
            rootObject.transform.SetParent(transform, false);
            pool = new GameObjectPool(key, rootObject.transform, maximumRetained);
            pools.Add(key, pool);
            return pool;
        }

        /// <summary>
        /// Unity 销毁管理器时释放全部池实例。
        /// </summary>
        private void OnDestroy()
        {
            ReleaseAllPools();
        }

        #endregion
    }
}
