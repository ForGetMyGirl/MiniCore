# Runtime：Global、组件、MTask 与事件

读取本页处理不依赖 UnityEngine 的框架能力。程序集为 `MiniCore.Runtime`，路径为 `Assets/Scripts/MiniCore/Runtime`，是依赖图底层。

```text
Runtime/
├── Core/Global/                 # Global 静态门面、运行时和 Scope
├── Model/Component/             # AComponent、初始化参数、能力目录
├── Model/Eventing/              # Bus、Channel、订阅模型与事件接口
├── Service/                     # Abstraction、Interface、Attribute、Registry、Group 与 Model
├── Threading/                   # Core、Source、Awaitable、Builder、Sharing、Execution、Ownership 等
└── Time/                        # 时间接口与 TimerService
```

关键入口：`Core/Global/Global.cs`、`Model/Component/AComponent.cs`、`Service/Abstraction/AAppService.cs`、`Service/Interface/IAppService.cs`、`Threading/Core/MTask.cs`、`Threading/Ownership/MTaskDomain.cs`、`Model/Eventing/Bus/EventBusCore.cs`、`Time/TimerService.cs`。

- 生命周期、owner 引用计数与 Scope：先读 `Docs/Architecture.md` 的 Global 章节。
- 异步、取消、Share/Forget、线程切换：先读 `Docs/MTask.md`。
- 新增事件、频道与订阅释放：先读 `Docs/Eventing.md`。
- Runtime 不可依赖 Unity；服务通过接口由 `Global.GetService<T>` 取得。
