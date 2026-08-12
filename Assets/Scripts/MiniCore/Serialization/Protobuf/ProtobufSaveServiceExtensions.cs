using System;
using Google.Protobuf;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Serialization
{
    /// <summary>
    /// 为二进制存档服务提供 Google.Protobuf 类型安全读写入口。
    /// </summary>
    public static class ProtobufSaveServiceExtensions
    {
        #region Public 公共成员

        /// <summary>
        /// 将 Protobuf 消息编码为二进制后写入保护存档。
        /// </summary>
        /// <typeparam name="TMessage">实现 Google.Protobuf 消息契约的类型。</typeparam>
        /// <param name="saveService">目标二进制存档服务。</param>
        /// <param name="slotName">逻辑槽位名称。</param>
        /// <param name="message">待保存消息。</param>
        /// <returns>保存完成任务。</returns>
        public static MTask SaveProtobufAsync<TMessage>(
            this ISaveService saveService,
            string slotName,
            TMessage message)
            where TMessage : IMessage
        {
            if (saveService == null)
            {
                throw new ArgumentNullException(nameof(saveService));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return saveService.SaveAsync(slotName, message.ToByteArray());
        }

        /// <summary>
        /// 读取保护存档并解析为 Protobuf 消息。
        /// </summary>
        /// <typeparam name="TMessage">具有公共无参构造函数的 Protobuf 消息类型。</typeparam>
        /// <param name="saveService">目标二进制存档服务。</param>
        /// <param name="slotName">逻辑槽位名称。</param>
        /// <returns>槽位不存在时返回空，否则返回解析后的消息。</returns>
        public static async MTask<TMessage> LoadProtobufAsync<TMessage>(
            this ISaveService saveService,
            string slotName)
            where TMessage : class, IMessage, new()
        {
            if (saveService == null)
            {
                throw new ArgumentNullException(nameof(saveService));
            }

            byte[] bytes = await saveService.LoadAsync(slotName);
            if (bytes == null)
            {
                return null;
            }

            var message = new TMessage();
            message.MergeFrom(bytes);
            return message;
        }

        #endregion
    }
}
