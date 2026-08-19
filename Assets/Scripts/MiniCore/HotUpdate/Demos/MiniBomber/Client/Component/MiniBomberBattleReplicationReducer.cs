using System;
using System.Collections.Generic;
using Google.Protobuf;
using MiniCore.Protocol.Generated;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 将战斗 PB 关键帧、增量和事件归并到客户端长期 Model。
    /// </summary>
    internal sealed class MiniBomberBattleReplicationReducer
    {
        #region Private 私有成员

        private readonly MiniBomberBattleModel model; // 由战斗组件拥有的长期 Model。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建战斗复制归并器。
        /// </summary>
        /// <param name="model">由战斗组件拥有的目标 Model。</param>
        internal MiniBomberBattleReplicationReducer(MiniBomberBattleModel model)
        {
            this.model = model;
        }

        /// <summary>
        /// 应用完整关键帧并重建全部长期 Model 基线。
        /// </summary>
        /// <param name="snapshot">服务器完整关键帧。</param>
        /// <returns>关键帧应用结果。</returns>
        internal MiniBomberReplicationApplyResult ApplyKeyframe(MiniBomberBattleSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return MiniBomberReplicationApplyResult.Ignored;
            }

            if (model.HasSnapshot && snapshot.MatchId == model.MatchId && snapshot.Revision < model.Revision)
            {
                return MiniBomberReplicationApplyResult.Ignored;
            }

            bool matchChanged = model.MatchId != snapshot.MatchId;
            model.MatchId = snapshot.MatchId;
            model.ServerTick = snapshot.ServerTick;
            model.RemainingMilliseconds = snapshot.RemainingMilliseconds;
            model.Revision = snapshot.Revision;
            model.LastEventId = snapshot.LastEventId;
            CopyDestroyedBreakableCells(snapshot.DestroyedBreakableCells);
            ApplyPlayers(snapshot.Players);
            ApplyBombs(snapshot.Bombs);
            ApplyPickups(snapshot.Pickups);
            if (matchChanged)
            {
                model.MutableRecentEvents.Clear();
                model.Result = null;
                model.EventRevision++;
            }

            model.RankingRevision++;
            return MiniBomberReplicationApplyResult.Applied;
        }

        /// <summary>
        /// 在基线连续时应用战斗玩家增量。
        /// </summary>
        /// <param name="delta">服务器玩家动态增量。</param>
        /// <returns>增量应用结果。</returns>
        internal MiniBomberReplicationApplyResult ApplyDelta(MiniBomberBattleDelta delta)
        {
            if (delta == null)
            {
                return MiniBomberReplicationApplyResult.Ignored;
            }

            if (!model.HasSnapshot || delta.MatchId != model.MatchId)
            {
                return MiniBomberReplicationApplyResult.RequiresResync;
            }

            if (delta.Revision <= model.Revision || delta.ServerTick <= model.ServerTick)
            {
                return MiniBomberReplicationApplyResult.Ignored;
            }

            if (delta.BaseTick != model.ServerTick)
            {
                return MiniBomberReplicationApplyResult.RequiresResync;
            }

            ApplyPlayers(delta.Players);
            model.ServerTick = delta.ServerTick;
            model.Revision = delta.Revision;
            model.RemainingMilliseconds = delta.RemainingMilliseconds;
            model.RankingRevision++;
            return MiniBomberReplicationApplyResult.Applied;
        }

        /// <summary>
        /// 校验并应用可靠战斗事件序列。
        /// </summary>
        /// <param name="batch">服务器即时事件批次。</param>
        /// <returns>事件批次应用结果。</returns>
        internal MiniBomberReplicationApplyResult ApplyEvents(MiniBomberBattleEventBatch batch)
        {
            if (batch == null || batch.Events.Count == 0)
            {
                return MiniBomberReplicationApplyResult.Ignored;
            }

            if (!model.HasSnapshot || batch.MatchId != model.MatchId)
            {
                return MiniBomberReplicationApplyResult.RequiresResync;
            }

            long lastBatchEventId = batch.Events[batch.Events.Count - 1].EventId;
            if (lastBatchEventId <= model.LastEventId)
            {
                return MiniBomberReplicationApplyResult.Ignored;
            }

            if (batch.PreviousEventId != model.LastEventId)
            {
                return MiniBomberReplicationApplyResult.RequiresResync;
            }

            long expectedEventId = model.LastEventId + 1;
            for (int index = 0; index < batch.Events.Count; index++)
            {
                if (batch.Events[index].EventId != expectedEventId++)
                {
                    return MiniBomberReplicationApplyResult.RequiresResync;
                }
            }

            for (int index = 0; index < batch.Events.Count; index++)
            {
                MiniBomberBattleEventDto source = batch.Events[index];
                ApplyEventToSnapshot(source);
                MiniBomberBattleEventModel target;
                if (model.MutableRecentEvents.Count >= 32)
                {
                    target = model.MutableRecentEvents[0];
                    model.MutableRecentEvents.RemoveAt(0);
                }
                else
                {
                    target = new MiniBomberBattleEventModel();
                }

                MiniBomberProtocolModelMapper.ApplyBattleEvent(source, target);
                model.MutableRecentEvents.Add(target);
            }

            model.LastEventId = lastBatchEventId;
            model.EventRevision++;
            return MiniBomberReplicationApplyResult.Applied;
        }

        /// <summary>
        /// 清空战斗 Model 并保留内部集合容量。
        /// </summary>
        internal void Reset()
        {
            model.MatchId = 0;
            model.ServerTick = 0;
            model.RemainingMilliseconds = 0;
            model.Revision = 0;
            model.LastEventId = 0;
            model.LastSnapshotReceiveTime = 0d;
            model.RankingRevision++;
            model.EventRevision++;
            model.MutableDestroyedBreakableCells = Array.Empty<byte>();
            model.Result = null;
            model.MutablePlayers.Clear();
            model.MutableBombs.Clear();
            model.MutablePickups.Clear();
            model.MutableRecentEvents.Clear();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将协议位图复制到可复用的客户端字节数组。
        /// </summary>
        /// <param name="source">服务器被摧毁木箱位图。</param>
        private void CopyDestroyedBreakableCells(ByteString source)
        {
            if (source == null || source.Length == 0)
            {
                model.MutableDestroyedBreakableCells = Array.Empty<byte>();
                return;
            }

            if (model.MutableDestroyedBreakableCells.Length != source.Length)
            {
                model.MutableDestroyedBreakableCells = new byte[source.Length];
            }

            source.CopyTo(model.MutableDestroyedBreakableCells, 0);
        }

        /// <summary>
        /// 复用现有玩家 Model 并复制协议玩家列表。
        /// </summary>
        /// <param name="source">协议玩家列表。</param>
        private void ApplyPlayers(IList<MiniBomberBattlePlayerDto> source)
        {
            EnsureCount(model.MutablePlayers, source.Count);
            for (int index = 0; index < source.Count; index++)
            {
                MiniBomberProtocolModelMapper.ApplyBattlePlayer(source[index], model.MutablePlayers[index]);
            }

            TrimExcess(model.MutablePlayers, source.Count);
        }

        /// <summary>
        /// 复用现有炸弹 Model 并复制协议炸弹列表。
        /// </summary>
        /// <param name="source">协议炸弹列表。</param>
        private void ApplyBombs(IList<MiniBomberBattleBombDto> source)
        {
            EnsureCount(model.MutableBombs, source.Count);
            for (int index = 0; index < source.Count; index++)
            {
                MiniBomberProtocolModelMapper.ApplyBattleBomb(source[index], model.MutableBombs[index]);
            }

            TrimExcess(model.MutableBombs, source.Count);
        }

        /// <summary>
        /// 复用现有道具 Model 并复制协议道具列表。
        /// </summary>
        /// <param name="source">协议道具列表。</param>
        private void ApplyPickups(IList<MiniBomberBattlePickupDto> source)
        {
            EnsureCount(model.MutablePickups, source.Count);
            for (int index = 0; index < source.Count; index++)
            {
                MiniBomberProtocolModelMapper.ApplyBattlePickup(source[index], model.MutablePickups[index]);
            }

            TrimExcess(model.MutablePickups, source.Count);
        }

        /// <summary>
        /// 把单个可靠事件投影到当前战斗 Model。
        /// </summary>
        /// <param name="battleEvent">顺序已经验证的协议事件。</param>
        private void ApplyEventToSnapshot(MiniBomberBattleEventDto battleEvent)
        {
            switch (battleEvent.Type)
            {
                case MiniBomberBattleEventType.MiniBomberEventBombPlaced:
                    if (FindBomb(battleEvent.EntityId) == null)
                    {
                        model.MutableBombs.Add(new MiniBomberBattleBombModel
                        {
                            BombId = battleEvent.EntityId,
                            OwnerPlayerId = battleEvent.ActorPlayerId,
                            CellX = battleEvent.CellX,
                            CellZ = battleEvent.CellZ
                        });
                    }
                    break;
                case MiniBomberBattleEventType.MiniBomberEventExplosionStarted:
                    RemoveBomb(battleEvent.EntityId);
                    break;
                case MiniBomberBattleEventType.MiniBomberEventPlayerKilled:
                    if (FindPlayer(battleEvent.TargetPlayerId) is MiniBomberBattlePlayerModel killed)
                    {
                        killed.IsAlive = false;
                    }
                    break;
                case MiniBomberBattleEventType.MiniBomberEventPlayerRespawned:
                    if (FindPlayer(battleEvent.TargetPlayerId) is MiniBomberBattlePlayerModel respawned)
                    {
                        respawned.IsAlive = true;
                    }
                    break;
                case MiniBomberBattleEventType.MiniBomberEventScoreChanged:
                    if (FindPlayer(battleEvent.TargetPlayerId) is MiniBomberBattlePlayerModel scored)
                    {
                        scored.Score = battleEvent.Score;
                        scored.Kills = battleEvent.Kills;
                        scored.Deaths = battleEvent.Deaths;
                        model.RankingRevision++;
                    }
                    break;
            }
        }

        /// <summary>
        /// 查找指定玩家 Model。
        /// </summary>
        /// <param name="playerId">玩家标识。</param>
        /// <returns>匹配玩家；不存在时返回 null。</returns>
        private MiniBomberBattlePlayerModel FindPlayer(long playerId)
        {
            for (int index = 0; index < model.MutablePlayers.Count; index++)
            {
                if (model.MutablePlayers[index].PlayerId == playerId)
                {
                    return model.MutablePlayers[index];
                }
            }

            return null;
        }

        /// <summary>
        /// 查找指定炸弹 Model。
        /// </summary>
        /// <param name="bombId">炸弹标识。</param>
        /// <returns>匹配炸弹；不存在时返回 null。</returns>
        private MiniBomberBattleBombModel FindBomb(long bombId)
        {
            for (int index = 0; index < model.MutableBombs.Count; index++)
            {
                if (model.MutableBombs[index].BombId == bombId)
                {
                    return model.MutableBombs[index];
                }
            }

            return null;
        }

        /// <summary>
        /// 移除已经爆炸的炸弹 Model。
        /// </summary>
        /// <param name="bombId">炸弹标识。</param>
        private void RemoveBomb(long bombId)
        {
            for (int index = model.MutableBombs.Count - 1; index >= 0; index--)
            {
                if (model.MutableBombs[index].BombId == bombId)
                {
                    model.MutableBombs.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// 将目标 Model 列表扩展到指定数量。
        /// </summary>
        /// <typeparam name="T">具有公共无参构造函数的 Model 类型。</typeparam>
        /// <param name="list">目标复用列表。</param>
        /// <param name="count">所需元素数量。</param>
        private static void EnsureCount<T>(List<T> list, int count) where T : new()
        {
            while (list.Count < count)
            {
                list.Add(new T());
            }
        }

        /// <summary>
        /// 移除目标 Model 列表末尾的多余元素。
        /// </summary>
        /// <typeparam name="T">Model 类型。</typeparam>
        /// <param name="list">目标复用列表。</param>
        /// <param name="count">保留元素数量。</param>
        private static void TrimExcess<T>(List<T> list, int count)
        {
            if (list.Count > count)
            {
                list.RemoveRange(count, list.Count - count);
            }
        }

        #endregion
    }
}
