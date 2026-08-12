using System;
using System.Collections.Generic;
using MiniCore.Protocol.Generated;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// Dedicated Server 内存中的 MiniBomber 房间。
    /// </summary>
    public sealed class MiniBomberServerRoom
    {
        #region Private 私有成员

        private readonly List<MiniBomberServerRoomMember> members = new List<MiniBomberServerRoomMember>(4); // 按加入顺序保存成员。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 房间身份。
        /// </summary>
        public long RoomId { get; set; }

        /// <summary>
        /// 房间显示名。
        /// </summary>
        public string RoomName { get; set; }

        /// <summary>
        /// 当前房主玩家身份。
        /// </summary>
        public long OwnerPlayerId { get; set; }

        /// <summary>
        /// 单局时长秒数。
        /// </summary>
        public int DurationSeconds { get; set; }

        /// <summary>
        /// 当前房间状态。
        /// </summary>
        public MiniBomberRoomState State { get; set; }

        /// <summary>
        /// 乐观并发和客户端刷新使用的房间修订号。
        /// </summary>
        public long Revision { get; set; }

        /// <summary>
        /// 当前比赛身份。
        /// </summary>
        public long MatchId { get; set; }

        /// <summary>
        /// 加载阶段截止的单调时间秒数。
        /// </summary>
        public double LoadingDeadline { get; set; }

        /// <summary>
        /// 只读房间成员列表。
        /// </summary>
        public IReadOnlyList<MiniBomberServerRoomMember> Members => members;

        /// <summary>
        /// 添加新成员。
        /// </summary>
        /// <param name="member">待加入成员。</param>
        public void AddMember(MiniBomberServerRoomMember member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            members.Add(member);
            Revision++;
        }

        /// <summary>
        /// 查找指定玩家对应的房间成员。
        /// </summary>
        /// <param name="playerId">玩家身份。</param>
        /// <param name="member">找到的成员。</param>
        /// <returns>房间包含该玩家时返回 true。</returns>
        public bool TryGetMember(long playerId, out MiniBomberServerRoomMember member)
        {
            for (int index = 0; index < members.Count; index++)
            {
                if (members[index].PlayerId == playerId)
                {
                    member = members[index];
                    return true;
                }
            }

            member = null;
            return false;
        }

        /// <summary>
        /// 移除指定玩家并保持其他成员加入顺序。
        /// </summary>
        /// <param name="playerId">玩家身份。</param>
        /// <returns>实际移除成员时返回 true。</returns>
        public bool RemoveMember(long playerId)
        {
            for (int index = 0; index < members.Count; index++)
            {
                if (members[index].PlayerId != playerId)
                {
                    continue;
                }

                members.RemoveAt(index);
                Revision++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 取消全部成员准备和场景加载状态。
        /// </summary>
        public void ResetReadiness()
        {
            for (int index = 0; index < members.Count; index++)
            {
                members[index].IsReady = false;
                members[index].IsSceneReady = false;
            }
        }

        #endregion
    }
}
