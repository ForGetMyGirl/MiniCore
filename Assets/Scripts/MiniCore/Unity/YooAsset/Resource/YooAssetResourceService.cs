using System;
using System.Collections.Generic;
using MiniCore.Model;
using MiniCore.Threading;
using MiniCore.Unity;
using UnityEngine;
using YooAsset;

namespace MiniCore.Service
{
    /// <summary>
    /// 基于 YooAsset 的资源服务 Provider，统一管理资源引用和实例生命周期。
    /// </summary>
    [AppService(
        "YooAsset 资源",
        typeof(IResourceService),
        Description = "基于 YooAsset 提供引用计数资源加载和实例生命周期管理。",
        InitArgsType = typeof(YooAssetResourceServiceInitArgs))]
    public sealed class YooAssetResourceService : AAppService, IResourceService
    {
        #region Private 私有成员

        private readonly Dictionary<string, AssetEntry> assets = new Dictionary<string, AssetEntry>(StringComparer.Ordinal); // 地址到共享资源句柄的映射。
        private readonly Dictionary<GameObject, AssetEntry> instances = new Dictionary<GameObject, AssetEntry>(); // 实例到其资源租约的映射。
        private readonly List<GameObject> instanceSnapshot = new List<GameObject>(); // 服务释放时复用的实例快照。
        private ResourcePackage package; // 当前绑定的 YooAsset 资源包。

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 异步实例化指定地址的 GameObject，并为实例保留独立资源引用。
        /// </summary>
        /// <param name="key">资源地址或资源键。</param>
        /// <param name="parent">实例化对象的父节点。</param>
        /// <returns>实例化完成的游戏对象。</returns>
        public async MTask<GameObject> InstantiateAsync(string key, Transform parent = null)
        {
            string normalizedKey = NormalizeKey(key);
            AssetEntry entry = GetOrCreateEntry<GameObject>(normalizedKey);
            entry.PendingInstanceCount++;
            try
            {
                await entry.Loading;
                InstantiateOperation operation = entry.Handle.InstantiateAsync(parent);
                await operation.ToMTask();
                if (operation.Status != EOperationStatus.Succeed || operation.Result == null)
                {
                    throw new InvalidOperationException($"YooAsset 实例化失败：{normalizedKey}，{operation.Error}");
                }

                GameObject instance = operation.Result;
                entry.InstanceReferenceCount++;
                instances.Add(instance, entry);
                return instance;
            }
            finally
            {
                entry.PendingInstanceCount--;
                TryReleaseEntry(entry);
            }
        }

        /// <summary>
        /// 加载并持有指定资源；同一地址的并发请求共享一次 YooAsset 加载。
        /// </summary>
        /// <typeparam name="T">资源对象类型。</typeparam>
        /// <param name="key">资源地址或资源键。</param>
        /// <returns>加载完成的资源对象。</returns>
        public async MTask<T> PreloadAssetAsync<T>(string key) where T : UnityEngine.Object
        {
            string normalizedKey = NormalizeKey(key);
            AssetEntry entry = GetOrCreateEntry<T>(normalizedKey);
            entry.AssetReferenceCount++;
            try
            {
                UnityEngine.Object asset = await entry.Loading;
                return (T)asset;
            }
            catch
            {
                if (assets.TryGetValue(normalizedKey, out AssetEntry current) && ReferenceEquals(current, entry))
                {
                    entry.AssetReferenceCount--;
                    TryReleaseEntry(entry);
                }

                throw;
            }
        }

        /// <summary>
        /// 释放指定地址的一份显式资源引用；实例引用不受影响。
        /// </summary>
        /// <param name="key">要释放的资源地址或资源键。</param>
        /// <returns>存在可释放的显式资源引用时返回 true。</returns>
        public bool ReleaseAsset(string key)
        {
            string normalizedKey = NormalizeKey(key);
            if (!assets.TryGetValue(normalizedKey, out AssetEntry entry) || entry.AssetReferenceCount <= 0)
            {
                return false;
            }

            entry.AssetReferenceCount--;
            TryReleaseEntry(entry);
            return true;
        }

        /// <summary>
        /// 释放由当前服务创建的实例及其独立资源引用。
        /// </summary>
        /// <param name="instance">要释放的游戏对象。</param>
        /// <returns>实例属于当前服务且已完成释放时返回 true。</returns>
        public bool ReleaseInstance(GameObject instance)
        {
            if (instance == null || !instances.TryGetValue(instance, out AssetEntry entry))
            {
                return false;
            }

            instances.Remove(instance);
            UnityEngine.Object.Destroy(instance);
            entry.InstanceReferenceCount--;
            TryReleaseEntry(entry);
            return true;
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 使用资源包参数初始化 YooAsset 资源 Provider。
        /// </summary>
        /// <param name="args">包含资源包名称的初始化参数。</param>
        public override void Awake(ComponentInitArgs args)
        {
            if (!(args is YooAssetResourceServiceInitArgs resourceArgs))
            {
                throw new ArgumentException("YooAsset 资源服务初始化参数类型不正确。", nameof(args));
            }

            if (string.IsNullOrWhiteSpace(resourceArgs.PackageName))
            {
                throw new ArgumentException("YooAsset 资源包名称不能为空。", nameof(args));
            }

            package = YooAssets.GetPackage(resourceArgs.PackageName);
            if (package == null)
            {
                throw new InvalidOperationException($"未找到 YooAsset 资源包：{resourceArgs.PackageName}。");
            }
        }

        /// <summary>
        /// 销毁全部仍存活的实例并释放所有 YooAsset 句柄。
        /// </summary>
        protected override void OnDispose()
        {
            instanceSnapshot.Clear();
            foreach (GameObject instance in instances.Keys)
            {
                instanceSnapshot.Add(instance);
            }

            for (int index = 0; index < instanceSnapshot.Count; index++)
            {
                GameObject instance = instanceSnapshot[index];
                if (instance != null)
                {
                    UnityEngine.Object.Destroy(instance);
                }
            }

            instanceSnapshot.Clear();
            instances.Clear();
            foreach (AssetEntry entry in assets.Values)
            {
                if (entry.Handle != null && entry.Handle.IsValid)
                {
                    entry.Handle.Release();
                }
            }

            assets.Clear();
            package = null;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 获取或创建指定地址和类型的共享资源条目。
        /// </summary>
        /// <typeparam name="T">资源对象类型。</typeparam>
        /// <param name="key">已规范化的资源地址。</param>
        /// <returns>共享资源条目。</returns>
        private AssetEntry GetOrCreateEntry<T>(string key) where T : UnityEngine.Object
        {
            if (assets.TryGetValue(key, out AssetEntry existing))
            {
                if (existing.AssetType != typeof(T))
                {
                    throw new InvalidOperationException(
                        $"资源地址 {key} 已按 {existing.AssetType.FullName} 加载，不能再按 {typeof(T).FullName} 使用。");
                }

                return existing;
            }

            AssetHandle handle = package.LoadAssetAsync<T>(key);
            var entry = new AssetEntry(key, typeof(T), handle);
            assets.Add(key, entry);
            entry.Loading = LoadEntryAsync(entry).Share();
            return entry;
        }

        /// <summary>
        /// 等待一次 YooAsset 加载并验证最终资源类型。
        /// </summary>
        /// <param name="entry">正在加载的共享条目。</param>
        /// <returns>加载完成的 Unity 资源对象。</returns>
        private async MTask<UnityEngine.Object> LoadEntryAsync(AssetEntry entry)
        {
            try
            {
                await entry.Handle.ToMTask();
                if (entry.Handle.Status != EOperationStatus.Succeed || entry.Handle.AssetObject == null)
                {
                    throw new InvalidOperationException($"YooAsset 资源加载失败：{entry.Key}，{entry.Handle.LastError}");
                }

                UnityEngine.Object asset = entry.Handle.AssetObject;
                if (!entry.AssetType.IsInstanceOfType(asset))
                {
                    throw new InvalidOperationException(
                        $"资源地址 {entry.Key} 的实际类型 {asset.GetType().FullName} 与请求类型 {entry.AssetType.FullName} 不一致。");
                }

                return asset;
            }
            catch
            {
                if (assets.TryGetValue(entry.Key, out AssetEntry current) && ReferenceEquals(current, entry))
                {
                    assets.Remove(entry.Key);
                }

                if (entry.Handle != null && entry.Handle.IsValid)
                {
                    entry.Handle.Release();
                }

                throw;
            }
        }

        /// <summary>
        /// 在资源没有显式引用、实例引用或待实例化操作时释放共享句柄。
        /// </summary>
        /// <param name="entry">待检查的共享资源条目。</param>
        private void TryReleaseEntry(AssetEntry entry)
        {
            if (entry.AssetReferenceCount != 0 || entry.InstanceReferenceCount != 0 || entry.PendingInstanceCount != 0)
            {
                return;
            }

            if (!assets.TryGetValue(entry.Key, out AssetEntry current) || !ReferenceEquals(current, entry))
            {
                return;
            }

            assets.Remove(entry.Key);
            if (entry.Handle != null && entry.Handle.IsValid)
            {
                entry.Handle.Release();
            }
        }

        /// <summary>
        /// 校验并规范化资源地址。
        /// </summary>
        /// <param name="key">调用方传入的资源地址。</param>
        /// <returns>去除首尾空白后的稳定地址。</returns>
        private static string NormalizeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("资源地址不能为空。", nameof(key));
            }

            return key.Trim();
        }

        /// <summary>
        /// 保存单个资源地址的共享加载状态与引用计数。
        /// </summary>
        private sealed class AssetEntry
        {
            #region Internal 内部成员

            internal readonly string Key; // 已规范化资源地址。
            internal readonly Type AssetType; // 首次请求锁定的资源类型。
            internal readonly AssetHandle Handle; // YooAsset 唯一共享句柄。
            internal MSharedTask<UnityEngine.Object> Loading; // 多调用方共享的加载任务。
            internal int AssetReferenceCount; // 显式 Preload 持有数量。
            internal int InstanceReferenceCount; // 当前存活实例持有数量。
            internal int PendingInstanceCount; // 正在加载或实例化的请求数量。

            /// <summary>
            /// 创建共享资源条目。
            /// </summary>
            /// <param name="key">资源地址。</param>
            /// <param name="assetType">资源类型。</param>
            /// <param name="handle">YooAsset 句柄。</param>
            internal AssetEntry(string key, Type assetType, AssetHandle handle)
            {
                Key = key;
                AssetType = assetType;
                Handle = handle;
            }

            #endregion
        }

        #endregion
    }
}
