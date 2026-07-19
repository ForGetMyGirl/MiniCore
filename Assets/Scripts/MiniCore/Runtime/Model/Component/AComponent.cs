using System;
using System.Collections.Generic;
using MiniCore.Core;
using MiniCore.Threading;

namespace MiniCore.Model
{
    /// <summary>
    /// 非 Unity 组件的基础实现。
    /// 负责管理子组件生命周期，并为 Global 等容器提供统一的初始化和更新入口。
    /// </summary>
    public abstract class AComponent : IDisposable, IMTaskOwner
    {
        #region Private 私有成员

        private Dictionary<Type, AComponent> components; // 按具体类型索引的子组件集合。
        private List<AComponent> componentSnapshot; // 遍历期间使用的子组件快照缓存。
        private Action childDisposedAction; // 子组件完成两阶段释放后的复用回调。
        private Action domainDrainedAction; // 当前任务域退场后的复用回调。
        private Action disposedCallbacks; // 等待当前组件最终释放的内部回调。
        private MTaskDomain mTaskDomain; // 首次启动 MTask 时延迟创建的生命周期域。
        private int pendingChildDisposals; // 仍在两阶段释放中的直接子组件数量。
        private bool domainDrained = true; // 当前任务域是否已经全部退场。
        private bool isDisposing; // 组件是否已进入两阶段释放。
        private bool isDisposed; // 组件是否已完成释放。
        private ComponentGroupId groupId; // 当前组件所属的 Global 分组身份。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取或设置组件是否处于激活状态。
        /// 仅激活组件会参与 MonoUpdate 调度。
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 获取组件是否已经完成 Dispose。
        /// 已释放组件不可再次注册或参与更新，应通过 Global 重新获取新实例。
        /// </summary>
        public bool IsDisposed => isDisposed;

        /// <summary>
        /// 获取组件是否已经开始释放但仍在等待异步任务退场。
        /// </summary>
        public bool IsDisposing => isDisposing;

        /// <summary>
        /// 获取当前组件所属的 Global 分组身份。
        /// 默认组表示传统全局单实例；非默认组用于房间、对局等多实例组件。
        /// </summary>
        public ComponentGroupId GroupId => groupId;

        /// <summary>
        /// 获取已添加的指定类型子组件。
        /// </summary>
        /// <typeparam name="T">要获取的子组件类型。</typeparam>
        /// <returns>已注册的子组件实例；未注册时返回 null。</returns>
        public T GetComponent<T>() where T : AComponent, new()
        {
            if (components == null)
            {
                return null;
            }

            components.TryGetValue(typeof(T), out AComponent component);
            return component as T;
        }

        /// <summary>
        /// 添加一个已有的子组件实例。
        /// 已存在相同具体类型时保持原组件不变。
        /// </summary>
        /// <param name="component">要添加的子组件实例。</param>
        public void AddComponent(AComponent component)
        {
            ThrowIfDisposed();
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            Type type = component.GetType();
            EnsureComponents();
            if (components.ContainsKey(type))
            {
                return;
            }

            component.InvokeAwake();
            components.Add(type, component);
            component.IsActive = true;
        }

        /// <summary>
        /// 使用无参方式初始化组件。
        /// 子类可重写此方法完成基础初始化。
        /// </summary>
        public virtual void Awake()
        {
        }

        /// <summary>
        /// 使用初始化参数初始化组件。
        /// 需要参数的组件应继承 AComponent&lt;TArgs&gt;，由泛型基类完成强类型校验。
        /// </summary>
        /// <param name="args">组件首次初始化所需的参数。</param>
        public virtual void Awake(ComponentInitArgs args)
        {
            throw new InvalidOperationException($"组件 {GetType().FullName} 不接受初始化参数 {args?.GetType().FullName ?? "null"}。");
        }

        /// <summary>
        /// 由 Global 运行时在组件初始化前写入所属分组身份。
        /// </summary>
        /// <param name="value">组件所属的分组身份。</param>
        internal void SetGroupId(ComponentGroupId value)
        {
            groupId = value;
        }

        /// <summary>
        /// 移除指定的子组件实例。
        /// 此方法保持既有行为，仅停用并移除引用，不主动调用 Dispose。
        /// </summary>
        /// <param name="component">要移除的子组件实例。</param>
        public void RemoveComponent(AComponent component)
        {
            ThrowIfDisposed();
            if (component == null || components == null)
            {
                return;
            }

            Type type = component.GetType();
            if (!components.ContainsKey(type))
            {
                return;
            }

            components.Remove(type);
            component.IsActive = false;
        }

        /// <summary>
        /// 创建并添加指定类型的无参子组件。
        /// 已存在相同类型时抛出异常，避免调用方误以为重新初始化成功。
        /// </summary>
        /// <typeparam name="T">要创建的子组件类型。</typeparam>
        /// <returns>新创建并激活的子组件实例。</returns>
        public T AddComponent<T>() where T : AComponent, new()
        {
            ThrowIfDisposed();
            Type type = typeof(T);
            EnsureComponents();
            if (components.ContainsKey(type))
            {
                throw new InvalidOperationException($"已存在的组件类型：{type.FullName}");
            }

            T component = new T();
            component.InvokeAwake();
            components.Add(type, component);
            component.IsActive = true;
            return component;
        }

        /// <summary>
        /// 移除指定类型的子组件。
        /// 此方法保持既有行为，仅停用并移除引用，不主动调用 Dispose。
        /// </summary>
        /// <typeparam name="T">要移除的子组件类型。</typeparam>
        public void RemoveComponent<T>() where T : AComponent, new()
        {
            ThrowIfDisposed();
            if (components == null)
            {
                return;
            }

            Type type = typeof(T);
            if (!components.TryGetValue(type, out AComponent component))
            {
                return;
            }

            component.IsActive = false;
            components.Remove(type);
        }

        /// <summary>
        /// 使用强类型初始化参数创建并添加指定类型的子组件。
        /// 已存在相同类型时抛出异常，避免新参数被悄悄忽略。
        /// </summary>
        /// <typeparam name="T">要创建的子组件类型。</typeparam>
        /// <param name="args">组件首次初始化所需的参数。</param>
        /// <returns>新创建并激活的子组件实例。</returns>
        public T AddComponent<T>(ComponentInitArgs args) where T : AComponent, new()
        {
            ThrowIfDisposed();
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            Type type = typeof(T);
            EnsureComponents();
            if (components.ContainsKey(type))
            {
                throw new InvalidOperationException($"已存在的组件类型：{type.FullName}");
            }

            T component = new T();
            component.InvokeAwake(args);
            components.Add(type, component);
            component.IsActive = true;
            return component;
        }

        /// <summary>
        /// 释放当前组件及其所有子组件。
        /// 释放过程中使用快照，允许子组件释放逻辑修改组件集合。
        /// </summary>
        public void Dispose()
        {
            if (isDisposed || isDisposing)
            {
                return;
            }

            isDisposing = true;
            IsActive = false;
            using (MTaskRuntime.EnterOwner(this))
            {
                OnDisposing();
            }

            if (components != null)
            {
                int snapshotCount = RefreshSnapshot();
                if (snapshotCount > 0)
                {
                    childDisposedAction ??= OnChildDisposed;
                    pendingChildDisposals = componentSnapshot.Count;
                    for (int i = 0; i < componentSnapshot.Count; i++)
                    {
                        AComponent component = componentSnapshot[i];
                        if (component == null || component.IsDisposed)
                        {
                            pendingChildDisposals--;
                            continue;
                        }

                        component.RegisterDisposed(childDisposedAction);
                        component.Dispose();
                    }
                }
            }

            if (mTaskDomain == null)
            {
                domainDrained = true;
            }
            else
            {
                MTaskDomain domain = mTaskDomain;
                domainDrained = false;
                domainDrainedAction ??= OnDomainDrained;
                domain.OnDrained(domainDrainedAction);
                domain.Dispose();
            }

            TryFinalizeDispose();
        }

        /// <summary>
        /// 调度组件及其子组件的每帧更新。
        /// 仅在组件处于激活状态时执行。
        /// </summary>
        public void MonoUpdate()
        {
            if (isDisposed || !IsActive)
            {
                return;
            }

            using (MTaskRuntime.EnterOwner(this))
            {
                if (components != null)
                {
                    int snapshotCount = RefreshSnapshot();
                    if (snapshotCount > 0)
                    {
                        for (int i = 0; i < componentSnapshot.Count; i++)
                        {
                            componentSnapshot[i]?.MonoUpdate();
                        }
                    }
                }

                Update();
            }
        }

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 验证当前组件尚未释放。
        /// 子类在初始化或需要修改内部状态的入口调用此方法，可尽早给出已销毁对象的诊断信息。
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (isDisposed || isDisposing)
            {
                throw new ObjectDisposedException(GetType().FullName, "组件已开始释放，不能继续管理子组件。");
            }
        }

        /// <summary>
        /// 在组件开始释放且任务域取消前执行同步停机操作。
        /// 子类应在此处关闭 Socket、停止外部 I/O 或解除会阻塞任务退出的资源。
        /// </summary>
        protected virtual void OnDisposing()
        {
        }

        /// <summary>
        /// 在当前组件及其子组件的异步任务全部退场后执行最终资源清理。
        /// 子类应重写此方法，不应重写 Dispose。
        /// </summary>
        protected virtual void OnDispose()
        {
        }

        /// <summary>
        /// 执行当前组件的每帧逻辑。
        /// 子类重写时应避免产生可观测的每帧 GC 分配。
        /// </summary>
        protected virtual void Update()
        {
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 确保子组件字典已创建。
        /// </summary>
        private void EnsureComponents()
        {
            if (components == null)
            {
                components = new Dictionary<Type, AComponent>();
            }
        }

        /// <summary>
        /// 刷新遍历使用的组件快照。
        /// </summary>
        /// <returns>快照中的组件数量。</returns>
        private int RefreshSnapshot()
        {
            if (components == null || components.Count == 0)
            {
                return 0;
            }

            if (componentSnapshot == null)
            {
                componentSnapshot = new List<AComponent>(Math.Max(components.Count, 2));
            }

            componentSnapshot.Clear();
            componentSnapshot.AddRange(components.Values);
            return componentSnapshot.Count;
        }

        /// <summary>
        /// 在当前组件 Owner 上下文中调用无参 Awake。
        /// </summary>
        internal void InvokeAwake()
        {
            using (MTaskRuntime.EnterOwner(this))
            {
                Awake();
            }
        }

        /// <summary>
        /// 在当前组件 Owner 上下文中调用带参数 Awake。
        /// </summary>
        /// <param name="args">组件初始化参数。</param>
        internal void InvokeAwake(ComponentInitArgs args)
        {
            using (MTaskRuntime.EnterOwner(this))
            {
                Awake(args);
            }
        }

        /// <summary>
        /// 注册组件最终完成释放后的内部通知。
        /// </summary>
        /// <param name="callback">组件完成释放后执行的回调。</param>
        internal void RegisterDisposed(Action callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (isDisposed)
            {
                callback();
                return;
            }

            disposedCallbacks += callback;
        }

        /// <summary>
        /// 处理一个直接子组件完成两阶段释放。
        /// </summary>
        private void OnChildDisposed()
        {
            if (pendingChildDisposals > 0)
            {
                pendingChildDisposals--;
            }

            TryFinalizeDispose();
        }

        /// <summary>
        /// 处理当前组件任务域全部退场。
        /// </summary>
        private void OnDomainDrained()
        {
            domainDrained = true;
            TryFinalizeDispose();
        }

        /// <summary>
        /// 在子组件和任务域均退场后执行最终资源清理。
        /// </summary>
        private void TryFinalizeDispose()
        {
            if (!isDisposing || isDisposed || !domainDrained || pendingChildDisposals != 0)
            {
                return;
            }

            using (MTaskRuntime.EnterOwner(this))
            {
                OnDispose();
            }

            Global.ReleaseAllIfInitialized(this);

            componentSnapshot?.Clear();
            components?.Clear();
            mTaskDomain?.Dispose();
            mTaskDomain = null;
            isDisposed = true;
            Action callbacks = disposedCallbacks;
            disposedCallbacks = null;
            callbacks?.Invoke();
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 获取当前组件延迟创建的 MTask 生命周期域。
        /// </summary>
        /// <returns>绑定当前组件生命周期的任务域。</returns>
        public MTaskDomain GetMTaskDomain()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName, "组件已完成释放，不能创建任务域。");
            }

            if (mTaskDomain == null)
            {
                if (isDisposing)
                {
                    throw new ObjectDisposedException(GetType().FullName, "组件正在释放，不能创建新的任务域。");
                }

                mTaskDomain = new MTaskDomain(GetType().FullName, MTaskRuntime.CurrentExecutor ?? MTaskExecutors.Unity);
            }

            return mTaskDomain;
        }

        #endregion
    }
}
