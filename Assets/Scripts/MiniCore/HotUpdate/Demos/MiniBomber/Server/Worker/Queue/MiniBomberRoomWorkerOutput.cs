using System;
using System.Collections.Generic;
using System.Threading;
using Google.Protobuf;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// Worker 返回主线程安全发送边界的一帧不可变结果。
    /// </summary>
    public sealed class MiniBomberRoomWorkerOutput
    {
        #region Public 公共成员

        /// <summary>
        /// 比赛身份。
        /// </summary>
        public long MatchId { get; internal set; }
        /// <summary>
        /// 房间身份。
        /// </summary>
        public long RoomId { get; internal set; }
        /// <summary>
        /// 仅发送给指定网络会话；空值表示广播房间。
        /// </summary>
        public string TargetNetworkSessionId { get; internal set; }
        /// <summary>
        /// 不可丢弃的有序事件批次。
        /// </summary>
        public MiniBomberBattleEventBatch Events { get; internal set; }
        /// <summary>
        /// 可由更新状态替换的玩家动态增量。
        /// </summary>
        public MiniBomberBattleDelta Delta { get; internal set; }
        /// <summary>
        /// 完整关键帧。
        /// </summary>
        public MiniBomberBattleSnapshot Keyframe { get; internal set; }
        /// <summary>
        /// 服务器唯一最终排名。
        /// </summary>
        public IReadOnlyList<MiniBomberMatchResult> Results { get; internal set; }

        /// <summary>
        /// 获取输出是否只包含可替换的位置类增量。
        /// </summary>
        public bool IsReplaceableDelta => Delta != null && Events == null && Keyframe == null && Results == null && string.IsNullOrEmpty(TargetNetworkSessionId);

        #endregion
    }
}
