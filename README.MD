# MiniCore

> 面向 Unity 中小型项目的模块化游戏框架。

MiniCore 以 Unity 为宿主，把项目运行时、网络、协议、资源热更新和业务代码组织为清晰的程序集层级。开发者通过 AppService 启动配置组装项目能力，在 `GameStartup` 中编写自己的首个业务动作；客户端与 Dedicated Server 共用同一套协议、网络与 HotUpdate 业务代码。

## 设计理念

- **小而可组合**：系统级能力使用可替换的 AppService，普通 `AComponent` 负责局部业务能力。
- **边界清晰**：Runtime、Protocol、Serialization 和 Network 保持纯 C#；Unity API 集中在适配层与业务层。
- **启动简单**：编辑器按 AppService 接口分组单选 Provider，自动按依赖顺序生成初始化代码；项目只维护一个 `GameStartup`，复杂玩法可委托给玩法目录内的普通 Startup Component。
- **异步结构化**：业务统一使用零第三方依赖的 `MTask`；任务自动继承父链与 Owner 生命周期，通常无需逐层传递取消令牌。
- **业务可热更新**：Bootstrap 通过 YooAsset 和 HybridCLR 按依赖顺序加载 `MiniCore.Protocol`、`MiniCore.HotUpdate` 及项目登记的其他热更新 DLL，Player 与业务程序集解耦。
- **网络可验证**：内置 TCP、KCP、UDP、RPC、Protobuf、Handler 生成和本机/跨进程网络冒烟测试。

## 核心能力

| 模块 | 能力 |
| --- | --- |
| `MiniCore.Runtime` | `Global` 组件容器、Owner 生命周期、MTask/任务域/执行器、AppService/AppModule 契约、Timer、事件、日志、时间抽象 |
| `MiniCore.Protocol` | 项目 Protobuf 消息、网络角色 partial 实现及无状态协议登记代码；作为独立热更新程序集先于业务程序集加载 |
| `MiniCore.Serialization` | Protobuf / JSON 编解码器，以及 Protobuf Parser 适配 |
| `MiniCore.Network` | TCP / KCP / UDP / WebSocket、Session、RPC、心跳、消息角色契约、协议 Registry、Handler 抽象与派发 |
| `MiniCore.Unity` | Unity 生命周期、时间、日志、Mono/UI 适配，以及存档、HTTP、音频、设备和运行数据服务 |
| `MiniCore.HotUpdate` | 资源与场景业务、UI 运行时、业务 Handler、Demo 和项目启动逻辑；依赖 `MiniCore.Protocol` |
| `Project.Bootstrap` | YooAsset、HybridCLR、HotUpdate DLL 加载与 Player 启动 |
| `MiniCore.HotUpdateAssembly.Editor` | 项目热更新程序集登记、依赖顺序和启动入口的共享 Editor 配置 |
| `MiniCore.Protocol.Editor` | protoc 调用、稳定 Opcode、角色代码与协议登记代码生成 |
| `MiniCore.Protocol.Handler.Editor` | 扫描全部已登记热更新程序集并生成业务 Handler 直接注册表 |
| `MiniCore.Editor` | 启动、UI、HybridCLR、YooAsset 与通用构建工具 |

## 快速开始

### 1. 打开项目

使用 Unity `2021.3.45f2c1` 打开工程，等待脚本编译完成。Build Settings 中使用 `Assets/Scenes/HotUpdateScene.unity` 作为启动场景。

### 2. 配置服务

打开 `MiniCore > 项目启动配置`：

1. 在 **AppService** 区按接口选择一个 Provider，或选择“不启用”；新接口默认关闭。
2. 仅为服务填写非敏感启动参数，例如超时、重试次数或对象池大小。
3. 在右侧“项目能力目录”查看可用的 Service、AppModule 与已标注职责的普通 `AComponent`。
4. 点击“保存启动参数并生成代码”，等待 Unity 编译完成。

配置保存于 `Assets/Settings/MiniCoreStartupSettings.asset`，生成代码位于 `Assets/Scripts/MiniCore/HotUpdate/Generated/Startup/MiniCoreStartup.Generated.cs`。

未选择 Provider 的服务不会自动注册或启动；它的代码、程序集和资源仍会保留在项目及最终包体中。通过 `Global.GetService<T>` 获取未启用服务会报错；可选服务请使用 `Global.TryGetService<T>` 判断。

### 3. 编写项目入口

在 `Assets/Scripts/MiniCore/HotUpdate/Entry/GameStartup.cs` 中实现项目的首个业务动作：客户端可进入登录或主界面，服务端可启动监听、加载配置或初始化业务服务。

```csharp
public sealed class GameStartup : AGameStartup
{
    public override async MTask StartAsync()
    {
        if (Application.isBatchMode)
        {
            INetworkService network = Global.GetService<INetworkService>(this);
            await network.StartKcpServerAsync("0.0.0.0", 20000);
            return;
        }

        // 客户端首个业务动作。
    }
}
```

### 4. 使用服务与组件

启动配置选择的系统服务只能按接口使用，并在 owner 不再需要时归还引用；局部临时流程仍可使用 `GlobalScope` 管理普通组件生命周期。

```csharp
IResourceService resources = Global.GetService<IResourceService>(this);
MiniCore.UI.IUIService ui = Global.GetService<MiniCore.UI.IUIService>(this);

await resources.PreloadAssetAsync<GameObject>("UI/Login");
await ui.OpenAsync<LoginWindow>();

Global.ReleaseAll(this);

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

### 6. 编写异步业务

公开异步方法返回 `MTask`。普通类中的 MTask 会自动加入当前父任务；组件、服务、模块和 `AMTaskBehaviour` 则是自动取消边界。不要为业务链路传递 `CancellationToken`、创建启动 Handle 或每次切线程都新建线程。

```csharp
public async MTask RefreshAsync()
{
    await MTask.SwitchTo(MTaskExecutors.ThreadPool);
    CalculateWithoutUnityAccess();
    await MTask.SwitchTo(MTaskExecutors.Unity);
    ApplyToView();
}
```

运行期组件释放会先调用 `OnDisposing()` 关闭 Socket 等阻塞 I/O，再等待任务 finally 并调用 `OnDispose()`。关闭 Player 或停止 Play Mode 使用快速退出，不阻塞主线程等待后台任务；详细语义见下方 MTask 文档。

## 典型启动链

```text
HotUpdateScene
  → UpdateMainWindow
  → YooAsset 初始化与资源更新
  → HybridCLR AOT 元数据加载
  → 已登记热更新 DLL 按依赖顺序加载
  → MiniCoreStartup.StartAsync()
  → 自动生成的统一 AppService 注册代码
  → GameStartup.StartAsync()
```

## 推荐阅读

| 想了解什么 | 阅读文档 |
| --- | --- |
| AppService、参数配置、能力目录、`GameStartup` 如何编写 | [项目启动与服务配置](Docs/StartupModules.md) |
| `MTask`、自动取消、Owner 任务域、执行器与快速退出 | [MTask 结构化异步](Docs/MTask.md) |
| 强类型事件、应用/局部频道、订阅生命周期与编辑器诊断 | [强类型事件中心](Docs/Eventing.md) |
| 程序集边界、Global 生命周期、完整启动链 | [架构总览](Docs/Architecture.md) |
| Proto、RPC、Handler、Opcode 的开发流程 | [网络与协议](Docs/NetworkLayerAnalysis.md) |
| Coordinator、多 Role Dedicated Server、服务发现与独立 .NET 服务边界 | [Dedicated Server 架构](Docs/DedicatedServerArchitecture.md) |
| WebGL、未来微信/抖音平台适配、WebSocket、存储与网关边界 | [WebGL 与小游戏平台适配](Docs/WebPlatformAdaptation.md) |
| UI Root、窗口生命周期、强类型 API、Authoring 与 KCP 示例 | [UI 框架](Docs/UIFramework.md) |
| 登录、大厅、房间、状态同步战斗、三端构建与热更新实战 | [MiniBomber 全链路 Demo](Docs/Demos/MiniBomber.md) |
| 从零部署 Coordinator、GameCluster、认证、数据库与客户端 | [框架部署入门](Docs/FrameworkDeploymentGettingStarted.md) |
| TCP/KCP/UDP 的 Editor 与 Player 冒烟测试 | [网络冒烟测试](Docs/NetworkSmokeTesting.md) |
| HybridCLR、YooAsset 首包构建与在线热更新发布 | [打包与热更新流程](Docs/BuildAndHotUpdateWorkflow.md) |
| 性能基线、测试方法和优化路线 | [性能测试指南](Docs/PerformanceTestingGuide.md) / [优化路线图](Docs/OptimizationRoadmap.md) / [阶段性优化总结](Docs/PhaseOptimizationSummary.md) |
| AI 或自动化修改前应遵循的边界与验证 | [AI 项目上下文](Docs/AI_CONTEXT.md) |
| 文档结构、交叉链接、记录归档与部署敏感信息边界 | [文档维护约定](Docs/DocumentationConventions.md) |

服务化迁移、加密存档、HTTP 绝对地址规则和“本地运行数据记录”的行为，均以 [项目启动与服务配置](Docs/StartupModules.md) 为准。

存档业务使用平台无关的逻辑键/字节存储；原生环境由“本地存储路径”定位文件，浏览器 WebGL 使用 IndexedDB。保护存档以 Protobuf 等二进制数据为输入，使用 AES 加密和 HMAC 防篡改，详情见 [加密存档](Docs/StartupModules.md#加密存档)。

## 技术栈

- Unity `2021.3.45f2c1`
- HybridCLR `v8.5.0`
- YooAsset `2.3.18`
- MiniCore MTask（核心无 UniTask、Burst、Cecil 依赖；Unity 2021.3 Owner 注入为 Editor-only 随附插件）
- Google.Protobuf `3.29.5`
- Newtonsoft Json
