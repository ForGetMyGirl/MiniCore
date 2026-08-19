using MiniCore.Protocol.Generated;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 将 MiniBomber Protobuf 消息同步复制到客户端长期 Model。
    /// </summary>
    internal static class MiniBomberProtocolModelMapper
    {
        #region Internal 内部成员

        /// <summary>
        /// 将协议目的地转换为客户端业务目的地。
        /// </summary>
        /// <param name="value">协议目的地。</param>
        /// <returns>客户端业务目的地。</returns>
        internal static MiniBomberClientDestinationKind MapDestination(MiniBomberClientDestination value)
        {
            switch (value)
            {
                case MiniBomberClientDestination.MiniBomberDestinationLobby:
                    return MiniBomberClientDestinationKind.Lobby;
                case MiniBomberClientDestination.MiniBomberDestinationRoom:
                    return MiniBomberClientDestinationKind.Room;
                case MiniBomberClientDestination.MiniBomberDestinationBattle:
                    return MiniBomberClientDestinationKind.Battle;
                case MiniBomberClientDestination.MiniBomberDestinationResult:
                    return MiniBomberClientDestinationKind.Result;
                default:
                    return MiniBomberClientDestinationKind.Login;
            }
        }

        /// <summary>
        /// 将协议房间状态转换为客户端业务状态。
        /// </summary>
        /// <param name="value">协议房间状态。</param>
        /// <returns>客户端房间状态。</returns>
        internal static MiniBomberRoomStatus MapRoomStatus(MiniBomberRoomState value)
        {
            switch (value)
            {
                case MiniBomberRoomState.MiniBomberRoomLoading:
                    return MiniBomberRoomStatus.Loading;
                case MiniBomberRoomState.MiniBomberRoomBattle:
                    return MiniBomberRoomStatus.Battle;
                case MiniBomberRoomState.MiniBomberRoomResult:
                    return MiniBomberRoomStatus.Result;
                default:
                    return MiniBomberRoomStatus.Waiting;
            }
        }

        /// <summary>
        /// 将协议战斗事件类型转换为客户端业务类型。
        /// </summary>
        /// <param name="value">协议事件类型。</param>
        /// <returns>客户端事件类型。</returns>
        internal static MiniBomberBattleEventKind MapBattleEventKind(MiniBomberBattleEventType value)
        {
            switch (value)
            {
                case MiniBomberBattleEventType.MiniBomberEventBombPlaced:
                    return MiniBomberBattleEventKind.BombPlaced;
                case MiniBomberBattleEventType.MiniBomberEventExplosionStarted:
                    return MiniBomberBattleEventKind.ExplosionStarted;
                case MiniBomberBattleEventType.MiniBomberEventBlockDestroyed:
                    return MiniBomberBattleEventKind.BlockDestroyed;
                case MiniBomberBattleEventType.MiniBomberEventPickupSpawned:
                    return MiniBomberBattleEventKind.PickupSpawned;
                case MiniBomberBattleEventType.MiniBomberEventPickupCollected:
                    return MiniBomberBattleEventKind.PickupCollected;
                case MiniBomberBattleEventType.MiniBomberEventPlayerKilled:
                    return MiniBomberBattleEventKind.PlayerKilled;
                case MiniBomberBattleEventType.MiniBomberEventPlayerRespawned:
                    return MiniBomberBattleEventKind.PlayerRespawned;
                case MiniBomberBattleEventType.MiniBomberEventScoreChanged:
                    return MiniBomberBattleEventKind.ScoreChanged;
                default:
                    return MiniBomberBattleEventKind.None;
            }
        }

        /// <summary>
        /// 将协议道具类型转换为客户端业务类型。
        /// </summary>
        /// <param name="value">协议道具类型。</param>
        /// <returns>客户端道具类型。</returns>
        internal static MiniBomberPickupKind MapPickupKind(MiniBomberPickupType value)
        {
            switch (value)
            {
                case MiniBomberPickupType.MiniBomberPickupBombCount:
                    return MiniBomberPickupKind.BombCount;
                case MiniBomberPickupType.MiniBomberPickupBombRange:
                    return MiniBomberPickupKind.BombRange;
                default:
                    return MiniBomberPickupKind.None;
            }
        }

        /// <summary>
        /// 创建并填充协议无关房间 Model。
        /// </summary>
        /// <param name="source">协议房间快照。</param>
        /// <returns>协议无关房间 Model；源为空时返回 null。</returns>
        internal static MiniBomberRoomModel CreateRoom(MiniBomberRoomSnapshotDto source)
        {
            if (source == null)
            {
                return null;
            }

            MiniBomberRoomModel target = new MiniBomberRoomModel();
            ApplyRoom(source, target);
            return target;
        }

        /// <summary>
        /// 将协议房间快照复制到现有房间 Model。
        /// </summary>
        /// <param name="source">协议房间快照。</param>
        /// <param name="target">目标房间 Model。</param>
        internal static void ApplyRoom(MiniBomberRoomSnapshotDto source, MiniBomberRoomModel target)
        {
            target.RoomId = source.RoomId;
            target.RoomName = source.RoomName ?? string.Empty;
            target.OwnerPlayerId = source.OwnerPlayerId;
            target.DurationSeconds = source.DurationSeconds;
            target.Status = MapRoomStatus(source.State);
            target.Revision = source.Revision;
            target.MatchId = source.MatchId;

            target.MutableMembers.Clear();
            for (int index = 0; index < source.Members.Count; index++)
            {
                MiniBomberRoomMemberDto item = source.Members[index];
                target.MutableMembers.Add(new MiniBomberRoomMemberModel
                {
                    PlayerId = item.PlayerId,
                    PlayerName = item.PlayerName ?? string.Empty,
                    IsOwner = item.IsOwner,
                    IsReady = item.IsReady,
                    IsOnline = item.IsOnline,
                    Score = item.Score
                });
            }
        }

        /// <summary>
        /// 将协议大厅房间摘要复制到现有 Model。
        /// </summary>
        /// <param name="source">协议房间摘要。</param>
        /// <param name="target">目标房间摘要 Model。</param>
        internal static void ApplyLobbyRoom(MiniBomberRoomSummaryDto source, MiniBomberLobbyRoomModel target)
        {
            target.RoomId = source.RoomId;
            target.RoomName = source.RoomName ?? string.Empty;
            target.OwnerName = source.OwnerName ?? string.Empty;
            target.PlayerCount = source.PlayerCount;
            target.MaxPlayerCount = source.MaxPlayerCount;
            target.DurationSeconds = source.DurationSeconds;
            target.Status = MapRoomStatus(source.State);
            target.Revision = source.Revision;
        }

        /// <summary>
        /// 将协议战斗玩家复制到现有 Model。
        /// </summary>
        /// <param name="source">协议玩家数据。</param>
        /// <param name="target">目标玩家 Model。</param>
        internal static void ApplyBattlePlayer(MiniBomberBattlePlayerDto source, MiniBomberBattlePlayerModel target)
        {
            target.PlayerId = source.PlayerId;
            target.PlayerName = source.PlayerName ?? string.Empty;
            target.PositionXMillimeters = source.PositionXMillimeters;
            target.PositionZMillimeters = source.PositionZMillimeters;
            target.FacingX = source.FacingX;
            target.FacingZ = source.FacingZ;
            target.IsAlive = source.IsAlive;
            target.RespawnTick = source.RespawnTick;
            target.InvulnerableUntilTick = source.InvulnerableUntilTick;
            target.Score = source.Score;
            target.Kills = source.Kills;
            target.Deaths = source.Deaths;
            target.BombCapacity = source.BombCapacity;
            target.BombRange = source.BombRange;
            target.AcknowledgedInputSequence = source.AcknowledgedInputSequence;
            target.IsOnline = source.IsOnline;
        }

        /// <summary>
        /// 将协议炸弹复制到现有 Model。
        /// </summary>
        /// <param name="source">协议炸弹数据。</param>
        /// <param name="target">目标炸弹 Model。</param>
        internal static void ApplyBattleBomb(MiniBomberBattleBombDto source, MiniBomberBattleBombModel target)
        {
            target.BombId = source.BombId;
            target.OwnerPlayerId = source.OwnerPlayerId;
            target.CellX = source.CellX;
            target.CellZ = source.CellZ;
            target.Range = source.Range;
            target.ExplodeTick = source.ExplodeTick;
            target.OwnerCanPass = source.OwnerCanPass;
        }

        /// <summary>
        /// 将协议道具复制到现有 Model。
        /// </summary>
        /// <param name="source">协议道具数据。</param>
        /// <param name="target">目标道具 Model。</param>
        internal static void ApplyBattlePickup(MiniBomberBattlePickupDto source, MiniBomberBattlePickupModel target)
        {
            target.PickupId = source.PickupId;
            target.Kind = MapPickupKind(source.Type);
            target.CellX = source.CellX;
            target.CellZ = source.CellZ;
        }

        /// <summary>
        /// 将协议即时事件复制到现有 Model。
        /// </summary>
        /// <param name="source">协议即时事件。</param>
        /// <param name="target">目标事件 Model。</param>
        internal static void ApplyBattleEvent(MiniBomberBattleEventDto source, MiniBomberBattleEventModel target)
        {
            target.EventId = source.EventId;
            target.Kind = MapBattleEventKind(source.Type);
            target.ServerTick = source.ServerTick;
            target.ActorPlayerId = source.ActorPlayerId;
            target.TargetPlayerId = source.TargetPlayerId;
            target.EntityId = source.EntityId;
            target.CellX = source.CellX;
            target.CellZ = source.CellZ;
            target.PickupKind = MapPickupKind(source.PickupType);
            target.ActorName = source.ActorName ?? string.Empty;
            target.TargetName = source.TargetName ?? string.Empty;
            target.Score = source.Score;
            target.Kills = source.Kills;
            target.Deaths = source.Deaths;
        }

        #endregion
    }
}
