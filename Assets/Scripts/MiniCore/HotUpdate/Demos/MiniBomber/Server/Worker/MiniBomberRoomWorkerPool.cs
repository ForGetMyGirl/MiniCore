using System;
using System.Collections.Generic;
using System.Threading;
using Google.Protobuf;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

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

        /// <summary>
        /// 获取固定 Worker 数量。
        /// </summary>
        public int WorkerCount => workers.Length;

        /// <summary>
        /// 获取池是否已经完成释放。
        /// </summary>
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
}
