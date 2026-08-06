using System;
using System.Collections.Generic;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 不依赖 Unity 类型的 MiniBomber 地图格坐标。
    /// </summary>
    public readonly struct MiniBomberCell
    {
        #region Public 公共成员

        /// <summary>横向格坐标。</summary>
        public int X { get; }

        /// <summary>纵向格坐标。</summary>
        public int Z { get; }

        /// <summary>
        /// 创建地图格坐标。
        /// </summary>
        /// <param name="x">横向坐标。</param>
        /// <param name="z">纵向坐标。</param>
        public MiniBomberCell(int x, int z)
        {
            X = x;
            Z = z;
        }

        #endregion
    }

    /// <summary>
    /// 权威模拟使用的只读地图数据副本。
    /// </summary>
    public sealed class MiniBomberBattleMap
    {
        #region Private 私有成员

        private readonly byte[] cells; // 地图格类型紧凑数组。
        private readonly MiniBomberCell[] spawnCells; // 玩家出生格数组。

        #endregion

        #region Public 公共成员

        /// <summary>地图横向格数。</summary>
        public int Width { get; }

        /// <summary>地图纵向格数。</summary>
        public int Height { get; }

        /// <summary>单格边长毫米数。</summary>
        public int CellSizeMillimeters { get; }

        /// <summary>出生格数量。</summary>
        public int SpawnCount => spawnCells.Length;

        /// <summary>
        /// 创建权威模拟持有的地图数据。
        /// </summary>
        /// <param name="width">地图横向格数。</param>
        /// <param name="height">地图纵向格数。</param>
        /// <param name="cellSizeMillimeters">单格边长毫米数。</param>
        /// <param name="cellData">长度必须等于宽乘高的格子数据。</param>
        /// <param name="spawns">至少包含一个出生格的数组。</param>
        public MiniBomberBattleMap(int width, int height, int cellSizeMillimeters, byte[] cellData, MiniBomberCell[] spawns)
        {
            if (width <= 0 || height <= 0 || cellSizeMillimeters <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "地图尺寸与格子边长必须大于零。");
            }

            if (cellData == null || cellData.Length != width * height)
            {
                throw new ArgumentException("地图格数据长度与宽高不一致。", nameof(cellData));
            }

            if (spawns == null || spawns.Length == 0)
            {
                throw new ArgumentException("地图至少需要一个出生格。", nameof(spawns));
            }

            Width = width;
            Height = height;
            CellSizeMillimeters = cellSizeMillimeters;
            cells = (byte[])cellData.Clone();
            spawnCells = (MiniBomberCell[])spawns.Clone();
        }

        /// <summary>
        /// 获取指定格子的初始地图类型。
        /// </summary>
        /// <param name="x">横向格坐标。</param>
        /// <param name="z">纵向格坐标。</param>
        /// <returns>地图外返回固定墙，地图内返回地图配置类型。</returns>
        public MiniBomberCellType GetCell(int x, int z)
        {
            return x < 0 || z < 0 || x >= Width || z >= Height
                ? MiniBomberCellType.Solid
                : (MiniBomberCellType)cells[(z * Width) + x];
        }

        /// <summary>
        /// 获取指定序号对应的循环出生格。
        /// </summary>
        /// <param name="index">出生格序号。</param>
        /// <returns>合法出生格。</returns>
        public MiniBomberCell GetSpawn(int index)
        {
            int normalized = index % spawnCells.Length;
            return spawnCells[normalized < 0 ? normalized + spawnCells.Length : normalized];
        }

        #endregion
    }

    /// <summary>
    /// 从 Unity 配置复制出的权威战斗规则值。
    /// </summary>
    public sealed class MiniBomberBattleRules
    {
        #region Public 公共成员

        public int TickRate { get; set; }
        public int InputHoldMilliseconds { get; set; }
        public int MovementSpeedMillimetersPerSecond { get; set; }
        public int PlayerRadiusMillimeters { get; set; }
        public int BombFuseMilliseconds { get; set; }
        public int InitialBombCapacity { get; set; }
        public int InitialBombRange { get; set; }
        public int RespawnDelayMilliseconds { get; set; }
        public int RespawnProtectionMilliseconds { get; set; }
        public int KillScore { get; set; }
        public int DeathScore { get; set; }

        /// <summary>
        /// 验证权威模拟需要的全部规则值。
        /// </summary>
        public void Validate()
        {
            if (TickRate <= 0 || MovementSpeedMillimetersPerSecond <= 0 || PlayerRadiusMillimeters <= 0 || BombFuseMilliseconds <= 0 || InitialBombCapacity <= 0 || InitialBombRange <= 0)
            {
                throw new InvalidOperationException("MiniBomber 战斗规则包含非正数关键参数。");
            }
        }

        #endregion
    }

    /// <summary>
    /// 加入权威战斗的玩家初始资料。
    /// </summary>
    public readonly struct MiniBomberBattleParticipant
    {
        #region Public 公共成员

        public long PlayerId { get; }
        public string PlayerName { get; }

        /// <summary>
        /// 创建战斗参与者资料。
        /// </summary>
        /// <param name="playerId">稳定玩家身份。</param>
        /// <param name="playerName">显示名称。</param>
        public MiniBomberBattleParticipant(long playerId, string playerName)
        {
            PlayerId = playerId;
            PlayerName = playerName ?? string.Empty;
        }

        #endregion
    }

    /// <summary>
    /// 客户端提交给权威模拟的量化输入。
    /// </summary>
    public readonly struct MiniBomberBattleInput
    {
        #region Public 公共成员

        public long Sequence { get; }
        public int MoveX { get; }
        public int MoveZ { get; }
        public bool PlaceBomb { get; }

        /// <summary>
        /// 创建量化战斗输入。
        /// </summary>
        /// <param name="sequence">单玩家递增输入序号。</param>
        /// <param name="moveX">范围为负一千到一千的横向输入。</param>
        /// <param name="moveZ">范围为负一千到一千的纵向输入。</param>
        /// <param name="placeBomb">本帧是否触发放置炸弹。</param>
        public MiniBomberBattleInput(long sequence, int moveX, int moveZ, bool placeBomb)
        {
            Sequence = sequence;
            MoveX = moveX;
            MoveZ = moveZ;
            PlaceBomb = placeBomb;
        }

        #endregion
    }

    /// <summary>
    /// 服务端权威玩家状态。
    /// </summary>
    public sealed class MiniBomberPlayerState
    {
        #region Public 公共成员

        public long PlayerId { get; internal set; }
        public string PlayerName { get; internal set; }
        public int PositionXMillimeters { get; internal set; }
        public int PositionZMillimeters { get; internal set; }
        public int FacingX { get; internal set; }
        public int FacingZ { get; internal set; }
        public int MoveX { get; internal set; }
        public int MoveZ { get; internal set; }
        public long MovementRemainderX { get; internal set; }
        public long MovementRemainderZ { get; internal set; }
        public long LastInputSequence { get; internal set; }
        public long LastInputServerTick { get; internal set; }
        public bool IsAlive { get; internal set; }
        public bool IsOnline { get; internal set; }
        public long RespawnTick { get; internal set; }
        public long InvulnerableUntilTick { get; internal set; }
        public int Score { get; internal set; }
        public int Kills { get; internal set; }
        public int Deaths { get; internal set; }
        public int BombCapacity { get; internal set; }
        public int BombRange { get; internal set; }
        public int SpawnIndex { get; internal set; }
        public bool PendingBombPlacement { get; internal set; }

        #endregion
    }

    /// <summary>
    /// 服务端权威炸弹状态。
    /// </summary>
    public sealed class MiniBomberBombState
    {
        #region Public 公共成员

        public long BombId { get; internal set; }
        public long OwnerPlayerId { get; internal set; }
        public int CellX { get; internal set; }
        public int CellZ { get; internal set; }
        public int Range { get; internal set; }
        public long ExplodeTick { get; internal set; }
        public bool OwnerCanPass { get; internal set; }

        #endregion
    }

    /// <summary>
    /// 权威模拟产生的离散事件类型。
    /// </summary>
    public enum MiniBomberSimulationEventType
    {
        BombPlaced,
        ExplosionStarted,
        BlockDestroyed,
        PlayerKilled,
        PlayerRespawned,
        ScoreChanged
    }

    /// <summary>
    /// 权威模拟单 Tick 产生的离散事件。
    /// </summary>
    public readonly struct MiniBomberSimulationEvent
    {
        #region Public 公共成员

        public long EventId { get; }
        public MiniBomberSimulationEventType Type { get; }
        public long ServerTick { get; }
        public long ActorPlayerId { get; }
        public long TargetPlayerId { get; }
        public long EntityId { get; }
        public int CellX { get; }
        public int CellZ { get; }

        /// <summary>
        /// 创建权威模拟事件。
        /// </summary>
        /// <param name="eventId">本局递增事件编号。</param>
        /// <param name="type">事件类型。</param>
        /// <param name="serverTick">事件发生的服务器 Tick。</param>
        /// <param name="actorPlayerId">行为发起玩家。</param>
        /// <param name="targetPlayerId">行为目标玩家。</param>
        /// <param name="entityId">炸弹等实体编号。</param>
        /// <param name="cellX">事件横向格坐标。</param>
        /// <param name="cellZ">事件纵向格坐标。</param>
        public MiniBomberSimulationEvent(long eventId, MiniBomberSimulationEventType type, long serverTick, long actorPlayerId, long targetPlayerId, long entityId, int cellX, int cellZ)
        {
            EventId = eventId;
            Type = type;
            ServerTick = serverTick;
            ActorPlayerId = actorPlayerId;
            TargetPlayerId = targetPlayerId;
            EntityId = entityId;
            CellX = cellX;
            CellZ = cellZ;
        }

        #endregion
    }

    /// <summary>
    /// 比赛结束后由服务器生成的稳定排名项。
    /// </summary>
    public readonly struct MiniBomberMatchResult
    {
        #region Public 公共成员

        public int Rank { get; }
        public long PlayerId { get; }
        public string PlayerName { get; }
        public int Score { get; }
        public int Kills { get; }
        public int Deaths { get; }
        public bool IsOnline { get; }

        /// <summary>
        /// 创建服务器最终排名项。
        /// </summary>
        /// <param name="rank">从一开始的名次。</param>
        /// <param name="player">权威玩家状态。</param>
        public MiniBomberMatchResult(int rank, MiniBomberPlayerState player)
        {
            Rank = rank;
            PlayerId = player.PlayerId;
            PlayerName = player.PlayerName;
            Score = player.Score;
            Kills = player.Kills;
            Deaths = player.Deaths;
            IsOnline = player.IsOnline;
        }

        #endregion
    }
}
