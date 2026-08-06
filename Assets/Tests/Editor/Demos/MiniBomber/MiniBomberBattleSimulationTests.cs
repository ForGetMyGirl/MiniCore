using System.Collections.Generic;
using MiniCore.Demo.MiniBomber;
using NUnit.Framework;

namespace MiniCore.Tests.Editor.Demos.MiniBomber
{
    /// <summary>
    /// MiniBomber 服务器权威整数模拟回归测试。
    /// </summary>
    public sealed class MiniBomberBattleSimulationTests
    {
        #region Public 公共成员

        /// <summary>
        /// 验证超过一百毫秒没有新输入只停止移动，不重置权威位置。
        /// </summary>
        [Test]
        public void InputTimeout_StopsVelocityAndKeepsCurrentPosition()
        {
            MiniBomberBattleSimulation simulation = CreateSimulation(30, 10, new[] { new MiniBomberBattleParticipant(1, "Alpha") });
            MiniBomberPlayerState player = simulation.Players[0];
            int spawnX = player.PositionXMillimeters;
            Assert.That(simulation.SubmitInput(1, new MiniBomberBattleInput(1, 1000, 0, false)), Is.True);
            simulation.Tick();
            simulation.Tick();
            simulation.Tick();
            int movedX = player.PositionXMillimeters;
            simulation.Tick();
            simulation.Tick();

            Assert.That(movedX, Is.GreaterThan(spawnX));
            Assert.That(player.PositionXMillimeters, Is.EqualTo(movedX));
            Assert.That(player.MoveX, Is.Zero);
            Assert.That(player.MoveZ, Is.Zero);
        }

        /// <summary>
        /// 验证自杀只扣死亡分，最终排名完全由服务器生成。
        /// </summary>
        [Test]
        public void SelfExplosion_AppliesDeathPenaltyAndServerRanking()
        {
            MiniBomberBattleSimulation simulation = CreateSimulation(10, 2, new[]
            {
                new MiniBomberBattleParticipant(1, "Alpha"),
                new MiniBomberBattleParticipant(2, "Bravo")
            });
            Assert.That(simulation.SubmitInput(1, new MiniBomberBattleInput(1, 0, 0, true)), Is.True);
            for (int index = 0; index < 20; index++)
            {
                simulation.Tick();
            }

            IReadOnlyList<MiniBomberMatchResult> results = simulation.BuildResults();
            Assert.That(simulation.Players[0].Deaths, Is.EqualTo(1));
            Assert.That(simulation.Players[0].Score, Is.EqualTo(-1));
            Assert.That(results[0].PlayerId, Is.EqualTo(2));
            Assert.That(results[0].Rank, Is.EqualTo(1));
            Assert.That(results[1].PlayerId, Is.EqualTo(1));
        }

        /// <summary>
        /// 验证玩家断线时速度意图归零且当前位置保持不变。
        /// </summary>
        [Test]
        public void SetPlayerOffline_StopsMovementWithoutTeleporting()
        {
            MiniBomberBattleSimulation simulation = CreateSimulation(30, 10, new[] { new MiniBomberBattleParticipant(9, "Offline") });
            simulation.SubmitInput(9, new MiniBomberBattleInput(1, 1000, 0, false));
            simulation.Tick();
            int currentX = simulation.Players[0].PositionXMillimeters;
            Assert.That(simulation.SetPlayerOnline(9, false), Is.True);
            simulation.Tick();

            Assert.That(simulation.Players[0].PositionXMillimeters, Is.EqualTo(currentX));
            Assert.That(simulation.Players[0].MoveX, Is.Zero);
            Assert.That(simulation.Players[0].IsOnline, Is.False);
        }

        /// <summary>
        /// 验证放置者在圆形占位完全离开炸弹格前持续拥有穿出权限，不会被自己的炸弹反锁。
        /// </summary>
        [Test]
        public void PlacedBomb_AllowsOwnerToLeaveBeforeBecomingBlocking()
        {
            MiniBomberBattleSimulation simulation = CreateSimulation(
                30,
                10,
                new[] { new MiniBomberBattleParticipant(1, "Alpha") },
                2500);
            MiniBomberPlayerState player = simulation.Players[0];
            for (int index = 1; index <= 12; index++)
            {
                bool placeBomb = index == 1;
                Assert.That(simulation.SubmitInput(1, new MiniBomberBattleInput(index, 1000, 0, placeBomb)), Is.True);
                simulation.Tick();
            }

            Assert.That(simulation.Bombs.Count, Is.EqualTo(1));
            Assert.That(player.PositionXMillimeters, Is.GreaterThanOrEqualTo(2600));
            Assert.That(simulation.Bombs[0].OwnerCanPass, Is.False);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 创建空旷测试地图和权威模拟。
        /// </summary>
        /// <param name="tickRate">逻辑频率。</param>
        /// <param name="durationSeconds">比赛时长。</param>
        /// <param name="participants">参与者。</param>
        /// <returns>可直接推进的模拟。</returns>
        private static MiniBomberBattleSimulation CreateSimulation(
            int tickRate,
            int durationSeconds,
            IReadOnlyList<MiniBomberBattleParticipant> participants,
            int bombFuseMilliseconds = 200)
        {
            const int width = 17;
            const int height = 13;
            var cells = new byte[width * height];
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    cells[(z * width) + x] = (byte)(x == 0 || z == 0 || x == width - 1 || z == height - 1 ? MiniBomberCellType.Solid : MiniBomberCellType.Road);
                }
            }

            var map = new MiniBomberBattleMap(width, height, 1000, cells, new[]
            {
                new MiniBomberCell(1, 1),
                new MiniBomberCell(15, 11)
            });
            var rules = new MiniBomberBattleRules
            {
                TickRate = tickRate,
                InputHoldMilliseconds = 100,
                MovementSpeedMillimetersPerSecond = 3000,
                PlayerRadiusMillimeters = 300,
                BombFuseMilliseconds = bombFuseMilliseconds,
                InitialBombCapacity = 1,
                InitialBombRange = 2,
                RespawnDelayMilliseconds = 3000,
                RespawnProtectionMilliseconds = 2000,
                KillScore = 2,
                DeathScore = -1
            };
            return new MiniBomberBattleSimulation(1, durationSeconds, map, rules, participants);
        }

        #endregion
    }
}
