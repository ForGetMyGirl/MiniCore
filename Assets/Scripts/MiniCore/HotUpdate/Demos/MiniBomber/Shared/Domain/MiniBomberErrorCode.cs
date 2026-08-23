namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// MiniBomber RPC 统一错误码。
    /// </summary>
    public static class MiniBomberErrorCode
    {
        #region Public 公共成员

        public const int Success = 0;
        public const int InvalidArgument = 1001;
        public const int AccountExists = 1002;
        public const int PlayerNameExists = 1003;
        public const int InvalidCredentials = 1004;
        public const int VersionMismatch = 1005;
        public const int SessionExpired = 1006;
        public const int NotAuthenticated = 1007;
        public const int RoomNotFound = 2001;
        public const int RoomFull = 2002;
        public const int RoomAlreadyStarted = 2003;
        public const int PermissionDenied = 2004;
        public const int PlayersNotReady = 2005;
        public const int RevisionConflict = 2006;
        public const int InvalidRoomState = 2007;
        public const int MatchNotFound = 3001;
        public const int MatchLoadingTimeout = 3002;

        /// <summary>
        /// 服务正在摘流量或暂时拒绝新业务工作。
        /// </summary>
        public const int ServerUnavailable = 5001;

        #endregion
    }
}
