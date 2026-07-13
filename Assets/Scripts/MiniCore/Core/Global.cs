using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using MiniCore.Model;
using UnityEngine;

namespace MiniCore.Core
{
    /// <summary>
    /// 全局组件容器。
    /// 所有业务访问必须携带 owner；组件仅在最后一个 owner 释放后卸载，常驻 Pin 由 Global 的私有根 owner 持有。
    /// </summary>
    public class Global : MonoBehaviour
    {
        #region Private 私有成员

        private static readonly OwnerReferenceComparer ownerReferenceComparer = new OwnerReferenceComparer(); // owner 身份比较器。
        private static Global com; // 全局容器单例实例。
        private static int unityMainThreadId; // Unity 运行时主线程标识。
        private readonly object rootOwner = new object(); // Global 内部常驻组件的根 owner。
        private readonly List<Type> releaseTypeSnapshot = new List<Type>(); // ReleaseAll 使用的类型快照缓存。
        private readonly List<AComponent> updateSnapshot = new List<AComponent>(); // Update 使用的组件快照缓存。
        private readonly List<AComponent> disposeSnapshot = new List<AComponent>(); // Global 关闭时使用的组件快照缓存。
        private readonly Dictionary<Type, ComponentEntry> componentEntries = new Dictionary<Type, ComponentEntry>(); // 全部组件生命周期条目。
        private bool disposed; // 容器是否已完成强制释放。
        private bool isQuitting; // Unity 是否正在退出。
        private int mainThreadId; // 创建 Global 的 Unity 主线程标识。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取全局组件容器。
        /// 场景不存在实例时自动创建持久化容器。
        /// </summary>
        public static Global Com
        {
            get
            {
                EnsureStaticMainThread();
                if (com == null)
                {
                    com = FindObjectOfType<Global>();
                    if (com == null)
                    {
                        new GameObject("Global_Singleton").AddComponent<Global>();
                    }
                    else
                    {
                        com.InitializeIfNeeded();
                    }
                }

                return com;
            }
        }

        /// <summary>
        /// 获取已经存在的组件，并为 owner 增加一份持有引用。
        /// 组件不存在或已销毁时抛出明确异常，调用方应改用 GetOrAdd。
        /// </summary>
        /// <typeparam name="T">要获取的组件类型。</typeparam>
        /// <param name="owner">本次持有组件的对象，通常传入 this。</param>
        /// <returns>已激活的组件实例。</returns>
        public T Get<T>(object owner) where T : AComponent, new()
        {
            EnsureAvailableOnMainThread();
            ValidateOwner(owner);

            Type componentType = typeof(T);
            if (!componentEntries.TryGetValue(componentType, out ComponentEntry entry))
            {
                throw new InvalidOperationException($"未找到全局组件：{componentType.FullName}。请先调用 GetOrAdd 或 Pin。");
            }

            T component = entry.Component as T;
            EnsureComponentUsable(componentType, component);
            entry.AddReference(owner);
            return component;
        }

        /// <summary>
        /// 获取或创建无参组件，并为 owner 增加一份持有引用。
        /// 首次创建时仅调用一次 Awake，后续调用只增加引用计数。
        /// </summary>
        /// <typeparam name="T">要获取或创建的组件类型。</typeparam>
        /// <param name="owner">本次持有组件的对象，通常传入 this。</param>
        /// <returns>已激活的组件实例。</returns>
        public T GetOrAdd<T>(object owner) where T : AComponent, new()
        {
            EnsureAvailableOnMainThread();
            ValidateOwner(owner);
            return GetOrCreate<T>(owner, null);
        }

        /// <summary>
        /// 获取或使用强类型参数创建组件，并为 owner 增加一份持有引用。
        /// 参数只在首次创建时生效，已有组件不会重复 Awake。
        /// </summary>
        /// <typeparam name="T">要获取或创建的组件类型。</typeparam>
        /// <param name="owner">本次持有组件的对象，通常传入 this。</param>
        /// <param name="args">首次创建组件所需的初始化参数。</param>
        /// <returns>已激活的组件实例。</returns>
        public T GetOrAdd<T>(object owner, ComponentInitArgs args) where T : AComponent, new()
        {
            EnsureAvailableOnMainThread();
            ValidateOwner(owner);
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return GetOrCreate<T>(owner, args);
        }

        /// <summary>
        /// 释放 owner 对指定组件持有的一份引用。
        /// owner 未持有该组件时，开发环境会抛出异常以阻止错误释放其他系统的资源。
        /// </summary>
        /// <typeparam name="T">要释放的组件类型。</typeparam>
        /// <param name="owner">此前获取组件的持有者。</param>
        public void Remove<T>(object owner) where T : AComponent, new()
        {
            EnsureMainThread();
            ValidateOwner(owner);

            Type componentType = typeof(T);
            if (!componentEntries.TryGetValue(componentType, out ComponentEntry entry))
            {
                ReportInvalidRelease(owner, componentType, "组件不存在或已经被释放。");
                return;
            }

            if (!entry.RemoveReference(owner))
            {
                ReportInvalidRelease(owner, componentType, "owner 未持有该组件。");
                return;
            }

            DisposeEntryWhenUnreferenced(componentType, entry);
        }

        /// <summary>
        /// 释放 owner 持有的全部全局组件引用。
        /// 应在 owner 的 Dispose、OnDestroy 或场景退出时调用，用于兜底未逐项 Remove 的引用。
        /// </summary>
        /// <param name="owner">要清理引用的持有者。</param>
        public void ReleaseAll(object owner)
        {
            EnsureMainThread();
            ValidateOwner(owner);

            releaseTypeSnapshot.Clear();
            foreach (KeyValuePair<Type, ComponentEntry> pair in componentEntries)
            {
                if (pair.Value.HasReference(owner))
                {
                    releaseTypeSnapshot.Add(pair.Key);
                }
            }

            for (int i = 0; i < releaseTypeSnapshot.Count; i++)
            {
                Type componentType = releaseTypeSnapshot[i];
                if (!componentEntries.TryGetValue(componentType, out ComponentEntry entry))
                {
                    continue;
                }

                entry.RemoveAllReferences(owner);
                DisposeEntryWhenUnreferenced(componentType, entry);
            }

            releaseTypeSnapshot.Clear();
        }

        /// <summary>
        /// 以 Global 根 owner 获取或创建常驻组件。
        /// 同一组件类型重复 Pin 是幂等操作，不会重复增加根引用。
        /// </summary>
        /// <typeparam name="T">要常驻的组件类型。</typeparam>
        /// <returns>已激活的常驻组件实例。</returns>
        public T Pin<T>() where T : AComponent, new()
        {
            EnsureAvailableOnMainThread();
            return PinInternal<T>(null);
        }

        /// <summary>
        /// 以 Global 根 owner 获取或使用初始化参数创建常驻组件。
        /// 参数仅在首次创建时生效，重复 Pin 不会重复 Awake 或增加根引用。
        /// </summary>
        /// <typeparam name="T">要常驻的组件类型。</typeparam>
        /// <param name="args">首次创建组件所需的初始化参数。</param>
        /// <returns>已激活的常驻组件实例。</returns>
        public T Pin<T>(ComponentInitArgs args) where T : AComponent, new()
        {
            EnsureAvailableOnMainThread();
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return PinInternal<T>(args);
        }

        /// <summary>
        /// 释放 Global 根 owner 对指定常驻组件的引用。
        /// 重复 Unpin 是安全操作；若仍有普通 owner 持有，组件会继续存活。
        /// </summary>
        /// <typeparam name="T">要取消常驻的组件类型。</typeparam>
        public void Unpin<T>() where T : AComponent, new()
        {
            EnsureMainThread();

            Type componentType = typeof(T);
            if (!componentEntries.TryGetValue(componentType, out ComponentEntry entry) || !entry.RemoveReference(rootOwner))
            {
                Debug.LogWarning($"重复 Unpin 或组件不存在：{componentType.FullName}");
                return;
            }

            DisposeEntryWhenUnreferenced(componentType, entry);
        }

        /// <summary>
        /// 立即清除指定组件的所有 owner 引用并销毁组件。
        /// 仅允许退出、切服等最高层流程使用，不能替代常规 Remove。
        /// </summary>
        /// <typeparam name="T">要强制移除的组件类型。</typeparam>
        public void ForceRemove<T>() where T : AComponent, new()
        {
            EnsureMainThread();

            Type componentType = typeof(T);
            if (!componentEntries.TryGetValue(componentType, out ComponentEntry entry))
            {
                return;
            }

            componentEntries.Remove(componentType);
            DisposeEntry(entry);
        }

        /// <summary>
        /// 强制释放当前所有存活组件。
        /// Global 退出或销毁时不等待引用归零，确保组件资源不会遗漏。
        /// </summary>
        public virtual void Dispose()
        {
            EnsureMainThread();
            if (disposed)
            {
                return;
            }

            disposed = true;
            disposeSnapshot.Clear();
            foreach (ComponentEntry entry in componentEntries.Values)
            {
                disposeSnapshot.Add(entry.Component);
            }

            componentEntries.Clear();
            for (int i = 0; i < disposeSnapshot.Count; i++)
            {
                DisposeComponent(disposeSnapshot[i]);
            }

            disposeSnapshot.Clear();
            releaseTypeSnapshot.Clear();
            updateSnapshot.Clear();
            if (!isQuitting)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
            }
        }

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 初始化全局容器。
        /// 子类可重写以注册框架级基础设施。
        /// </summary>
        protected virtual void Init()
        {
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 在场景初始化前记录 Unity 主线程标识。
        /// 该标识用于阻止后台线程通过 Com 首次创建容器。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CaptureUnityMainThread()
        {
            unityMainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        /// 初始化 Unity 生命周期中的 Global 实例和主线程标识。
        /// </summary>
        private void Awake()
        {
            if (com != null && !ReferenceEquals(com, this))
            {
                return;
            }

            com = this;
            InitializeIfNeeded();
        }

        /// <summary>
        /// 标记应用退出并强制释放全部组件。
        /// </summary>
        private void OnApplicationQuit()
        {
            isQuitting = true;
            Shutdown();
        }

        /// <summary>
        /// 在 Unity 销毁容器时强制释放全部组件。
        /// </summary>
        private void OnDestroy()
        {
            Shutdown();
            if (ReferenceEquals(com, this))
            {
                com = null;
            }
        }

        /// <summary>
        /// 调度所有仍处于激活状态的全局组件。
        /// 使用独立快照以允许更新过程安全增删组件。
        /// </summary>
        private void Update()
        {
            if (disposed || componentEntries.Count == 0)
            {
                return;
            }

            updateSnapshot.Clear();
            foreach (ComponentEntry entry in componentEntries.Values)
            {
                updateSnapshot.Add(entry.Component);
            }

            for (int i = 0; i < updateSnapshot.Count; i++)
            {
                updateSnapshot[i]?.MonoUpdate();
            }

            updateSnapshot.Clear();
        }

        /// <summary>
        /// 初始化主线程标识并执行一次容器初始化。
        /// </summary>
        private void InitializeIfNeeded()
        {
            if (mainThreadId == 0)
            {
                if (unityMainThreadId == 0)
                {
                    unityMainThreadId = Thread.CurrentThread.ManagedThreadId;
                }

                mainThreadId = Thread.CurrentThread.ManagedThreadId;
                Init();
            }
        }

        /// <summary>
        /// 获取已有组件或创建组件后记录 owner 引用。
        /// </summary>
        /// <typeparam name="T">要获取或创建的组件类型。</typeparam>
        /// <param name="owner">本次持有组件的对象。</param>
        /// <param name="args">首次创建时使用的初始化参数；无参创建时为 null。</param>
        /// <returns>已激活的组件实例。</returns>
        private T GetOrCreate<T>(object owner, ComponentInitArgs args) where T : AComponent, new()
        {
            Type componentType = typeof(T);
            if (componentEntries.TryGetValue(componentType, out ComponentEntry entry))
            {
                T existingComponent = entry.Component as T;
                EnsureComponentUsable(componentType, existingComponent);
                entry.AddReference(owner);
                return existingComponent;
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
                throw new InvalidOperationException($"组件 {componentType.FullName} 在 Awake 期间已被释放，无法注册到 Global。");
            }

            component.IsActive = true;
            ComponentEntry newEntry = new ComponentEntry(component);
            newEntry.AddReference(owner);
            componentEntries.Add(componentType, newEntry);
            return component;
        }

        /// <summary>
        /// 获取或创建常驻组件并确保根 owner 仅持有一份引用。
        /// </summary>
        /// <typeparam name="T">要常驻的组件类型。</typeparam>
        /// <param name="args">首次创建时使用的初始化参数。</param>
        /// <returns>已激活的常驻组件实例。</returns>
        private T PinInternal<T>(ComponentInitArgs args) where T : AComponent, new()
        {
            Type componentType = typeof(T);
            if (componentEntries.TryGetValue(componentType, out ComponentEntry entry))
            {
                T existingComponent = entry.Component as T;
                EnsureComponentUsable(componentType, existingComponent);
                if (!entry.HasReference(rootOwner))
                {
                    entry.AddReference(rootOwner);
                }

                return existingComponent;
            }

            return GetOrCreate<T>(rootOwner, args);
        }

        /// <summary>
        /// 在组件总引用归零时从容器移除并释放组件。
        /// 先摘除条目再 Dispose，允许 Dispose 重入获取同类型组件并安全创建新实例。
        /// </summary>
        /// <param name="componentType">要检查的组件类型。</param>
        /// <param name="entry">对应的组件生命周期条目。</param>
        private void DisposeEntryWhenUnreferenced(Type componentType, ComponentEntry entry)
        {
            if (entry.ReferenceCount > 0)
            {
                return;
            }

            if (componentEntries.TryGetValue(componentType, out ComponentEntry currentEntry) && ReferenceEquals(currentEntry, entry))
            {
                componentEntries.Remove(componentType);
            }

            DisposeEntry(entry);
        }

        /// <summary>
        /// 释放组件条目中的实例。
        /// </summary>
        /// <param name="entry">已从容器摘除的组件条目。</param>
        private void DisposeEntry(ComponentEntry entry)
        {
            entry.ClearReferences();
            DisposeComponent(entry.Component);
        }

        /// <summary>
        /// 停用并释放组件实例。
        /// </summary>
        /// <param name="component">要释放的组件实例。</param>
        private void DisposeComponent(AComponent component)
        {
            if (component == null || component.IsDisposed)
            {
                return;
            }

            component.IsActive = false;
            component.Dispose();
        }

        /// <summary>
        /// 验证 owner 参数有效。
        /// </summary>
        /// <param name="owner">要验证的 owner 对象。</param>
        private void ValidateOwner(object owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner), "Global 组件引用必须提供 owner，通常传入 this。");
            }

            if (owner is AComponent componentOwner && componentOwner.IsDisposed)
            {
                throw new ObjectDisposedException(owner.GetType().FullName, "已释放的 AComponent 不能继续持有 Global 组件。");
            }

            if (owner is UnityEngine.Object unityOwner && unityOwner == null)
            {
                throw new ObjectDisposedException(owner.GetType().FullName, "已销毁的 Unity 对象不能继续持有 Global 组件。");
            }
        }

        /// <summary>
        /// 验证组件实例仍可被业务层使用。
        /// </summary>
        /// <param name="componentType">组件类型。</param>
        /// <param name="component">要验证的组件实例。</param>
        private void EnsureComponentUsable(Type componentType, AComponent component)
        {
            if (component == null || component.IsDisposed || !component.IsActive)
            {
                throw new InvalidOperationException($"全局组件 {componentType.FullName} 已失效。请重新调用 GetOrAdd 或确认生命周期顺序。");
            }
        }

        /// <summary>
        /// 确保 Global 可接受新的获取或创建请求。
        /// </summary>
        private void EnsureAvailableOnMainThread()
        {
            EnsureMainThread();
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(Global), "Global 正在关闭，不能再创建或获取组件。");
            }
        }

        /// <summary>
        /// 确保组件管理操作发生在 Unity 主线程。
        /// </summary>
        private void EnsureMainThread()
        {
            if (mainThreadId == 0 || Thread.CurrentThread.ManagedThreadId != mainThreadId)
            {
                throw new InvalidOperationException("Global 组件的创建、获取、释放和销毁必须在 Unity 主线程执行。");
            }
        }

        /// <summary>
        /// 在尚未存在 Global 实例时验证当前线程仍是 Unity 主线程。
        /// </summary>
        private static void EnsureStaticMainThread()
        {
            if (unityMainThreadId != 0 && Thread.CurrentThread.ManagedThreadId != unityMainThreadId)
            {
                throw new InvalidOperationException("Global 组件容器只能在 Unity 主线程创建或访问。");
            }
        }

        /// <summary>
        /// 报告 owner 未持有组件时的错误释放。
        /// </summary>
        /// <param name="owner">尝试释放组件的 owner。</param>
        /// <param name="componentType">被释放的组件类型。</param>
        /// <param name="reason">错误原因。</param>
        private void ReportInvalidRelease(object owner, Type componentType, string reason)
        {
            string message = $"Global.Remove<{componentType.Name}> 失败：{reason} owner:{owner.GetType().FullName}";
            if (Application.isEditor || Debug.isDebugBuild)
            {
                throw new InvalidOperationException(message);
            }

            Debug.LogWarning(message);
        }

        /// <summary>
        /// 防止 OnApplicationQuit、OnDestroy 和手动 Dispose 重复释放。
        /// </summary>
        private void Shutdown()
        {
            if (disposed)
            {
                return;
            }

            Dispose();
        }

        /// <summary>
        /// 保存组件实例及其所有 owner 的精确引用计数。
        /// </summary>
        private sealed class ComponentEntry
        {
            #region Private 私有成员

            private readonly Dictionary<object, int> ownerReferences = new Dictionary<object, int>(ownerReferenceComparer); // owner 到持有次数的身份映射。

            #endregion

            #region Public 公共成员

            /// <summary>
            /// 获取条目管理的组件实例。
            /// </summary>
            public AComponent Component { get; }

            /// <summary>
            /// 获取当前所有 owner 持有的引用总数。
            /// </summary>
            public int ReferenceCount { get; private set; }

            /// <summary>
            /// 使用组件实例创建生命周期条目。
            /// </summary>
            /// <param name="component">要管理的组件实例。</param>
            public ComponentEntry(AComponent component)
            {
                Component = component;
            }

            /// <summary>
            /// 为 owner 增加一份组件引用。
            /// </summary>
            /// <param name="owner">新增引用的 owner。</param>
            public void AddReference(object owner)
            {
                ownerReferences.TryGetValue(owner, out int count);
                ownerReferences[owner] = count + 1;
                ReferenceCount++;
            }

            /// <summary>
            /// 释放 owner 持有的一份组件引用。
            /// </summary>
            /// <param name="owner">要释放引用的 owner。</param>
            /// <returns>owner 原本持有引用时返回 true；否则返回 false。</returns>
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
            /// 移除 owner 持有的全部组件引用。
            /// </summary>
            /// <param name="owner">要移除全部引用的 owner。</param>
            /// <returns>实际移除的引用数量。</returns>
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
            /// 判断 owner 是否持有该组件。
            /// </summary>
            /// <param name="owner">要检查的 owner。</param>
            /// <returns>owner 持有至少一份引用时返回 true。</returns>
            public bool HasReference(object owner)
            {
                return ownerReferences.ContainsKey(owner);
            }

            /// <summary>
            /// 清空全部 owner 引用记录。
            /// </summary>
            public void ClearReferences()
            {
                ownerReferences.Clear();
                ReferenceCount = 0;
            }

            #endregion
        }

        /// <summary>
        /// 使用对象身份而非 Equals 语义比较 owner。
        /// </summary>
        private sealed class OwnerReferenceComparer : IEqualityComparer<object>
        {
            #region Public 公共成员

            /// <summary>
            /// 判断两个 owner 是否是同一个对象实例。
            /// </summary>
            /// <param name="left">第一个 owner。</param>
            /// <param name="right">第二个 owner。</param>
            /// <returns>两个引用指向同一对象时返回 true。</returns>
            public new bool Equals(object left, object right)
            {
                return ReferenceEquals(left, right);
            }

            /// <summary>
            /// 获取 owner 对象的身份哈希值。
            /// </summary>
            /// <param name="owner">要计算哈希值的 owner。</param>
            /// <returns>owner 的运行时身份哈希值。</returns>
            public int GetHashCode(object owner)
            {
                return RuntimeHelpers.GetHashCode(owner);
            }

            #endregion
        }

        #endregion
    }
}
