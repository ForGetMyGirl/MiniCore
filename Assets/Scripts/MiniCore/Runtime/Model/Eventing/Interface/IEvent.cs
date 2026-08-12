namespace MiniCore.Eventing
{
    /// <summary>
    /// 标记可由 MiniCore 事件频道派发的强类型事件。
    /// 事件应使用不可变数据表达业务事实，不能再使用字符串或整数充当事件标识；频道本身不缓存历史事件。
    /// </summary>
    public interface IEvent
    {
    }
}
