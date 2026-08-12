namespace MiniCore.Service
{
    /// <summary>
    /// 标记由项目启动配置选择并在应用生命周期内常驻的服务契约。
    /// 外部调用方只能通过该类接口从 Global 获取服务，不能直接依赖具体实现。
    /// </summary>
    public interface IAppService
    {
    }
}
