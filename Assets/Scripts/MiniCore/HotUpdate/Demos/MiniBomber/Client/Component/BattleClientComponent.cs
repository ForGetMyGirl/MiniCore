using System;
using System.Collections.Generic;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using MiniCore.Threading;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// MiniBomber 客户端战斗快照、即时事件、成绩和输入发送组件。
    /// </summary>
    public sealed class BattleClientComponent : AComponent
    {
        #region Private 私有成员

        private readonly List<MiniBomberBattleEventDto> recentEvents = new List<MiniBomberBattleEventDto>(32); // Presenter 消费的近期即时事件。
        private readonly MiniBomberBattleReplicationState replication = new MiniBomberBattleReplicationState(); // 纯 C# 基线和事件序列状态机。
        private INetworkService network; // 项目网络服务。
        private AccountSessionComponent account; // 当前账号会话。
        private long nextInputSequence = 1; // 下一个客户端输入序号。
        private bool resyncPending; // 是否已有完整关键帧请求在途。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 战斗快照变化事件。
        /// </summary>
        public event Action SnapshotChanged;

        /// <summary>
        /// 收到即时战斗事件时触发。
        /// </summary>
        public event Action EventsChanged;

        /// <summary>
        /// 收到服务器唯一比赛结果时触发。
        /// </summary>
        public event Action ResultChanged;

        /// <summary>
        /// 当前最新权威快照。
        /// </summary>
        public MiniBomberBattleSnapshot Snapshot => replication.Snapshot;

        /// <summary>
        /// 最新权威快照到达客户端的单调时间。
        /// </summary>
        public double LastSnapshotReceiveTime { get; private set; }

        /// <summary>
        /// 当前近期即时事件。
        /// </summary>
        public IReadOnlyList<MiniBomberBattleEventDto> RecentEvents => recentEvents;

        /// <summary>
        /// 当前比赛最终结果。
        /// </summary>
        public MiniBomberMatchResultNotice Result { get; private set; }

        /// <summary>
        /// 取得账号和网络依赖。
        /// </summary>
        public override void Awake()
        {
            network = Global.GetService<INetworkService>(this);
            account = Global.Get<AccountSessionComponent>(this);
        }

        /// <summary>
        /// 发送单个量化输入帧；客户端预测可以立即使用相同输入，最终以服务器快照校正。
        /// </summary>
        /// <param name="matchId">比赛身份。</param>
        /// <param name="clientTick">客户端本地 Tick。</param>
        /// <param name="moveX">负一千到一千的横向输入。</param>
        /// <param name="moveZ">负一千到一千的纵向输入。</param>
        /// <param name="placeBomb">本帧是否按下炸弹按钮。</param>
        /// <returns>网络队列接受状态。</returns>
        public NetworkSendResult SendInput(long matchId, long clientTick, int moveX, int moveZ, bool placeBomb)
        {
            var batch = new MiniBomberBattleInputBatch
            {
                PlayerId = account.PlayerId,
                MatchId = matchId
            };
            batch.Frames.Add(new MiniBomberInputFrameDto
            {
                Sequence = nextInputSequence++,
                ClientTick = clientTick,
                MoveX = Mathf.Clamp(moveX, -1000, 1000),
                MoveZ = Mathf.Clamp(moveZ, -1000, 1000),
                PlaceBomb = placeBomb
            });
            return network.TrySend(MiniBomberConstants.DefaultSessionId, batch);
        }

        /// <summary>
        /// 应用顺序更新的服务器权威快照。
        /// </summary>
        /// <param name="snapshot">服务器快照。</param>
        public void ApplySnapshot(MiniBomberBattleSnapshot snapshot)
        {
            if (replication.ApplyKeyframe(snapshot) != MiniBomberReplicationApplyResult.Applied)
            {
                return;
            }

            resyncPending = false;
            LastSnapshotReceiveTime = Global.Time.UnscaledTime;
            SnapshotChanged?.Invoke();
        }

        /// <summary>
        /// 应用房间级玩家动态增量，检测到基线丢失时自动请求完整关键帧。
        /// </summary>
        /// <param name="delta">服务器玩家动态增量。</param>
        public void ApplyDelta(MiniBomberBattleDelta delta)
        {
            MiniBomberReplicationApplyResult result = replication.ApplyDelta(delta);
            if (result == MiniBomberReplicationApplyResult.RequiresResync)
            {
                RequestResyncAsync(delta?.MatchId ?? Snapshot?.MatchId ?? 0).Forget();
                return;
            }

            if (result == MiniBomberReplicationApplyResult.Applied)
            {
                LastSnapshotReceiveTime = Global.Time.UnscaledTime;
                SnapshotChanged?.Invoke();
            }
        }

        /// <summary>
        /// 应用服务器即时事件批次并限制本地缓存长度。
        /// </summary>
        /// <param name="batch">即时事件批次。</param>
        public void ApplyEvents(MiniBomberBattleEventBatch batch)
        {
            MiniBomberReplicationApplyResult result = replication.ApplyEvents(batch);
            if (result == MiniBomberReplicationApplyResult.RequiresResync)
            {
                RequestResyncAsync(batch?.MatchId ?? Snapshot?.MatchId ?? 0).Forget();
                return;
            }

            if (result != MiniBomberReplicationApplyResult.Applied)
            {
                return;
            }

            recentEvents.AddRange(batch.Events);
            if (recentEvents.Count > 32)
            {
                recentEvents.RemoveRange(0, recentEvents.Count - 32);
            }

            EventsChanged?.Invoke();
        }

        /// <summary>
        /// 应用服务器最终成绩，客户端不重新排序或计算名次。
        /// </summary>
        /// <param name="result">服务器唯一成绩消息。</param>
        public void ApplyResult(MiniBomberMatchResultNotice result)
        {
            Result = result;
            ResultChanged?.Invoke();
        }

        /// <summary>
        /// 清空上一局客户端战斗状态。
        /// </summary>
        public void ResetBattle()
        {
            replication.Reset();
            LastSnapshotReceiveTime = 0d;
            Result = null;
            recentEvents.Clear();
            nextInputSequence = 1;
            resyncPending = false;
            SnapshotChanged?.Invoke();
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 清空战斗状态、事件和依赖引用。
        /// </summary>
        protected override void OnDispose()
        {
            SnapshotChanged = null;
            EventsChanged = null;
            ResultChanged = null;
            replication.Reset();
            LastSnapshotReceiveTime = 0d;
            Result = null;
            recentEvents.Clear();
            network = null;
            account = null;
            resyncPending = false;
            Global.ReleaseAll(this);
            base.OnDispose();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 合并重复请求并要求服务器随后发送完整战斗关键帧。
        /// </summary>
        /// <param name="matchId">需要重同步的比赛身份。</param>
        /// <returns>服务器接受请求后的任务。</returns>
        private async MTask RequestResyncAsync(long matchId)
        {
            if (resyncPending || matchId <= 0 || account == null || network == null)
            {
                return;
            }

            resyncPending = true;
            MiniBomberBattleSnapshot snapshot = Snapshot;
            MiniBomberBattleResyncResponse response = await network.CallAsync<MiniBomberBattleResyncRequest, MiniBomberBattleResyncResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberBattleResyncRequest
            {
                PlayerId = account.PlayerId,
                MatchId = matchId,
                KnownServerTick = snapshot?.ServerTick ?? 0,
                KnownRevision = snapshot?.Revision ?? 0,
                KnownEventId = replication.LastEventId
            });
            if (response == null || response.Code != MiniBomberErrorCode.Success)
            {
                resyncPending = false;
                return;
            }

            if (response.Snapshot != null)
            {
                ApplySnapshot(response.Snapshot);
            }
        }

        #endregion
    }
}
