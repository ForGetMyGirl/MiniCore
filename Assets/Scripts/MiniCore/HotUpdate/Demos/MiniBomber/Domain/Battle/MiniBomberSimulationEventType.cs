using System;
using System.Collections.Generic;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 权威模拟产生的离散事件类型。
    /// </summary>
    public enum MiniBomberSimulationEventType
    {
        BombPlaced,
        ExplosionStarted,
        BlockDestroyed,
        PlayerKilled,
        PlayerRespawned,
        ScoreChanged
    }
}
