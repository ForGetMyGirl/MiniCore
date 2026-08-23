# MiniCore 多 Role 与独立服务架构

本文描述通用 Dedicated Server Role、Coordinator 服务发现、外部实例配置、可选 AuthenticationServer / DatabaseServer 和客户端裁剪边界。自动发布操作见[MiniCore Deploy](MiniCoreDeploy.md)，协议与 Handler 基础规则见[网络与协议](NetworkLayerAnalysis.md)。

## 1. 框架与业务边界

MiniCore 联网服务端框架唯一保留的特殊能力是 Coordinator 控制面。框架不固定定义 Lobby、Match、Game、Chat 等业务 Role，也不要求启用 AuthenticationServer 或 DatabaseServer。

- Coordinator：注册、租约、状态、服务目录和轮询发现，不转发业务消息。
- 普通 Dedicated Server：承载项目自己定义的一个或多个 Role。
- AuthenticationServer：可替换业务系统，不是框架启动依赖，也不注册 Coordinator。
- DatabaseServer：可选的示例持久化服务；只有业务选择数据库持久化时才依赖它。
- 客户端：只知道项目明确公开的认证入口或 `clientDiscoverable` 服务标识，不知道完整内部拓扑。

因此不部署 Auth/DB 时，Coordinator 与普通 DS 仍可独立启动、注册、发现和处理不依赖它们的业务。MiniBomber 的认证、账号库和游戏数据库是业务示例，不是 MiniCore Runtime 的强制组成。

## 2. Role 表示

框架使用 `ServerRoleMask` 保存通用 `ulong` 位掩码，只保留：

```text
Coordinator = 1UL << 0
```

项目业务在自己的 Server 热更新程序集定义 Role。MiniBomber 示例：

```csharp
public enum MiniBomberServerRole : ulong
{
    Lobby = 1UL << 1,
    Match = 1UL << 2,
    Game = 1UL << 3
}
```

每个业务 Role 使用 `ServerRoleDefinitionAttribute` 声明稳定键、显示名称和是否允许客户端发现。规则如下：

- 位值必须是非零单一 bit；
- 稳定键和位值发布后不得复用；
- 新增 Role 需要重新构建 Server 热更新程序集和 DS 制品；
- 不需要修改 MiniCore AOT Runtime；
- 控制协议兼容时可以先发布新 Role，再滚动更新其他实例；
- 稳定环境最终仍收敛到同一个 ReleaseVersion。

Role 不是热修改运行中的程序集结构。发布前由项目代码和 Catalog 确定；实例配置只决定该制品中的哪些已知 Role 在当前进程启用。

## 3. Role Catalog

`ServerRoleCatalogGenerator` 扫描项目 Role 定义并生成：

```text
Server/DedicatedServer/Config/ServerRoleCatalog.json
Assets/Scripts/MiniCore/HotUpdate/Client/Generated/PublicServiceIds.Generated.cs
```

Catalog 包含稳定键、`ulong` 位值、显示名称和 `clientDiscoverable`。构建 DS 时 Catalog 随不可变制品进入 StreamingAssets，因为它与实例、服务器地址和端口无关。

客户端不引用完整业务 Role 枚举。只有 `clientDiscoverable=true` 的项目服务才生成公开 `ServiceId` 常量。例如 MiniBomber 客户端知道 Lobby 的公开服务 ID，但不知道 Match、Game 或 Database 的完整内部拓扑。

## 4. Handler 绑定

框架特性持有通用 `ulong RequiredRoleMask`：

```csharp
[ServerHandler(ulong.MaxValue)]
```

项目应提供语义明确的业务包装特性：

```csharp
[MiniBomberServerHandler(MiniBomberServerRole.Match)]
public sealed class EnqueueMatchHandler
    : ARpcHandler<EnqueueMatchRequest, EnqueueMatchResponse>
{
}
```

这不是框架枚举。包装特性接收项目自己的 enum，并把稳定 `ulong` 值交给 `ServerHandlerAttribute`。`OpcodeRegistryGenerator` 生成 `ServerHotUpdateHandlerRegistration.Register(builder, activeRoles)`，只注册与当前实例 Role Mask 有交集的 Handler。

新增 Role 或 Handler 需要重新生成注册表和业务制品，但不要求修改 Coordinator 代码。多个 Role 可以组合在一个进程，现有一体化能力继续保留。

## 5. Coordinator 通用注册协议

Coordinator 使用以下通用信息：

```text
InstanceId
RoleMask 或独立 ServiceId
Inner / Outer Endpoint
Starting / Ready / Draining 状态
ControlProtocolVersion
租约到期时间
```

Coordinator 保存 `ServiceId -> 多个 Ready 实例`，不理解 Lobby、Match、Game 的业务含义。查询时每个 ServiceId 使用独立轮询游标；调用者取得具体 InstanceId 和 Endpoint 后通过现有 `INetworkService` 直连。

普通 DS 执行：

```text
RegisterServer(Starting)
  -> 启动业务 Role
  -> SetServerState(Ready)
  -> 每 5 秒 Heartbeat 续约和同步目录
```

断线后服务发现清空不可用于新连接的旧目录，以 `1/2/4/8/15` 秒上限退避重连并重新注册。包含 Coordinator Role 的一体化进程使用本地目录。

## 6. 两种拓扑

生产默认使用独立 Coordinator：

```text
进程 1：Coordinator
进程 2..N：项目业务 Role
```

小项目可以使用一体化进程：

```text
Coordinator + minibomber.lobby + minibomber.match + minibomber.game
```

两种拓扑使用同一个 Role Mask、同一个 DS Player 和同一条启动链，不增加 `CoordinatorMode` 等额外抽象。MiniCore Deploy 提供两个拓扑预设，自动化发布解决独立 Coordinator 带来的多进程启动负担。

## 7. 不可变 DS 制品

DS Player、HotUpdate、YooAsset 与 Role Catalog 是不可变制品。构建时不注入服务器地址、InstanceId、端口或 Token。

DS 必须通过参数读取外部实例配置：

```text
MiniCoreServer --minicore-config <absolute-path>/MiniCoreServerRuntime.json
```

配置包含：

```json
{
  "environmentId": "production",
  "instanceId": "lobby-01",
  "releaseVersion": "1.4.0",
  "controlProtocolVersion": "1",
  "roles": ["minibomber.lobby"],
  "coordinator": { "innerHost": "10.0.0.10", "innerPort": 7000 },
  "listeners": {
    "innerHost": "0.0.0.0", "innerPort": 7100,
    "outerHost": "0.0.0.0", "outerPort": 7101,
    "outerPath": "/minicore"
  },
  "advertised": {
    "innerHost": "10.0.0.11", "innerPort": 7100,
    "outerWebSocketUrl": "wss://game.example.com/minicore"
  },
  "management": {
    "host": "127.0.0.1", "port": 7199,
    "tokenFile": "/opt/minicore/instances/lobby-01/config/management.token"
  },
  "logPath": "/opt/minicore/instances/lobby-01/logs",
  "persistenceMode": "None",
  "configVersion": "1.4.0",
  "configSha256": "<sha256>"
}
```

同一制品可被多个实例共享；实例之间只改变外部配置、端口、日志目录和服务名。Unity Editor 本地运行也使用显式开发配置路径，不形成线上、线下两套读取逻辑。

## 8. 启动链

客户端仍由热更新 `MiniCoreStartup.StartAsync` 启动 Client/All AppService、协议和客户端业务。

Dedicated Server 由 `MiniCoreServerStartup.StartAsync` 把业务应用交给 AOT `DedicatedServerHost`：

```text
读取 --minicore-config
  -> 校验 Role Catalog 与配置 SHA-256
  -> 设置 DedicatedServerRuntimeContext.ActiveRoles
  -> 装配 INetworkService 和固定控制面协议
  -> 业务注册协议与按 Role Handler
  -> 创建 IServiceDiscoveryService
  -> 启动 Inner/Outer Listener
  -> 启动 Coordinator 目录或注册 Starting
  -> 启动业务 Role
  -> 启动回环管理端
  -> 报告 Ready
```

固定 `MiniCore.Server` 只引用 AOT Control/Control.Inner，不引用任何项目业务 Role 或 HotUpdate Server 类型。

## 9. Drain 与本机管理端

框架管理端只监听 `127.0.0.1`，使用实例本机 Token 文件鉴权。`MiniCore.ServerCtl` 提供 `status`、`health`、`drain`、`drain-status` 和 `shutdown`。

框架扩展点 `IDedicatedServerDrainParticipant` 只定义通用排空契约。项目业务负责：

- 停止接收新玩家、新房间、新比赛或新任务；
- 返回剩余活动量；
- 返回阻塞原因；
- 判断是否已经安全排空。

MiniBomber 实现玩家、房间、比赛和匹配队列阻塞信息，作为业务示例存在，框架不硬编码这些名词。

## 10. Auth 与 DB 可选性

AuthenticationServer：

- ASP.NET Core Minimal API 示例；
- 管理自己的账号数据库；
- 不向 Coordinator 注册；
- 不属于 Coordinator 或 DS 启动依赖。

DatabaseServer：

- .NET Worker 示例；
- 以保留 `FrameworkServiceIds.Database` 注册 Coordinator；
- 使用 Revision 和幂等边界提供 Load/Save；
- 不自动执行数据库 Migration。

`persistenceMode=None` 时 DS 完全不等待 DatabaseServer。`persistenceMode=Database` 是 MiniBomber 业务选择，此时业务启动等待 Ready Database 服务且不静默降级。

## 11. 程序集与 Proto 边界

| 程序集 | 客户端 | Dedicated Server | 内容 |
| --- | --- | --- | --- |
| `MiniCore.Protocol.Control` | AOT | AOT | 服务 ID、地址和客户端查询 |
| `MiniCore.Protocol.Control.Inner` | 排除 | AOT | 注册、心跳、状态和目录同步 |
| `MiniCore.Protocol.Common/Outer` | HotUpdate | HotUpdate | 项目共享和客户端业务协议 |
| `MiniCore.Protocol.Inner` | 排除 | HotUpdate | 项目服务间协议 |
| `MiniCore.HotUpdate.Shared/Client` | 包含 | 按设置 | 共享与客户端业务 |
| `MiniCore.HotUpdate.Server` | 排除 | 包含 | 业务 Role、组件和 Handler |

控制面变化必须发布新 Player。兼容的业务协议、Role 和 Handler 变化可以随 Server 热更新制品发布，但稳定环境最终仍使用同一个 ReleaseVersion。

## 12. 客户端泄漏保护

客户端构建会阻止：

- `MiniCoreServerRuntime.json` 出现在 `Assets/StreamingAssets`；
- `MiniCore.Protocol.Inner`、Control.Inner 或 `MiniCore.HotUpdate.Server` 进入客户端；
- 服务端 Handler 注册表进入客户端热更新资源；
- 完整内部 Role Catalog 生成到客户端业务代码。

客户端只得到项目标为 `clientDiscoverable` 的公开 ServiceId。公开服务地址仍由 Coordinator 在运行时返回。
