namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 客户端与服务器共同使用的稳定常量。
    /// </summary>
    public static class MiniBomberConstants
    {
        #region Public 公共成员

        /// <summary>
        /// 当前协议版本。
        /// </summary>
        public const int ProtocolVersion = 1;

        /// <summary>
        /// 当前规则版本。
        /// </summary>
        public const int RuleVersion = 1;

        /// <summary>
        /// 默认 KCP 端口。
        /// </summary>
        public const int DefaultServerPort = 20000;

        /// <summary>
        /// MiniBomber KCP 会话固定 Conv。
        /// </summary>
        public const uint KcpConversation = 0x4D424F4D;

        /// <summary>
        /// 客户端默认网络会话标识。
        /// </summary>
        public const string DefaultSessionId = "MiniBomber";

        /// <summary>
        /// 服务器账号数据库存档槽位。
        /// </summary>
        public const string AccountDatabaseSlot = "MiniBomberServerAccounts";

        /// <summary>
        /// 客户端会话恢复存档槽位。
        /// </summary>
        public const string ClientSessionSlot = "MiniBomberClientSession";

        /// <summary>
        /// 运行时配置 YooAsset 地址。
        /// </summary>
        public const string RuntimeConfigAddress = "MiniBomberRuntimeConfig";

        /// <summary>
        /// 规则配置 YooAsset 地址。
        /// </summary>
        public const string RuleConfigAddress = "MiniBomberRuleConfig";

        /// <summary>
        /// 默认地图 YooAsset 地址。
        /// </summary>
        public const string DefaultMapAddress = "MiniBomberDefaultMap";

        /// <summary>
        /// 登录、大厅和房间全屏窗口共用的导航组。
        /// </summary>
        public const string MainNavigationGroup = "Main";

        /// <summary>
        /// 登录窗口稳定路由名。
        /// </summary>
        public const string LoginWindowRoute = "LoginWindow";

        /// <summary>
        /// 大厅窗口稳定路由名。
        /// </summary>
        public const string LobbyWindowRoute = "LobbyWindow";

        /// <summary>
        /// 房间窗口稳定路由名。
        /// </summary>
        public const string RoomWindowRoute = "RoomWindow";

        /// <summary>
        /// 战斗 HUD 稳定路由名。
        /// </summary>
        public const string BattleHudWindowRoute = "BattleHudWindow";

        /// <summary>
        /// 比赛结果窗口稳定路由名。
        /// </summary>
        public const string MatchResultWindowRoute = "MatchResultWindow";

        /// <summary>
        /// 重连遮罩稳定路由名。
        /// </summary>
        public const string ReconnectOverlayRoute = "ReconnectOverlay";

        /// <summary>
        /// 场景加载窗口稳定路由名。
        /// </summary>
        public const string SceneLoadingWindowRoute = "SceneLoadingWindow";

        /// <summary>
        /// 短时消息提示窗口稳定路由名。
        /// </summary>
        public const string MessageToastWindowRoute = "MessageToastWindow";

        /// <summary>
        /// 网络诊断窗口稳定路由名。
        /// </summary>
        public const string NetworkDebugWindowRoute = "NetworkDebugWindow";

        #endregion
    }
}
