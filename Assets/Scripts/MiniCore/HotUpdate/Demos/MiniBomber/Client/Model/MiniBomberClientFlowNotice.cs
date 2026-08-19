namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 客户端流程的结构化状态提示。
    /// </summary>
    public enum MiniBomberClientFlowNotice
    {
        None,
        RestoringBattle,
        EnteringRoom,
        EnteringLobby,
        ReturningLogin,
        LoadingBattle,
        SceneReadyFailed,
        Disconnected,
        ReconnectWaiting,
        ReconnectTimedOut,
        ReconnectFailed,
        BattleLoadingTimedOut
    }
}
