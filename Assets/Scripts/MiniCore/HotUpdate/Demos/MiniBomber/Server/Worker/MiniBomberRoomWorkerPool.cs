using System;
using System.Collections.Generic;
using System.Threading;
using Google.Protobuf;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 房间到固定 Worker 的稳定分配策略扩展点。
    /// </summary>
    public interface IMiniBomberRoomAssignmentStrategy
    {
        /// <summary>
        /// 为房间选择固定 Worker 下标。
        /// </summary>
        /// <param name="roomId">稳定房间身份。</param>
        /// <param name="workerCount">当前固定 Worker 数量。</param>
        /// <returns>零到 Worker 数量减一之间的下标。</returns>
        int SelectWorker(long roomId, int workerCount);
    }

    /// <summary>
    /// 按房间身份稳定取模的默认分配策略。
    /// </summary>
    public sealed class MiniBomberModuloRoomAssignmentStrategy : IMiniBomberRoomAssignmentStrategy
    {
        /// <summary>
        /// 使用无符号房间身份取模选择 Worker。
        /// </summary>
        /// <param name="roomId">稳定房间身份。</param>
        /// <param name="workerCount">当前固定 Worker 数量。</param>
        /// <returns>稳定 Worker 下标。</returns>
        public int SelectWorker(long roomId, int workerCount)
        {
            if (workerCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(workerCount));
            }

            return (int)((ulong)roomId % (uint)workerCount);
        }
    }

    /// <summary>
    /// 单个 RoomWorker 的只读诊断快照。
    /// </summary>
    public readonly struct MiniBomberRoomWorkerMetrics
    {
        #region Public 公共成员

        /// <summary>Worker 下标。</summary>
        public int WorkerIndex { get; }
        /// <summary>独占线程托管标识。</summary>
        public int ThreadId { get; }
        /// <summary>当前归属比赛数量。</summary>
        public int ActiveMatchCount { get; }
        /// <summary>当前输入队列深度。</summary>
        public int InputQueueDepth { get; }
        /// <summary>当前输出队列深度。</summary>
        public int OutputQueueDepth { get; }
        /// <summary>因输入队列满而拒绝的命令数。</summary>
        public long RejectedInputCount { get; }
        /// <summary>因输出背压而跳过的逻辑步数。</summary>
        public long OutputBackpressureCount { get; }
        /// <summary>已经完成的比赛逻辑步数。</summary>
        public long ProcessedTickCount { get; }

        /// <summary>
        /// 创建 RoomWorker 诊断快照。
        /// </summary>
        /// <param name="workerIndex">Worker 下标。</param>
        /// <param name="threadId">独占线程标识。</param>
        /// <param name="activeMatchCount">归属比赛数量。</param>
        /// <param name="inputQueueDepth">输入队列深度。</param>
        /// <param name="outputQueueDepth">输出队列深度。</param>
        /// <param name="rejectedInputCount">拒绝输入数量。</param>
        /// <param name="outputBackpressureCount">输出背压数量。</param>
        /// <param name="processedTickCount">已完成逻辑步数量。</param>
        public MiniBomberRoomWorkerMetrics(int workerIndex, int threadId, int activeMatchCount, int inputQueueDepth, int outputQueueDepth, long rejectedInputCount, long outputBackpressureCount, long processedTickCount)
        {
            WorkerIndex = workerIndex;
            ThreadId = threadId;
            ActiveMatchCount = activeMatchCount;
            InputQueueDepth = inputQueueDepth;
            OutputQueueDepth = outputQueueDepth;
            RejectedInputCount = rejectedInputCount;
            OutputBackpressureCount = outputBackpressureCount;
            ProcessedTickCount = processedTickCount;
        }

        #endregion
    }

    /// <summary>
    /// Worker 返回主线程安全发送边界的一帧不可变结果。
    /// </summary>
    public sealed class MiniBomberRoomWorkerOutput
    {
        #region Public 公共成员

        /// <summary>比赛身份。</summary>
        public long MatchId { get; internal set; }
        /// <summary>房间身份。</summary>
        public long RoomId { get; internal set; }
        /// <summary>仅发送给指定网络会话；空值表示广播房间。</summary>
        public string TargetNetworkSessionId { get; internal set; }
        /// <summary>不可丢弃的有序事件批次。</summary>
        public MiniBomberBattleEventBatch Events { get; internal set; }
        /// <summary>可由更新状态替换的玩家动态增量。</summary>
        public MiniBomberBattleDelta Delta { get; internal set; }
        /// <summary>完整关键帧。</summary>
        public MiniBomberBattleSnapshot Keyframe { get; internal set; }
        /// <summary>服务器唯一最终排名。</summary>
        public IReadOnlyList<MiniBomberMatchResult> Results { get; internal set; }

        /// <summary>获取输出是否只包含可替换的位置类增量。</summary>
        public bool IsReplaceableDelta => Delta != null && Events == null && Keyframe == null && Results == null && string.IsNullOrEmpty(TargetNetworkSessionId);

        #endregion
    }

    /// <summary>
    /// 固定数量 RoomWorker 池；只在 Worker 内修改各房间的权威 Simulation。
    /// </summary>
    public sealed class MiniBomberRoomWorkerPool : IDisposable
    {
        #region Private 私有成员

        private const int MaximumWorkerCount = 16; // Demo 防止错误配置创建过多线程的硬上限。
        private readonly MiniBomberRoomWorker[] workers; // 固定生命周期 Worker 集合。
        private readonly Dictionary<long, int> workerByRoomId = new Dictionary<long, int>(); // 房间稳定归属诊断索引。
        private readonly Dictionary<long, int> workerByMatchId = new Dictionary<long, int>(); // 比赛快速投递索引。
        private readonly IMiniBomberRoomAssignmentStrategy assignmentStrategy; // 未来节点分片可替换的本地分配策略。
        private bool disposed; // 池是否已经释放。

        #endregion

        #region Public 公共成员

        /// <summary>获取固定 Worker 数量。</summary>
        public int WorkerCount => workers.Length;

        /// <summary>获取池是否已经完成释放。</summary>
        public bool IsDisposed => disposed;

        /// <summary>
        /// 创建固定数量且队列容量有界的 RoomWorker 池。
        /// </summary>
        /// <param name="workerCount">Worker 数量，会限制在一到十六。</param>
        /// <param name="inputQueueCapacity">每个 Worker 输入命令容量。</param>
        /// <param name="outputQueueCapacity">每个 Worker 输出帧容量。</param>
        /// <param name="deltaIntervalTicks">玩家动态增量间隔 Tick。</param>
        /// <param name="keyframeIntervalTicks">完整关键帧间隔 Tick。</param>
        /// <param name="strategy">房间归属策略；为空时使用稳定取模。</param>
        public MiniBomberRoomWorkerPool(int workerCount, int inputQueueCapacity, int outputQueueCapacity, int deltaIntervalTicks, int keyframeIntervalTicks, IMiniBomberRoomAssignmentStrategy strategy = null)
        {
            int resolvedCount = Math.Max(1, Math.Min(workerCount, MaximumWorkerCount));
            if (inputQueueCapacity <= 0 || outputQueueCapacity <= 0 || deltaIntervalTicks <= 0 || keyframeIntervalTicks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inputQueueCapacity), "队列容量和同步间隔必须大于零。");
            }

            assignmentStrategy = strategy ?? new MiniBomberModuloRoomAssignmentStrategy();
            workers = new MiniBomberRoomWorker[resolvedCount];
            for (int index = 0; index < workers.Length; index++)
            {
                workers[index] = new MiniBomberRoomWorker(index, inputQueueCapacity, outputQueueCapacity, deltaIntervalTicks, keyframeIntervalTicks);
            }
        }

        /// <summary>
        /// 将新比赛稳定分配到所属房间的 Worker。
        /// </summary>
        /// <param name="roomId">房间身份。</param>
        /// <param name="matchId">比赛身份。</param>
        /// <param name="durationSeconds">比赛时长秒数。</param>
        /// <param name="map">只读地图配置。</param>
        /// <param name="rules">只读规则配置。</param>
        /// <param name="participants">参与者快照。</param>
        /// <returns>输入队列接受创建命令时返回 true。</returns>
        public bool TryCreateMatch(long roomId, long matchId, int durationSeconds, MiniBomberBattleMap map, MiniBomberBattleRules rules, IReadOnlyList<MiniBomberBattleParticipant> participants)
        {
            ThrowIfDisposed();
            if (workerByMatchId.ContainsKey(matchId))
            {
                return false;
            }

            int workerIndex = GetOrAssignWorker(roomId);
            var participantCopy = new MiniBomberBattleParticipant[participants.Count];
            for (int index = 0; index < participants.Count; index++)
            {
                participantCopy[index] = participants[index];
            }

            if (!workers[workerIndex].TryEnqueue(MiniBomberRoomWorkerCommand.Create(roomId, matchId, durationSeconds, map, CloneRules(rules), participantCopy)))
            {
                return false;
            }

            workerByMatchId.Add(matchId, workerIndex);
            return true;
        }

        /// <summary>
        /// 激活已经创建的比赛，使后续固定步命令开始推进。
        /// </summary>
        /// <param name="matchId">比赛身份。</param>
        /// <returns>命令进入有界队列时返回 true。</returns>
        public bool TryStartMatch(long matchId)
        {
            return TryEnqueueMatch(matchId, MiniBomberRoomWorkerCommand.Start(matchId));
        }

        /// <summary>
        /// 投递玩家量化输入，同一比赛始终由一个 Worker 串行处理。
        /// </summary>
        /// <param name="matchId">比赛身份。</param>
        /// <param name="playerId">玩家身份。</param>
        /// <param name="input">量化输入。</param>
        /// <returns>命令进入有界队列时返回 true。</returns>
        public bool TrySubmitInput(long matchId, long playerId, MiniBomberBattleInput input)
        {
            return TryEnqueueMatch(matchId, MiniBomberRoomWorkerCommand.CreateInput(matchId, playerId, input));
        }

        /// <summary>
        /// 投递玩家在线状态变化。
        /// </summary>
        /// <param name="matchId">比赛身份。</param>
        /// <param name="playerId">玩家身份。</param>
        /// <param name="isOnline">是否在线。</param>
        /// <returns>命令进入有界队列时返回 true。</returns>
        public bool TrySetPlayerOnline(long matchId, long playerId, bool isOnline)
        {
            return TryEnqueueMatch(matchId, MiniBomberRoomWorkerCommand.Online(matchId, playerId, isOnline));
        }

        /// <summary>
        /// 请求为单个会话生成完整关键帧。
        /// </summary>
        /// <param name="matchId">比赛身份。</param>
        /// <param name="networkSessionId">目标网络会话标识。</param>
        /// <returns>命令进入有界队列时返回 true。</returns>
        public bool TryRequestKeyframe(long matchId, string networkSessionId)
        {
            return TryEnqueueMatch(matchId, MiniBomberRoomWorkerCommand.Keyframe(matchId, networkSessionId));
        }

        /// <summary>
        /// 向全部 Worker 各投递一个固定逻辑步命令。
        /// </summary>
        /// <returns>所有 Worker 均接受命令时返回 true。</returns>
        public bool TryTickAll()
        {
            ThrowIfDisposed();
            bool accepted = true;
            for (int index = 0; index < workers.Length; index++)
            {
                accepted &= workers[index].TryEnqueue(MiniBomberRoomWorkerCommand.Tick());
            }

            return accepted;
        }

        /// <summary>
        /// 移除比赛并释放 Worker 独占的 Simulation。
        /// </summary>
        /// <param name="matchId">比赛身份。</param>
        /// <returns>命令进入有界队列时返回 true。</returns>
        public bool TryRemoveMatch(long matchId)
        {
            ThrowIfDisposed();
            if (!workerByMatchId.TryGetValue(matchId, out int workerIndex) || !workers[workerIndex].TryEnqueue(MiniBomberRoomWorkerCommand.Remove(matchId)))
            {
                return false;
            }

            workerByMatchId.Remove(matchId);
            return true;
        }

        /// <summary>
        /// 在主线程抽取下一份 Worker 结果。
        /// </summary>
        /// <param name="output">可安全交给网络发送边界的结果。</param>
        /// <returns>存在结果时返回 true。</returns>
        public bool TryDequeueOutput(out MiniBomberRoomWorkerOutput output)
        {
            ThrowIfDisposed();
            for (int index = 0; index < workers.Length; index++)
            {
                if (workers[index].TryDequeueOutput(out output))
                {
                    return true;
                }
            }

            output = null;
            return false;
        }

        /// <summary>
        /// 查询房间当前稳定归属的 Worker。
        /// </summary>
        /// <param name="roomId">房间身份。</param>
        /// <param name="workerIndex">Worker 下标。</param>
        /// <returns>房间已经分配时返回 true。</returns>
        public bool TryGetAssignedWorker(long roomId, out int workerIndex)
        {
            return workerByRoomId.TryGetValue(roomId, out workerIndex);
        }

        /// <summary>
        /// 取得全部 Worker 的瞬时诊断指标。
        /// </summary>
        /// <returns>按 Worker 下标排列的指标数组。</returns>
        public MiniBomberRoomWorkerMetrics[] GetMetrics()
        {
            var result = new MiniBomberRoomWorkerMetrics[workers.Length];
            for (int index = 0; index < workers.Length; index++)
            {
                result[index] = workers[index].GetMetrics();
            }

            return result;
        }

        /// <summary>
        /// 停止所有 Worker、清空有界队列并等待独占线程退出。
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            for (int index = 0; index < workers.Length; index++)
            {
                workers[index].Dispose();
            }

            workerByMatchId.Clear();
            workerByRoomId.Clear();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 获取已有房间归属或首次建立稳定归属。
        /// </summary>
        /// <param name="roomId">房间身份。</param>
        /// <returns>Worker 下标。</returns>
        private int GetOrAssignWorker(long roomId)
        {
            if (workerByRoomId.TryGetValue(roomId, out int workerIndex))
            {
                return workerIndex;
            }

            workerIndex = assignmentStrategy.SelectWorker(roomId, workers.Length);
            if (workerIndex < 0 || workerIndex >= workers.Length)
            {
                throw new InvalidOperationException("MiniBomber 房间分配策略返回了越界 Worker 下标。");
            }

            workerByRoomId.Add(roomId, workerIndex);
            return workerIndex;
        }

        /// <summary>
        /// 向比赛所属 Worker 投递命令。
        /// </summary>
        /// <param name="matchId">比赛身份。</param>
        /// <param name="command">待投递命令。</param>
        /// <returns>有界队列接受命令时返回 true。</returns>
        private bool TryEnqueueMatch(long matchId, MiniBomberRoomWorkerCommand command)
        {
            ThrowIfDisposed();
            return workerByMatchId.TryGetValue(matchId, out int workerIndex) && workers[workerIndex].TryEnqueue(command);
        }

        /// <summary>
        /// 复制规则值，避免主线程配置对象成为 Worker 可变共享状态。
        /// </summary>
        /// <param name="source">源规则。</param>
        /// <returns>独立规则副本。</returns>
        private static MiniBomberBattleRules CloneRules(MiniBomberBattleRules source)
        {
            return new MiniBomberBattleRules
            {
                TickRate = source.TickRate,
                InputHoldMilliseconds = source.InputHoldMilliseconds,
                MovementSpeedMillimetersPerSecond = source.MovementSpeedMillimetersPerSecond,
                PlayerRadiusMillimeters = source.PlayerRadiusMillimeters,
                BombFuseMilliseconds = source.BombFuseMilliseconds,
                InitialBombCapacity = source.InitialBombCapacity,
                InitialBombRange = source.InitialBombRange,
                RespawnDelayMilliseconds = source.RespawnDelayMilliseconds,
                RespawnProtectionMilliseconds = source.RespawnProtectionMilliseconds,
                KillScore = source.KillScore,
                DeathScore = source.DeathScore
            };
        }

        /// <summary>
        /// 在释放后阻止新命令进入 Worker。
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(MiniBomberRoomWorkerPool));
            }
        }

        #endregion
    }

    /// <summary>
    /// 单个独占执行器上的多房间串行 Worker。
    /// </summary>
    internal sealed class MiniBomberRoomWorker : IDisposable
    {
        #region Private 私有成员

        private readonly int workerIndex; // 固定 Worker 下标。
        private readonly int deltaIntervalTicks; // 动态增量间隔。
        private readonly int keyframeIntervalTicks; // 完整关键帧间隔。
        private readonly MDedicatedThreadExecutor executor; // MTask 提供的独占后台线程。
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
            executor = MTaskExecutors.CreateDedicated($"MiniBomber.RoomWorker.{index}");
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

    /// <summary>
    /// Worker 独占的单局状态。
    /// </summary>
    internal sealed class MiniBomberWorkerMatchState
    {
        #region Internal 内部成员

        internal long RoomId { get; }
        internal MiniBomberBattleSimulation Simulation { get; }
        internal bool IsStarted { get; set; }
        internal bool ResultCreated { get; set; }
        internal long LastPublishedTick { get; set; }
        internal long LastEventId { get; set; }

        /// <summary>
        /// 创建 Worker 独占比赛状态。
        /// </summary>
        /// <param name="roomId">房间身份。</param>
        /// <param name="simulation">权威模拟。</param>
        internal MiniBomberWorkerMatchState(long roomId, MiniBomberBattleSimulation simulation)
        {
            RoomId = roomId;
            Simulation = simulation;
        }

        #endregion
    }

    /// <summary>
    /// RoomWorker 命令类型。
    /// </summary>
    internal enum MiniBomberRoomWorkerCommandType
    {
        Create,
        Start,
        Input,
        Online,
        Keyframe,
        Remove,
        Tick
    }

    /// <summary>
    /// 主线程投递给 RoomWorker 的值类型命令。
    /// </summary>
    internal readonly struct MiniBomberRoomWorkerCommand
    {
        #region Internal 内部成员

        internal MiniBomberRoomWorkerCommandType Type { get; }
        internal long RoomId { get; }
        internal long MatchId { get; }
        internal long PlayerId { get; }
        internal int DurationSeconds { get; }
        internal bool IsOnline { get; }
        internal MiniBomberBattleInput Input { get; }
        internal MiniBomberBattleMap Map { get; }
        internal MiniBomberBattleRules Rules { get; }
        internal IReadOnlyList<MiniBomberBattleParticipant> Participants { get; }
        internal string NetworkSessionId { get; }

        /// <summary>创建比赛命令。</summary>
        internal static MiniBomberRoomWorkerCommand Create(long roomId, long matchId, int durationSeconds, MiniBomberBattleMap map, MiniBomberBattleRules rules, IReadOnlyList<MiniBomberBattleParticipant> participants) => new MiniBomberRoomWorkerCommand(MiniBomberRoomWorkerCommandType.Create, roomId, matchId, 0, durationSeconds, false, default, map, rules, participants, null);
        /// <summary>创建开始比赛命令。</summary>
        internal static MiniBomberRoomWorkerCommand Start(long matchId) => new MiniBomberRoomWorkerCommand(MiniBomberRoomWorkerCommandType.Start, 0, matchId, 0, 0, false, default, null, null, null, null);
        /// <summary>创建玩家输入命令。</summary>
        internal static MiniBomberRoomWorkerCommand CreateInput(long matchId, long playerId, MiniBomberBattleInput input) => new MiniBomberRoomWorkerCommand(MiniBomberRoomWorkerCommandType.Input, 0, matchId, playerId, 0, false, input, null, null, null, null);
        /// <summary>创建在线状态命令。</summary>
        internal static MiniBomberRoomWorkerCommand Online(long matchId, long playerId, bool online) => new MiniBomberRoomWorkerCommand(MiniBomberRoomWorkerCommandType.Online, 0, matchId, playerId, 0, online, default, null, null, null, null);
        /// <summary>创建指定会话关键帧命令。</summary>
        internal static MiniBomberRoomWorkerCommand Keyframe(long matchId, string sessionId) => new MiniBomberRoomWorkerCommand(MiniBomberRoomWorkerCommandType.Keyframe, 0, matchId, 0, 0, false, default, null, null, null, sessionId);
        /// <summary>创建移除比赛命令。</summary>
        internal static MiniBomberRoomWorkerCommand Remove(long matchId) => new MiniBomberRoomWorkerCommand(MiniBomberRoomWorkerCommandType.Remove, 0, matchId, 0, 0, false, default, null, null, null, null);
        /// <summary>创建全 Worker 固定步命令。</summary>
        internal static MiniBomberRoomWorkerCommand Tick() => new MiniBomberRoomWorkerCommand(MiniBomberRoomWorkerCommandType.Tick, 0, 0, 0, 0, false, default, null, null, null, null);

        /// <summary>
        /// 创建完整 Worker 命令值。
        /// </summary>
        private MiniBomberRoomWorkerCommand(MiniBomberRoomWorkerCommandType type, long roomId, long matchId, long playerId, int durationSeconds, bool isOnline, MiniBomberBattleInput input, MiniBomberBattleMap map, MiniBomberBattleRules rules, IReadOnlyList<MiniBomberBattleParticipant> participants, string networkSessionId)
        {
            Type = type;
            RoomId = roomId;
            MatchId = matchId;
            PlayerId = playerId;
            DurationSeconds = durationSeconds;
            IsOnline = isOnline;
            Input = input;
            Map = map;
            Rules = rules;
            Participants = participants;
            NetworkSessionId = networkSessionId;
        }

        #endregion
    }

    /// <summary>
    /// 使用短锁保护的固定容量环形队列。
    /// </summary>
    internal sealed class MiniBomberBoundedQueue<T>
    {
        #region Private 私有成员

        private readonly object gate = new object(); // 保护跨线程读写。
        private readonly T[] items; // 固定容量存储。
        private int head; // 下一读取下标。
        private int tail; // 下一写入下标。
        private int count; // 当前元素数量。

        #endregion

        #region Internal 内部成员

        /// <summary>获取当前元素数量。</summary>
        internal int Count
        {
            get
            {
                lock (gate)
                {
                    return count;
                }
            }
        }

        /// <summary>获取队列当前是否已满。</summary>
        internal bool IsFull
        {
            get
            {
                lock (gate)
                {
                    return count == items.Length;
                }
            }
        }

        /// <summary>
        /// 创建固定容量队列。
        /// </summary>
        /// <param name="capacity">大于零的容量。</param>
        internal MiniBomberBoundedQueue(int capacity)
        {
            items = new T[capacity > 0 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity))];
        }

        /// <summary>
        /// 尝试追加元素。
        /// </summary>
        /// <param name="item">待追加元素。</param>
        /// <returns>存在剩余容量时返回 true。</returns>
        internal bool TryEnqueue(T item)
        {
            lock (gate)
            {
                if (count == items.Length)
                {
                    return false;
                }

                items[tail] = item;
                tail = (tail + 1) % items.Length;
                count++;
                return true;
            }
        }

        /// <summary>
        /// 尝试取出队首元素。
        /// </summary>
        /// <param name="item">取出的元素。</param>
        /// <returns>队列非空时返回 true。</returns>
        internal bool TryDequeue(out T item)
        {
            lock (gate)
            {
                if (count == 0)
                {
                    item = default;
                    return false;
                }

                item = items[head];
                items[head] = default;
                head = (head + 1) % items.Length;
                count--;
                return true;
            }
        }

        /// <summary>
        /// 从最新元素开始替换第一个满足条件的元素。
        /// </summary>
        /// <param name="predicate">替换匹配条件。</param>
        /// <param name="replacement">新元素。</param>
        /// <returns>完成替换时返回 true。</returns>
        internal bool TryReplaceLatest(Predicate<T> predicate, T replacement)
        {
            lock (gate)
            {
                for (int offset = 1; offset <= count; offset++)
                {
                    int index = (tail - offset + items.Length) % items.Length;
                    if (predicate(items[index]))
                    {
                        items[index] = replacement;
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// 清空队列并释放元素引用。
        /// </summary>
        internal void Clear()
        {
            lock (gate)
            {
                Array.Clear(items, 0, items.Length);
                head = 0;
                tail = 0;
                count = 0;
            }
        }

        #endregion
    }
}
