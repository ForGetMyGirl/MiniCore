namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 登录或恢复会话后的协议无关结果。
    /// </summary>
    public sealed class MiniBomberSessionResult
    {
        #region Public 公共成员

        /// <summary>
        /// 获取业务命令结果。
        /// </summary>
        public MiniBomberCommandResult Command { get; }

        /// <summary>
        /// 获取服务器决定的客户端目的地。
        /// </summary>
        public MiniBomberClientDestinationKind Destination { get; }

        /// <summary>
        /// 获取恢复会话携带的可选房间数据。
        /// </summary>
        public MiniBomberRoomModel Room { get; }

        /// <summary>
        /// 创建协议无关的会话结果。
        /// </summary>
        /// <param name="command">业务命令结果。</param>
        /// <param name="destination">客户端目的地。</param>
        /// <param name="room">可选房间数据。</param>
        public MiniBomberSessionResult(
            MiniBomberCommandResult command,
            MiniBomberClientDestinationKind destination = MiniBomberClientDestinationKind.Login,
            MiniBomberRoomModel room = null)
        {
            Command = command;
            Destination = destination;
            Room = room;
        }

        #endregion
    }
}
