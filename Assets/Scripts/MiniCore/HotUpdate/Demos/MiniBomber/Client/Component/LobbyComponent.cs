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
    /// MiniBomber 客户端大厅列表状态和命令组件。
    /// </summary>
    public sealed class LobbyComponent : AComponent
    {
        #region Private 私有成员

        private readonly List<MiniBomberRoomSummaryDto> rooms = new List<MiniBomberRoomSummaryDto>(32); // 当前大厅房间列表。
        private INetworkService network; // 项目网络服务。
        private AccountSessionComponent account; // 当前账号会话。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 大厅列表变化事件。
        /// </summary>
        public event Action Changed;

        /// <summary>
        /// 只读房间列表。
        /// </summary>
        public IReadOnlyList<MiniBomberRoomSummaryDto> Rooms => rooms;

        /// <summary>
        /// 大厅修订号。
        /// </summary>
        public long Revision { get; private set; }

        /// <summary>
        /// 服务器报告的在线人数。
        /// </summary>
        public int OnlinePlayerCount { get; private set; }

        /// <summary>
        /// 取得账号和网络依赖。
        /// </summary>
        public override void Awake()
        {
            network = Global.GetService<INetworkService>(this);
            account = Global.Get<AccountSessionComponent>(this);
        }

        /// <summary>
        /// 请求并替换完整大厅快照。
        /// </summary>
        /// <returns>服务器响应。</returns>
        public async MTask<MiniBomberLobbySnapshotResponse> RefreshAsync()
        {
            MiniBomberLobbySnapshotResponse response = await network.CallAsync<MiniBomberLobbySnapshotRequest, MiniBomberLobbySnapshotResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberLobbySnapshotRequest
            {
                PlayerId = account.PlayerId
            });
            if (response.Code == MiniBomberErrorCode.Success)
            {
                rooms.Clear();
                rooms.AddRange(response.Rooms);
                Revision = response.Revision;
                OnlinePlayerCount = response.OnlinePlayerCount;
                Changed?.Invoke();
            }

            return response;
        }

        /// <summary>
        /// 记录服务器大厅修订通知；Presenter 可据此决定立即刷新。
        /// </summary>
        /// <param name="revision">服务器最新修订号。</param>
        public void ApplyChangedNotice(long revision)
        {
            if (revision > Revision)
            {
                Revision = revision;
                Changed?.Invoke();
            }
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 清空大厅状态和依赖引用。
        /// </summary>
        protected override void OnDispose()
        {
            Changed = null;
            rooms.Clear();
            network = null;
            account = null;
            Global.ReleaseAll(this);
            base.OnDispose();
        }

        #endregion
    }
}
