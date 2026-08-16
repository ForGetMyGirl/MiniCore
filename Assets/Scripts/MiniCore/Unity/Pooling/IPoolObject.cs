namespace MiniCore.Pooling
{
    /// <summary>
    /// 可由 GameObject 对象池租用与归还的业务组件契约。
    /// </summary>
    public interface IPoolObject
    {
        /// <summary>
        /// 对象完成租用并激活后初始化本次使用状态。
        /// </summary>
        void Init();

        /// <summary>
        /// 对象归还缓存前清理本次使用状态。
        /// </summary>
        void Clear();
    }
}
