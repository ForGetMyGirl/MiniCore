# MTask 结构化异步

`MTask` 是 MiniCore 的默认业务异步类型。C# 编译器仍负责生成 async 状态机；MiniCore 只提供 Builder、池化 Runner、任务树、执行器和 Owner 生命周期。业务不需要 `Start`、Handle、Scope 或逐层传递 `CancellationToken`。

运行时核心代码位于 `Assets/Scripts/MiniCore/Runtime/Threading`，保持纯 C#；Unity 主线程驱动与 Mono Owner 适配位于 `Assets/Scripts/MiniCore/Unity/Threading`。Unity 2021.3 的自动注入构建工具在 `Tools/MTaskCodeGen`，随项目交付的 Editor-only 插件位于 `Assets/Plugins/MiniCore/MTask/Editor`。

## 开发者用法

```csharp
public sealed class BattleModule : AAppModule
{
    public async MTask EnterBattleAsync()
    {
        PlainLoader loader = new PlainLoader();
        await loader.LoadAsync();
        HeartbeatAsync().Forget();
    }

    public async MTask PreloadAsync()
    {
        await MTask.Delay(TimeSpan.FromMilliseconds(10));
    }

    protected override void OnDispose()
    {
        // 只清理 Module 自身资源。
    }
}
```

每次调用 `EnterBattleAsync` 或 `PreloadAsync` 都是 Module 域下相互独立的入口。`PlainLoader` 不需要继承框架类型：只要它在当前 MTask 内被调用，其 MTask 就会自动成为当前节点的子任务。父方法结束时，仍未结束且没有 `.Forget()` 的子任务会收到取消，父任务会等它们执行完 `finally` 后才最终完成。

`AComponent`、AppService、AppModule、`GlobalScope`、`ComponentGroup` 自动是 Owner。Unity 类优先继承 `AMTaskBehaviour`；不能更换基类时标记 `[MTaskOwner]`。Editor IL 后处理只为 Owner 的 MTask 入口、直接启动 MTask 的同步方法和 Unity 生命周期边界注入 Owner 上下文。

Unity 2021.3 的 Owner 自动注入以 `Assets/Plugins/MiniCore/MTask/Editor/MiniCore.MTask.CodeGen.dll` 的 Editor-only 插件随项目交付。它只在编辑器编译期间使用 Unity 自带的 ILPostProcessor/Cecil API，不会进入 Player，也不会为 MTask 增加 Burst、Cecil、UniTask 或额外 UPM 依赖。处理器源码与重建脚本位于 `Tools/MTaskCodeGen`；升级 Unity LTS 时使用对应编辑器重新生成并验证该插件。

## 等待、Forget 与共享

```csharp
await MTask.Delay(100);
await MTask.Yield();
await MTask.WhenAll(first, second);
int winner = await MTask.WhenAny(first, second);

BackgroundLoopAsync().Forget();
MSharedTask<Result> shared = LoadOnceAsync().Share();
Result a = await shared;
Result b = await shared;
```

普通 `MTask` 使用池化结果源和版本号，只允许消费一次；第二次 await 会抛出明确的非法消费异常。`.Share()` 是显式的共享状态分配点，它只消费一次底层任务，每个等待者的取消互不影响。

`.Forget()` 不是简单丢弃异常。它会把任务从当前方法父节点转移到最近 Owner 监督域；Owner 释放时仍会取消它，未处理异常通过 `MTaskSupervisor.UnhandledException` 上报。

## 取消和外部 API

`Delay`、`Yield`、组合操作、CompletionSource 和执行器切换都会响应当前 Node 取消。Owner Dispose、父任务失败或父方法提前结束会递归唤醒后代。没有 await 的长 CPU 循环必须主动加入协作点：

```csharp
while (hasWork)
{
    MTask.ThrowIfCancellationRequested();
    ProcessBatch();
    await MTask.Yield();
}
```

BCL 或第三方 API 确实要求 Token 时，仅在适配层调用 `MTaskExternal.GetCancellationToken()`。CTS 会按 Node 惰性创建，Node 回池前释放；业务方法签名不公开 Token。

## 线程与性能

```csharp
using (MDedicatedThreadExecutor serializer = MTaskExecutors.CreateDedicated("MiniCore.Serialization"))
{
    await MTask.SwitchTo(serializer);
    SerializeSnapshot();
    await MTask.SwitchTo(MTaskExecutors.Unity);
    ApplyToGameState();
}

await MTask.SwitchTo(MTaskExecutors.ThreadPool);
CalculateWithoutUnityAccess();
```

`MTaskExecutors.Unity` 由 `UnityGlobalDriver.Update` 抽取；`NetworkService` 创建并持有自己的独占执行器。`CreateDedicated` 每次调用才创建一条长期工作线程，调用模块必须在释放时 Dispose；`ThreadPool` 复用 CLR 线程池而不创建固定线程。`SwitchTo` 只改变当前任务后续代码的执行位置，等待它的父任务仍回到父方捕获的执行器。

Promise、状态机 Runner、队列工作项、计时节点和共享等待者均使用有容量上限的池。Release 编译下预热后的 Completed 和 Yield 成功路径不产生托管分配。可以在压测、场景退出和网络重连后检查：

```csharp
MTaskDiagnosticsSnapshot snapshot = MTaskDiagnostics.Capture();
// PoolHits / PoolExpansions / PoolRecycleFailures / ActiveNodes / ActiveTimers
```

`MTaskDiagnostics.MaxRetainedPerType` 默认为 256。首次状态机类型、池扩容、异常、`.Share()` 共享状态和第三方内部 Task 分配应与稳态成功路径分开统计。

## 退出语义

运行期组件释放采用两阶段策略：`OnDisposing()` 先同步关闭 Socket、外部 I/O 等会阻塞任务退场的资源；任务 finally 退出后才调用 `OnDispose()`。

应用退出或停止 Play Mode 则进入快速退出：运行时取消任务、主线程只抽取一次队列，不等待任务 finally 或专用线程 Join。开发环境会输出仍在退场的任务与计时器数量；退出优先保证响应速度，不保证后台工作完成。
