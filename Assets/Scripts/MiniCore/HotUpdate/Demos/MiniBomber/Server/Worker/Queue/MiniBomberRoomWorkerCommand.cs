using System;
using System.Collections.Generic;
using System.Threading;
using Google.Protobuf;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

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

        /// <summary>
        /// 创建比赛命令。
        /// </summary>
        internal static MiniBomberRoomWorkerCommand Create(long roomId, long matchId, int durationSeconds, MiniBomberBattleMap map, MiniBomberBattleRules rules, IReadOnlyList<MiniBomberBattleParticipant> participants) => new MiniBomberRoomWorkerCommand(MiniBomberRoomWorkerCommandType.Create, roomId, matchId, 0, durationSeconds, false, default, map, rules, participants, null);
        /// <summary>
        /// 创建开始比赛命令。
        /// </summary>
        internal static MiniBomberRoomWorkerCommand Start(long matchId) => new MiniBomberRoomWorkerCommand(MiniBomberRoomWorkerCommandType.Start, 0, matchId, 0, 0, false, default, null, null, null, null);
        /// <summary>
        /// 创建玩家输入命令。
        /// </summary>
        internal static MiniBomberRoomWorkerCommand CreateInput(long matchId, long playerId, MiniBomberBattleInput input) => new MiniBomberRoomWorkerCommand(MiniBomberRoomWorkerCommandType.Input, 0, matchId, playerId, 0, false, input, null, null, null, null);
        /// <summary>
        /// 创建在线状态命令。
        /// </summary>
        internal static MiniBomberRoomWorkerCommand Online(long matchId, long playerId, bool online) => new MiniBomberRoomWorkerCommand(MiniBomberRoomWorkerCommandType.Online, 0, matchId, playerId, 0, online, default, null, null, null, null);
        /// <summary>
        /// 创建指定会话关键帧命令。
        /// </summary>
        internal static MiniBomberRoomWorkerCommand Keyframe(long matchId, string sessionId) => new MiniBomberRoomWorkerCommand(MiniBomberRoomWorkerCommandType.Keyframe, 0, matchId, 0, 0, false, default, null, null, null, sessionId);
        /// <summary>
        /// 创建移除比赛命令。
        /// </summary>
        internal static MiniBomberRoomWorkerCommand Remove(long matchId) => new MiniBomberRoomWorkerCommand(MiniBomberRoomWorkerCommandType.Remove, 0, matchId, 0, 0, false, default, null, null, null, null);
        /// <summary>
        /// 创建全 Worker 固定步命令。
        /// </summary>
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
}
