namespace MiniCore.Model
{
    public static class NetErrorCode
    {
        public const int Success = 0;
        public const int Unknown = 1;
        public const int InvalidRequest = 100001;
        public const int PlayerIdInvalid = 100002;
        public const int RoomNotFound = 100003;
        public const int RoomFull = 100004;
        public const int ServerNotReady = 100005;
        public const int AlreadyInRoom = 100006;
        public const int RoomNotJoinable = 100007;
    }
}

