using System;
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

        private readonly MiniBomberBattleModel model = new MiniBomberBattleModel(); // 当前战斗长期业务数据。
        private readonly MiniBomberBattleReplicationReducer replication; // PB 到 Model 的复制归并器。
        private INetworkService network; // 项目网络服务。
        private AccountSessionComponent account; // 当前账号会话。
        private long nextInputSequence = 1; // 下一个客户端输入序号。
        private bool resyncPending; // 是否已有完整关键帧请求在途。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 创建客户端战斗状态组件并绑定长期 Model 归并器。
        /// </summary>
        public BattleClientComponent()
        {
            replication = new MiniBomberBattleReplicationReducer(model);
        }

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
        /// 获取当前战斗的只读业务数据。
        /// </summary>
        public MiniBomberBattleModel Model => model;

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
                PlayerId = account.Model.PlayerId,
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
            model.LastSnapshotReceiveTime = Global.Time.UnscaledTime;
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
                RequestResyncAsync(delta?.MatchId ?? model.MatchId).Forget();
                return;
            }

            if (result == MiniBomberReplicationApplyResult.Applied)
            {
                model.LastSnapshotReceiveTime = Global.Time.UnscaledTime;
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
                RequestResyncAsync(batch?.MatchId ?? model.MatchId).Forget();
                return;
            }

            if (result != MiniBomberReplicationApplyResult.Applied)
            {
                return;
            }

            EventsChanged?.Invoke();
        }

        /// <summary>
        /// 应用服务器最终成绩，客户端不重新排序或计算名次。
        /// </summary>
        /// <param name="result">服务器唯一成绩消息。</param>
        public void ApplyResult(MiniBomberMatchResultNotice result)
        {
            if (result == null)
            {
                return;
            }

            MiniBomberMatchResultModel target = model.Result ?? new MiniBomberMatchResultModel();
            target.RoomId = result.RoomId;
            target.MatchId = result.MatchId;
            target.ReturnToRoomMilliseconds = result.ReturnToRoomMilliseconds;
            while (target.MutableEntries.Count < result.Results.Count)
            {
                target.MutableEntries.Add(new MiniBomberMatchResultEntryModel());
            }

            for (int index = 0; index < result.Results.Count; index++)
            {
                MiniBomberMatchResultEntryDto source = result.Results[index];
                MiniBomberMatchResultEntryModel entry = target.MutableEntries[index];
                entry.Rank = source.Rank;
                entry.PlayerId = source.PlayerId;
                entry.PlayerName = source.PlayerName ?? string.Empty;
                entry.Score = source.Score;
                entry.Kills = source.Kills;
                entry.Deaths = source.Deaths;
                entry.IsOnline = source.IsOnline;
            }

            if (target.MutableEntries.Count > result.Results.Count)
            {
                target.MutableEntries.RemoveRange(result.Results.Count, target.MutableEntries.Count - result.Results.Count);
            }

            model.Result = target;
            ResultChanged?.Invoke();
        }

        /// <summary>
        /// 清空上一局客户端战斗状态。
        /// </summary>
        public void ResetBattle()
        {
            replication.Reset();
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
            MiniBomberBattleResyncResponse response = await network.CallAsync<MiniBomberBattleResyncRequest, MiniBomberBattleResyncResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberBattleResyncRequest
            {
                PlayerId = account.Model.PlayerId,
                MatchId = matchId,
                KnownServerTick = model.ServerTick,
                KnownRevision = model.Revision,
                KnownEventId = model.LastEventId
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
