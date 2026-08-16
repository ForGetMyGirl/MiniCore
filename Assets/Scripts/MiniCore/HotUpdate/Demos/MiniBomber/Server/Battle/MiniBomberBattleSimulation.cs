using System;
using System.Collections.Generic;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 不依赖 Unity 场景和物理系统的 MiniBomber 服务器权威状态模拟。
    /// </summary>
    public sealed class MiniBomberBattleSimulation
    {
        #region Private 私有成员

        private static readonly int[] ExplosionDirectionX = { 1, -1, 0, 0 }; // 爆炸四方向横向偏移。
        private static readonly int[] ExplosionDirectionZ = { 0, 0, 1, -1 }; // 爆炸四方向纵向偏移。
        private readonly MiniBomberBattleMap map; // 本局只读地图。
        private readonly MiniBomberBattleRules rules; // 本局权威规则。
        private readonly List<MiniBomberPlayerState> players; // 稳定顺序的全部玩家。
        private readonly Dictionary<long, MiniBomberPlayerState> playerById; // 按玩家身份索引状态。
        private readonly List<MiniBomberBombState> bombs; // 当前活动炸弹。
        private readonly List<MiniBomberBombState> explosionQueue; // 当前 Tick 待爆炸队列。
        private readonly HashSet<long> queuedExplosionBombs; // 防止连锁炸弹重复入队。
        private readonly List<MiniBomberSimulationEvent> events; // 当前 Tick 事件复用列表。
        private readonly bool[] destroyedBreakables; // 已摧毁木箱位图。
        private readonly List<MiniBomberPlayerState> resultSortBuffer; // 比赛结束排名排序缓存。
        private long nextBombId = 1; // 本局下一个炸弹编号。
        private long nextEventId = 1; // 本局下一个事件编号。
        private bool isFinished; // 比赛是否已经结束。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 当前比赛身份。
        /// </summary>
        public long MatchId { get; }

        /// <summary>
        /// 当前服务器逻辑 Tick。
        /// </summary>
        public long ServerTick { get; private set; }

        /// <summary>
        /// 比赛总逻辑 Tick 数。
        /// </summary>
        public long DurationTicks { get; }

        /// <summary>
        /// 当前比赛是否结束。
        /// </summary>
        public bool IsFinished => isFinished;

        /// <summary>
        /// 稳定顺序的权威玩家状态。
        /// </summary>
        public IReadOnlyList<MiniBomberPlayerState> Players => players;

        /// <summary>
        /// 当前活动炸弹状态。
        /// </summary>
        public IReadOnlyList<MiniBomberBombState> Bombs => bombs;

        /// <summary>
        /// 最近一次 Tick 产生的离散事件。
        /// </summary>
        public IReadOnlyList<MiniBomberSimulationEvent> Events => events;

        /// <summary>
        /// 当前已经产生的最后一个有序事件编号。
        /// </summary>
        public long LastEventId => nextEventId - 1;

        /// <summary>
        /// 比赛剩余毫秒数。
        /// </summary>
        public int RemainingMilliseconds
        {
            get
            {
                long remainingTicks = DurationTicks - ServerTick;
                return remainingTicks <= 0 ? 0 : (int)((remainingTicks * 1000L) / rules.TickRate);
            }
        }

        /// <summary>
        /// 创建并初始化一局服务器权威战斗。
        /// </summary>
        /// <param name="matchId">稳定比赛身份。</param>
        /// <param name="durationSeconds">本局总时长秒数。</param>
        /// <param name="mapData">权威地图数据。</param>
        /// <param name="battleRules">权威战斗规则。</param>
        /// <param name="participants">稳定加入顺序的参与者。</param>
        public MiniBomberBattleSimulation(long matchId, int durationSeconds, MiniBomberBattleMap mapData, MiniBomberBattleRules battleRules, IReadOnlyList<MiniBomberBattleParticipant> participants)
        {
            if (durationSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            }

            map = mapData ?? throw new ArgumentNullException(nameof(mapData));
            rules = battleRules ?? throw new ArgumentNullException(nameof(battleRules));
            rules.Validate();
            if (participants == null || participants.Count == 0)
            {
                throw new ArgumentException("比赛至少需要一名参与者。", nameof(participants));
            }

            MatchId = matchId;
            DurationTicks = (long)durationSeconds * rules.TickRate;
            players = new List<MiniBomberPlayerState>(participants.Count);
            playerById = new Dictionary<long, MiniBomberPlayerState>(participants.Count);
            bombs = new List<MiniBomberBombState>(participants.Count * 2);
            explosionQueue = new List<MiniBomberBombState>(participants.Count * 2);
            queuedExplosionBombs = new HashSet<long>();
            events = new List<MiniBomberSimulationEvent>(32);
            destroyedBreakables = new bool[map.Width * map.Height];
            resultSortBuffer = new List<MiniBomberPlayerState>(participants.Count);
            for (int index = 0; index < participants.Count; index++)
            {
                MiniBomberBattleParticipant participant = participants[index];
                MiniBomberCell spawn = map.GetSpawn(index);
                var player = new MiniBomberPlayerState
                {
                    PlayerId = participant.PlayerId,
                    PlayerName = participant.PlayerName,
                    PositionXMillimeters = CellCenter(spawn.X),
                    PositionZMillimeters = CellCenter(spawn.Z),
                    FacingZ = 1000,
                    IsAlive = true,
                    IsOnline = true,
                    BombCapacity = rules.InitialBombCapacity,
                    BombRange = rules.InitialBombRange,
                    SpawnIndex = index
                };
                players.Add(player);
                playerById.Add(player.PlayerId, player);
            }
        }

        /// <summary>
        /// 设置玩家当前在线状态；离线时立即把移动输入和速度意图归零，但保持当前位置。
        /// </summary>
        /// <param name="playerId">目标玩家身份。</param>
        /// <param name="isOnline">新的在线状态。</param>
        /// <returns>找到玩家并完成修改时返回 true。</returns>
        public bool SetPlayerOnline(long playerId, bool isOnline)
        {
            if (!playerById.TryGetValue(playerId, out MiniBomberPlayerState player))
            {
                return false;
            }

            player.IsOnline = isOnline;
            if (!isOnline)
            {
                StopPlayer(player);
            }

            return true;
        }

        /// <summary>
        /// 提交玩家最新输入；旧序号输入会被丢弃，炸弹只接受输入边沿。
        /// </summary>
        /// <param name="playerId">输入所属玩家。</param>
        /// <param name="input">量化输入。</param>
        /// <returns>输入被权威状态接受时返回 true。</returns>
        public bool SubmitInput(long playerId, MiniBomberBattleInput input)
        {
            if (isFinished || !playerById.TryGetValue(playerId, out MiniBomberPlayerState player) || !player.IsOnline || input.Sequence <= player.LastInputSequence)
            {
                return false;
            }

            player.LastInputSequence = input.Sequence;
            player.LastInputServerTick = ServerTick;
            int moveX = ClampInput(input.MoveX);
            int moveZ = ClampInput(input.MoveZ);
            NormalizeInput(ref moveX, ref moveZ);
            player.MoveX = moveX;
            player.MoveZ = moveZ;
            if (player.MoveX != 0 || player.MoveZ != 0)
            {
                player.FacingX = player.MoveX;
                player.FacingZ = player.MoveZ;
            }

            player.PendingBombPlacement |= input.PlaceBomb;
            return true;
        }

        /// <summary>
        /// 推进一个固定服务器逻辑步骤。
        /// </summary>
        public void Tick()
        {
            events.Clear();
            if (isFinished)
            {
                return;
            }

            ServerTick++;
            UpdatePlayers();
            UpdateBombPassThrough();
            CollectDueBombs();
            ResolveExplosions();
            if (ServerTick >= DurationTicks)
            {
                isFinished = true;
                for (int index = 0; index < players.Count; index++)
                {
                    StopPlayer(players[index]);
                }
            }
        }

        /// <summary>
        /// 复制当前已摧毁木箱位图，供完整状态快照和断线恢复使用。
        /// </summary>
        /// <returns>按地图格索引排列的紧凑字节数组。</returns>
        public byte[] CopyDestroyedBreakables()
        {
            int byteCount = (destroyedBreakables.Length + 7) / 8;
            var result = new byte[byteCount];
            for (int index = 0; index < destroyedBreakables.Length; index++)
            {
                if (destroyedBreakables[index])
                {
                    result[index >> 3] |= (byte)(1 << (index & 7));
                }
            }

            return result;
        }

        /// <summary>
        /// 生成服务器唯一最终排名。
        /// </summary>
        /// <returns>按得分、击杀、死亡和玩家身份稳定排序的结果。</returns>
        public IReadOnlyList<MiniBomberMatchResult> BuildResults()
        {
            resultSortBuffer.Clear();
            resultSortBuffer.AddRange(players);
            resultSortBuffer.Sort(CompareResult);
            var result = new MiniBomberMatchResult[resultSortBuffer.Count];
            for (int index = 0; index < resultSortBuffer.Count; index++)
            {
                MiniBomberPlayerState player = resultSortBuffer[index];
                result[index] = new MiniBomberMatchResult(
                    index + 1,
                    player.PlayerId,
                    player.PlayerName,
                    player.Score,
                    player.Kills,
                    player.Deaths,
                    player.IsOnline);
            }

            return result;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 更新玩家移动、放置炸弹和复活状态。
        /// </summary>
        private void UpdatePlayers()
        {
            long holdTicks = ((long)rules.InputHoldMilliseconds * rules.TickRate + 999L) / 1000L;
            for (int index = 0; index < players.Count; index++)
            {
                MiniBomberPlayerState player = players[index];
                if (!player.IsAlive)
                {
                    if (ServerTick >= player.RespawnTick)
                    {
                        Respawn(player);
                    }
                    continue;
                }

                if (!player.IsOnline || ServerTick - player.LastInputServerTick > holdTicks)
                {
                    StopPlayer(player);
                }

                MovePlayer(player);
                if (player.PendingBombPlacement)
                {
                    player.PendingBombPlacement = false;
                    TryPlaceBomb(player);
                }
            }
        }

        /// <summary>
        /// 将玩家按整数余数累计方式移动，并分别解决两个坐标轴的阻挡。
        /// </summary>
        /// <param name="player">待移动玩家。</param>
        private void MovePlayer(MiniBomberPlayerState player)
        {
            if (player.MoveX == 0 && player.MoveZ == 0)
            {
                return;
            }

            long denominator = 1000L * rules.TickRate;
            long numeratorX = player.MovementRemainderX + ((long)player.MoveX * rules.MovementSpeedMillimetersPerSecond);
            long numeratorZ = player.MovementRemainderZ + ((long)player.MoveZ * rules.MovementSpeedMillimetersPerSecond);
            int deltaX = (int)(numeratorX / denominator);
            int deltaZ = (int)(numeratorZ / denominator);
            player.MovementRemainderX = numeratorX % denominator;
            player.MovementRemainderZ = numeratorZ % denominator;

            int proposedX = player.PositionXMillimeters + deltaX;
            if (!IsBlocked(player, proposedX, player.PositionZMillimeters))
            {
                player.PositionXMillimeters = proposedX;
            }
            else
            {
                player.MovementRemainderX = 0;
            }

            int proposedZ = player.PositionZMillimeters + deltaZ;
            if (!IsBlocked(player, player.PositionXMillimeters, proposedZ))
            {
                player.PositionZMillimeters = proposedZ;
            }
            else
            {
                player.MovementRemainderZ = 0;
            }
        }

        /// <summary>
        /// 判断玩家圆形占位在目标毫米坐标是否与地图或炸弹阻挡相交。
        /// </summary>
        /// <param name="player">待检测玩家。</param>
        /// <param name="positionX">目标横向毫米坐标。</param>
        /// <param name="positionZ">目标纵向毫米坐标。</param>
        /// <returns>存在阻挡时返回 true。</returns>
        private bool IsBlocked(MiniBomberPlayerState player, int positionX, int positionZ)
        {
            int radius = rules.PlayerRadiusMillimeters;
            int minX = FloorToCell(positionX - radius);
            int maxX = FloorToCell(positionX + radius);
            int minZ = FloorToCell(positionZ - radius);
            int maxZ = FloorToCell(positionZ + radius);
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (IsBlockingMapCell(x, z) && CircleIntersectsCell(positionX, positionZ, radius, x, z))
                    {
                        return true;
                    }
                }
            }

            for (int index = 0; index < bombs.Count; index++)
            {
                MiniBomberBombState bomb = bombs[index];
                if (bomb.OwnerPlayerId == player.PlayerId && bomb.OwnerCanPass)
                {
                    continue;
                }

                if (CircleIntersectsCell(positionX, positionZ, radius, bomb.CellX, bomb.CellZ))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 尝试在玩家当前格放置一枚服务器权威炸弹。
        /// </summary>
        /// <param name="player">放置炸弹的玩家。</param>
        private void TryPlaceBomb(MiniBomberPlayerState player)
        {
            if (!player.IsAlive || CountOwnedBombs(player.PlayerId) >= player.BombCapacity)
            {
                return;
            }

            int cellX = FloorToCell(player.PositionXMillimeters);
            int cellZ = FloorToCell(player.PositionZMillimeters);
            if (IsBlockingMapCell(cellX, cellZ) || FindBombAt(cellX, cellZ) != null)
            {
                return;
            }

            long fuseTicks = ((long)rules.BombFuseMilliseconds * rules.TickRate + 999L) / 1000L;
            var bomb = new MiniBomberBombState
            {
                BombId = nextBombId++,
                OwnerPlayerId = player.PlayerId,
                CellX = cellX,
                CellZ = cellZ,
                Range = player.BombRange,
                ExplodeTick = ServerTick + fuseTicks,
                OwnerCanPass = true
            };
            bombs.Add(bomb);
            AddEvent(MiniBomberSimulationEventType.BombPlaced, player.PlayerId, 0, bomb.BombId, cellX, cellZ);
        }

        /// <summary>
        /// 当放置者的圆形占位完全离开炸弹格后关闭仅属于放置者的穿出权限。
        /// </summary>
        private void UpdateBombPassThrough()
        {
            for (int index = 0; index < bombs.Count; index++)
            {
                MiniBomberBombState bomb = bombs[index];
                if (!bomb.OwnerCanPass || !playerById.TryGetValue(bomb.OwnerPlayerId, out MiniBomberPlayerState owner))
                {
                    continue;
                }

                if (!CircleIntersectsCell(
                        owner.PositionXMillimeters,
                        owner.PositionZMillimeters,
                        rules.PlayerRadiusMillimeters,
                        bomb.CellX,
                        bomb.CellZ))
                {
                    bomb.OwnerCanPass = false;
                }
            }
        }

        /// <summary>
        /// 收集本 Tick 到期的炸弹并准备处理连锁爆炸。
        /// </summary>
        private void CollectDueBombs()
        {
            explosionQueue.Clear();
            queuedExplosionBombs.Clear();
            for (int index = 0; index < bombs.Count; index++)
            {
                MiniBomberBombState bomb = bombs[index];
                if (bomb.ExplodeTick <= ServerTick)
                {
                    QueueExplosion(bomb);
                }
            }
        }

        /// <summary>
        /// 依次处理到期炸弹和连锁炸弹，并从活动集合移除已爆炸实体。
        /// </summary>
        private void ResolveExplosions()
        {
            for (int queueIndex = 0; queueIndex < explosionQueue.Count; queueIndex++)
            {
                MiniBomberBombState bomb = explosionQueue[queueIndex];
                ResolveExplosionCell(bomb, bomb.CellX, bomb.CellZ, false);
                for (int direction = 0; direction < ExplosionDirectionX.Length; direction++)
                {
                    for (int distance = 1; distance <= bomb.Range; distance++)
                    {
                        int x = bomb.CellX + (ExplosionDirectionX[direction] * distance);
                        int z = bomb.CellZ + (ExplosionDirectionZ[direction] * distance);
                        MiniBomberCellType cell = map.GetCell(x, z);
                        if (cell == MiniBomberCellType.Solid)
                        {
                            break;
                        }

                        bool stop = ResolveExplosionCell(bomb, x, z, cell == MiniBomberCellType.Breakable);
                        if (stop)
                        {
                            break;
                        }
                    }
                }
            }

            for (int index = bombs.Count - 1; index >= 0; index--)
            {
                if (queuedExplosionBombs.Contains(bombs[index].BombId))
                {
                    bombs.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// 处理爆炸覆盖的单个地图格及其中的木箱、炸弹和玩家。
        /// </summary>
        /// <param name="source">爆炸来源炸弹。</param>
        /// <param name="cellX">横向格坐标。</param>
        /// <param name="cellZ">纵向格坐标。</param>
        /// <param name="mayContainBreakable">初始地图是否把该格标为木箱。</param>
        /// <returns>该方向是否应在本格后停止。</returns>
        private bool ResolveExplosionCell(MiniBomberBombState source, int cellX, int cellZ, bool mayContainBreakable)
        {
            int cellIndex = (cellZ * map.Width) + cellX;
            bool activeBreakable = mayContainBreakable && cellIndex >= 0 && cellIndex < destroyedBreakables.Length && !destroyedBreakables[cellIndex];
            AddEvent(MiniBomberSimulationEventType.ExplosionStarted, source.OwnerPlayerId, 0, source.BombId, cellX, cellZ);
            if (activeBreakable)
            {
                destroyedBreakables[cellIndex] = true;
                AddEvent(MiniBomberSimulationEventType.BlockDestroyed, source.OwnerPlayerId, 0, 0, cellX, cellZ);
            }

            MiniBomberBombState chainedBomb = FindBombAt(cellX, cellZ);
            if (chainedBomb != null)
            {
                QueueExplosion(chainedBomb);
            }

            for (int index = 0; index < players.Count; index++)
            {
                MiniBomberPlayerState player = players[index];
                if (player.IsAlive && ServerTick >= player.InvulnerableUntilTick && FloorToCell(player.PositionXMillimeters) == cellX && FloorToCell(player.PositionZMillimeters) == cellZ)
                {
                    KillPlayer(source.OwnerPlayerId, player);
                }
            }

            return activeBreakable;
        }

        /// <summary>
        /// 将炸弹加入本 Tick 爆炸队列，并保证连锁只处理一次。
        /// </summary>
        /// <param name="bomb">待爆炸炸弹。</param>
        private void QueueExplosion(MiniBomberBombState bomb)
        {
            if (queuedExplosionBombs.Add(bomb.BombId))
            {
                explosionQueue.Add(bomb);
            }
        }

        /// <summary>
        /// 结算单名玩家死亡和击杀归属。
        /// </summary>
        /// <param name="killerPlayerId">炸弹拥有者身份。</param>
        /// <param name="victim">死亡玩家。</param>
        private void KillPlayer(long killerPlayerId, MiniBomberPlayerState victim)
        {
            victim.IsAlive = false;
            victim.Deaths++;
            victim.Score += rules.DeathScore;
            victim.RespawnTick = ServerTick + MillisecondsToTicks(rules.RespawnDelayMilliseconds);
            StopPlayer(victim);
            if (killerPlayerId != victim.PlayerId && playerById.TryGetValue(killerPlayerId, out MiniBomberPlayerState killer))
            {
                killer.Kills++;
                killer.Score += rules.KillScore;
                AddEvent(MiniBomberSimulationEventType.ScoreChanged, killerPlayerId, killerPlayerId, 0, FloorToCell(killer.PositionXMillimeters), FloorToCell(killer.PositionZMillimeters));
            }

            AddEvent(MiniBomberSimulationEventType.PlayerKilled, killerPlayerId, victim.PlayerId, 0, FloorToCell(victim.PositionXMillimeters), FloorToCell(victim.PositionZMillimeters));
            AddEvent(MiniBomberSimulationEventType.ScoreChanged, killerPlayerId, victim.PlayerId, 0, FloorToCell(victim.PositionXMillimeters), FloorToCell(victim.PositionZMillimeters));
        }

        /// <summary>
        /// 在玩家固定出生序号对应的位置复活并开启保护。
        /// </summary>
        /// <param name="player">待复活玩家。</param>
        private void Respawn(MiniBomberPlayerState player)
        {
            MiniBomberCell spawn = FindSafeSpawn(player.SpawnIndex);
            player.PositionXMillimeters = CellCenter(spawn.X);
            player.PositionZMillimeters = CellCenter(spawn.Z);
            player.IsAlive = true;
            player.InvulnerableUntilTick = ServerTick + MillisecondsToTicks(rules.RespawnProtectionMilliseconds);
            StopPlayer(player);
            AddEvent(MiniBomberSimulationEventType.PlayerRespawned, player.PlayerId, player.PlayerId, 0, spawn.X, spawn.Z);
        }

        /// <summary>
        /// 从玩家原出生点开始循环查找当前未被炸弹占用的出生格。
        /// </summary>
        /// <param name="preferredIndex">首选出生格序号。</param>
        /// <returns>可用出生格；全部占用时返回首选格。</returns>
        private MiniBomberCell FindSafeSpawn(int preferredIndex)
        {
            for (int offset = 0; offset < map.SpawnCount; offset++)
            {
                MiniBomberCell spawn = map.GetSpawn(preferredIndex + offset);
                if (!IsBlockingMapCell(spawn.X, spawn.Z) && FindBombAt(spawn.X, spawn.Z) == null)
                {
                    return spawn;
                }
            }

            return map.GetSpawn(preferredIndex);
        }

        /// <summary>
        /// 清空玩家移动意图和整数移动余数，保持当前位置不变。
        /// </summary>
        /// <param name="player">待停止玩家。</param>
        private static void StopPlayer(MiniBomberPlayerState player)
        {
            player.MoveX = 0;
            player.MoveZ = 0;
            player.MovementRemainderX = 0;
            player.MovementRemainderZ = 0;
            player.PendingBombPlacement = false;
        }

        /// <summary>
        /// 判断地图格是否仍然阻挡玩家移动。
        /// </summary>
        /// <param name="x">横向格坐标。</param>
        /// <param name="z">纵向格坐标。</param>
        /// <returns>固定墙或尚未摧毁的木箱返回 true。</returns>
        private bool IsBlockingMapCell(int x, int z)
        {
            MiniBomberCellType cell = map.GetCell(x, z);
            if (cell == MiniBomberCellType.Solid)
            {
                return true;
            }

            if (cell != MiniBomberCellType.Breakable || x < 0 || z < 0 || x >= map.Width || z >= map.Height)
            {
                return false;
            }

            return !destroyedBreakables[(z * map.Width) + x];
        }

        /// <summary>
        /// 判断玩家圆形占位是否与指定完整地图格相交。
        /// </summary>
        /// <param name="positionX">玩家横向毫米坐标。</param>
        /// <param name="positionZ">玩家纵向毫米坐标。</param>
        /// <param name="radius">玩家碰撞半径。</param>
        /// <param name="cellX">格子横向坐标。</param>
        /// <param name="cellZ">格子纵向坐标。</param>
        /// <returns>圆形与矩形相交时返回 true。</returns>
        private bool CircleIntersectsCell(int positionX, int positionZ, int radius, int cellX, int cellZ)
        {
            int minX = cellX * map.CellSizeMillimeters;
            int minZ = cellZ * map.CellSizeMillimeters;
            int maxX = minX + map.CellSizeMillimeters;
            int maxZ = minZ + map.CellSizeMillimeters;
            int nearestX = positionX < minX ? minX : positionX > maxX ? maxX : positionX;
            int nearestZ = positionZ < minZ ? minZ : positionZ > maxZ ? maxZ : positionZ;
            long deltaX = positionX - nearestX;
            long deltaZ = positionZ - nearestZ;
            return (deltaX * deltaX) + (deltaZ * deltaZ) < (long)radius * radius;
        }

        /// <summary>
        /// 获取指定格内当前活动炸弹。
        /// </summary>
        /// <param name="cellX">横向格坐标。</param>
        /// <param name="cellZ">纵向格坐标。</param>
        /// <returns>找到的炸弹；不存在时返回 null。</returns>
        private MiniBomberBombState FindBombAt(int cellX, int cellZ)
        {
            for (int index = 0; index < bombs.Count; index++)
            {
                MiniBomberBombState bomb = bombs[index];
                if (bomb.CellX == cellX && bomb.CellZ == cellZ && !queuedExplosionBombs.Contains(bomb.BombId))
                {
                    return bomb;
                }
            }

            return null;
        }

        /// <summary>
        /// 统计玩家当前尚未爆炸的炸弹数量。
        /// </summary>
        /// <param name="playerId">炸弹拥有者。</param>
        /// <returns>活动炸弹数量。</returns>
        private int CountOwnedBombs(long playerId)
        {
            int count = 0;
            for (int index = 0; index < bombs.Count; index++)
            {
                if (bombs[index].OwnerPlayerId == playerId)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 追加本 Tick 离散事件。
        /// </summary>
        /// <param name="type">事件类型。</param>
        /// <param name="actor">行为玩家。</param>
        /// <param name="target">目标玩家。</param>
        /// <param name="entity">关联实体。</param>
        /// <param name="cellX">横向格坐标。</param>
        /// <param name="cellZ">纵向格坐标。</param>
        private void AddEvent(MiniBomberSimulationEventType type, long actor, long target, long entity, int cellX, int cellZ)
        {
            events.Add(new MiniBomberSimulationEvent(nextEventId++, type, ServerTick, actor, target, entity, cellX, cellZ));
        }

        /// <summary>
        /// 将毫米坐标换算为向负无穷取整的格坐标。
        /// </summary>
        /// <param name="millimeters">毫米坐标。</param>
        /// <returns>格坐标。</returns>
        private int FloorToCell(int millimeters)
        {
            if (millimeters >= 0)
            {
                return millimeters / map.CellSizeMillimeters;
            }

            return ((millimeters + 1) / map.CellSizeMillimeters) - 1;
        }

        /// <summary>
        /// 将格坐标转换为格子中心毫米坐标。
        /// </summary>
        /// <param name="cell">格坐标。</param>
        /// <returns>格子中心毫米坐标。</returns>
        private int CellCenter(int cell)
        {
            return (cell * map.CellSizeMillimeters) + (map.CellSizeMillimeters / 2);
        }

        /// <summary>
        /// 把毫秒时长向上换算为逻辑 Tick。
        /// </summary>
        /// <param name="milliseconds">时长毫秒数。</param>
        /// <returns>至少完整覆盖时长的 Tick 数。</returns>
        private long MillisecondsToTicks(int milliseconds)
        {
            return ((long)milliseconds * rules.TickRate + 999L) / 1000L;
        }

        /// <summary>
        /// 把输入轴限制在协议允许范围内。
        /// </summary>
        /// <param name="value">输入轴值。</param>
        /// <returns>负一千到一千之间的值。</returns>
        private static int ClampInput(int value)
        {
            return value < -1000 ? -1000 : value > 1000 ? 1000 : value;
        }

        /// <summary>
        /// 使用整数平方根把超过单位圆的量化输入缩回单位圆。
        /// </summary>
        /// <param name="x">横向输入引用。</param>
        /// <param name="z">纵向输入引用。</param>
        private static void NormalizeInput(ref int x, ref int z)
        {
            long magnitudeSquared = ((long)x * x) + ((long)z * z);
            if (magnitudeSquared <= 1000000L)
            {
                return;
            }

            long magnitude = IntegerSquareRoot(magnitudeSquared);
            x = (int)(((long)x * 1000L) / magnitude);
            z = (int)(((long)z * 1000L) / magnitude);
        }

        /// <summary>
        /// 计算非负整数平方根的向下取整值。
        /// </summary>
        /// <param name="value">非负输入。</param>
        /// <returns>平方不大于输入的最大整数。</returns>
        private static long IntegerSquareRoot(long value)
        {
            long result = 0;
            long bit = 1L << 62;
            while (bit > value)
            {
                bit >>= 2;
            }

            while (bit != 0)
            {
                if (value >= result + bit)
                {
                    value -= result + bit;
                    result = (result >> 1) + bit;
                }
                else
                {
                    result >>= 1;
                }

                bit >>= 2;
            }

            return result;
        }

        /// <summary>
        /// 按得分、击杀、死亡和玩家身份比较最终排名。
        /// </summary>
        /// <param name="left">左侧玩家。</param>
        /// <param name="right">右侧玩家。</param>
        /// <returns>标准排序比较结果。</returns>
        private static int CompareResult(MiniBomberPlayerState left, MiniBomberPlayerState right)
        {
            int value = right.Score.CompareTo(left.Score);
            if (value != 0)
            {
                return value;
            }

            value = right.Kills.CompareTo(left.Kills);
            if (value != 0)
            {
                return value;
            }

            value = left.Deaths.CompareTo(right.Deaths);
            return value != 0 ? value : left.PlayerId.CompareTo(right.PlayerId);
        }

        #endregion
    }
}
