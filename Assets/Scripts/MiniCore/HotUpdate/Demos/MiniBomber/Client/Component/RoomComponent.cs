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
    /// MiniBomber 客户端当前房间权威快照和房间命令组件。
    /// </summary>
    public sealed class RoomComponent : AComponent
    {
        #region Private 私有成员

        private INetworkService network; // 项目网络服务。
        private AccountSessionComponent account; // 当前账号会话。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 房间权威状态变化事件。
        /// </summary>
        public event Action Changed;

        /// <summary>
        /// 当前房间权威快照。
        /// </summary>
        public MiniBomberRoomSnapshotDto Current { get; private set; }

        /// <summary>
        /// 当前玩家是否为房主。
        /// </summary>
        public bool IsOwner => Current != null && Current.OwnerPlayerId == account.PlayerId;

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
        public async MTask<MiniBomberCreateRoomResponse> CreateAsync(string roomName, int durationSeconds)
        {
            MiniBomberCreateRoomResponse response = await network.CallAsync<MiniBomberCreateRoomRequest, MiniBomberCreateRoomResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberCreateRoomRequest
            {
                PlayerId = account.PlayerId,
                RoomName = roomName ?? string.Empty,
                DurationSeconds = durationSeconds
            });
            ApplySuccessfulRoom(response.Code, response.Room);
            return response;
        }

        /// <summary>
        /// 加入指定房间并应用返回的权威快照。
        /// </summary>
        /// <param name="roomId">房间身份。</param>
        /// <returns>服务器响应。</returns>
        public async MTask<MiniBomberJoinRoomResponse> JoinAsync(long roomId)
        {
            MiniBomberJoinRoomResponse response = await network.CallAsync<MiniBomberJoinRoomRequest, MiniBomberJoinRoomResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberJoinRoomRequest
            {
                PlayerId = account.PlayerId,
                RoomId = roomId
            });
            ApplySuccessfulRoom(response.Code, response.Room);
            return response;
        }

        /// <summary>
        /// 离开当前等待状态房间。
        /// </summary>
        /// <returns>服务器响应。</returns>
        public async MTask<MiniBomberLeaveRoomResponse> LeaveAsync()
        {
            long roomId = Current?.RoomId ?? 0;
            MiniBomberLeaveRoomResponse response = await network.CallAsync<MiniBomberLeaveRoomRequest, MiniBomberLeaveRoomResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberLeaveRoomRequest
            {
                PlayerId = account.PlayerId,
                RoomId = roomId
            });
            if (response.Code == MiniBomberErrorCode.Success)
            {
                Current = null;
                Changed?.Invoke();
            }

            return response;
        }

        /// <summary>
        /// 由房主修改名称和局时长。
        /// </summary>
        /// <param name="roomName">新房间名。</param>
        /// <param name="durationSeconds">新局时长。</param>
        /// <returns>服务器响应。</returns>
        public async MTask<MiniBomberUpdateRoomResponse> UpdateSettingsAsync(string roomName, int durationSeconds)
        {
            MiniBomberUpdateRoomResponse response = await network.CallAsync<MiniBomberUpdateRoomRequest, MiniBomberUpdateRoomResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberUpdateRoomRequest
            {
                PlayerId = account.PlayerId,
                RoomId = Current?.RoomId ?? 0,
                RoomName = roomName ?? string.Empty,
                DurationSeconds = durationSeconds,
                ExpectedRevision = Current?.Revision ?? 0
            });
            if (response.Room != null)
            {
                ApplySnapshot(response.Room);
            }

            return response;
        }

        /// <summary>
        /// 修改当前玩家准备状态。
        /// </summary>
        /// <param name="ready">目标准备状态。</param>
        /// <returns>服务器响应。</returns>
        public async MTask<MiniBomberSetReadyResponse> SetReadyAsync(bool ready)
        {
            MiniBomberSetReadyResponse response = await network.CallAsync<MiniBomberSetReadyRequest, MiniBomberSetReadyResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberSetReadyRequest
            {
                PlayerId = account.PlayerId,
                RoomId = Current?.RoomId ?? 0,
                IsReady = ready
            });
            ApplySuccessfulRoom(response.Code, response.Room);
            return response;
        }

        /// <summary>
        /// 房主请求开始比赛。
        /// </summary>
        /// <returns>服务器响应。</returns>
        public MTask<MiniBomberStartMatchResponse> StartMatchAsync()
        {
            return network.CallAsync<MiniBomberStartMatchRequest, MiniBomberStartMatchResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberStartMatchRequest
            {
                PlayerId = account.PlayerId,
                RoomId = Current?.RoomId ?? 0
            });
        }

        /// <summary>
        /// 应用服务器推送或恢复返回的权威房间快照。
        /// </summary>
        /// <param name="snapshot">新房间快照。</param>
        public void ApplySnapshot(MiniBomberRoomSnapshotDto snapshot)
        {
            if (snapshot == null || (Current != null && snapshot.Revision < Current.Revision))
            {
                return;
            }

            Current = snapshot;
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
            Current = null;
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

        #endregion
    }
}
