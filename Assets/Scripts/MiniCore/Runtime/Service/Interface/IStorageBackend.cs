using MiniCore.Threading;

namespace MiniCore.Service
{
    /// <summary>
    /// 以逻辑键读写二进制数据的平台存储后端契约。
    /// </summary>
    public interface IStorageBackend
    {
        #region Public 公共成员

        /// <summary>
        /// 读取指定逻辑键的完整二进制内容。
        /// </summary>
        /// <param name="key">不包含平台文件路径语义的逻辑键。</param>
        /// <returns>键不存在时返回空，否则返回独立字节数组。</returns>
        MTask<byte[]> ReadAsync(string key);

        /// <summary>
        /// 覆盖写入指定逻辑键。
        /// </summary>
        /// <param name="key">不包含平台文件路径语义的逻辑键。</param>
        /// <param name="bytes">需要完整保存的字节数组。</param>
        /// <returns>平台确认写入完成时结束的任务。</returns>
        MTask WriteAsync(string key, byte[] bytes);

        /// <summary>
        /// 删除指定逻辑键；键不存在时也视为成功。
        /// </summary>
        /// <param name="key">不包含平台文件路径语义的逻辑键。</param>
        /// <returns>删除事务完成时结束的任务。</returns>
        MTask DeleteAsync(string key);

        /// <summary>
        /// 查询指定逻辑键是否存在。
        /// </summary>
        /// <param name="key">不包含平台文件路径语义的逻辑键。</param>
        /// <returns>键存在时返回 true。</returns>
        MTask<bool> ExistsAsync(string key);

        #endregion
    }
}
