namespace MiniCore.Model
{
    /// <summary>
    /// 网络 RPC 使用的通用业务错误码。
    /// </summary>
    public static class NetErrorCode
    {
        /// <summary>
        /// 请求执行成功。
        /// </summary>
        public const int Success = 0;
        /// <summary>
        /// 未分类错误。
        /// </summary>
        public const int Unknown = 1;
        /// <summary>
        /// 请求参数或请求状态无效。
        /// </summary>
        public const int InvalidRequest = 100001;
        /// <summary>
        /// 玩家标识无效。
        /// </summary>
        public const int PlayerIdInvalid = 100002;
        /// <summary>
        /// 目标房间不存在。
        /// </summary>
        public const int RoomNotFound = 100003;
        /// <summary>
        /// 目标房间人数已满。
        /// </summary>
        public const int RoomFull = 100004;
        /// <summary>
        /// 服务端尚未准备完成。
        /// </summary>
        public const int ServerNotReady = 100005;
        /// <summary>
        /// 玩家已在房间内。
        /// </summary>
        public const int AlreadyInRoom = 100006;
        /// <summary>
        /// 房间当前不可加入。
        /// </summary>
        public const int RoomNotJoinable = 100007;
    }
}
