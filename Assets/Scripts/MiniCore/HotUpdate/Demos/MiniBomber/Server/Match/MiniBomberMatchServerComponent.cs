using System.Collections.Generic;
using MiniCore.Model;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// Match Role 独占的低频匹配队列业务组件。
    /// </summary>
    public sealed class MiniBomberMatchServerComponent : AComponent
    {
        #region Private 私有成员

        private readonly List<MatchCandidate> waiting = new List<MatchCandidate>(128); // 保持稳定入队顺序的等待列表。
        private readonly Dictionary<long, MatchCandidate> candidateByPlayerId = new Dictionary<long, MatchCandidate>(); // 玩家到当前票据的唯一映射。
        private long nextTicketId = 1; // 当前进程单调递增的匹配票据。
        private bool acceptingNewWork = true; // Drain 后禁止新玩家进入匹配队列。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取仍在等待匹配的玩家数量。
        /// </summary>
        public int WaitingCount => waiting.Count;

        /// <summary>
        /// 停止接受新的匹配请求，已存在票据仍可取消或被取出。
        /// </summary>
        public void BeginDrain()
        {
            acceptingNewWork = false;
        }

        /// <summary>
        /// 将一个尚未排队的玩家加入当前 Match 实例。
        /// </summary>
        /// <param name="playerId">全局玩家标识。</param>
        /// <param name="rating">用于后续匹配策略的当前分值。</param>
        /// <param name="ticketId">成功时返回当前实例签发的票据。</param>
        /// <returns>玩家成功入队时返回 true。</returns>
        public bool TryEnqueue(long playerId, int rating, out long ticketId)
        {
            if (!acceptingNewWork || playerId <= 0 || candidateByPlayerId.ContainsKey(playerId))
            {
                ticketId = 0;
                return false;
            }

            ticketId = nextTicketId++;
            var candidate = new MatchCandidate(playerId, rating, ticketId);
            waiting.Add(candidate);
            candidateByPlayerId.Add(playerId, candidate);
            return true;
        }

        /// <summary>
        /// 使用玩家和票据共同取消仍在等待的匹配请求。
        /// </summary>
        /// <param name="playerId">全局玩家标识。</param>
        /// <param name="ticketId">入队时获得的匹配票据。</param>
        /// <returns>找到并移除对应等待项时返回 true。</returns>
        public bool TryCancel(long playerId, long ticketId)
        {
            if (!candidateByPlayerId.TryGetValue(playerId, out MatchCandidate candidate)
                || candidate.TicketId != ticketId)
            {
                return false;
            }

            candidateByPlayerId.Remove(playerId);
            waiting.Remove(candidate);
            return true;
        }

        /// <summary>
        /// 按入队顺序取出指定数量玩家并结束其等待票据。
        /// </summary>
        /// <param name="playerCount">本次需要组成的玩家数量。</param>
        /// <param name="playerIds">由调用方复用的结果缓冲。</param>
        /// <returns>等待人数足够并成功取出时返回 true。</returns>
        public bool TryTake(int playerCount, List<long> playerIds)
        {
            if (playerIds == null || playerCount < 2 || playerCount > 16 || waiting.Count < playerCount)
            {
                return false;
            }

            playerIds.Clear();
            for (int index = 0; index < playerCount; index++)
            {
                MatchCandidate candidate = waiting[index];
                playerIds.Add(candidate.PlayerId);
                candidateByPlayerId.Remove(candidate.PlayerId);
            }

            waiting.RemoveRange(0, playerCount);
            return true;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 保存一个等待玩家的稳定票据和匹配分值。
        /// </summary>
        private sealed class MatchCandidate
        {
            /// <summary>
            /// 创建一条不可变的等待候选记录。
            /// </summary>
            /// <param name="playerId">玩家标识。</param>
            /// <param name="rating">入队时匹配分值。</param>
            /// <param name="ticketId">当前实例签发的票据。</param>
            public MatchCandidate(long playerId, int rating, long ticketId)
            {
                PlayerId = playerId;
                Rating = rating;
                TicketId = ticketId;
            }

            /// <summary>
            /// 获取候选玩家标识。
            /// </summary>
            public long PlayerId { get; }

            /// <summary>
            /// 获取玩家进入队列时的匹配分值。
            /// </summary>
            public int Rating { get; }

            /// <summary>
            /// 获取当前 Match 实例签发的唯一票据。
            /// </summary>
            public long TicketId { get; }
        }

        #endregion
    }
}
