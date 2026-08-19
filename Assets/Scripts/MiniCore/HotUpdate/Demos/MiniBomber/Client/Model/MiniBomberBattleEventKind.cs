namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 客户端战斗即时事件的协议无关类型。
    /// </summary>
    public enum MiniBomberBattleEventKind
    {
        None,
        BombPlaced,
        ExplosionStarted,
        BlockDestroyed,
        PickupSpawned,
        PickupCollected,
        PlayerKilled,
        PlayerRespawned,
        ScoreChanged
    }
}
