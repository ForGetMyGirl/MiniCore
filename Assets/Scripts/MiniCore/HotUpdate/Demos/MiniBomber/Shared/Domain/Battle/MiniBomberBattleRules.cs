using System;
using System.Collections.Generic;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 从 Unity 配置复制出的权威战斗规则值。
    /// </summary>
    public sealed class MiniBomberBattleRules
    {
        #region Public 公共成员

        public int TickRate { get; set; }
        public int InputHoldMilliseconds { get; set; }
        public int MovementSpeedMillimetersPerSecond { get; set; }
        public int PlayerRadiusMillimeters { get; set; }
        public int BombFuseMilliseconds { get; set; }
        public int InitialBombCapacity { get; set; }
        public int InitialBombRange { get; set; }
        public int RespawnDelayMilliseconds { get; set; }
        public int RespawnProtectionMilliseconds { get; set; }
        public int KillScore { get; set; }
        public int DeathScore { get; set; }

        /// <summary>
        /// 验证权威模拟需要的全部规则值。
        /// </summary>
        public void Validate()
        {
            if (TickRate <= 0 || MovementSpeedMillimetersPerSecond <= 0 || PlayerRadiusMillimeters <= 0 || BombFuseMilliseconds <= 0 || InitialBombCapacity <= 0 || InitialBombRange <= 0)
            {
                throw new InvalidOperationException("MiniBomber 战斗规则包含非正数关键参数。");
            }
        }

        #endregion
    }
}
