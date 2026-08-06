using System;
using System.Collections.Generic;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Threading;
using MiniCore.UI;
using UnityEngine;

namespace MiniCore.Service
{
    /// <summary>
    /// 基于生成注册表、WindowSession 和资源租约的项目 UI 应用服务。
    /// </summary>
    [AppService(
        "UI 框架",
        typeof(MiniCore.UI.IUIService),
        Description = "加载项目 UI Profile 和持久 Root，并提供强类型窗口生命周期、导航、缓存及资源租约。",
        RequiresServices = new[] { typeof(IResourceService) },
        InitArgsType = typeof(UIServiceInitArgs),
        RunInBatchMode = false)]
    public sealed class UIService : AAppService, MiniCore.UI.IUIService, IAsyncAppService, IUIWindowSessionHost
    {
        #region Private 私有成员

        private readonly Dictionary<UIWindowInstanceId, UIWindowSession> sessions = new Dictionary<UIWindowInstanceId, UIWindowSession>(); // 当前加载或活动会话。
        private readonly Dictionary<UIWindowLogicalKey, UIWindowSession> logicalSessions = new Dictionary<UIWindowLogicalKey, UIWindowSession>(); // Singleton、PerKey、Queue 和 Replace 查找表。
        private readonly Dictionary<UIWindowId, Stack<AUIWindowView>> viewCaches = new Dictionary<UIWindowId, Stack<AUIWindowView>>(); // 按窗口定义缓存 View。
        private readonly Dictionary<string, UIResourceLeaseRecord> resourceLeases = new Dictionary<string, UIResourceLeaseRecord>(StringComparer.Ordinal); // UI 地址引用计数。
        private readonly Dictionary<UIWindowId, uint> generations = new Dictionary<UIWindowId, uint>(); // 各窗口最新代次。
        private readonly Dictionary<string, UIWindowHandle> navigationGroups = new Dictionary<string, UIWindowHandle>(StringComparer.Ordinal); // 各导航组当前 Screen。
        private readonly List<UIWindowSession> disposeSnapshot = new List<UIWindowSession>(); // 服务退出时无分配遍历快照。
        private IResourceService resourceService; // YooAsset 资源服务。
        private UIServiceInitArgs initArgs; // 启动配置参数。
        private UIProjectProfile profile; // 项目 UI Profile。
        private ApplicationUIRoot root; // 持久化 UI Root。
        private GameObject rootInstance; // 资源服务创建的 Root 实例。
        private long multipleSequence; // Multiple 和 Queue 使用的唯一实例键。
        private string profileAddress; // 当前持有的 Profile 地址。
        private string rootAddress; // 当前持有的 Root 地址。
        private bool initialized; // 异步初始化是否成功。
        private bool disposing; // 服务是否正在退出。

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 读取启动参数并获取资源服务。
        /// </summary>
        /// <param name="args">UIServiceInitArgs 启动参数。</param>
        public override void Awake(ComponentInitArgs args)
        {
            initArgs = args as UIServiceInitArgs ?? throw new ArgumentException("UIService 初始化参数类型不正确。", nameof(args));
            if (string.IsNullOrWhiteSpace(initArgs.ProfileAddress))
            {
                throw new ArgumentException("UIServiceInitArgs.ProfileAddress 不能为空。", nameof(args));
            }

            resourceService = Global.GetService<IResourceService>(this);
        }

        /// <summary>
        /// 同步终止会话、销毁 Root、清空缓存并释放全部 UI 资源租约。
        /// </summary>
        protected override void OnDispose()
        {
            disposing = true;
            disposeSnapshot.Clear();
            foreach (UIWindowSession session in sessions.Values)
            {
                disposeSnapshot.Add(session);
            }

            for (int i = 0; i < disposeSnapshot.Count; i++)
            {
                disposeSnapshot[i].Abort();
            }

            disposeSnapshot.Clear();
            DestroyAllCachedViews();
            if (rootInstance != null)
            {
                resourceService?.ReleaseInstance(rootInstance);
                rootInstance = null;
                root = null;
            }

            if (!string.IsNullOrEmpty(rootAddress))
            {
                resourceService?.ReleaseAsset(rootAddress);
            }

            if (!string.IsNullOrEmpty(profileAddress))
            {
                resourceService?.ReleaseAsset(profileAddress);
            }

            sessions.Clear();
            logicalSessions.Clear();
            resourceLeases.Clear();
            generations.Clear();
            navigationGroups.Clear();
            UIWindowRegistry.Reset();
            Global.ReleaseAll(this);
            resourceService = null;
            profile = null;
            initialized = false;
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 按稳定路由名称打开数据驱动窗口。
        /// </summary>
        /// <param name="routeName">窗口 RouteName。</param>
        /// <returns>活动窗口句柄。</returns>
        public MTask<UIWindowHandle> OpenAsync(string routeName)
        {
            return OpenCoreAsync(UIWindowRegistry.Get(routeName), null, null);
        }

        /// <summary>
        /// 加载 Profile、持有 Root 资源并实例化持久化 ApplicationUIRoot。
        /// </summary>
        /// <returns>UI 运行时初始化完成任务。</returns>
        public async MTask InitializeAsync()
        {
            if (initialized)
            {
                return;
            }

            profileAddress = initArgs.ProfileAddress;
            profile = await resourceService.PreloadAssetAsync<UIProjectProfile>(profileAddress);
            if (profile == null)
            {
                throw new InvalidOperationException($"未能加载 UIProjectProfile：{profileAddress}。");
            }

            if (!profile.Validate(out string error))
            {
                throw new InvalidOperationException(error);
            }

            rootAddress = profile.ApplicationRootAddress;
            await resourceService.PreloadAssetAsync<GameObject>(rootAddress);
            rootInstance = await resourceService.InstantiateAsync(rootAddress);
            root = rootInstance != null ? rootInstance.GetComponent<ApplicationUIRoot>() : null;
            if (root == null)
            {
                if (rootInstance != null)
                {
                    resourceService.ReleaseInstance(rootInstance);
                    rootInstance = null;
                }

                resourceService.ReleaseAsset(rootAddress);
                resourceService.ReleaseAsset(profileAddress);
                throw new InvalidOperationException($"UI Root 资源 {rootAddress} 缺少 ApplicationUIRoot 组件。");
            }

            UIWindowRegistry.Initialize();
            root.Initialize(profile);
            initialized = true;
        }

        /// <summary>
        /// 打开不带业务参数的强类型窗口。
        /// </summary>
        /// <typeparam name="TRoute">生成的窗口路由。</typeparam>
        /// <returns>活动窗口句柄。</returns>
        public MTask<UIWindowHandle> OpenAsync<TRoute>() where TRoute : IUIWindowRoute
        {
            return OpenCoreAsync(UIWindowRegistry.Get<TRoute>(), null, null);
        }

        /// <summary>
        /// 使用只属于目标路由的参数打开窗口。
        /// </summary>
        /// <typeparam name="TRoute">生成的窗口路由。</typeparam>
        /// <param name="args">强类型打开参数。</param>
        /// <returns>活动窗口句柄。</returns>
        public MTask<UIWindowHandle> OpenAsync<TRoute>(IUIWindowArgs<TRoute> args) where TRoute : IUIWindowRoute
        {
            return OpenCoreAsync(UIWindowRegistry.Get<TRoute>(), args, null);
        }

        /// <summary>
        /// 并行准备目标 Screen，成功激活后再关闭旧 Screen。
        /// </summary>
        /// <typeparam name="TRoute">生成的 Screen 路由。</typeparam>
        /// <returns>导航完成任务。</returns>
        public async MTask NavigateAsync<TRoute>() where TRoute : IUIWindowRoute
        {
            EnsureInitialized();
            UIWindowDefinition definition = UIWindowRegistry.Get<TRoute>();
            string group = definition.NavigationGroup;
            navigationGroups.TryGetValue(group, out UIWindowHandle previous);
            UIWindowHandle next = await OpenCoreAsync(definition, null, null);
            navigationGroups[group] = next;
            if (previous != null && previous.IsValid && previous != next)
            {
                await CloseAsync(previous);
            }
        }

        /// <summary>
        /// 按稳定路由名称导航到 Screen 窗口。
        /// </summary>
        /// <param name="routeName">窗口 RouteName。</param>
        /// <returns>导航完成任务。</returns>
        public async MTask NavigateAsync(string routeName)
        {
            EnsureInitialized();
            UIWindowDefinition definition = UIWindowRegistry.Get(routeName);
            string group = definition.NavigationGroup;
            navigationGroups.TryGetValue(group, out UIWindowHandle previous);
            UIWindowHandle next = await OpenCoreAsync(definition, null, null);
            navigationGroups[group] = next;
            if (previous != null && previous.IsValid && previous != next)
            {
                await CloseAsync(previous);
            }
        }

        /// <summary>
        /// 关闭指定导航组当前的 Screen，并只清理由该句柄占用的导航状态。
        /// </summary>
        /// <param name="navigationGroup">目标导航组名称。</param>
        /// <returns>当前 Screen 关闭完成任务；导航组没有活动窗口时立即完成。</returns>
        public async MTask CloseNavigationAsync(string navigationGroup)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(navigationGroup))
            {
                throw new ArgumentException("导航组名称不能为空。", nameof(navigationGroup));
            }

            string group = navigationGroup.Trim();
            if (!navigationGroups.TryGetValue(group, out UIWindowHandle handle))
            {
                return;
            }

            if (handle != null && handle.IsValid)
            {
                await CloseAsync(handle);
            }

            if (navigationGroups.TryGetValue(group, out UIWindowHandle current) && current == handle)
            {
                navigationGroups.Remove(group);
            }
        }

        /// <summary>
        /// 预加载目标窗口资源和指定数量的缓存 View。
        /// </summary>
        /// <typeparam name="TRoute">生成的窗口路由。</typeparam>
        /// <param name="count">希望缓存的 View 数量。</param>
        /// <returns>预加载完成任务。</returns>
        public async MTask PrefetchAsync<TRoute>(int count = 1) where TRoute : IUIWindowRoute
        {
            EnsureInitialized();
            if (count <= 0)
            {
                return;
            }

            UIWindowDefinition definition = UIWindowRegistry.Get<TRoute>();
            Stack<AUIWindowView> cache = GetOrCreateCache(definition.Id);
            int limit = ResolveCacheLimit(definition);
            int target = Mathf.Min(count, limit);
            while (cache.Count < target)
            {
                AUIWindowView view = await CreateViewAsync(definition);
                CacheView(cache, view);
            }
        }

        /// <summary>
        /// 关闭句柄精确指向的当前代窗口。
        /// </summary>
        /// <param name="handle">窗口句柄。</param>
        /// <returns>关闭完成任务。</returns>
        public MTask CloseAsync(UIWindowHandle handle)
        {
            EnsureInitialized();
            if (handle == null || !handle.IsValid)
            {
                return MTask.CompletedTask;
            }

            return sessions.TryGetValue(handle.InstanceId, out UIWindowSession session) ? session.CloseAsync() : MTask.CompletedTask;
        }

        /// <summary>
        /// 聚焦句柄精确指向的当前代窗口。
        /// </summary>
        /// <param name="handle">窗口句柄。</param>
        /// <returns>句柄有效且窗口可聚焦时返回 true。</returns>
        public bool Focus(UIWindowHandle handle)
        {
            EnsureInitialized();
            if (handle == null || !handle.IsValid)
            {
                return false;
            }

            return sessions.TryGetValue(handle.InstanceId, out UIWindowSession session) && session.Focus();
        }

        /// <summary>
        /// 打开窗口并等待 Presenter 提交强类型业务结果。
        /// </summary>
        /// <typeparam name="TRoute">生成的窗口路由。</typeparam>
        /// <typeparam name="TArgs">路由专用参数类型。</typeparam>
        /// <typeparam name="TResult">业务结果类型。</typeparam>
        /// <param name="args">窗口打开参数。</param>
        /// <returns>窗口提交的业务结果。</returns>
        public async MTask<TResult> ShowAsync<TRoute, TArgs, TResult>(TArgs args)
            where TRoute : IUIWindowRoute
            where TArgs : IUIWindowArgs<TRoute>
        {
            UIWindowResultChannel<TResult> channel = new UIWindowResultChannel<TResult>();
            await OpenCoreAsync(UIWindowRegistry.Get<TRoute>(), args, channel);
            return await channel.Task;
        }

        /// <summary>
        /// 获取缓存 View 或加载一个新实例。
        /// </summary>
        /// <param name="definition">窗口定义。</param>
        /// <returns>准备进入 Staging 的 View。</returns>
        public MTask<AUIWindowView> AcquireViewAsync(UIWindowDefinition definition)
        {
            if (viewCaches.TryGetValue(definition.Id, out Stack<AUIWindowView> cache) && cache.Count > 0)
            {
                AUIWindowView cached = cache.Pop();
                return MTask.FromResult(cached);
            }

            return CreateViewAsync(definition);
        }

        /// <summary>
        /// 按窗口缓存策略归还或销毁 View。
        /// </summary>
        /// <param name="definition">窗口定义。</param>
        /// <param name="view">待回收 View。</param>
        public void ReleaseView(UIWindowDefinition definition, AUIWindowView view)
        {
            if (view == null)
            {
                return;
            }

            int limit = ResolveCacheLimit(definition);
            Stack<AUIWindowView> cache = GetOrCreateCache(definition.Id);
            bool shouldCache = definition.CachePolicy != UICachePolicy.DestroyOnClose && cache.Count < limit && !disposing;
            if (shouldCache)
            {
                CacheView(cache, view);
                return;
            }

            DestroyView(definition, view);
        }

        /// <summary>
        /// 从活动映射移除已完成会话，并清理对应导航句柄。
        /// </summary>
        /// <param name="session">进入唯一终态的会话。</param>
        void IUIWindowSessionHost.CompleteSession(UIWindowSession session)
        {
            sessions.Remove(session.Handle.InstanceId);
            UIWindowLogicalKey key = new UIWindowLogicalKey(session.Definition.Id, session.Handle.InstanceId.InstanceKey);
            if (logicalSessions.TryGetValue(key, out UIWindowSession current) && ReferenceEquals(current, session))
            {
                logicalSessions.Remove(key);
            }

            if (!string.IsNullOrEmpty(session.Definition.NavigationGroup) &&
                navigationGroups.TryGetValue(session.Definition.NavigationGroup, out UIWindowHandle navigation) &&
                navigation == session.Handle)
            {
                navigationGroups.Remove(session.Definition.NavigationGroup);
            }
        }

        ApplicationUIRoot IUIWindowSessionHost.Root => root;
        UIProjectProfile IUIWindowSessionHost.Profile => profile;
        MiniCore.UI.IUIService IUIWindowSessionHost.Service => this;

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 应用实例和重复策略后创建或复用窗口会话。
        /// </summary>
        /// <param name="definition">生成窗口定义。</param>
        /// <param name="arguments">可选打开参数。</param>
        /// <param name="resultChannel">可选 ShowAsync 结果通道。</param>
        /// <returns>活动窗口句柄。</returns>
        private async MTask<UIWindowHandle> OpenCoreAsync(UIWindowDefinition definition, object arguments, IUIWindowResultChannel resultChannel)
        {
            EnsureInitialized();
            root.LoadingOverlay?.Begin();
            try
            {
                UIWindowInstanceKey instanceKey = ResolveInstanceKey(definition, arguments);
                UIWindowLogicalKey logicalKey = new UIWindowLogicalKey(definition.Id, instanceKey);
                if (logicalSessions.TryGetValue(logicalKey, out UIWindowSession existing))
                {
                    if (resultChannel != null)
                    {
                        throw new InvalidOperationException($"ShowAsync 窗口 {definition.RouteName} 已存在，不能将新的结果通道附加到旧会话。");
                    }

                    if (definition.InstancePolicy == UIInstancePolicy.Replace)
                    {
                        await existing.CloseAsync();
                    }
                    else if (definition.InstancePolicy == UIInstancePolicy.Queue)
                    {
                        await existing.WaitUntilClosedAsync();
                    }
                    else
                    {
                        return await HandleDuplicateAsync(existing, definition, arguments);
                    }
                }

                uint generation = NextGeneration(definition.Id);
                UIWindowHandle handle = new UIWindowHandle(new UIWindowInstanceId(definition.Id, instanceKey, generation));
                UIWindowSession session = new UIWindowSession(this, definition, handle, arguments, resultChannel);
                sessions.Add(handle.InstanceId, session);
                if (definition.InstancePolicy != UIInstancePolicy.Multiple)
                {
                    logicalSessions[logicalKey] = session;
                }

                session.Start();
                return await session.WaitUntilActiveAsync();
            }
            finally
            {
                root.LoadingOverlay?.End();
            }
        }

        /// <summary>
        /// 执行窗口定义配置的重复打开策略。
        /// </summary>
        /// <param name="existing">已经存在的会话。</param>
        /// <param name="definition">窗口定义。</param>
        /// <param name="arguments">本次新参数。</param>
        /// <returns>复用会话的句柄。</returns>
        private async MTask<UIWindowHandle> HandleDuplicateAsync(UIWindowSession existing, UIWindowDefinition definition, object arguments)
        {
            if (definition.DuplicateOpenPolicy == UIDuplicateOpenPolicy.Reject)
            {
                throw new InvalidOperationException($"窗口 {definition.RouteName} 已存在，当前策略拒绝重复打开。");
            }

            UIWindowHandle handle = await existing.WaitUntilActiveAsync();
            if (definition.DuplicateOpenPolicy == UIDuplicateOpenPolicy.Refresh)
            {
                await existing.RefreshAsync(arguments);
            }

            if (definition.DuplicateOpenPolicy == UIDuplicateOpenPolicy.Focus || definition.DuplicateOpenPolicy == UIDuplicateOpenPolicy.Refresh)
            {
                existing.Focus();
            }

            return handle;
        }

        /// <summary>
        /// 按实例策略计算逻辑键；SingletonPerKey 强制要求参数提供键。
        /// </summary>
        /// <param name="definition">窗口定义。</param>
        /// <param name="arguments">打开参数。</param>
        /// <returns>本次会话实例键。</returns>
        private UIWindowInstanceKey ResolveInstanceKey(UIWindowDefinition definition, object arguments)
        {
            if (definition.InstancePolicy == UIInstancePolicy.SingletonPerKey)
            {
                if (!(arguments is IUIWindowKeyProvider provider) || provider.InstanceKey.IsEmpty)
                {
                    throw new InvalidOperationException($"SingletonPerKey 窗口 {definition.RouteName} 的参数必须实现 IUIWindowKeyProvider 并提供非空键。");
                }

                return provider.InstanceKey;
            }

            if (definition.InstancePolicy == UIInstancePolicy.Multiple)
            {
                multipleSequence++;
                return new UIWindowInstanceKey(multipleSequence);
            }

            return UIWindowInstanceKey.Empty;
        }

        /// <summary>
        /// 生成窗口下一代实例编号。
        /// </summary>
        /// <param name="id">窗口稳定身份。</param>
        /// <returns>非零代次。</returns>
        private uint NextGeneration(UIWindowId id)
        {
            generations.TryGetValue(id, out uint generation);
            generation++;
            if (generation == 0U)
            {
                generation = 1U;
            }

            generations[id] = generation;
            return generation;
        }

        /// <summary>
        /// 持有地址租约、实例化 Prefab 并验证生成注册表与 Prefab 一致。
        /// </summary>
        /// <param name="definition">窗口定义。</param>
        /// <returns>有效窗口 View。</returns>
        private async MTask<AUIWindowView> CreateViewAsync(UIWindowDefinition definition)
        {
            await AcquireResourceLeaseAsync(definition.Address);
            GameObject instance = null;
            try
            {
                RectTransform parent = root.GetWindowParent(definition.RenderSpace, definition.Layer);
                instance = await resourceService.InstantiateAsync(definition.Address, parent);
                AUIWindowView view = definition.ResolveView(instance);
                if (view == null)
                {
                    throw new InvalidOperationException($"窗口资源 {definition.Address} 缺少生成注册表指定的 View。");
                }

                if (view.GetWindowId() != definition.Id)
                {
                    throw new InvalidOperationException($"窗口 {definition.RouteName} 的 Prefab WindowId 与生成注册表不一致，请重新生成 UI Registry。");
                }

                instance.SetActive(false);
                return view;
            }
            catch
            {
                if (instance != null)
                {
                    resourceService.ReleaseInstance(instance);
                }

                ReleaseResourceLease(definition.Address);
                throw;
            }
        }

        /// <summary>
        /// 增加 UI 地址引用，并让并发消费者共享唯一预加载任务。
        /// </summary>
        /// <param name="address">YooAsset 地址。</param>
        /// <returns>资源可实例化时完成的任务。</returns>
        private async MTask AcquireResourceLeaseAsync(string address)
        {
            if (!resourceLeases.TryGetValue(address, out UIResourceLeaseRecord record))
            {
                record = new UIResourceLeaseRecord(resourceService.PreloadAssetAsync<GameObject>(address).Share());
                resourceLeases.Add(address, record);
            }

            record.Count++;
            try
            {
                await record.LoadTask;
                record.Loaded = true;
            }
            catch
            {
                record.Count--;
                if (record.Count == 0)
                {
                    resourceLeases.Remove(address);
                }

                throw;
            }
        }

        /// <summary>
        /// 减少 UI 地址引用，并仅在活动、缓存和预加载租约都归零时释放资源。
        /// </summary>
        /// <param name="address">YooAsset 地址。</param>
        private void ReleaseResourceLease(string address)
        {
            if (!resourceLeases.TryGetValue(address, out UIResourceLeaseRecord record))
            {
                return;
            }

            record.Count--;
            if (record.Count > 0)
            {
                return;
            }

            resourceLeases.Remove(address);
            if (record.Loaded)
            {
                resourceService.ReleaseAsset(address);
            }
        }

        /// <summary>
        /// 将 View 在所属层原地禁用并放入缓存栈。
        /// </summary>
        /// <param name="cache">目标窗口缓存栈。</param>
        /// <param name="view">待缓存 View。</param>
        private void CacheView(Stack<AUIWindowView> cache, AUIWindowView view)
        {
            view.gameObject.SetActive(false);
            cache.Push(view);
        }

        /// <summary>
        /// 销毁 View 实例并归还一个地址租约。
        /// </summary>
        /// <param name="definition">窗口定义。</param>
        /// <param name="view">待销毁 View。</param>
        private void DestroyView(UIWindowDefinition definition, AUIWindowView view)
        {
            resourceService.ReleaseInstance(view.gameObject);
            ReleaseResourceLease(definition.Address);
        }

        /// <summary>
        /// 获取或创建某窗口的 View 缓存栈。
        /// </summary>
        /// <param name="id">窗口稳定身份。</param>
        /// <returns>对应缓存栈。</returns>
        private Stack<AUIWindowView> GetOrCreateCache(UIWindowId id)
        {
            if (!viewCaches.TryGetValue(id, out Stack<AUIWindowView> cache))
            {
                cache = new Stack<AUIWindowView>();
                viewCaches.Add(id, cache);
            }

            return cache;
        }

        /// <summary>
        /// 获取窗口最终缓存容量。
        /// </summary>
        /// <param name="definition">窗口定义。</param>
        /// <returns>非负缓存容量。</returns>
        private int ResolveCacheLimit(UIWindowDefinition definition)
        {
            if (definition.CachePolicy == UICachePolicy.DestroyOnClose)
            {
                return 0;
            }

            int configured = definition.MaxCacheCount > 0 ? definition.MaxCacheCount : profile.DefaultCacheCount;
            return definition.CachePolicy == UICachePolicy.Resident ? Mathf.Max(1, configured) : Mathf.Max(0, configured);
        }

        /// <summary>
        /// 服务退出时销毁全部缓存 View 并释放对应地址租约。
        /// </summary>
        private void DestroyAllCachedViews()
        {
            foreach (KeyValuePair<UIWindowId, Stack<AUIWindowView>> pair in viewCaches)
            {
                if (!UIWindowRegistry.TryGet(pair.Key, out UIWindowDefinition definition))
                {
                    continue;
                }

                Stack<AUIWindowView> cache = pair.Value;
                while (cache.Count > 0)
                {
                    DestroyView(definition, cache.Pop());
                }
            }

            viewCaches.Clear();
        }

        /// <summary>
        /// 阻止初始化前或释放后的业务调用。
        /// </summary>
        private void EnsureInitialized()
        {
            if (!initialized || disposing)
            {
                throw new InvalidOperationException("UIService 尚未初始化完成或已经释放。");
            }
        }

        #endregion

        #region Private 类型

        /// <summary>
        /// Singleton 查找使用的窗口定义与业务实例键组合。
        /// </summary>
        private readonly struct UIWindowLogicalKey : IEquatable<UIWindowLogicalKey>
        {
            private readonly UIWindowId id; // 窗口稳定身份。
            private readonly UIWindowInstanceKey key; // 业务实例键。

            /// <summary>
            /// 创建逻辑窗口键。
            /// </summary>
            /// <param name="windowId">窗口稳定身份。</param>
            /// <param name="instanceKey">业务实例键。</param>
            public UIWindowLogicalKey(UIWindowId windowId, UIWindowInstanceKey instanceKey)
            {
                id = windowId;
                key = instanceKey;
            }

            /// <summary>
            /// 比较两个逻辑窗口键。
            /// </summary>
            /// <param name="other">待比较键。</param>
            /// <returns>身份和业务键均相同时返回 true。</returns>
            public bool Equals(UIWindowLogicalKey other)
            {
                return id.Equals(other.id) && key.Equals(other.key);
            }

            /// <summary>
            /// 比较目标对象是否为同一逻辑窗口键。
            /// </summary>
            /// <param name="obj">待比较对象。</param>
            /// <returns>对象为同一键时返回 true。</returns>
            public override bool Equals(object obj)
            {
                return obj is UIWindowLogicalKey other && Equals(other);
            }

            /// <summary>
            /// 获取组合哈希值。
            /// </summary>
            /// <returns>逻辑窗口键哈希。</returns>
            public override int GetHashCode()
            {
                unchecked
                {
                    return (id.GetHashCode() * 397) ^ key.GetHashCode();
                }
            }
        }

        /// <summary>
        /// 一个 UI 地址的共享加载任务和引用计数。
        /// </summary>
        private sealed class UIResourceLeaseRecord
        {
            /// <summary>
            /// 当前活动、缓存或预加载 View 数量。
            /// </summary>
            public int Count;

            /// <summary>
            /// 获取可被多个实例等待的唯一加载任务。
            /// </summary>
            public MSharedTask<GameObject> LoadTask { get; }

            /// <summary>
            /// 记录资源服务是否已经持有成功句柄。
            /// </summary>
            public bool Loaded;

            /// <summary>
            /// 创建资源地址租约记录。
            /// </summary>
            /// <param name="loadTask">共享资源加载任务。</param>
            public UIResourceLeaseRecord(MSharedTask<GameObject> loadTask)
            {
                LoadTask = loadTask ?? throw new ArgumentNullException(nameof(loadTask));
            }
        }

        #endregion
    }
}
