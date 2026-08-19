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
    /// MiniBomber 客户端大厅列表状态和命令组件。
    /// </summary>
    public sealed class LobbyComponent : AComponent
    {
        #region Private 私有成员

        private readonly MiniBomberLobbyModel model = new MiniBomberLobbyModel(); // 当前大厅长期业务数据。
        private INetworkService network; // 项目网络服务。
        private AccountSessionComponent account; // 当前账号会话。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 大厅列表变化事件。
        /// </summary>
        public event Action Changed;

        /// <summary>
        /// 获取当前大厅的只读业务数据。
        /// </summary>
        public MiniBomberLobbyModel Model => model;

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
        public async MTask<MiniBomberCommandResult> RefreshAsync()
        {
            MiniBomberLobbySnapshotResponse response = await network.CallAsync<MiniBomberLobbySnapshotRequest, MiniBomberLobbySnapshotResponse>(MiniBomberConstants.DefaultSessionId, new MiniBomberLobbySnapshotRequest
            {
                PlayerId = account.Model.PlayerId
            });
            if (response.Code == MiniBomberErrorCode.Success)
            {
                ApplySnapshot(response);
                Changed?.Invoke();
            }

            return new MiniBomberCommandResult(response.Code, response.Msg);
        }

        /// <summary>
        /// 记录服务器大厅修订通知；Presenter 可据此决定立即刷新。
        /// </summary>
        /// <param name="revision">服务器最新修订号。</param>
        public void ApplyChangedNotice(long revision)
        {
            if (revision > model.Revision)
            {
                model.Revision = revision;
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
            model.MutableRooms.Clear();
            model.Revision = 0;
            model.OnlinePlayerCount = 0;
            network = null;
            account = null;
            Global.ReleaseAll(this);
            base.OnDispose();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将大厅响应中的 PB 房间摘要复制到长期 Model，并复用已有条目。
        /// </summary>
        /// <param name="response">服务器大厅快照响应。</param>
        private void ApplySnapshot(MiniBomberLobbySnapshotResponse response)
        {
            model.Revision = response.Revision;
            model.OnlinePlayerCount = response.OnlinePlayerCount;
            while (model.MutableRooms.Count < response.Rooms.Count)
            {
                model.MutableRooms.Add(new MiniBomberLobbyRoomModel());
            }

            for (int index = 0; index < response.Rooms.Count; index++)
            {
                MiniBomberProtocolModelMapper.ApplyLobbyRoom(response.Rooms[index], model.MutableRooms[index]);
            }

            if (model.MutableRooms.Count > response.Rooms.Count)
            {
                model.MutableRooms.RemoveRange(response.Rooms.Count, model.MutableRooms.Count - response.Rooms.Count);
            }
        }

        #endregion
    }
}
