using System;
using System.Collections.Generic;

namespace MiniCore.Model
{
    /// <summary>
    /// 非 Unity 组件的基础实现。
    /// 负责管理子组件生命周期，并为 Global 等容器提供统一的初始化和更新入口。
    /// </summary>
    public abstract class AComponent : IDisposable
    {
        #region Private 私有成员

        private Dictionary<Type, AComponent> components; // 按具体类型索引的子组件集合。
        private List<AComponent> componentSnapshot; // 遍历期间使用的子组件快照缓存。
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

            component.Awake();
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
            component.Awake();
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
            component.Awake(args);
            components.Add(type, component);
            component.IsActive = true;
            return component;
        }

        /// <summary>
        /// 释放当前组件及其所有子组件。
        /// 释放过程中使用快照，允许子组件释放逻辑修改组件集合。
        /// </summary>
        public virtual void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            IsActive = false;
            if (components != null)
            {
                int snapshotCount = RefreshSnapshot();
                if (snapshotCount > 0)
                {
                    for (int i = 0; i < componentSnapshot.Count; i++)
                    {
                        componentSnapshot[i]?.Dispose();
                    }
                }

                componentSnapshot?.Clear();
                components.Clear();
            }

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

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 验证当前组件尚未释放。
        /// 子类在初始化或需要修改内部状态的入口调用此方法，可尽早给出已销毁对象的诊断信息。
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName, "组件已释放，不能继续管理子组件。");
            }
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

        #endregion
    }
}
