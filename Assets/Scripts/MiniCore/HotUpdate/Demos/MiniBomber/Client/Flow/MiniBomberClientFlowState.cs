namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 客户端高层流程状态。
    /// </summary>
    public enum MiniBomberClientFlowState
    {
        Login,
        Lobby,
        Room,
        LoadingBattle,
        Battle,
        Result,
        Reconnecting
    }
}
