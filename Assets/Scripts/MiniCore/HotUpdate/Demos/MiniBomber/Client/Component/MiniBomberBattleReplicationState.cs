using MiniCore.Protocol.Generated;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 不依赖 Unity 和网络服务的 MiniBomber 客户端复制状态机。
    /// </summary>
    public sealed class MiniBomberBattleReplicationState
    {
        #region Public 公共成员

        /// <summary>
        /// 当前可供表现层读取的权威状态。
        /// </summary>
        public MiniBomberBattleSnapshot Snapshot { get; private set; }

        /// <summary>
        /// 最后连续应用的可靠事件编号。
        /// </summary>
        public long LastEventId { get; private set; }

        /// <summary>
        /// 应用完整关键帧并重建所有同步基线。
        /// </summary>
        /// <param name="snapshot">服务器完整关键帧。</param>
        /// <returns>新关键帧应用结果。</returns>
        public MiniBomberReplicationApplyResult ApplyKeyframe(MiniBomberBattleSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return MiniBomberReplicationApplyResult.Ignored;
            }

            if (Snapshot != null && snapshot.MatchId == Snapshot.MatchId && snapshot.Revision < Snapshot.Revision)
            {
                return MiniBomberReplicationApplyResult.Ignored;
            }

            Snapshot = snapshot.Clone();
            LastEventId = snapshot.LastEventId;
            return MiniBomberReplicationApplyResult.Applied;
        }

        /// <summary>
        /// 仅在 BaseTick 与当前关键帧或增量基线连续时应用玩家动态增量。
        /// </summary>
        /// <param name="delta">房间级公共玩家动态增量。</param>
        /// <returns>应用、忽略或请求重同步结果。</returns>
        public MiniBomberReplicationApplyResult ApplyDelta(MiniBomberBattleDelta delta)
        {
            if (delta == null)
            {
                return MiniBomberReplicationApplyResult.Ignored;
            }

            if (Snapshot == null || delta.MatchId != Snapshot.MatchId)
            {
                return MiniBomberReplicationApplyResult.RequiresResync;
            }

            if (delta.Revision <= Snapshot.Revision || delta.ServerTick <= Snapshot.ServerTick)
            {
                return MiniBomberReplicationApplyResult.Ignored;
            }

            if (delta.BaseTick != Snapshot.ServerTick)
            {
                return MiniBomberReplicationApplyResult.RequiresResync;
            }

            Snapshot.Players.Clear();
            for (int index = 0; index < delta.Players.Count; index++)
            {
                Snapshot.Players.Add(delta.Players[index].Clone());
            }

            Snapshot.ServerTick = delta.ServerTick;
            Snapshot.Revision = delta.Revision;
            Snapshot.RemainingMilliseconds = delta.RemainingMilliseconds;
            return MiniBomberReplicationApplyResult.Applied;
        }

        /// <summary>
        /// 校验并应用可靠事件序列；缺号或乱序时拒绝修改本地状态。
        /// </summary>
        /// <param name="batch">有序可靠事件批次。</param>
        /// <returns>应用、忽略或请求重同步结果。</returns>
        public MiniBomberReplicationApplyResult ApplyEvents(MiniBomberBattleEventBatch batch)
        {
            if (batch == null || batch.Events.Count == 0)
            {
                return MiniBomberReplicationApplyResult.Ignored;
            }

            if (Snapshot == null || batch.MatchId != Snapshot.MatchId)
            {
                return MiniBomberReplicationApplyResult.RequiresResync;
            }

            long lastBatchEventId = batch.Events[batch.Events.Count - 1].EventId;
            if (lastBatchEventId <= LastEventId)
            {
                return MiniBomberReplicationApplyResult.Ignored;
            }

            if (batch.PreviousEventId != LastEventId)
            {
                return MiniBomberReplicationApplyResult.RequiresResync;
            }

            long expectedEventId = LastEventId + 1;
            for (int index = 0; index < batch.Events.Count; index++)
            {
                MiniBomberBattleEventDto battleEvent = batch.Events[index];
                if (battleEvent.EventId != expectedEventId)
                {
                    return MiniBomberReplicationApplyResult.RequiresResync;
                }

                expectedEventId++;
            }

            for (int index = 0; index < batch.Events.Count; index++)
            {
                ApplyEvent(batch.Events[index]);
            }

            LastEventId = lastBatchEventId;
            Snapshot.LastEventId = LastEventId;
            return MiniBomberReplicationApplyResult.Applied;
        }

        /// <summary>
        /// 清空当前比赛与全部同步基线。
        /// </summary>
        public void Reset()
        {
            Snapshot = null;
            LastEventId = 0;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将单个可靠事件投影到当前客户端状态。
        /// </summary>
        /// <param name="battleEvent">顺序已验证的事件。</param>
        private void ApplyEvent(MiniBomberBattleEventDto battleEvent)
        {
            switch (battleEvent.Type)
            {
                case MiniBomberBattleEventType.MiniBomberEventBombPlaced:
                    if (FindBomb(battleEvent.EntityId) == null)
                    {
                        Snapshot.Bombs.Add(new MiniBomberBattleBombDto
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
                    if (FindPlayer(battleEvent.TargetPlayerId) is MiniBomberBattlePlayerDto killed)
                    {
                        killed.IsAlive = false;
                    }
                    break;
                case MiniBomberBattleEventType.MiniBomberEventPlayerRespawned:
                    if (FindPlayer(battleEvent.TargetPlayerId) is MiniBomberBattlePlayerDto respawned)
                    {
                        respawned.IsAlive = true;
                    }
                    break;
                case MiniBomberBattleEventType.MiniBomberEventScoreChanged:
                    if (FindPlayer(battleEvent.TargetPlayerId) is MiniBomberBattlePlayerDto scored)
                    {
                        scored.Score = battleEvent.Score;
                        scored.Kills = battleEvent.Kills;
                        scored.Deaths = battleEvent.Deaths;
                    }
                    break;
            }
        }

        /// <summary>
        /// 查找当前客户端玩家状态。
        /// </summary>
        /// <param name="playerId">玩家身份。</param>
        /// <returns>找到的状态；不存在时返回 null。</returns>
        private MiniBomberBattlePlayerDto FindPlayer(long playerId)
        {
            for (int index = 0; index < Snapshot.Players.Count; index++)
            {
                if (Snapshot.Players[index].PlayerId == playerId)
                {
                    return Snapshot.Players[index];
                }
            }

            return null;
        }

        /// <summary>
        /// 查找当前客户端炸弹状态。
        /// </summary>
        /// <param name="bombId">炸弹身份。</param>
        /// <returns>找到的状态；不存在时返回 null。</returns>
        private MiniBomberBattleBombDto FindBomb(long bombId)
        {
            for (int index = 0; index < Snapshot.Bombs.Count; index++)
            {
                if (Snapshot.Bombs[index].BombId == bombId)
                {
                    return Snapshot.Bombs[index];
                }
            }

            return null;
        }

        /// <summary>
        /// 从当前客户端状态移除已经爆炸的炸弹。
        /// </summary>
        /// <param name="bombId">炸弹身份。</param>
        private void RemoveBomb(long bombId)
        {
            for (int index = Snapshot.Bombs.Count - 1; index >= 0; index--)
            {
                if (Snapshot.Bombs[index].BombId == bombId)
                {
                    Snapshot.Bombs.RemoveAt(index);
                }
            }
        }

        #endregion
    }
}
