using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 服务器权威玩法规则配置。
    /// </summary>
    [CreateAssetMenu(fileName = "MiniBomberRuleConfig", menuName = "MiniCore/Demos/MiniBomber/Rule Config")]
    public sealed class MiniBomberRuleConfig : ScriptableObject
    {
        #region Private 私有成员

        [SerializeField, Range(2, 8)] private int maxPlayers = 4; // 单房间最大人数。
        [SerializeField, Range(1, 8)] private int minimumPlayers = 2; // 正式开局最少人数。
        [SerializeField] private int[] durationOptions = { 120, 300, 600 }; // 房主可选单局时长。
        [SerializeField] private int killScore = 2; // 击杀其他玩家得分。
        [SerializeField] private int deathScore = -1; // 玩家死亡得分变化。
        [SerializeField, Min(1)] private int movementSpeedMillimetersPerSecond = 3500; // 玩家每秒移动毫米数。
        [SerializeField, Min(100)] private int playerRadiusMillimeters = 350; // 权威碰撞半径。
        [SerializeField, Min(100)] private int bombFuseMilliseconds = 2500; // 炸弹引信时间。
        [SerializeField, Min(100)] private int explosionDurationMilliseconds = 500; // 爆炸表现持续时间。
        [SerializeField, Min(1)] private int initialBombCapacity = 1; // 玩家初始并存炸弹数。
        [SerializeField, Min(1)] private int initialBombRange = 2; // 玩家初始爆炸距离。
        [SerializeField, Min(100)] private int respawnDelayMilliseconds = 3000; // 死亡到复活的等待时间。
        [SerializeField, Min(100)] private int respawnProtectionMilliseconds = 2000; // 复活保护时间。
        [SerializeField] private bool enablePowerUps; // 是否启用预留的两类强化道具。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取单房间最多玩家数。
        /// </summary>
        public int MaxPlayers => maxPlayers;
        /// <summary>
        /// 获取允许开局的最少玩家数。
        /// </summary>
        public int MinimumPlayers => minimumPlayers;
        /// <summary>
        /// 获取击杀其他玩家的得分。
        /// </summary>
        public int KillScore => killScore;
        /// <summary>
        /// 获取玩家死亡时的得分变化。
        /// </summary>
        public int DeathScore => deathScore;
        /// <summary>
        /// 获取权威移动速度，单位为毫米每秒。
        /// </summary>
        public int MovementSpeedMillimetersPerSecond => movementSpeedMillimetersPerSecond;
        /// <summary>
        /// 获取权威玩家碰撞半径，单位为毫米。
        /// </summary>
        public int PlayerRadiusMillimeters => playerRadiusMillimeters;
        /// <summary>
        /// 获取炸弹引信时长。
        /// </summary>
        public int BombFuseMilliseconds => bombFuseMilliseconds;
        /// <summary>
        /// 获取爆炸表现与伤害格保留时长。
        /// </summary>
        public int ExplosionDurationMilliseconds => explosionDurationMilliseconds;
        /// <summary>
        /// 获取玩家初始可并存炸弹数量。
        /// </summary>
        public int InitialBombCapacity => initialBombCapacity;
        /// <summary>
        /// 获取玩家初始爆炸范围。
        /// </summary>
        public int InitialBombRange => initialBombRange;
        /// <summary>
        /// 获取玩家死亡后的复活等待时长。
        /// </summary>
        public int RespawnDelayMilliseconds => respawnDelayMilliseconds;
        /// <summary>
        /// 获取玩家复活后的保护时长。
        /// </summary>
        public int RespawnProtectionMilliseconds => respawnProtectionMilliseconds;
        /// <summary>
        /// 获取本版本是否启用强化道具。
        /// </summary>
        public bool EnablePowerUps => enablePowerUps;

        /// <summary>
        /// 判断目标秒数是否为房间允许选择的单局时长。
        /// </summary>
        /// <param name="seconds">待验证的秒数。</param>
        /// <returns>配置中存在该时长时返回 true。</returns>
        public bool IsDurationAllowed(int seconds)
        {
            if (durationOptions == null)
            {
                return false;
            }

            for (int index = 0; index < durationOptions.Length; index++)
            {
                if (durationOptions[index] == seconds)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
