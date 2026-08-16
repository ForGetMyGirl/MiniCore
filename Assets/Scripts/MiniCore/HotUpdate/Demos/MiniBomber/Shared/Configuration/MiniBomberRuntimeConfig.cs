using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 客户端与服务端共享的玩法频率和资源地址配置。
    /// </summary>
    [CreateAssetMenu(fileName = "MiniBomberRuntimeConfig", menuName = "MiniCore/Demos/MiniBomber/Runtime Config")]
    public sealed class MiniBomberRuntimeConfig : ScriptableObject
    {
        #region Private 私有成员

        [SerializeField, Min(1)] private int serverTickRate = 30; // 权威服务器逻辑频率。
        [SerializeField, Min(1)] private int inputSendRate = 30; // 客户端输入发送频率。
        [SerializeField, Min(1)] private int snapshotRate = 15; // 服务器世界快照频率。
        [SerializeField, Range(1, 16)] private int roomWorkerCount = 2; // 固定 RoomWorker 数量。
        [SerializeField, Min(16)] private int roomWorkerInputQueueCapacity = 1024; // 单 Worker 有界输入队列容量。
        [SerializeField, Min(8)] private int roomWorkerOutputQueueCapacity = 256; // 单 Worker 有界输出队列容量。
        [SerializeField, Range(1000, 3000)] private int fullKeyframeIntervalMilliseconds = 2000; // 完整战斗关键帧间隔。
        [SerializeField, Min(0)] private int inputHoldMilliseconds = 100; // 输入中断后允许短暂沿用的时间。
        [SerializeField, Min(1000)] private int reconnectGraceMilliseconds = 15000; // 断线恢复宽限时间。
        [SerializeField, Min(1000)] private int sceneLoadingTimeoutMilliseconds = 30000; // 战斗场景加载超时。
        [SerializeField] private string loginSceneAddress = "LoginScene"; // 登录场景 YooAsset 地址。
        [SerializeField] private string lobbySceneAddress = "LobbyScene"; // 大厅场景 YooAsset 地址。
        [SerializeField] private string battleSceneAddress = "BattleScene"; // 战斗场景 YooAsset 地址。
        [SerializeField] private string mapAddress = "MiniBomberDefaultMap"; // 默认地图配置地址。
        [SerializeField] private string ruleAddress = "MiniBomberRuleConfig"; // 默认规则配置地址。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取服务器权威逻辑频率。
        /// </summary>
        public int ServerTickRate => serverTickRate;
        /// <summary>
        /// 获取客户端输入发送频率。
        /// </summary>
        public int InputSendRate => inputSendRate;
        /// <summary>
        /// 获取服务器玩家动态增量发送频率。
        /// </summary>
        public int SnapshotRate => snapshotRate;
        /// <summary>
        /// 获取固定 RoomWorker 数量。
        /// </summary>
        public int RoomWorkerCount => roomWorkerCount;
        /// <summary>
        /// 获取单 Worker 输入队列容量。
        /// </summary>
        public int RoomWorkerInputQueueCapacity => roomWorkerInputQueueCapacity;
        /// <summary>
        /// 获取单 Worker 输出队列容量。
        /// </summary>
        public int RoomWorkerOutputQueueCapacity => roomWorkerOutputQueueCapacity;
        /// <summary>
        /// 获取完整关键帧间隔毫秒数。
        /// </summary>
        public int FullKeyframeIntervalMilliseconds => fullKeyframeIntervalMilliseconds;
        /// <summary>
        /// 获取服务器沿用最后移动方向的最长时间。
        /// </summary>
        public int InputHoldMilliseconds => inputHoldMilliseconds;
        /// <summary>
        /// 获取断线会话保留时间。
        /// </summary>
        public int ReconnectGraceMilliseconds => reconnectGraceMilliseconds;
        /// <summary>
        /// 获取战斗场景加载超时时间。
        /// </summary>
        public int SceneLoadingTimeoutMilliseconds => sceneLoadingTimeoutMilliseconds;
        /// <summary>
        /// 获取登录场景的 YooAsset 地址。
        /// </summary>
        public string LoginSceneAddress => loginSceneAddress;
        /// <summary>
        /// 获取大厅场景的 YooAsset 地址。
        /// </summary>
        public string LobbySceneAddress => lobbySceneAddress;
        /// <summary>
        /// 获取战斗场景的 YooAsset 地址。
        /// </summary>
        public string BattleSceneAddress => battleSceneAddress;
        /// <summary>
        /// 获取默认地图的 YooAsset 地址。
        /// </summary>
        public string MapAddress => mapAddress;
        /// <summary>
        /// 获取玩法规则的 YooAsset 地址。
        /// </summary>
        public string RuleAddress => ruleAddress;

        #endregion
    }
}
