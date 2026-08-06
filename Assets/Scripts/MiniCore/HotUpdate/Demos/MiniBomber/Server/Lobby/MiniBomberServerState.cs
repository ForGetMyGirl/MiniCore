using System;
using System.Collections.Generic;
using MiniCore.Protocol.Generated;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// Dedicated Server 持有的已认证玩家会话。
    /// </summary>
    public sealed class MiniBomberServerPlayerSession
    {
        #region Public 公共成员

        /// <summary>稳定玩家身份。</summary>
        public long PlayerId { get; set; }

        /// <summary>玩家显示名。</summary>
        public string PlayerName { get; set; }

        /// <summary>当前网络会话标识。</summary>
        public string NetworkSessionId { get; set; }

        /// <summary>用于断线恢复的随机令牌。</summary>
        public string SessionToken { get; set; }

        /// <summary>当前所在房间身份。</summary>
        public long RoomId { get; set; }

        /// <summary>当前参与比赛身份。</summary>
        public long MatchId { get; set; }

        /// <summary>当前是否在线。</summary>
        public bool IsOnline { get; set; }

        /// <summary>断线宽限截止的单调时间秒数。</summary>
        public double ReconnectDeadline { get; set; }

        #endregion
    }

    /// <summary>
    /// Dedicated Server 内存中的房间成员。
    /// </summary>
    public sealed class MiniBomberServerRoomMember
    {
        #region Public 公共成员

        /// <summary>成员玩家身份。</summary>
        public long PlayerId { get; set; }

        /// <summary>成员玩家显示名。</summary>
        public string PlayerName { get; set; }

        /// <summary>成员是否已经准备。</summary>
        public bool IsReady { get; set; }

        /// <summary>成员是否在线。</summary>
        public bool IsOnline { get; set; }

        /// <summary>战斗期间的当前得分。</summary>
        public int Score { get; set; }

        /// <summary>战斗场景是否加载完成。</summary>
        public bool IsSceneReady { get; set; }

        #endregion
    }

    /// <summary>
    /// Dedicated Server 内存中的 MiniBomber 房间。
    /// </summary>
    public sealed class MiniBomberServerRoom
    {
        #region Private 私有成员

        private readonly List<MiniBomberServerRoomMember> members = new List<MiniBomberServerRoomMember>(4); // 按加入顺序保存成员。

        #endregion

        #region Public 公共成员

        /// <summary>房间身份。</summary>
        public long RoomId { get; set; }

        /// <summary>房间显示名。</summary>
        public string RoomName { get; set; }

        /// <summary>当前房主玩家身份。</summary>
        public long OwnerPlayerId { get; set; }

        /// <summary>单局时长秒数。</summary>
        public int DurationSeconds { get; set; }

        /// <summary>当前房间状态。</summary>
        public MiniBomberRoomState State { get; set; }

        /// <summary>乐观并发和客户端刷新使用的房间修订号。</summary>
        public long Revision { get; set; }

        /// <summary>当前比赛身份。</summary>
        public long MatchId { get; set; }

        /// <summary>加载阶段截止的单调时间秒数。</summary>
        public double LoadingDeadline { get; set; }

        /// <summary>只读房间成员列表。</summary>
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

    /// <summary>
    /// Dedicated Server 持有的一局战斗运行状态。
    /// </summary>
    public sealed class MiniBomberServerMatch
    {
        #region Public 公共成员

        /// <summary>比赛身份。</summary>
        public long MatchId { get; set; }

        /// <summary>所属房间身份。</summary>
        public long RoomId { get; set; }

        /// <summary>稳定归属的 RoomWorker 下标。</summary>
        public int WorkerIndex { get; set; }

        /// <summary>倒计时结束并开始模拟的单调时间秒数。</summary>
        public double StartTime { get; set; }

        /// <summary>是否已经向 RoomWorker 投递开始命令。</summary>
        public bool IsStarted { get; set; }

        /// <summary>是否已经广播最终成绩。</summary>
        public bool ResultBroadcasted { get; set; }

        /// <summary>成绩展示结束并返回房间的单调时间秒数。</summary>
        public double ReturnToRoomTime { get; set; }

        #endregion
    }
}
