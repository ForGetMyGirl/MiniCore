namespace MiniCore.Model
{
    /// <summary>
    /// 网络协议消息在普通消息或 RPC 流程中的固定角色。
    /// </summary>
    public enum NetworkMessageRole
    {
        /// <summary>
        /// 无效或尚未指定的消息角色。
        /// </summary>
        None = 0,

        /// <summary>
        /// 不要求请求响应关联的普通消息。
        /// </summary>
        Normal = 1,

        /// <summary>
        /// 由包头 RpcId 关联响应的 RPC 请求。
        /// </summary>
        RpcRequest = 2,

        /// <summary>
        /// 与 RPC 请求关联的响应消息。
        /// </summary>
        RpcResponse = 3
    }
}
