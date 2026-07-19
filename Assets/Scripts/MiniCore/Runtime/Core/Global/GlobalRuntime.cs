using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using MiniCore.Model;

namespace MiniCore.Core
{
    /// <summary>
    /// 纯 C# 全局组件运行时，负责 owner 引用计数、生命周期与 Tick 调度。
    /// </summary>
    internal sealed class GlobalRuntime : IDisposable
    {
        #region Private 私有成员

        private static readonly OwnerReferenceComparer OwnerComparer = new OwnerReferenceComparer(); // owner 的引用身份比较器。
        private readonly object rootOwner = new object(); // 常驻组件的内部 owner。
        private readonly Dictionary<Type, ComponentEntry> entries = new Dictionary<Type, ComponentEntry>(); // 按组件类型保存的生命周期条目。
        private readonly Dictionary<ComponentKey, ComponentEntry> groupEntries = new Dictionary<ComponentKey, ComponentEntry>(); // 按类型和分组保存的多实例组件条目。
        private readonly List<ComponentKey> releaseGroupKeys = new List<ComponentKey>(); // 分组组件释放时复用的键快照。
        private readonly List<ComponentKey> destroyGroupKeys = new List<ComponentKey>(); // 销毁指定分组时复用的键快照。
        private readonly List<Type> releaseTypes = new List<Type>(); // ReleaseAll 的复用快照。
        private readonly List<AComponent> tickSnapshot = new List<AComponent>(); // Tick 的复用快照。
        private readonly List<AComponent> disposeSnapshot = new List<AComponent>(); // Shutdown 的复用快照。
        private readonly int ownerThreadId; // 初始化运行时的唯一管理线程。
        private bool disposed; // 是否已经关闭。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 在当前线程创建组件运行时。
        /// </summary>
        internal GlobalRuntime()
        {
            ownerThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        /// 获取已经存在的组件并增加 owner 引用。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="owner">本次持有组件的 owner。</param>
        /// <returns>已激活的组件。</returns>
        internal T Get<T>(object owner) where T : AComponent, new()
        {
            EnsureAvailable();
            ValidateOwner(owner);
            Type componentType = typeof(T);
            if (!entries.TryGetValue(componentType, out ComponentEntry entry))
            {
                throw new InvalidOperationException($"未找到全局组件：{componentType.FullName}。请先调用 GetOrAdd 或 Pin。");
            }

            T component = entry.Component as T;
            EnsureUsable(componentType, component);
            entry.AddReference(owner);
            return component;
        }

        /// <summary>
        /// 获取指定分组中已经存在的组件并增加 owner 引用。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="owner">本次持有组件的 owner。</param>
        /// <param name="groupId">组件所属分组。</param>
        /// <returns>已激活的分组组件。</returns>
        internal T Get<T>(object owner, ComponentGroupId groupId) where T : AComponent, new()
        {
            if (groupId.IsDefault)
            {
                return Get<T>(owner);
            }

            EnsureAvailable();
            ValidateOwner(owner);
            ComponentKey key = new ComponentKey(typeof(T), groupId);
            if (!groupEntries.TryGetValue(key, out ComponentEntry entry))
            {
                throw new InvalidOperationException($"未找到分组组件：{typeof(T).FullName} group:{groupId}。请先调用 GetOrAdd。");
            }

            T component = entry.Component as T;
            EnsureUsable(typeof(T), component);
            entry.AddReference(owner);
            return component;
        }

        /// <summary>
        /// 获取或创建无参组件并增加 owner 引用。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="owner">本次持有组件的 owner。</param>
        /// <returns>已激活的组件。</returns>
        internal T GetOrAdd<T>(object owner) where T : AComponent, new()
        {
            return GetOrCreate<T>(owner, null);
        }

        /// <summary>
        /// 获取或创建带初始化参数的组件并增加 owner 引用。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="owner">本次持有组件的 owner。</param>
        /// <param name="args">首次创建时使用的参数。</param>
        /// <returns>已激活的组件。</returns>
        internal T GetOrAdd<T>(object owner, ComponentInitArgs args) where T : AComponent, new()
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return GetOrCreate<T>(owner, args);
        }

        /// <summary>
        /// 获取或创建指定分组中的无参组件并增加 owner 引用。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="owner">本次持有组件的 owner。</param>
        /// <param name="groupId">组件所属分组。</param>
        /// <returns>已激活的分组组件。</returns>
        internal T GetOrAdd<T>(object owner, ComponentGroupId groupId) where T : AComponent, new()
        {
            return GetOrCreateGrouped<T>(owner, groupId, null);
        }

        /// <summary>
        /// 获取或创建指定分组中的带参数组件并增加 owner 引用。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="owner">本次持有组件的 owner。</param>
        /// <param name="groupId">组件所属分组。</param>
        /// <param name="args">首次创建时使用的参数。</param>
        /// <returns>已激活的分组组件。</returns>
        internal T GetOrAdd<T>(object owner, ComponentGroupId groupId, ComponentInitArgs args) where T : AComponent, new()
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return GetOrCreateGrouped<T>(owner, groupId, args);
        }

        /// <summary>
        /// 强制销毁指定分组中的所有组件。
        /// </summary>
        /// <param name="groupId">待销毁的非默认分组身份。</param>
        /// <param name="groupName">用于诊断的分组名称。</param>
        internal void DestroyGroup(ComponentGroupId groupId, string groupName)
        {
            EnsureThread();
            if (groupId.IsDefault)
            {
                throw new InvalidOperationException("默认全局组不能通过 DestroyGroup 销毁。");
            }

            destroyGroupKeys.Clear();
            foreach (KeyValuePair<ComponentKey, ComponentEntry> pair in groupEntries)
            {
                if (pair.Key.GroupId == groupId)
                {
                    destroyGroupKeys.Add(pair.Key);
                }
            }

            for (int i = 0; i < destroyGroupKeys.Count; i++)
            {
                ComponentKey key = destroyGroupKeys[i];
                if (groupEntries.TryGetValue(key, out ComponentEntry entry))
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (entry.ReferenceCount > 0)
                    {
                        LogSwitch.Warning($"销毁组件分组时仍存在 owner 引用：{key.ComponentType.FullName} group:{groupId} owners:{entry.DescribeOwners(rootOwner)}");
                    }
#endif
                    groupEntries.Remove(key);
                    DisposeEntry(entry);
                }
            }

            if (destroyGroupKeys.Count > 0)
            {
                LogSwitch.Warning($"已强制销毁组件分组：{groupName} group:{groupId} count:{destroyGroupKeys.Count}");
            }

            destroyGroupKeys.Clear();
        }

        /// <summary>
        /// 以根 owner 获取或创建常驻组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <returns>已激活的常驻组件。</returns>
        internal T Pin<T>() where T : AComponent, new()
        {
            return PinInternal<T>(null);
        }

        /// <summary>
        /// 以根 owner 获取或创建带参数的常驻组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="args">首次创建时使用的参数。</param>
        /// <returns>已激活的常驻组件。</returns>
        internal T Pin<T>(ComponentInitArgs args) where T : AComponent, new()
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return PinInternal<T>(args);
        }

        /// <summary>
        /// 解除根 owner 对指定组件的常驻引用。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        internal void Unpin<T>() where T : AComponent, new()
        {
            EnsureThread();
            Type componentType = typeof(T);
            if (!entries.TryGetValue(componentType, out ComponentEntry entry) || !entry.RemoveReference(rootOwner))
            {
                LogSwitch.Warning($"重复 Unpin 或组件不存在：{componentType.FullName}");
                return;
            }

            DisposeWhenUnreferenced(componentType, entry);
        }

        /// <summary>
        /// 释放 owner 对指定组件持有的一份引用。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="owner">此前持有组件的 owner。</param>
        internal void Remove<T>(object owner) where T : AComponent, new()
        {
            EnsureThread();
            ValidateOwner(owner);
            Type componentType = typeof(T);
            if (!entries.TryGetValue(componentType, out ComponentEntry entry) || !entry.RemoveReference(owner))
            {
                throw new InvalidOperationException($"Global.Remove<{componentType.Name}> 失败：owner 未持有组件。");
            }

            DisposeWhenUnreferenced(componentType, entry);
        }

        /// <summary>
        /// 释放 owner 持有的全部组件引用。
        /// </summary>
        /// <param name="owner">要释放引用的 owner。</param>
        internal void ReleaseAll(object owner)
        {
            EnsureThread();
            ValidateOwner(owner);
            releaseTypes.Clear();
            releaseGroupKeys.Clear();
            foreach (KeyValuePair<Type, ComponentEntry> pair in entries)
            {
                if (pair.Value.HasReference(owner))
                {
                    releaseTypes.Add(pair.Key);
                }
            }
            foreach (KeyValuePair<ComponentKey, ComponentEntry> pair in groupEntries)
            {
                if (pair.Value.HasReference(owner))
                {
                    releaseGroupKeys.Add(pair.Key);
                }
            }

            for (int i = 0; i < releaseTypes.Count; i++)
            {
                Type componentType = releaseTypes[i];
                if (entries.TryGetValue(componentType, out ComponentEntry entry))
                {
                    entry.RemoveAllReferences(owner);
                    DisposeWhenUnreferenced(componentType, entry);
                }
            }

            for (int i = 0; i < releaseGroupKeys.Count; i++)
            {
                ComponentKey key = releaseGroupKeys[i];
                if (groupEntries.TryGetValue(key, out ComponentEntry entry))
                {
                    entry.RemoveAllReferences(owner);
                    if (entry.ReferenceCount <= 0)
                    {
                        groupEntries.Remove(key);
                        DisposeEntry(entry);
                    }
                }
            }

            releaseTypes.Clear();
            releaseGroupKeys.Clear();
        }

        /// <summary>
        /// 忽略全部 owner 引用并立即销毁组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        internal void ForceRemove<T>() where T : AComponent, new()
        {
            EnsureThread();
            Type componentType = typeof(T);
            if (!entries.TryGetValue(componentType, out ComponentEntry entry))
            {
                return;
            }

            entries.Remove(componentType);
            DisposeEntry(entry);
        }

        /// <summary>
        /// 调度所有激活组件的一次 Tick。
        /// </summary>
        internal void Tick()
        {
            EnsureAvailable();
            EnsureThread();
            if (entries.Count == 0 && groupEntries.Count == 0)
            {
                return;
            }

            tickSnapshot.Clear();
            foreach (ComponentEntry entry in entries.Values)
            {
                tickSnapshot.Add(entry.Component);
            }
            foreach (ComponentEntry entry in groupEntries.Values)
            {
                tickSnapshot.Add(entry.Component);
            }

            for (int i = 0; i < tickSnapshot.Count; i++)
            {
                tickSnapshot[i]?.MonoUpdate();
            }

            tickSnapshot.Clear();
        }

        /// <summary>
        /// 强制释放全部组件并关闭运行时。
        /// </summary>
        public void Dispose()
        {
            EnsureThread();
            if (disposed)
            {
                return;
            }

            disposed = true;
            disposeSnapshot.Clear();
            foreach (ComponentEntry entry in entries.Values)
            {
                disposeSnapshot.Add(entry.Component);
            }
            foreach (ComponentEntry entry in groupEntries.Values)
            {
                disposeSnapshot.Add(entry.Component);
            }

            entries.Clear();
            groupEntries.Clear();
            for (int i = 0; i < disposeSnapshot.Count; i++)
            {
                DisposeComponent(disposeSnapshot[i]);
            }

            disposeSnapshot.Clear();
            releaseTypes.Clear();
            releaseGroupKeys.Clear();
            destroyGroupKeys.Clear();
            tickSnapshot.Clear();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 获取已有组件或在首次访问时创建组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="owner">本次持有组件的 owner。</param>
        /// <param name="args">首次创建参数。</param>
        /// <returns>已激活的组件。</returns>
        private T GetOrCreate<T>(object owner, ComponentInitArgs args) where T : AComponent, new()
        {
            EnsureAvailable();
            ValidateOwner(owner);
            Type componentType = typeof(T);
            if (entries.TryGetValue(componentType, out ComponentEntry entry))
            {
                T existing = entry.Component as T;
                EnsureUsable(componentType, existing);
                entry.AddReference(owner);
                return existing;
            }

            T component = new T();
            if (args == null)
            {
                component.Awake();
            }
            else
            {
                component.Awake(args);
            }

            if (component.IsDisposed)
            {
                throw new InvalidOperationException($"组件 {componentType.FullName} 在 Awake 期间已被释放。");
            }

            component.IsActive = true;
            ComponentEntry newEntry = new ComponentEntry(component);
            newEntry.AddReference(owner);
            entries.Add(componentType, newEntry);
            return component;
        }

        /// <summary>
        /// 获取已有分组组件或在首次访问时创建组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="owner">本次持有组件的 owner。</param>
        /// <param name="groupId">组件所属分组。</param>
        /// <param name="args">首次创建参数。</param>
        /// <returns>已激活的分组组件。</returns>
        private T GetOrCreateGrouped<T>(object owner, ComponentGroupId groupId, ComponentInitArgs args) where T : AComponent, new()
        {
            if (groupId.IsDefault)
            {
                return args == null ? GetOrCreate<T>(owner, null) : GetOrCreate<T>(owner, args);
            }

            EnsureAvailable();
            ValidateOwner(owner);
            ComponentKey key = new ComponentKey(typeof(T), groupId);
            if (groupEntries.TryGetValue(key, out ComponentEntry entry))
            {
                T existing = entry.Component as T;
                EnsureUsable(typeof(T), existing);
                entry.AddReference(owner);
                return existing;
            }

            T component = new T();
            component.SetGroupId(groupId);
            if (args == null)
            {
                component.Awake();
            }
            else
            {
                component.Awake(args);
            }

            if (component.IsDisposed)
            {
                throw new InvalidOperationException($"分组组件 {typeof(T).FullName} 在 Awake 期间已被释放。");
            }

            component.IsActive = true;
            ComponentEntry newEntry = new ComponentEntry(component);
            newEntry.AddReference(owner);
            groupEntries.Add(key, newEntry);
            return component;
        }

        /// <summary>
        /// 确保根 owner 仅持有一份常驻引用。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="args">首次创建参数。</param>
        /// <returns>已激活的组件。</returns>
        private T PinInternal<T>(ComponentInitArgs args) where T : AComponent, new()
        {
            EnsureAvailable();
            Type componentType = typeof(T);
            if (entries.TryGetValue(componentType, out ComponentEntry entry))
            {
                T existing = entry.Component as T;
                EnsureUsable(componentType, existing);
                if (!entry.HasReference(rootOwner))
                {
                    entry.AddReference(rootOwner);
                }

                return existing;
            }

            return GetOrCreate<T>(rootOwner, args);
        }

        /// <summary>
        /// 在最后一份引用释放后销毁组件。
        /// </summary>
        /// <param name="componentType">组件类型。</param>
        /// <param name="entry">生命周期条目。</param>
        private void DisposeWhenUnreferenced(Type componentType, ComponentEntry entry)
        {
            if (entry.ReferenceCount > 0)
            {
                return;
            }

            if (entries.TryGetValue(componentType, out ComponentEntry current) && ReferenceEquals(current, entry))
            {
                entries.Remove(componentType);
            }

            DisposeEntry(entry);
        }

        /// <summary>
        /// 清空引用并释放条目中的组件。
        /// </summary>
        /// <param name="entry">要释放的条目。</param>
        private void DisposeEntry(ComponentEntry entry)
        {
            entry.ClearReferences();
            DisposeComponent(entry.Component);
        }

        /// <summary>
        /// 停用并释放组件。
        /// </summary>
        /// <param name="component">要释放的组件。</param>
        private static void DisposeComponent(AComponent component)
        {
            if (component == null || component.IsDisposed)
            {
                return;
            }

            component.IsActive = false;
            component.Dispose();
        }

        /// <summary>
        /// 验证 owner 可作为组件引用持有者。
        /// </summary>
        /// <param name="owner">待验证 owner。</param>
        private static void ValidateOwner(object owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner), "Global 组件引用必须提供 owner。");
            }

            if (owner is AComponent component && component.IsDisposed)
            {
                throw new ObjectDisposedException(owner.GetType().FullName, "已释放组件不能继续持有 Global 组件。");
            }
        }

        /// <summary>
        /// 验证组件仍可继续使用。
        /// </summary>
        /// <param name="componentType">组件类型。</param>
        /// <param name="component">组件实例。</param>
        private static void EnsureUsable(Type componentType, AComponent component)
        {
            if (component == null || component.IsDisposed || !component.IsActive)
            {
                throw new InvalidOperationException($"全局组件 {componentType.FullName} 已失效。");
            }
        }

        /// <summary>
        /// 验证运行时仍可接受操作。
        /// </summary>
        private void EnsureAvailable()
        {
            EnsureThread();
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(GlobalRuntime));
            }
        }

        /// <summary>
        /// 验证组件管理操作在初始化线程执行。
        /// </summary>
        private void EnsureThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != ownerThreadId)
            {
                throw new InvalidOperationException("Global 组件管理操作必须在运行时初始化线程执行。");
            }
        }

        /// <summary>
        /// 标识一个 Global 组件实例的类型与分组组合键。
        /// 默认分组不写入 groupEntries，非默认分组使用该键实现同类型多实例。
        /// </summary>
        private readonly struct ComponentKey : IEquatable<ComponentKey>
        {
            #region Public 公共成员

            /// <summary>
            /// 获取组件具体类型。
            /// </summary>
            public Type ComponentType { get; }

            /// <summary>
            /// 获取组件所属分组。
            /// </summary>
            public ComponentGroupId GroupId { get; }

            /// <summary>
            /// 创建组件实例键。
            /// </summary>
            /// <param name="componentType">组件具体类型。</param>
            /// <param name="groupId">组件所属分组。</param>
            public ComponentKey(Type componentType, ComponentGroupId groupId)
            {
                ComponentType = componentType;
                GroupId = groupId;
            }

            /// <summary>
            /// 判断两个组件实例键是否相同。
            /// </summary>
            /// <param name="other">待比较键。</param>
            /// <returns>类型与分组都相同时返回 true。</returns>
            public bool Equals(ComponentKey other)
            {
                return ComponentType == other.ComponentType && GroupId == other.GroupId;
            }

            /// <summary>
            /// 判断当前对象是否为相同组件实例键。
            /// </summary>
            /// <param name="obj">待比较对象。</param>
            /// <returns>对象为相同键时返回 true。</returns>
            public override bool Equals(object obj)
            {
                return obj is ComponentKey other && Equals(other);
            }

            /// <summary>
            /// 获取组件实例键哈希值。
            /// </summary>
            /// <returns>类型与分组组合的哈希值。</returns>
            public override int GetHashCode()
            {
                unchecked
                {
                    return ((ComponentType != null ? ComponentType.GetHashCode() : 0) * 397) ^ GroupId.GetHashCode();
                }
            }

            #endregion
        }

        /// <summary>
        /// 保存组件实例及其 owner 引用计数。
        /// </summary>
        private sealed class ComponentEntry
        {
            #region Private 私有成员

            private readonly Dictionary<object, int> ownerReferences = new Dictionary<object, int>(OwnerComparer); // owner 到引用次数的映射。

            #endregion

            #region Public 公共成员

            /// <summary>
            /// 获取条目管理的组件。
            /// </summary>
            public AComponent Component { get; }

            /// <summary>
            /// 获取全部 owner 的引用总数。
            /// </summary>
            public int ReferenceCount { get; private set; }

            /// <summary>
            /// 使用组件创建生命周期条目。
            /// </summary>
            /// <param name="component">要管理的组件。</param>
            public ComponentEntry(AComponent component)
            {
                Component = component;
            }

            /// <summary>
            /// 增加 owner 的一份引用。
            /// </summary>
            /// <param name="owner">持有者。</param>
            public void AddReference(object owner)
            {
                ownerReferences.TryGetValue(owner, out int count);
                ownerReferences[owner] = count + 1;
                ReferenceCount++;
            }

            /// <summary>
            /// 释放 owner 的一份引用。
            /// </summary>
            /// <param name="owner">持有者。</param>
            /// <returns>实际移除引用时返回 true。</returns>
            public bool RemoveReference(object owner)
            {
                if (!ownerReferences.TryGetValue(owner, out int count) || count <= 0)
                {
                    return false;
                }

                if (count == 1)
                {
                    ownerReferences.Remove(owner);
                }
                else
                {
                    ownerReferences[owner] = count - 1;
                }

                ReferenceCount--;
                return true;
            }

            /// <summary>
            /// 移除 owner 的全部引用。
            /// </summary>
            /// <param name="owner">持有者。</param>
            /// <returns>移除的引用数。</returns>
            public int RemoveAllReferences(object owner)
            {
                if (!ownerReferences.TryGetValue(owner, out int count))
                {
                    return 0;
                }

                ownerReferences.Remove(owner);
                ReferenceCount -= count;
                return count;
            }

            /// <summary>
            /// 判断 owner 是否持有组件。
            /// </summary>
            /// <param name="owner">待检查持有者。</param>
            /// <returns>持有时返回 true。</returns>
            public bool HasReference(object owner)
            {
                return ownerReferences.ContainsKey(owner);
            }

            /// <summary>
            /// 构建仅用于 Development/Editor 销毁诊断的 owner 引用摘要。
            /// </summary>
            /// <param name="root">Global 的常驻根 owner。</param>
            /// <returns>owner 类型与引用计数摘要。</returns>
            public string DescribeOwners(object root)
            {
                var descriptions = new List<string>(ownerReferences.Count);
                foreach (KeyValuePair<object, int> pair in ownerReferences)
                {
                    string ownerName = ReferenceEquals(pair.Key, root) ? "Global.Pin" : pair.Key?.GetType().FullName ?? "null";
                    descriptions.Add(ownerName + "x" + pair.Value);
                }

                return string.Join(", ", descriptions);
            }

            /// <summary>
            /// 清空全部 owner 引用。
            /// </summary>
            public void ClearReferences()
            {
                ownerReferences.Clear();
                ReferenceCount = 0;
            }

            #endregion
        }

        /// <summary>
        /// 以对象身份而不是 Equals 比较 owner。
        /// </summary>
        private sealed class OwnerReferenceComparer : IEqualityComparer<object>
        {
            /// <summary>
            /// 判断两个 owner 是否是同一引用。
            /// </summary>
            /// <param name="left">第一个 owner。</param>
            /// <param name="right">第二个 owner。</param>
            /// <returns>同一引用时返回 true。</returns>
            public new bool Equals(object left, object right)
            {
                return ReferenceEquals(left, right);
            }

            /// <summary>
            /// 获取对象身份哈希值。
            /// </summary>
            /// <param name="owner">owner 对象。</param>
            /// <returns>身份哈希值。</returns>
            public int GetHashCode(object owner)
            {
                return RuntimeHelpers.GetHashCode(owner);
            }
        }

        #endregion
    }
}
