# MiniCore

> 面向 Unity 中小型项目的模块化游戏框架。

MiniCore 以 Unity 为宿主，把项目运行时、网络、协议、资源热更新和业务代码组织为清晰的程序集层级。开发者通过启动模块配置组装项目能力，在 `GameStartup` 中编写自己的首个业务动作；客户端与 Dedicated Server 共用同一套协议、网络与 HotUpdate 业务代码。

## 设计理念

- **小而可组合**：每个 `AComponent` 负责一项明确能力，按项目需要加载、移除和替换。
- **边界清晰**：Runtime、Protocol、Serialization 和 Network 保持纯 C#；Unity API 集中在适配层与业务层。
- **启动简单**：编辑器扫描启动模块，自动按依赖顺序生成初始化代码；项目只维护一个 `GameStartup`。
- **业务可热更新**：Bootstrap 通过 YooAsset 和 HybridCLR 加载 `MiniCore.HotUpdate.dll`，Player 与业务程序集解耦。
- **网络可验证**：内置 TCP、KCP、UDP、RPC、Protobuf、Handler 生成和本机/跨进程网络冒烟测试。

## 核心能力

| 模块 | 能力 |
| --- | --- |
| `MiniCore.Runtime` | `Global` 组件容器、owner 生命周期、Timer、事件、日志、时间抽象 |
| `MiniCore.Protocol` | Protobuf 消息、网络角色接口、Opcode 与 Parser 注册表 |
| `MiniCore.Serialization` | Protobuf 序列化和 JSON 对照实现 |
| `MiniCore.Network` | TCP / KCP / UDP、Session、RPC、心跳、Handler 派发 |
| `MiniCore.Unity` | Unity 生命周期、时间、日志、Mono/UI 适配 |
| `MiniCore.HotUpdate` | 资源、UI、业务 Handler、项目启动逻辑 |
| `Project.Bootstrap` | YooAsset、HybridCLR、HotUpdate DLL 加载与 Player 启动 |
| `MiniCore.Editor` | 启动模块、Proto、Opcode、HybridCLR 与构建工具 |

## 快速开始

### 1. 打开项目

使用 Unity `2021.3.45f2c1` 打开工程，等待脚本编译完成。Build Settings 中使用 `Assets/Scenes/HotUpdateScene.unity` 作为启动场景。

### 2. 配置启动模块

打开 `MiniCore > 项目启动配置`：

1. 勾选 Client 或 Server 需要的模块。
2. 为带 `AComponent<TArgs>` 的模块填写启动参数。
3. 点击“保存启动参数并生成代码”。

配置保存于 `Assets/Settings/MiniCoreStartupSettings.asset`，生成代码位于 `Assets/Scripts/MiniCore/HotUpdate/Generated/Startup/MiniCoreStartup.Generated.cs`。

### 3. 编写项目入口

在 `Assets/Scripts/MiniCore/HotUpdate/Entry/GameStartup.cs` 中实现项目的首个业务动作：客户端可进入登录或主界面，服务端可启动监听、加载配置或初始化业务服务。

```csharp
public sealed class GameStartup : AGameStartup
{
    public override async Task StartAsync()
    {
        if (Application.isBatchMode)
        {
            NetworkMessageComponent network = Global.Get<NetworkMessageComponent>(this);
            await network.StartKcpServerAsync("0.0.0.0", 20000).AsTask();
            return;
        }

        // 客户端首个业务动作。
    }
}
```

### 4. 使用组件

基础设施使用 `Global.Pin` 常驻；临时流程使用 `GlobalScope` 管理引用生命周期。

```csharp
NetworkMessageComponent network = Global.Pin<NetworkMessageComponent>();

using (GlobalScope battle = Global.CreateScope("Battle"))
{
    TimerComponent timer = battle.GetOrAdd<TimerComponent>();
}
```

### 5. 新增网络消息

1. 在根目录 `Proto/` 中新增或修改业务 `.proto`。
2. 在 Unity 执行 `MiniCore > Protocol > Generate All`。
3. 在 `Assets/Scripts/MiniCore/HotUpdate` 编写 `AMHandler<T>` 或 `ARpcHandler<TRequest, TResponse>`。
4. 等待编译完成，Opcode 与 Handler 注册表会自动同步。

## 典型启动链

```text
HotUpdateScene
  → UpdateMainWindow
  → YooAsset 初始化与资源更新
  → HybridCLR AOT 元数据加载
  → MiniCore.HotUpdate.dll 加载
  → MiniCoreStartup.StartAsync()
  → 自动生成的 Client / Server 模块代码
  → GameStartup.StartAsync()
```

## 推荐阅读

| 想了解什么 | 阅读文档 |
| --- | --- |
| 启动模块、参数配置、`GameStartup` 如何编写 | [项目启动方式](Docs/StartupModules.md) |
| 程序集边界、Global 生命周期、完整启动链 | [架构总览](Docs/Architecture.md) |
| Proto、RPC、Handler、Opcode 的开发流程 | [网络与协议](Docs/NetworkLayerAnalysis.md) |
| TCP/KCP/UDP 的 Editor 与 Player 冒烟测试 | [网络冒烟测试](Docs/NetworkSmokeTesting.md) |
| 性能基线、测试方法和优化路线 | [性能测试指南](Docs/PerformanceTestingGuide.md) / [优化路线图](Docs/OptimizationRoadmap.md) |
| 全部文档索引 | [Docs 文档入口](Docs/README.md) |

## 技术栈

- Unity `2021.3.45f2c1`
- HybridCLR `v8.5.0`
- YooAsset `2.3.18`
- UniTask
- Google.Protobuf `3.29.5`
- Newtonsoft Json
