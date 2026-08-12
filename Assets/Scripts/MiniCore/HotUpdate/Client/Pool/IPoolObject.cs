namespace MiniCore.Model
{
    /// <summary>
    /// 可由 GameObject 对象池租用和归还的组件契约。
    /// 资源地址和分组由管理器持有，不写回业务组件。
    /// </summary>
    public interface IPoolObject
    {
        #region Public 公共成员

        /// <summary>
        /// 对象被租用后执行运行状态初始化。
        /// </summary>
        void Init();

        /// <summary>
        /// 对象归还前清理本次使用状态。
        /// </summary>
        void Clear();

        #endregion
    }
}
