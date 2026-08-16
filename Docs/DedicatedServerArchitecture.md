# MiniCore 多 Role 与独立 .NET 服务架构

本文描述当前已经实现的客户端认证、Dedicated Server Role、Coordinator 服务发现、独立 AuthenticationServer / DatabaseServer，以及客户端与服务端代码裁剪边界。网络帧和 Handler 基础规则见[网络与协议](NetworkLayerAnalysis.md)，热更新产物流程见[打包与热更新流程](BuildAndHotUpdateWorkflow.md)。

## 1. 总体边界

MiniCore 框架不保存任何具体游戏的认证、Coordinator、Lobby、Match、Game 或 DatabaseServer 地址。当前 MiniBomber 采用如下业务链：

```text
MiniBomberClientNetworkProfile.AuthenticationBaseUrl
  -> HTTPS AuthenticationServer
  -> 返回 AccountId / SessionToken / CoordinatorWebSocketUrl
  -> 客户端用 Outer RPC 连接 Coordinator 并查询 Lobby
  -> Coordinator 返回 LobbyWebSocketUrl
  -> 客户端断开 Coordinator，直连 Lobby
```

- 客户端只静态知道 MiniBomber 自己的认证入口；离线项目不需要该配置资产。
- AuthenticationServer 是可替换业务系统，不属于 MiniCore 启动依赖，也不注册 Coordinator。
- Coordinator 只负责注册、租约、状态和服务目录，不转发业务消息。
- Lobby、Match、Game、DatabaseServer 取得目标地址后使用现有 MiniCore RPC 直连。
- 本阶段没有 Gateway、Coordinator 选主、服务迁移和加密协议扩展。

## 2. 程序集物理隔离

协议程序集：

| 程序集 | 内容 | 客户端 | Dedicated Server |
| --- | --- | --- | --- |
| `MiniCore.Protocol.Control` | 服务类型/地址、客户端查询 Coordinator | AOT 包含 | AOT 包含 |
| `MiniCore.Protocol.Control.Inner` | DS 注册、心跳、状态和目录同步 | 排除 | AOT 包含 |
| `MiniCore.Protocol.Common` | MiniBomber 存档及共享业务 DTO | 热更新包含 | 热更新包含 |
| `MiniCore.Protocol.Outer` | MiniBomber/NetworkLab 客户端业务消息 | 热更新包含 | 热更新包含 |
| `MiniCore.Protocol.Inner` | Match、Database 等服务间 RPC | 排除 | 热更新包含 |

业务程序集：

| 程序集 | 内容 | 客户端 | Dedicated Server |
| --- | --- | --- | --- |
| `MiniCore.HotUpdate.Shared` | 两侧共用规则、地图、资源契约和领域模型 | 包含 | 包含 |
| `MiniCore.HotUpdate.Client` | UI、客户端流程、Outer Handler、`MiniCoreStartup` | 包含 | 默认排除 |
| `MiniCore.HotUpdate.Server` | MiniBomber 服务端组件、业务 Handler 和薄 `MiniCoreServerStartup` | 排除 | 包含 |

固定 AOT 程序集不进入 HybridCLR 清单：`MiniCore.Protocol.Control/Control.Inner` 提供 Coordinator 控制面契约，`MiniCore.Unity` 提供通用 Unity 框架能力，`MiniCore.Unity.YooAsset` 提供 YooAsset Provider，`MiniCore.Server` 提供 DS 配置、固定宿主、Coordinator 控制面与服务发现。这些程序集都不得引用 MiniBomber 业务类型或业务 Common/Outer/Inner 等 HybridCLR 程序集。

`Project/MiniCore/Hot Update Assemblies` 的 `DS 额外包含 Client` 默认关闭；仅开发工具确实需要客户端代码时才打开。客户端目标没有允许包含 Server 的反向选项。

程序集隔离由嵌套 asmdef / asmref、AOT 目标约束和业务 HybridCLR 清单共同实现，不依赖业务源码中的大面积 `#if`。Client asmdef 使用 `UNITY_EDITOR || !UNITY_SERVER`，Server 与 Inner asmdef 使用 `UNITY_EDITOR || UNITY_SERVER`：Editor 可同时生成和检查两侧代码，真正 Player 编译时则只保留目标侧程序集。客户端生成的 Handler 表只直接构造客户端 Handler，服务端生成表只直接构造带 `[ServerHandler]` 的 Handler。

## 3. Dedicated Server 配置与 Role

项目内唯一 DS 源配置：

```text
Server/DedicatedServer/Config/MiniCoreServerRuntime.json
```

它位于 `Assets` 外。Dedicated Server Player 构建时，`DedicatedServerConfigBuildProcessor` 使用 `AddAdditionalPathToStreamingAssets` 将它注入为：

```text
StreamingAssets/MiniCoreServerRuntime.json
```

普通客户端构建不会注入它。部署人员复制同一份 DS 包后，直接修改各副本的 JSON，不需要重新编译。配置结构始终相同，Coordinator 自己也保留 `coordinator` 字段：

```json
{
  "instanceId": "Lobby-01",
  "roles": ["Lobby"],
  "coordinator": { "innerHost": "127.0.0.1", "innerPort": 7000 },
  "listeners": {
    "innerHost": "0.0.0.0", "innerPort": 7100,
    "outerHost": "0.0.0.0", "outerPort": 7101,
    "outerPath": "/minicore"
  },
  "advertised": {
    "innerHost": "10.0.1.11", "innerPort": 7100,
    "outerWebSocketUrl": "wss://lobby.example.com/minicore"
  },
  "persistenceMode": "None"
}
```

`DedicatedServerRole` 是 Flags：`Coordinator=1`、`Lobby=2`、`Match=4`、`Game=8`，`All` 是四者并集。一个进程可以配置多个 Role；同一集群只能部署一个包含 Coordinator 的实例，该约束目前由部署负责。

## 4. 两条启动链

客户端启动入口是 `MiniCore.HotUpdate.MiniCoreStartup.StartAsync`：

```text
装配 Client/All AppService
  -> 注册 AOT Coordinator Outer + 业务 Common/Outer
  -> 注册客户端 Handler
  -> GameStartup.StartAsync
  -> MiniBomberClientStartupComponent
```

Dedicated Server 的反射入口是热更新业务侧 `MiniCoreServerStartup.StartAsync`，它只把 `MiniBomberDedicatedServerApplication` 交给 AOT `DedicatedServerHost`：

```text
读取 StreamingAssets DS 配置
  -> 设置 DedicatedServerRuntimeContext.ActiveRoles
  -> AOT 宿主强制创建 INetworkService
  -> AOT 宿主注册固定控制面协议与 Handler
  -> 业务入口注册业务协议及按 Role Handler
  -> AOT 宿主创建 IServiceDiscoveryService
  -> 启动 Inner TCP 和 Outer WebSocket
  -> 启动 Coordinator 目录或注册当前实例为 Starting
  -> 启动 MiniBomber Role 业务 Component
  -> 报告 Ready
  -> 计划停服时调用 StopAsync 报告 Draining 后再退出
```

服务发现与 Coordinator 固定 Handler 完全位于 AOT `MiniCore.Server`，不进入 HotUpdate 或客户端 `GameStartup`。UI、音频、HTTP、客户端设置等服务也不进入 DS 启动链。运行目标由程序集和 `AppServiceRuntimeTargets` 决定，不再用 `Application.isBatchMode` 代替目标模型。

## 5. Coordinator 控制面

`IServiceDiscoveryService` 是 DS 自动装配的框架 AppService。它只保存 Coordinator 返回的本地目录快照和轮询游标，不是新的独立进程，也不代理业务 RPC。

Role 包含 Coordinator 时创建 `CoordinatorRegistryComponent`；其他 DS 则连接配置中的 Coordinator 并执行：

```text
RegisterServer(Starting)
  -> 返回目录修订号与快照
  -> 业务启动
  -> SetServerState(Ready)
  -> 每 5 秒 ServerHeartbeat 续约并按修订号获取变化
```

普通 DS 的控制面 RPC 使用 `3` 秒超时。断线、RPC 本地错误、注册 `404` 或超时后，服务发现会清空不可继续用于新连接的旧目录，并以 `1/2/4/8/15` 秒上限退避恢复唯一的 Coordinator 会话；重新连接后再次注册相同 `InstanceId/Role`、恢复此前的 `Starting/Ready/Draining` 状态并取得完整目录。包含 Coordinator Role 的本机模式始终走本地目录，不进入远程重连流程。

Coordinator 按 `ServiceKind` 保存同一类型的多个 Ready 实例。查询时每种类型使用独立轮询游标，因此多个 Game 或 Match 实例可以注册相同功能；调用者拿到具体 `InstanceId + Endpoint` 后直连该实例。

服务间调用范式：

```text
IServiceDiscoveryService.TryResolve(ServiceKind.Match)
  -> INetworkService.ConnectTcpSessionAsync(endpoint.InnerHost, endpoint.InnerPort)
  -> INetworkService.CallAsync<TRequest,TResponse>()
```

连接和 RPC 都是现有 `INetworkService` 能力，不再增加 MatchClient、PersistenceClient 或 Coordinator 转发服务。业务可以封装普通 Component 复用连接，但它不是 AppService 或新进程。

## 6. Handler 与业务 Component

服务端 Handler 使用 Role 特性：

```csharp
[ServerHandler(DedicatedServerRole.Match)]
public sealed class EnqueueMatchHandler
    : ARpcHandler<EnqueueMatchRequest, EnqueueMatchResponse>
{
}
```

生成器输出两个注册入口：

- `HotUpdateHandlerRegistration.Register(builder)`：只包含客户端 Handler；
- `ServerHotUpdateHandlerRegistration.Register(builder, activeRoles)`：只注册与当前 Role 有交集的服务端 Handler。

同一 DS 包含所有服务端源码，但未启用的 Role 不会注册对应 Handler。业务 Component 仍由业务入口创建，不加入 AppService 配置。

当前 Match Role 会创建 `MiniBomberMatchServerComponent`，并注册 Inner 入队、取消和成组取出 RPC。Lobby 或其他 DS 发现 Match 地址后直接调用这些 RPC。Lobby/Game 的现有房间和战斗代码位于 Server 程序集，其外部请求同样分别受 Lobby/Game Role 注册表约束。

## 7. Proto 边界

```text
Proto/Control/Common + Outer -> Protocol/Control/Generated -> MiniCore.Protocol.Control (AOT)
Proto/Control/Inner          -> Protocol/Control/Generated -> MiniCore.Protocol.Control.Inner (AOT)
Proto/Business/Common        -> Protocol/Generated/Common  -> MiniCore.Protocol.Common (HybridCLR)
Proto/Business/Outer         -> Protocol/Generated/Outer   -> MiniCore.Protocol.Outer (HybridCLR)
Proto/Business/Inner         -> Protocol/Generated/Inner   -> MiniCore.Protocol.Inner (HybridCLR)
```

- Control 包含服务类型、地址与 Client ↔ Coordinator 查询；Control.Inner 包含 DS 注册、心跳、状态和目录同步。
- 业务 Common 是业务两侧共享的 DTO，Outer 包含 Client ↔ Lobby/Game 及 NetworkLab 消息，Inner 包含 Match 和 Database RPC。
- 客户端热更新清单在编译和发布阶段都不包含业务 Inner 或 Control.Inner。
- Control 变化必须重新构建 Player；遵守追加字段/新消息规则的业务协议可随 HybridCLR/YooAsset 更新。
- 已删除的旧登录/注册 Opcode 继续保留在 `OpcodeManifest.json`，但 DS 不再注册旧账号 Handler。

## 8. MiniBomber 客户端认证配置

`MiniBomberClientNetworkProfile` 是 MiniBomber 自己的 YooAsset ScriptableObject：

```text
EnableNetwork
EnableAuthentication
AuthenticationBaseUrl
```

它不放在 StreamingAssets，也不包含 Coordinator、Lobby、Match、Game 或 DatabaseServer 地址。`EnableNetwork=false` 时 MiniBomber 不创建自己的账号/连接流程。其他 MiniCore 项目不需要该资产。

`AccountSessionComponent` 使用 `IHttpService` 调用认证 API；登录响应下发 Coordinator 地址，随后用 `INetworkService` 查询并直连 Lobby。框架 HTTP 服务仍只接受完整 URL，不拥有业务 BaseUrl。

## 9. 根目录 .NET 10 服务

```text
Server/
  global.json                    固定 10.0.100
  MiniCore.Server.sln
  Shared/
    MiniCore.Server.Protocol     共用 Protobuf
    MiniCore.Server.Rpc          共用帧、Opcode、RpcId 客户端
  AuthenticationServer/
  DatabaseServer/
  DedicatedServer/Config/
```

### AuthenticationServer

- `Microsoft.NET.Sdk.Web` + ASP.NET Core Minimal API；
- `POST /api/auth/register`、`POST /api/auth/login`；
- EF Core 9 + Pomelo 9 + MySQL 8；
- 使用 `IDbContextFactory<AuthenticationDbContext>`；
- 账号表由自己管理，不调用 DatabaseServer；
- 不引用 MiniCore RPC，不注册 Coordinator；
- 登录成功动态下发 `CoordinatorWebSocketUrl`。

### DatabaseServer

- `.NET 10` Worker Service / Generic Host，不需要 WebAPI；
- 使用与 Unity 完全相同的 `4 字节大端长度 + 12 字节 Opcode/RpcId + Protobuf` 帧；
- 通过 Inner TCP 注册为 `ServiceKind.Database`，报告 Ready 并续约；
- 到 Coordinator 的控制 RPC 使用 `3` 秒超时，长连接每 `2` 秒 Ping、`10` 秒无 Pong 失效；控制连接断开时业务 Listener 不停止，并以 `1/2/4/8/15` 秒上限退避重新注册和恢复 Ready；
- EF Core 9 + Pomelo 9 + MySQL 8；
- `IDbContextFactory<GameDbContext>` 每个 RPC 创建并释放一个 DbContext；
- 全局 `SemaphoreSlim` 设置并发上限，满载立即返回 `429 Overloaded`；
- `LoadPlayerData` / `SavePlayerData` 使用 Revision 乐观并发；
- 迁移源码存在，但进程启动不会自动连接并修改表结构。

DbContext 不能做 Singleton。它包含 Change Tracker，且不保证并发线程安全；把多个 RPC 串行排队到一个 DbContext 会降低吞吐，也不能替代数据库事务和并发控制。MySQL 连接由连接池复用，短生命周期 DbContext 不等于每次重新创建物理连接。

`persistenceMode=None` 时 DS 完全不依赖 DatabaseServer；`Database` 时服务发现必须在业务启动前找到 Ready DatabaseServer，不做静默本地降级。

MiniBomber 的数据库业务连接在每次调用前检查 Session，并让并发调用共享同一个连接任务。Load 使用 `5` 秒超时，断线后重新发现 Ready DatabaseServer 并重试一次；Save 使用 `8` 秒超时且不无条件重发。首次创建的 Save 结果未知时，GameCluster 会先重连和 Load：记录已存在即视为成功，仍为 `404` 才再次使用 `ExpectedRevision=0` 创建。最终不可用统一返回 `503 DatabaseUnavailable`，不向客户端泄漏底层 Session 文本。

## 10. 客户端泄漏保护

客户端构建前会检查：

- `Assets/StreamingAssets` 没有 `MiniCoreServerRuntime.json`；
- 客户端热更新清单没有 `MiniCore.Protocol.Inner` 或 `MiniCore.HotUpdate.Server`，AOT 边界没有 Control.Inner；
- 热更资源目录没有 Control/Control.Inner、业务 Inner、Server 或拆分前的混合 DLL；
- 客户端 Handler 表不直接引用服务端 Handler。

业务 `MiniCore.Protocol.Common/Outer/Inner.dll.bytes` 是按运行目标筛选的合法热更产物；Control/Control.Inner、旧混合 `MiniCore.Protocol.dll.bytes` / `MiniCore.HotUpdate.dll.bytes` 不是合法热更产物。切换 Client 与 Dedicated Server 目标后必须重新生成对应 HybridCLR/YooAsset 业务资源，不能复用另一目标的 DLL 目录。

## 11. 部署顺序

1. 为 AuthenticationServer 和 DatabaseServer 分别填写自己项目目录内的 MySQL 8 连接配置。
2. 先按审核流程应用 EF Migration；应用进程不会自动改阿里云数据库。
3. 启动一个包含 Coordinator Role 的 DS。
4. 启动 DatabaseServer；需要数据库的 DS 等待其 Ready。
5. 启动 Lobby、Match、Game 或多 Role DS 副本。
6. 启动 AuthenticationServer，并让登录响应下发 Coordinator 外网 WSS 地址。
7. 客户端只发布 MiniBomber 认证入口配置。

同机部署多个进程时必须给每个副本设置不冲突的监听端口；跨主机部署可以复用相同端口，但 `advertised` 必须填写其他服务或客户端实际可达的地址。
