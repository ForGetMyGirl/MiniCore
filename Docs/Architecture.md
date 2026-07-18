# MiniCore 架构总览

本文描述当前重构后的 MiniCore。它是项目的架构事实来源；旧目录名、`Global.Com`、`MiniCore.Model/Core`、消息对象内 Opcode、JSON 默认网络协议等说法均不再适用。

## 1. 设计目标

- 核心框架可以脱离 Unity 编译和运行；Unity 只负责适配生命周期、时间、日志、Mono/UI 组件。
- 任意业务位置可以通过静态 `Global` 获取组件，但组件必须有明确 owner 与释放时机。
- 客户端与 Dedicated Server 共用协议、网络、定时器与热更新业务程序集。
- 业务消息使用 Protobuf；网络 Opcode 由实际 Handler 绑定自动生成，并保持历史稳定。
- Player 不静态引用业务热更新类型。YooAsset 下载 DLL，HybridCLR 补充 AOT 元数据后，Bootstrap 反射一次启动业务入口。

## 2. 目录与程序集

| 程序集 | 路径 | 是否引用 Unity | 作用 | 可以依赖 |
| --- | --- | --- | --- | --- |
| `MiniCore.Runtime` | `Assets/Scripts/MiniCore/Runtime` | 否 | `Global`、组件基类、Timer、事件、日志门面、时间接口、热更入口契约 | 无 Unity；最底层公共模型 |
| `MiniCore.Serialization` | `Assets/Scripts/MiniCore/Serialization` | 否 | `INetworkSerializer`、Protobuf 默认实现、Newtonsoft Json 对比实现 | Runtime、Protocol、第三方序列化库 |
| `MiniCore.Protocol` | `Assets/Scripts/MiniCore/Protocol` | 否 | 消息角色接口、`OpcodeRegistry`、Proto 生成消息、Parser 注册表 | Runtime、Google.Protobuf |
| `MiniCore.Network` | `Assets/Scripts/MiniCore/Network` | 否 | 收发、会话、RPC、心跳、Handler 基类、TCP/UDP/KCP | Runtime、Protocol、Serialization |
| `MiniCore.Unity` | `Assets/Scripts/MiniCore/Unity` | 是 | `UnityGlobalDriver`、Unity 时间/日志、输入、Mono 与 UI 契约 | Runtime、Serialization；可使用 Unity API |
| `MiniCore.HotUpdate` | `Assets/Scripts/MiniCore/HotUpdate` | 是 | 客户端/服务端业务入口、资源/UI 业务、业务 Handler、生成 Handler 表 | Runtime、Protocol、Serialization、Network、Unity |
| `Project.Bootstrap` | `Assets/Scripts/Project/Bootstrap` | 是 | 场景启动、YooAsset、HybridCLR、DLL 加载、选择 Client/Server Entry | Runtime、Unity、YooAsset、HybridCLR；不可静态引用 HotUpdate |
| `MiniCore.Editor` | `Assets/Scripts/MiniCore/Editor` | Editor | Proto、Opcode、HybridCLR、构建校验与工具窗口 | 运行时程序集与 UnityEditor |

依赖方向只能从上向下，不能反向：`Runtime <- Protocol/Serialization <- Network <- HotUpdate`。`MiniCore.Unity` 是适配层，不应成为 Runtime/Protocol/Network 的依赖。

```mermaid
flowchart BT
    Runtime["MiniCore.Runtime\n纯 C# 生命周期与 Global"]
    Protocol["MiniCore.Protocol\n消息契约与生成代码"]
    Serialization["MiniCore.Serialization\nProtobuf / JSON"]
    Network["MiniCore.Network\n会话、传输、Handler"]
    Unity["MiniCore.Unity\nUnity 适配"]
    HotUpdate["MiniCore.HotUpdate\n业务与 Handler"]
    Bootstrap["Project.Bootstrap\nYooAsset + HybridCLR"]

    Protocol --> Runtime
    Serialization --> Runtime
    Serialization --> Protocol
    Network --> Runtime
    Network --> Protocol
    Network --> Serialization
    Unity --> Runtime
    Unity --> Serialization
    HotUpdate --> Network
    HotUpdate --> Unity
    HotUpdate --> Protocol
    Bootstrap --> Unity
    Bootstrap -. "动态加载" .-> HotUpdate
```

## 3. Global 组件运行时

`Global` 位于 `MiniCore.Runtime`，是全局组件容器的静态门面；内部由 `GlobalRuntime` 管理组件实例、owner 引用计数、Tick 快照与线程校验。业务无需持有 App 或 Context 对象。

### 生命周期规则

| API | 适用场景 | 释放方式 |
| --- | --- | --- |
| `Global.Initialize(provider)` | 显式初始化；首次调用其他 API 时也会以系统时间自动初始化 | `Global.Shutdown()` |
| `Global.Get<T>(owner)` | 只取已存在组件，并增加 owner 引用 | `Global.Remove<T>(owner)` |
| `Global.GetOrAdd<T>(owner[, args])` | 临时或业务组件；首次创建时执行 `Awake` | `Global.Remove<T>(owner)` 或 `ReleaseAll(owner)` |
| `Global.Pin<T>([args])` | 网络、计时器、资源等常驻基础设施 | `Global.Unpin<T>()` |
| `Global.CreateScope(name)` | 场景、战斗、临时玩法等成组生命周期 | `scope.Dispose()` 自动 `ReleaseAll(scope)` |
| `Global.Tick()` | 驱动活动组件的 `MonoUpdate` | 由宿主每帧调用 |
| `Global.ForceRemove<T>()` | 退出、切服等最高层中断 | 不用于普通业务 |

同一 owner 每获取一次就对应持有一份引用。组件在最后一份 owner 引用释放时执行 `Dispose` 并从容器移除。`Pin` 使用 Global 内部 root owner，重复 Pin 不会叠加常驻引用。

```csharp
public sealed class BattleFlow : IDisposable
{
    private readonly GlobalScope scope = Global.CreateScope("Battle");

    public void Start()
    {
        TimerComponent timer = scope.GetOrAdd<TimerComponent>();
    }

    public void Dispose()
    {
        scope.Dispose();
    }
}
```

不要将 `Global` 再包装为 `Global.Com`；不要把 `ForceRemove` 当成普通释放；不要遗漏 owner 释放。

### Unity 与非 Unity 宿主

- Unity：`UnityGlobalDriver` 的 `Awake` 调用 `Global.Initialize(new UnityTimeProvider())`，`Update` 调用 `Global.Tick()`，退出时调用 `Global.Shutdown()`。Tick 使用内部快照复用，不创建每帧 Context。
- 非 Unity：宿主自行调用 `Global.Initialize(customTimeProvider)`，在自己的循环中调用 `Global.Tick()`，进程退出时 `Global.Shutdown()`。Runtime、Protocol、Serialization、Network 不需要 UnityEngine。

## 4. 启动与热更新链

启动场景中的 `UpdateMainWindow` 是稳定 Bootstrap。它不直接引用 `MiniCore.HotUpdate` 中的具体业务类型。

```mermaid
sequenceDiagram
    participant Scene as Bootstrap 场景
    participant Boot as UpdateMainWindow
    participant Yoo as YooAsset
    participant HCLR as HybridCLR
    participant DLL as MiniCore.HotUpdate.dll
    participant Startup as MiniCoreStartup

    Scene->>Boot: Awake
    Boot->>Yoo: 初始化包 / 获取版本 / 更新清单 / 下载
    Boot->>HCLR: 加载配置的 AOT 元数据 DLL
    Boot->>Yoo: LoadAssetAsync(HotUpdate.bytes)
    Yoo-->>Boot: DLL bytes
    Boot->>DLL: Assembly.Load(bytes)
    Boot->>Startup: 反射一次调用静态 StartAsync
    Startup->>Startup: 装配 Client 或 Server 模块并调用 GameStartup
```

Bootstrap 在加载 DLL 后反射调用固定静态类型 `MiniCore.HotUpdate.MiniCoreStartup.StartAsync()`。该方法根据 `Application.isBatchMode` 选择 Client 或 Dedicated Server 的模块列表，随后调用 `GameStartup`。

Server 通过 `-serverPort <port>` 指定端口，缺省为 `20000`。任一步骤失败时不应启动监听。

### 项目启动模块与 GameStartup

通过 `MiniCore > 项目启动配置` 为每个带 `[MiniCoreStartupModule]` 的组件选择 Client、Server 或两者，并填写 Args 参数。生成器解析 `DependsOn` 后写入 `HotUpdate/Generated/Startup/MiniCoreStartup.Generated.cs`，按依赖顺序初始化组件；选择网络模块时同步注册 HotUpdate Handler。

所有模块完成初始化后，生成代码调用项目唯一的 `GameStartup.StartAsync()`。完整接入步骤和新增模块示例见 [项目启动模块配置](StartupModules.md)。

### HybridCLR 与 YooAsset

- `MiniCore.HotUpdate.dll` 必须作为 YooAsset 资源，以固定地址 `HotUpdate`（由 Bootstrap 生成配置定义）进入包。
- AOT 补充元数据由 `HybridClrAotMetadata.Generated.cs` 中的地址表决定；只保留热更代码真实触发的泛型/反射/AOT 泛型调用所需 DLL。
- 先加载 AOT 元数据，再加载 HotUpdate DLL，最后调用 Entry。
- 改动 HotUpdate 业务后，要重新构建 DLL 并更新 YooAsset 包；仅改 C# 源码不会让已发布 Player 获得更新。

## 5. 协议、Handler 与 Opcode

详细步骤见 [网络与协议](NetworkLayerAnalysis.md)。这里记录边界：

- `.proto` 位于仓库根目录 `Proto/`，按业务域组织；不要再按 ClientToServer/ServerToClient 拆分。
- 业务消息在 Proto 中标记 `//[INormalMessage]`、`//[IRpcRequest]`、`//[IRpcResponse]`。
- `IRpcResponse` 必须定义 `code = 1`、`msg = 2`。`RpcId` 不写入 Proto Body，而在 12 字节网络包头中传输，反序列化后写到生成 partial 的运行时属性。
- `MiniCore > Protocol > Generate All` 使用仓库内置 `Proto/Tools/protoc-29.5` 生成 Message、Role 与 Parser Registry。
- Handler 在 `MiniCore.HotUpdate` 中继承 `AMHandler<TMessage>` 或 `ARpcHandler<TRequest, TResponse>`。
- 脚本编译后的 Editor 自动扫描 Handler，写入 `OpcodeManifest.json`、`OpcodeRegistry.Generated.cs` 和 `HotUpdateHandlerRegistry.Generated.cs`。
- **仅绑定了 Handler 的消息才有运行时 Opcode。** RPC Handler 的 Request 和 Response 均会登记；未绑定消息不会被网络层发送。
- 删除协议的号码保留在 Manifest，不能人工重用或重排。

自动生成时使用直接 `new Handler()` 注册，运行时不扫描 AppDomain、不按字符串找 Handler、不用 `Activator.CreateInstance` 构造 Handler。为避免删除或改名 Handler 时旧生成表阻断首轮编译，Editor 会先生成安全空表，随后在编译成功后生成正确的直接注册表。

## 6. 编码与目录规则

### 放置规则

| 内容 | 放置位置 |
| --- | --- |
| 纯组件、事件、计时、日志、时间抽象 | `Runtime/Core`、`Runtime/Model`、`Runtime/Time` |
| 消息角色、Opcode、Protobuf 生成物 | `Protocol/Model`、`Protocol/Generated` |
| 通用序列化器 | `Serialization/Interface`、`Serialization/Protobuf`、`Serialization/NewtonsoftJson` |
| 网络会话、收发、Handler 基类、传输实现 | `Network/Core`、`Network/Handler`、`Network/Transport` |
| Unity 生命周期、输入、UI 契约、Unity serializer | `Unity/Driver`、`Unity/Mono`、`Unity/UI`、`Unity/Serialization` |
| 客户端资源/UI/对象池业务 | `HotUpdate/Client` |
| Client/Server 热更新启动入口 | `HotUpdate/Entry` |
| 业务网络 Handler | `HotUpdate/Network/Handler` |
| Bootstrap 与生成的 AOT 地址表 | `Project/Bootstrap` |
| Unity Editor 生成器、构建校验和性能工具 | `MiniCore/Editor` |

不要为了方便把不同职责的类堆入一个大目录；不要将 Unity API 引入 no-engine 程序集；不要让 Base 程序集静态引用 HotUpdate 类型。

### C# 规则

- 修改 C# 时遵循 `.codex/skills/csharp-performance-conventions/SKILL.md`。
- 新增或修改的方法写中文多行 XML 文档；公共 API 同样需要中文说明。
- 成员按 `UnityProperty`、`Public`、`Internal`、`Private`、`Interface`、`Override` 等 region 整理。
- 新文件 UTF-8 无 BOM；已有文件保持原编码。
- `Update`、网络收发、队列处理、循环中避免 LINQ、闭包、临时集合和无必要 `new`。
- 不回退用户已有改动；生成文件由生成器维护，业务代码不手改。

## 7. 构建前检查

构建前会执行以下校验：

1. Proto 注解、RPC `Code/Msg` 固定字段、生成的 Message/Role/Parser Registry 一致性。
2. Opcode Manifest、当前 Handler、Opcode Registry 与 HotUpdate Handler Registry 一致性。
3. HybridCLR 配置、AOT 元数据与热更新 DLL/YooAsset 资源同步。
4. Dedicated Server 不得引入客户端 UI 依赖。

若校验报错，先修正源 Proto 或 Handler，等待 Unity 自动编译和同步完成，再执行构建；不要手改生成映射绕过校验。
