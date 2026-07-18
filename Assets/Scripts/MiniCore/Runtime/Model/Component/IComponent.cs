namespace MiniCore.Model
{
    /// <summary>
    /// 组件容器的基础操作约定。
    /// 带参数初始化统一使用 ComponentInitArgs，避免 object 数组的顺序和类型错误。
    /// </summary>
    public interface IComponent
    {
        #region Public 公共成员

        /// <summary>
        /// 创建并添加指定类型的组件。
        /// </summary>
        /// <typeparam name="T">要添加的组件类型。</typeparam>
        /// <returns>创建完成的组件实例。</returns>
        T AddComponent<T>() where T : class, IComponent;

        /// <summary>
        /// 使用强类型初始化参数创建并添加指定类型的组件。
        /// </summary>
        /// <typeparam name="T">要添加的组件类型。</typeparam>
        /// <param name="args">组件首次初始化所需的参数。</param>
        /// <returns>创建完成的组件实例。</returns>
        T AddComponent<T>(ComponentInitArgs args) where T : class, IComponent;

        /// <summary>
        /// 获取已添加的指定类型组件。
        /// </summary>
        /// <typeparam name="T">要获取的组件类型。</typeparam>
        /// <returns>已注册的组件实例。</returns>
        T GetComponent<T>() where T : class, IComponent;

        /// <summary>
        /// 添加一个已有的组件实例。
        /// </summary>
        /// <param name="component">要添加的组件实例。</param>
        void AddComponent(IComponent component);

        /// <summary>
        /// 移除指定类型的组件。
        /// </summary>
        /// <typeparam name="T">要移除的组件类型。</typeparam>
        void RemoveComponent<T>() where T : class, IComponent;

        /// <summary>
        /// 移除指定的组件实例。
        /// </summary>
        /// <param name="component">要移除的组件实例。</param>
        void RemoveComponent(IComponent component);

        /// <summary>
        /// 使用无参方式初始化组件。
        /// </summary>
        void Awake();

        /// <summary>
        /// 获取或设置组件是否处于激活状态。
        /// 任意 Awake 成功完成后，组件应进入激活状态。
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// 获取组件是否已经完成释放。
        /// 已释放组件不应继续参与业务调用或更新。
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// 使用初始化参数初始化组件。
        /// </summary>
        /// <param name="args">组件首次初始化所需的参数。</param>
        void Awake(ComponentInitArgs args);

        /// <summary>
        /// 执行组件的每帧更新逻辑。
        /// </summary>
        void Update();

        #endregion
    }
}
