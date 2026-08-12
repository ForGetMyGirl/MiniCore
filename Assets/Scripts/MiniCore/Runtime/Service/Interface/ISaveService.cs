using MiniCore.Threading;

namespace MiniCore.Service
{
    /// <summary>
    /// 提供版本化保护二进制存档读写能力的系统服务契约。
    /// </summary>
    public interface ISaveService : IAppService
    {
        /// <summary>
        /// 异步保存一个逻辑槽位的二进制数据。
        /// </summary>
        /// <param name="slotName">逻辑槽位名称。</param>
        /// <param name="data">待保护并保存的二进制数据。</param>
        /// <returns>保存完成任务。</returns>
        MTask SaveAsync(string slotName, byte[] data);

        /// <summary>
        /// 异步读取、校验并解密一个逻辑槽位的二进制数据。
        /// </summary>
        /// <param name="slotName">逻辑槽位名称。</param>
        /// <returns>槽位不存在时返回空；否则返回独立明文字节数组。</returns>
        MTask<byte[]> LoadAsync(string slotName);
    }
}
