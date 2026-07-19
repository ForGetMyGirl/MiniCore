# 强类型事件中心

本文是 MiniCore 强类型事件中心的使用与边界说明。程序集位置、`Global` 生命周期与启动链见 [架构总览](Architecture.md)；`MTask` 的取消和执行器规则见 [MTask 结构化异步](MTask.md)。

## 适用范围

事件中心只表达“某件事刚刚发生，其他对象可选择观察”。它不保存状态、不回放历史，也不负责“等待对象已经到达某状态”。

- 跨模块、低频的应用通知使用 `IApplicationEventBus`。
- 房间、对局、窗口、场景等局部范围使用 `ScopedEventBus`；将它放进对应的 `GlobalScope` 或 `ComponentGroup`。
- 高频 Update、实体循环和网络批处理使用直接调用、领域队列或对象自身 API，不能穿透事件中心。
- “等待战斗已结束”“等待会话已断开”这类持久状态应由领域对象提供状态化 API；`WaitNextAsync` 只表示等待调用之后的下一条通知。

## 定义事件

事件是不可变 `sealed class`，业务数据可以包含 `string`、`int`、ID 或结果对象；这些值不再承担事件名或参数协议的职责。

```csharp
public sealed class DemoMessageReceivedEvent : ISyncEvent
{
    public long SessionId { get; }
    public string Message { get; }

    public DemoMessageReceivedEvent(long sessionId, string message)
    {
        SessionId = sessionId;
        Message = message;
    }
}
```

事件必须二选一：

| 标记 | 派发 | 监听器 |
| --- | --- | --- |
| `ISyncEvent` | `Publish` | `IEventHandler<TEvent>` 或 `Action<TEvent>`，返回 `void` |
| `IAsyncEvent` | `PublishAsync` | `IAsyncEventHandler<TEvent>` 或 `Func<TEvent, MTask>`，返回 `MTask` |

同一类型不能同时实现两个标记。同步事件不能注册异步监听器，异步事件不能用同步方式派发。

## 应用级频道

`IApplicationEventBus` 是 AppModule。启动生成代码会在服务启动前注册它，业务只依赖接口取得模块。

```csharp
private IApplicationEventBus eventBus;
private EventSubscription messageSubscription;

protected override void OnBind()
{
    eventBus = Global.GetOrAddModule<IApplicationEventBus>(this);
    messageSubscription = eventBus.Subscribe<DemoMessageReceivedEvent>(OnMessageReceived);
}

public override void UnbindView()
{
    messageSubscription.Dispose();
    eventBus = null;
    Global.ReleaseAll(this);
    base.UnbindView();
}

private void OnMessageReceived(DemoMessageReceivedEvent @event)
{
    // 观察通知。
}
```

发布者同样先取得频道、发布，再在自身生命周期结束时归还 `Global` 引用。`Publish` 和 `PublishAsync` 在发布者当前 MTask 执行器/线程运行，不会隐式切换到 Unity 主线程；需要 Unity API 的异步监听器应自行 `await MTask.SwitchTo(MTaskExecutors.Unity)`。

## 局部频道

同一事件类型在不同局部频道之间天然隔离。房间、对局、窗口等边界各创建自己的频道，不要以 `SessionId` 谓词在全局频道中筛选。

```csharp
using ComponentGroup battleGroup = Global.CreateGroup("Battle", battleId);
ScopedEventBus battleEvents = battleGroup.GetOrAdd<ScopedEventBus>();

EventSubscription subscription = battleEvents.Subscribe<RoundEndedEvent>(OnRoundEnded);
battleEvents.Publish(new RoundEndedEvent(result));
```

`GlobalScope.Dispose()` 或 `ComponentGroup.Dispose()` 会释放其中的 `ScopedEventBus`，并自动解除其所有订阅、取消所有未完成的等待者。

## 异步派发与等待下一条事件

`PublishAsync` 按注册顺序逐个等待异步监听器。某个监听器失败不会阻止后续监听器，全部完成后统一抛出 `AggregateException`。

`WaitNextAsync<TEvent>` 可用于确实只关心“下一条通知”的短生命周期场景：

```csharp
DemoMessageReceivedEvent @event = await eventBus.WaitNextAsync<DemoMessageReceivedEvent>(
    new EventWaitOptions(TimeSpan.FromSeconds(5)));
```

等待者在事件已被接收、监听器开始分发时完成，不等待异步监听器执行结束。等待者的父 MTask 被取消、频道销毁或超时都会结束等待；该 API 不支持谓词筛选、历史重放或 sticky event。

## 订阅与性能规则

`Subscribe` 返回 `EventSubscription` 值类型 token。必须保存 token，并在 `OnDispose`、`UnbindView`、`OnDestroy` 等生命周期出口先 `Dispose` token、再归还 Global 引用。重复 `Dispose` 安全；过期 token 也不会误取消已经复用的槽位。

频道不使用 multicast delegate 的 `+=` / `-=`。每个事件类型独立维护监听槽位、空闲链表与版本号；派发取稳定快照，因此派发期间的订阅/取消只影响下一次派发。快照按实际注册顺序执行，即使中间复用了旧槽位也不会改变顺序。

优先使用命名方法或让已有对象实现 `IEventHandler<TEvent>` / `IAsyncEventHandler<TEvent>`。直接 lambda 或匿名方法可能产生闭包分配，也更难在生命周期结束时解除。

## 编辑器诊断

`Assets/Plugins/MiniCore/Eventing/Editor/MiniCore.Eventing.Analyzers.dll` 是 Editor-only Roslyn Analyzer：

| 诊断 | 含义 |
| --- | --- |
| `MCEVT001` | 直接向 `Subscribe` / `SubscribeAsync` 传递 lambda 或匿名方法 |
| `MCEVT002` | 丢弃订阅 token，未保存 `EventSubscription` |
| `MCEVT003` | 一个事件类型同时实现 `ISyncEvent` 与 `IAsyncEvent` |

诊断默认是警告，可按标准 C# `#pragma warning disable MCEVT00x` 在局部抑制。分析器源码、构建和验证脚本位于 `Tools/EventAnalyzer/`。
