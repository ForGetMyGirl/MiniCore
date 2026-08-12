using System;
using System.Collections.Generic;
using System.Threading;
using Google.Protobuf;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 单个独占执行器上的多房间串行 Worker。
    /// </summary>
    internal sealed class MiniBomberRoomWorker : IDisposable
    {
        #region Private 私有成员

        private readonly int workerIndex; // 固定 Worker 下标。
        private readonly int deltaIntervalTicks; // 动态增量间隔。
        private readonly int keyframeIntervalTicks; // 完整关键帧间隔。
        private readonly MSingleThreadExecutor executor; // MTask 提供的独占后台线程。
        private readonly MiniBomberBoundedQueue<MiniBomberRoomWorkerCommand> inputQueue; // Demo 自有有界输入队列。
        private readonly MiniBomberBoundedQueue<MiniBomberRoomWorkerOutput> outputQueue; // Demo 自有有界输出队列。
        private readonly Dictionary<long, MiniBomberWorkerMatchState> matches = new Dictionary<long, MiniBomberWorkerMatchState>(); // 只允许 Worker 线程访问的比赛状态。
        private int drainScheduled; // 是否已有唯一抽取任务进入执行器。
        private int stopping; // 是否开始关闭。
        private int threadId; // 独占线程托管标识。
        private int activeMatchCount; // 供跨线程读取的比赛数量。
        private long rejectedInputCount; // 队列过载拒绝数。
        private long outputBackpressureCount; // 输出背压跳步数。
        private long processedTickCount; // 已处理逻辑步数。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建单个 RoomWorker。
        /// </summary>
        /// <param name="index">Worker 下标。</param>
        /// <param name="inputCapacity">输入容量。</param>
        /// <param name="outputCapacity">输出容量。</param>
        /// <param name="deltaTicks">增量间隔。</param>
        /// <param name="keyframeTicks">关键帧间隔。</param>
        internal MiniBomberRoomWorker(int index, int inputCapacity, int outputCapacity, int deltaTicks, int keyframeTicks)
        {
            workerIndex = index;
            deltaIntervalTicks = deltaTicks;
            keyframeIntervalTicks = keyframeTicks;
            inputQueue = new MiniBomberBoundedQueue<MiniBomberRoomWorkerCommand>(inputCapacity);
            outputQueue = new MiniBomberBoundedQueue<MiniBomberRoomWorkerOutput>(outputCapacity);
            executor = MTaskExecutors.CreateSingleThread($"MiniBomber.RoomWorker.{index}");
        }

        /// <summary>
        /// 将命令加入 Demo 有界队列并保证执行器至多挂一个抽取任务。
        /// </summary>
        /// <param name="command">待执行命令。</param>
        /// <returns>队列接受命令时返回 true。</returns>
        internal bool TryEnqueue(MiniBomberRoomWorkerCommand command)
        {
            if (Volatile.Read(ref stopping) != 0 || !inputQueue.TryEnqueue(command))
            {
                Interlocked.Increment(ref rejectedInputCount);
                return false;
            }

            ScheduleDrain();
            return true;
        }

        /// <summary>
        /// 抽取下一份 Worker 输出。
        /// </summary>
        /// <param name="output">输出帧。</param>
        /// <returns>队列非空时返回 true。</returns>
        internal bool TryDequeueOutput(out MiniBomberRoomWorkerOutput output)
        {
            return outputQueue.TryDequeue(out output);
        }

        /// <summary>
        /// 获取不暴露可变状态的诊断指标。
        /// </summary>
        /// <returns>当前指标快照。</returns>
        internal MiniBomberRoomWorkerMetrics GetMetrics()
        {
            return new MiniBomberRoomWorkerMetrics(
                workerIndex,
                Volatile.Read(ref threadId),
                Volatile.Read(ref activeMatchCount),
                inputQueue.Count,
                outputQueue.Count,
                Interlocked.Read(ref rejectedInputCount),
                Interlocked.Read(ref outputBackpressureCount),
                Interlocked.Read(ref processedTickCount));
        }

        /// <summary>
        /// 清理比赛与队列并停止独占执行器。
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref stopping, 1) != 0)
            {
                return;
            }

            using var completed = new ManualResetEventSlim(false);
            executor.Post(() =>
            {
                inputQueue.Clear();
                matches.Clear();
                Volatile.Write(ref activeMatchCount, 0);
                completed.Set();
            });
            completed.Wait(TimeSpan.FromSeconds(3));
            executor.Dispose();
            outputQueue.Clear();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 在执行器无待抽取任务时派发唯一抽取回调。
        /// </summary>
        private void ScheduleDrain()
        {
            if (Interlocked.CompareExchange(ref drainScheduled, 1, 0) == 0)
            {
                executor.Post(DrainInputs);
            }
        }

        /// <summary>
        /// 在独占线程串行处理当前全部有界命令。
        /// </summary>
        private void DrainInputs()
        {
            Volatile.Write(ref threadId, Thread.CurrentThread.ManagedThreadId);
            while (Volatile.Read(ref stopping) == 0 && inputQueue.TryDequeue(out MiniBomberRoomWorkerCommand command))
            {
                ProcessCommand(command);
            }

            Volatile.Write(ref drainScheduled, 0);
            if (Volatile.Read(ref stopping) == 0 && inputQueue.Count > 0)
            {
                ScheduleDrain();
            }
        }

        /// <summary>
        /// 执行一个 Worker 命令。
        /// </summary>
        /// <param name="command">待执行命令。</param>
        private void ProcessCommand(MiniBomberRoomWorkerCommand command)
        {
            switch (command.Type)
            {
                case MiniBomberRoomWorkerCommandType.Create:
                    matches[command.MatchId] = new MiniBomberWorkerMatchState(command.RoomId, new MiniBomberBattleSimulation(command.MatchId, command.DurationSeconds, command.Map, command.Rules, command.Participants));
                    Volatile.Write(ref activeMatchCount, matches.Count);
                    break;
                case MiniBomberRoomWorkerCommandType.Start:
                    if (matches.TryGetValue(command.MatchId, out MiniBomberWorkerMatchState startMatch))
                    {
                        startMatch.IsStarted = true;
                    }
                    break;
                case MiniBomberRoomWorkerCommandType.Input:
                    if (matches.TryGetValue(command.MatchId, out MiniBomberWorkerMatchState inputMatch))
                    {
                        inputMatch.Simulation.SubmitInput(command.PlayerId, command.Input);
                    }
                    break;
                case MiniBomberRoomWorkerCommandType.Online:
                    if (matches.TryGetValue(command.MatchId, out MiniBomberWorkerMatchState onlineMatch))
                    {
                        onlineMatch.Simulation.SetPlayerOnline(command.PlayerId, command.IsOnline);
                    }
                    break;
                case MiniBomberRoomWorkerCommandType.Keyframe:
                    if (matches.TryGetValue(command.MatchId, out MiniBomberWorkerMatchState keyframeMatch))
                    {
                        EnqueueReliable(CreateOutput(keyframeMatch, null, null, CreateSnapshot(command.MatchId, keyframeMatch), null, command.NetworkSessionId));
                    }
                    break;
                case MiniBomberRoomWorkerCommandType.Remove:
                    matches.Remove(command.MatchId);
                    Volatile.Write(ref activeMatchCount, matches.Count);
                    break;
                case MiniBomberRoomWorkerCommandType.Tick:
                    TickMatches();
                    break;
            }
        }

        /// <summary>
        /// 串行推进本 Worker 上全部已开始比赛。
        /// </summary>
        private void TickMatches()
        {
            if (outputQueue.IsFull)
            {
                Interlocked.Increment(ref outputBackpressureCount);
                return;
            }

            foreach (KeyValuePair<long, MiniBomberWorkerMatchState> pair in matches)
            {
                if (outputQueue.IsFull)
                {
                    Interlocked.Increment(ref outputBackpressureCount);
                    break;
                }

                MiniBomberWorkerMatchState match = pair.Value;
                if (!match.IsStarted || match.ResultCreated)
                {
                    continue;
                }

                match.Simulation.Tick();
                Interlocked.Increment(ref processedTickCount);
                MiniBomberBattleEventBatch events = CreateEvents(pair.Key, match);
                MiniBomberBattleDelta delta = null;
                MiniBomberBattleSnapshot keyframe = null;
                IReadOnlyList<MiniBomberMatchResult> results = null;
                if ((match.Simulation.ServerTick % deltaIntervalTicks) == 0)
                {
                    delta = CreateDelta(pair.Key, match);
                }

                if ((match.Simulation.ServerTick % keyframeIntervalTicks) == 0)
                {
                    keyframe = CreateSnapshot(pair.Key, match);
                    delta = null;
                    match.LastPublishedTick = match.Simulation.ServerTick;
                }

                if (match.Simulation.IsFinished)
                {
                    results = match.Simulation.BuildResults();
                    match.ResultCreated = true;
                    if (keyframe == null)
                    {
                        keyframe = CreateSnapshot(pair.Key, match);
                        delta = null;
                    }
                }

                if (events != null || delta != null || keyframe != null || results != null)
                {
                    MiniBomberRoomWorkerOutput output = CreateOutput(match, events, delta, keyframe, results, null);
                    long replacedBaseTick = 0;
                    if (output.IsReplaceableDelta && outputQueue.TryReplaceLatest(item =>
                        {
                            bool matchesDelta = item.MatchId == output.MatchId && item.IsReplaceableDelta;
                            if (matchesDelta)
                            {
                                replacedBaseTick = item.Delta.BaseTick;
                            }

                            return matchesDelta;
                        }, output))
                    {
                        output.Delta.BaseTick = replacedBaseTick;
                        continue;
                    }

                    EnqueueReliable(output);
                }
            }
        }

        /// <summary>
        /// 创建当前 Tick 的有序事件批次。
        /// </summary>
        /// <param name="matchId">比赛身份。</param>
        /// <param name="match">Worker 比赛状态。</param>
        /// <returns>无事件时返回 null。</returns>
        private static MiniBomberBattleEventBatch CreateEvents(long matchId, MiniBomberWorkerMatchState match)
        {
            IReadOnlyList<MiniBomberSimulationEvent> sourceEvents = match.Simulation.Events;
            if (sourceEvents.Count == 0)
            {
                return null;
            }

            var batch = new MiniBomberBattleEventBatch
            {
                MatchId = matchId,
                ServerTick = match.Simulation.ServerTick,
                PreviousEventId = match.LastEventId,
                Revision = match.Simulation.ServerTick
            };
            for (int index = 0; index < sourceEvents.Count; index++)
            {
                MiniBomberSimulationEvent source = sourceEvents[index];
                MiniBomberPlayerState scorePlayer = FindPlayer(match.Simulation.Players, source.TargetPlayerId != 0 ? source.TargetPlayerId : source.ActorPlayerId);
                batch.Events.Add(new MiniBomberBattleEventDto
                {
                    EventId = source.EventId,
                    Type = ConvertEventType(source.Type),
                    ServerTick = source.ServerTick,
                    ActorPlayerId = source.ActorPlayerId,
                    TargetPlayerId = source.TargetPlayerId,
                    EntityId = source.EntityId,
                    CellX = source.CellX,
                    CellZ = source.CellZ,
                    ActorName = FindPlayerName(match.Simulation.Players, source.ActorPlayerId),
                    TargetName = FindPlayerName(match.Simulation.Players, source.TargetPlayerId),
                    Score = scorePlayer?.Score ?? 0,
                    Kills = scorePlayer?.Kills ?? 0,
                    Deaths = scorePlayer?.Deaths ?? 0
                });
                match.LastEventId = source.EventId;
            }

            return batch;
        }

        /// <summary>
        /// 创建房间级玩家动态增量。
        /// </summary>
        /// <param name="matchId">比赛身份。</param>
        /// <param name="match">Worker 比赛状态。</param>
        /// <returns>包含全部玩家最新动态状态的增量。</returns>
        private static MiniBomberBattleDelta CreateDelta(long matchId, MiniBomberWorkerMatchState match)
        {
            var delta = new MiniBomberBattleDelta
            {
                MatchId = matchId,
                BaseTick = match.LastPublishedTick,
                ServerTick = match.Simulation.ServerTick,
                Revision = match.Simulation.ServerTick,
                RemainingMilliseconds = match.Simulation.RemainingMilliseconds
            };
            AddPlayers(delta.Players, match.Simulation.Players);
            match.LastPublishedTick = match.Simulation.ServerTick;
            return delta;
        }

        /// <summary>
        /// 创建包含动态实体和木箱位图的完整关键帧。
        /// </summary>
        /// <param name="matchId">比赛身份。</param>
        /// <param name="match">Worker 独占比赛状态。</param>
        /// <returns>完整关键帧。</returns>
        private static MiniBomberBattleSnapshot CreateSnapshot(long matchId, MiniBomberWorkerMatchState match)
        {
            MiniBomberBattleSimulation simulation = match.Simulation;
            var snapshot = new MiniBomberBattleSnapshot
            {
                MatchId = matchId,
                ServerTick = simulation.ServerTick,
                Revision = simulation.ServerTick,
                LastEventId = match.LastEventId,
                RemainingMilliseconds = simulation.RemainingMilliseconds,
                DestroyedBreakableCells = ByteString.CopyFrom(simulation.CopyDestroyedBreakables())
            };
            AddPlayers(snapshot.Players, simulation.Players);
            for (int index = 0; index < simulation.Bombs.Count; index++)
            {
                MiniBomberBombState bomb = simulation.Bombs[index];
                snapshot.Bombs.Add(new MiniBomberBattleBombDto
                {
                    BombId = bomb.BombId,
                    OwnerPlayerId = bomb.OwnerPlayerId,
                    CellX = bomb.CellX,
                    CellZ = bomb.CellZ,
                    Range = bomb.Range,
                    ExplodeTick = bomb.ExplodeTick,
                    OwnerCanPass = bomb.OwnerCanPass
                });
            }

            return snapshot;
        }

        /// <summary>
        /// 向协议玩家集合复制权威动态状态。
        /// </summary>
        /// <param name="destination">协议目标集合。</param>
        /// <param name="players">权威玩家列表。</param>
        private static void AddPlayers(Google.Protobuf.Collections.RepeatedField<MiniBomberBattlePlayerDto> destination, IReadOnlyList<MiniBomberPlayerState> players)
        {
            for (int index = 0; index < players.Count; index++)
            {
                MiniBomberPlayerState player = players[index];
                destination.Add(new MiniBomberBattlePlayerDto
                {
                    PlayerId = player.PlayerId,
                    PlayerName = player.PlayerName,
                    PositionXMillimeters = player.PositionXMillimeters,
                    PositionZMillimeters = player.PositionZMillimeters,
                    FacingX = player.FacingX,
                    FacingZ = player.FacingZ,
                    IsAlive = player.IsAlive,
                    RespawnTick = player.RespawnTick,
                    InvulnerableUntilTick = player.InvulnerableUntilTick,
                    Score = player.Score,
                    Kills = player.Kills,
                    Deaths = player.Deaths,
                    BombCapacity = player.BombCapacity,
                    BombRange = player.BombRange,
                    AcknowledgedInputSequence = player.LastInputSequence,
                    IsOnline = player.IsOnline
                });
            }
        }

        /// <summary>
        /// 将 Simulation 事件映射为 Demo 协议事件。
        /// </summary>
        /// <param name="type">Simulation 事件类型。</param>
        /// <returns>协议事件类型。</returns>
        private static MiniBomberBattleEventType ConvertEventType(MiniBomberSimulationEventType type)
        {
            return type switch
            {
                MiniBomberSimulationEventType.BombPlaced => MiniBomberBattleEventType.MiniBomberEventBombPlaced,
                MiniBomberSimulationEventType.ExplosionStarted => MiniBomberBattleEventType.MiniBomberEventExplosionStarted,
                MiniBomberSimulationEventType.BlockDestroyed => MiniBomberBattleEventType.MiniBomberEventBlockDestroyed,
                MiniBomberSimulationEventType.PlayerKilled => MiniBomberBattleEventType.MiniBomberEventPlayerKilled,
                MiniBomberSimulationEventType.PlayerRespawned => MiniBomberBattleEventType.MiniBomberEventPlayerRespawned,
                MiniBomberSimulationEventType.ScoreChanged => MiniBomberBattleEventType.MiniBomberEventScoreChanged,
                _ => MiniBomberBattleEventType.MiniBomberEventNone
            };
        }

        /// <summary>
        /// 按身份查找权威玩家。
        /// </summary>
        /// <param name="players">玩家集合。</param>
        /// <param name="playerId">玩家身份。</param>
        /// <returns>找到的玩家；不存在时返回 null。</returns>
        private static MiniBomberPlayerState FindPlayer(IReadOnlyList<MiniBomberPlayerState> players, long playerId)
        {
            for (int index = 0; index < players.Count; index++)
            {
                if (players[index].PlayerId == playerId)
                {
                    return players[index];
                }
            }

            return null;
        }

        /// <summary>
        /// 按身份查找显示名。
        /// </summary>
        /// <param name="players">玩家集合。</param>
        /// <param name="playerId">玩家身份。</param>
        /// <returns>玩家显示名；不存在时返回空字符串。</returns>
        private static string FindPlayerName(IReadOnlyList<MiniBomberPlayerState> players, long playerId)
        {
            return FindPlayer(players, playerId)?.PlayerName ?? string.Empty;
        }

        /// <summary>
        /// 创建 Worker 输出封装。
        /// </summary>
        /// <param name="match">Worker 比赛状态。</param>
        /// <param name="events">可靠事件。</param>
        /// <param name="delta">可替换增量。</param>
        /// <param name="keyframe">完整关键帧。</param>
        /// <param name="results">比赛结果。</param>
        /// <param name="targetSessionId">可选单会话目标。</param>
        /// <returns>不可再由 Worker 修改的输出。</returns>
        private static MiniBomberRoomWorkerOutput CreateOutput(MiniBomberWorkerMatchState match, MiniBomberBattleEventBatch events, MiniBomberBattleDelta delta, MiniBomberBattleSnapshot keyframe, IReadOnlyList<MiniBomberMatchResult> results, string targetSessionId)
        {
            return new MiniBomberRoomWorkerOutput
            {
                MatchId = match.Simulation.MatchId,
                RoomId = match.RoomId,
                TargetNetworkSessionId = targetSessionId,
                Events = events,
                Delta = delta,
                Keyframe = keyframe,
                Results = results
            };
        }

        /// <summary>
        /// 可靠写入输出；满载时记录背压且不继续推进产生新事件。
        /// </summary>
        /// <param name="output">待写入结果。</param>
        private void EnqueueReliable(MiniBomberRoomWorkerOutput output)
        {
            if (!outputQueue.TryEnqueue(output))
            {
                Interlocked.Increment(ref outputBackpressureCount);
            }
        }

        #endregion
    }
}
