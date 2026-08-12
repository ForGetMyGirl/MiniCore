using MiniCore.Protocol.Generated;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 客户端应用战斗同步消息的结果。
    /// </summary>
    public enum MiniBomberReplicationApplyResult
    {
        /// <summary>
        /// 消息已应用。
        /// </summary>
        Applied,
        /// <summary>
        /// 消息为重复或旧消息，已安全忽略。
        /// </summary>
        Ignored,
        /// <summary>
        /// 基线或事件序列不连续，需要请求完整关键帧。
        /// </summary>
        RequiresResync
    }
}
