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
    /// MiniBomber 客户端当前房间权威快照和房间命令组件。
    /// </summary>
    public sealed class RoomComponent : AComponent
    {
        #region Private 私有成员

        private readonly MiniBomberRoomModel model = new MiniBomberRoomModel(); // 当前房间长期业务数据。
        private INetworkService network; // 项目网络服务。
        private AccountSessionComponent account; // 当前账号会话。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 房间权威状态变化事件。
        /// </summary>
        public event Action Changed;

        /// <summary>
        /// 获取当前房间的只读业务数据。
        /// </summary>
        public MiniBomberRoomModel Model => model;

        /// <summary>
        /// 当前玩家是否为房主。
        /// </summary>
        public bool IsOwner => model.HasRoom && model.OwnerPlayerId == account.Model.PlayerId;

        /// <summary>
        /// 取得账号和网络依赖。
        /// </summary>
        public override void Awake()
        {
            network = Global.GetService<INetworkService>(this);
            account = Global.Get<AccountSessionComponent>(this);
        }

        /// <summary>
        /// 创建房间并应用返回的权威快照。
        /// </summary>
        /// <param name="roomName">房间名。</param>
        /// <param name="durationSeconds">局时长秒数。</param>
        /// <returns>服务器响应。</returns>
        public async MTask<MiniBomberCommandResult> CreateAsync(string roomName, int durationSeconds)
        {
            MiniBomberCreateRoomResponse response = await network.CallAsync<MiniBomberCreateRoomRequest, MiniBomberCreateRoomResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberCreateRoomRequest
            {
                PlayerId = account.Model.PlayerId,
                RoomName = roomName ?? string.Empty,
                DurationSeconds = durationSeconds
            });
            ApplySuccessfulRoom(response.Code, response.Room);
            return new MiniBomberCommandResult(response.Code, response.Msg);
        }

        /// <summary>
        /// 加入指定房间并应用返回的权威快照。
        /// </summary>
        /// <param name="roomId">房间身份。</param>
        /// <returns>服务器响应。</returns>
        public async MTask<MiniBomberCommandResult> JoinAsync(long roomId)
        {
            MiniBomberJoinRoomResponse response = await network.CallAsync<MiniBomberJoinRoomRequest, MiniBomberJoinRoomResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberJoinRoomRequest
            {
                PlayerId = account.Model.PlayerId,
                RoomId = roomId
            });
            ApplySuccessfulRoom(response.Code, response.Room);
            return new MiniBomberCommandResult(response.Code, response.Msg);
        }

        /// <summary>
        /// 离开当前等待状态房间。
        /// </summary>
        /// <returns>服务器响应。</returns>
        public async MTask<MiniBomberCommandResult> LeaveAsync()
        {
            long roomId = model.RoomId;
            MiniBomberLeaveRoomResponse response = await network.CallAsync<MiniBomberLeaveRoomRequest, MiniBomberLeaveRoomResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberLeaveRoomRequest
            {
                PlayerId = account.Model.PlayerId,
                RoomId = roomId
            });
            if (response.Code == MiniBomberErrorCode.Success)
            {
                ClearModel();
                Changed?.Invoke();
            }

            return new MiniBomberCommandResult(response.Code, response.Msg);
        }

        /// <summary>
        /// 由房主修改名称和局时长。
        /// </summary>
        /// <param name="roomName">新房间名。</param>
        /// <param name="durationSeconds">新局时长。</param>
        /// <returns>服务器响应。</returns>
        public async MTask<MiniBomberCommandResult> UpdateSettingsAsync(string roomName, int durationSeconds)
        {
            MiniBomberUpdateRoomResponse response = await network.CallAsync<MiniBomberUpdateRoomRequest, MiniBomberUpdateRoomResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberUpdateRoomRequest
            {
                PlayerId = account.Model.PlayerId,
                RoomId = model.RoomId,
                RoomName = roomName ?? string.Empty,
                DurationSeconds = durationSeconds,
                ExpectedRevision = model.Revision
            });
            if (response.Room != null)
            {
                ApplySnapshot(response.Room);
            }

            return new MiniBomberCommandResult(response.Code, response.Msg);
        }

        /// <summary>
        /// 修改当前玩家准备状态。
        /// </summary>
        /// <param name="ready">目标准备状态。</param>
        /// <returns>服务器响应。</returns>
        public async MTask<MiniBomberCommandResult> SetReadyAsync(bool ready)
        {
            MiniBomberSetReadyResponse response = await network.CallAsync<MiniBomberSetReadyRequest, MiniBomberSetReadyResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberSetReadyRequest
            {
                PlayerId = account.Model.PlayerId,
                RoomId = model.RoomId,
                IsReady = ready
            });
            ApplySuccessfulRoom(response.Code, response.Room);
            return new MiniBomberCommandResult(response.Code, response.Msg);
        }

        /// <summary>
        /// 房主请求开始比赛。
        /// </summary>
        /// <returns>服务器响应。</returns>
        public async MTask<MiniBomberCommandResult> StartMatchAsync()
        {
            MiniBomberStartMatchResponse response = await network.CallAsync<MiniBomberStartMatchRequest, MiniBomberStartMatchResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberStartMatchRequest
            {
                PlayerId = account.Model.PlayerId,
                RoomId = model.RoomId
            }, timeoutSeconds: 15);
            return new MiniBomberCommandResult(response.Code, response.Msg);
        }

        /// <summary>
        /// 应用服务器推送或恢复返回的权威房间快照。
        /// </summary>
        /// <param name="snapshot">新房间快照。</param>
        public void ApplySnapshot(MiniBomberRoomSnapshotDto snapshot)
        {
            if (snapshot == null || (model.HasRoom && snapshot.Revision < model.Revision))
            {
                return;
            }

            MiniBomberProtocolModelMapper.ApplyRoom(snapshot, model);
            Changed?.Invoke();
        }

        /// <summary>
        /// 接管登录或恢复结果中的协议无关房间数据。
        /// </summary>
        /// <param name="source">待应用房间 Model。</param>
        public void ApplyModel(MiniBomberRoomModel source)
        {
            if (source == null || (model.HasRoom && source.Revision < model.Revision))
            {
                return;
            }

            if (ReferenceEquals(source, model))
            {
                Changed?.Invoke();
                return;
            }

            CopyModel(source);
            Changed?.Invoke();
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 清空房间状态和依赖引用。
        /// </summary>
        protected override void OnDispose()
        {
            Changed = null;
            ClearModel();
            network = null;
            account = null;
            Global.ReleaseAll(this);
            base.OnDispose();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 在 RPC 成功时应用房间快照。
        /// </summary>
        /// <param name="code">业务错误码。</param>
        /// <param name="snapshot">服务器房间快照。</param>
        private void ApplySuccessfulRoom(int code, MiniBomberRoomSnapshotDto snapshot)
        {
            if (code == MiniBomberErrorCode.Success)
            {
                ApplySnapshot(snapshot);
            }
        }

        /// <summary>
        /// 将外部协议无关房间数据复制到当前组件拥有的 Model。
        /// </summary>
        /// <param name="source">源房间 Model。</param>
        private void CopyModel(MiniBomberRoomModel source)
        {
            model.RoomId = source.RoomId;
            model.RoomName = source.RoomName;
            model.OwnerPlayerId = source.OwnerPlayerId;
            model.DurationSeconds = source.DurationSeconds;
            model.Status = source.Status;
            model.Revision = source.Revision;
            model.MatchId = source.MatchId;
            model.MutableMembers.Clear();
            for (int index = 0; index < source.Members.Count; index++)
            {
                MiniBomberRoomMemberModel item = source.Members[index];
                model.MutableMembers.Add(new MiniBomberRoomMemberModel
                {
                    PlayerId = item.PlayerId,
                    PlayerName = item.PlayerName,
                    IsOwner = item.IsOwner,
                    IsReady = item.IsReady,
                    IsOnline = item.IsOnline,
                    Score = item.Score
                });
            }
        }

        /// <summary>
        /// 清空当前房间 Model，保留可复用集合容量。
        /// </summary>
        private void ClearModel()
        {
            model.RoomId = 0;
            model.RoomName = string.Empty;
            model.OwnerPlayerId = 0;
            model.DurationSeconds = 0;
            model.Status = MiniBomberRoomStatus.Waiting;
            model.Revision = 0;
            model.MatchId = 0;
            model.MutableMembers.Clear();
        }

        #endregion
    }
}
