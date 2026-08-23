# 项目启动与服务配置

MiniCore 将客户端和 Dedicated Server 的启动入口、程序集与配置物理分离。客户端使用“AppService 启动配置 + `GameStartup`”；Dedicated Server 的热更新 `MiniCoreServerStartup` 只是业务薄入口，固定 AOT `DedicatedServerHost` 自动装配框架控制面，再进入按 Role 驱动的业务组件。UI 的窗口开发规则见 [UI 框架](UIFramework.md)，服务端完整边界见 [Dedicated Server 架构](DedicatedServerArchitecture.md)。

## 项目启动流程

客户端启动链：

1. Bootstrap 场景的 `UpdateMainWindow` 初始化 YooAsset、HybridCLR；AOT Control 已随 Player 固定携带，并按顺序加载业务 Common、Outer、Shared、Client 热更新程序集。
2. `MiniCoreStartup.StartAsync()` 为每个已启用且运行目标包含 `Client` 的 AppService 选择 Provider，注册并按依赖顺序初始化。
3. 客户端协议注册表先装配 Coordinator Outer 控制面，再装配业务 Common/Outer 与客户端 Handler。
4. 服务完成后调用 `GameStartup.StartAsync()`，由具体游戏进入首个业务流程。

Dedicated Server 启动链：

1. Dedicated Server Player 固定携带 AOT Control/Control.Inner；Bootstrap 加载业务 Common、Outer、Inner、Shared、Server 热更新程序集。
2. `MiniCoreServerStartup.StartAsync()` 把业务实现交给 AOT `DedicatedServerHost`；宿主从 `--minicore-config <absolute-path>` 指定的外部实例配置读取 Role 和监听参数，并校验随制品发布的 Role Catalog。
3. 宿主自动装配 `INetworkService`、固定控制面协议及 Handler 与必需的 `IServiceDiscoveryService`，随后调用业务入口注册业务协议和 Role Handler。
4. 启动监听；Coordinator Role 创建本地目录，其余 Role 向 Coordinator 注册 `Starting`。
5. 调用 `MiniBomberServerStartupComponent` 创建当前 Role 的业务 Component，最后自动报告 `Ready`；计划停服时先调用 `StopAsync` 报告 `Draining`。

`GameStartup` 因此只属于客户端业务入口。Coordinator 注册、心跳、目录维护与 Dedicated Server 监听都已在业务启动前完成，不依赖 `Application.isBatchMode` 分支。

## MTask 生命周期与退出

启动代码、AppService、AppModule 和业务公开异步 API 均使用 `MTask`，不在业务签名中传递 `CancellationToken`。Owner 域会自动管理入口任务及其普通类子调用；需要后台常驻的工作显式调用 `.Forget()`，由最近 Owner 监督，而不是保存 Handle 或手工创建 Task Scope。

服务或组件释放时，把关闭 Socket、停止监听器、解除外部 I/O 等同步动作放进 `OnDisposing()`；把依赖任务 finally 已退出的资源回收放进 `OnDispose()`。应用退出和 Editor 停止 Play Mode 为快速退出，只请求取消和停止专用执行器，不等待后台任务或线程完整收尾；因此退出路径不能承担存盘、遥测上传等必须完成的业务。

模块需要独立单线程时用 `MTaskExecutors.CreateSingleThread(name)` 创建并由模块持有，在正常 `OnDispose()` 中释放。无线程平台用 `TryCreateSingleThread` 探测并选择主循环方案；`MTask.SwitchTo(executor)` 只切换到已有执行器。不要把“网络线程”当作全局单例，也不要在每次调用时新建线程。

## 配置服务与能力目录

打开 Unity 菜单 `MiniCore > 项目启动配置`：

1. 左侧 **客户端 AppService** 只按运行目标包含 `Client` 的服务接口分组，每个颜色块代表一个 `IAppService` 接口。使用 `Provider` 下拉框选择唯一实现，或选择“不启用”；新发现的接口默认不启用。`DedicatedServer` 专用 Provider 不会出现在左侧，DS 必需服务由固定宿主自动装配；普通 `AComponent` 也不作为左侧启动项配置。
2. 每个接口块显示所选 Provider 的具体类型、职责、全部接口、依赖和运行目标。多接口 Provider 在任一相关接口中选择后会整体同步；关闭其中任一接口也会关闭该 Provider。编辑器不会自动选择依赖，缺失依赖会显示黄色提示。
3. 展开所选实现的“启动参数”区域，可覆盖非敏感 Args 的代码默认值；未勾选覆盖时采用 Args 类中的默认值。Args 仍按具体实现分别保存，切换 Provider 不会删除另一实现已经填写的参数。
4. 右侧“项目能力目录”只读列出所有运行目标下已发现的 Service、AppModule 和已标注具体职责的普通 AComponent，可折叠，用于查找项目当前可调用能力，不会改变启动配置。因此 `IServiceDiscoveryService` 等 DS 专用能力会出现在右侧目录，但不能在客户端配置中选择。左侧会显示服务完整命名空间、接口、依赖、描述与可编辑 Args；右侧按类别显示用途、接口和完整类型名。
5. `AppServiceAttribute`、`AppModuleAttribute`、`ComponentCatalogAttribute` 和传统 `MiniCoreStartupModuleAttribute` 都支持命名参数 `Description`。未填写描述时目录会明确显示“未填写用途说明”。普通组件只有显式添加 `ComponentCatalogAttribute` 才会显示，避免把框架内部实现误当作可调用功能：

   ```csharp
   [ComponentCatalog("匹配队列", Description = "维护当前 Match Role 的等待队列与配对结果。")]
   public sealed class MatchQueueComponent : AComponent
   {
   }
   ```

   服务和 AppModule 同样使用命名参数：`[AppService("网络", typeof(INetworkService), Description = "管理多会话网络通信。")]`、`[AppModule(typeof(IExampleModule), Description = "提供示例业务能力。")]`。
6. 点击“保存启动参数并生成代码”。历史资产若同时启用了同一接口的多个实现，接口块会显示红色冲突；生成器仍会最终拦截冲突、缺失依赖、依赖循环和无效 Args。生成后应等待 Unity 编译完成再运行。

客户端 Provider 与 Args 覆盖值保存于 `Assets/Settings/MiniCoreStartupSettings.asset`，客户端启动代码生成到 `Assets/Scripts/MiniCore/HotUpdate/Generated/Startup/MiniCoreStartup.Generated.cs`。生成器会校验 AppService 接口、Provider、依赖和循环；这份资产不会保存 Dedicated Server Role、监听端口或 Coordinator 地址。

每个 Provider 通过 `AppServiceRuntimeTargets` 声明 `Client`、`DedicatedServer` 或 `All`。客户端生成器只把包含 `Client` 的 Provider 写入启动配置；服务端必需能力设置 `RequiredInDedicatedServer = true`，由 AOT `DedicatedServerHost` 自动装配。`RunInBatchMode` 只保留为单个实现是否允许在无图形环境运行的补充约束，不再承担客户端/服务端边界。

客户端网络与 HTTP 都是可选能力。离线项目可以不启用 `INetworkService` 和 `IHttpService`；Dedicated Server 则始终启用 `INetworkService` 与 `IServiceDiscoveryService`。UI、音频、客户端设置、客户端场景等 Provider 的运行目标仅为 `Client`，不会进入服务端启动链。

## 未启用服务的行为

Provider 选择只控制 AppService 的自动注册和启动，不会裁剪代码、程序集或资源；未选择的实现仍会保留在项目和最终包体中。

通过 `Global.GetService<T>` 获取未启用服务会抛出“未注册应用服务接口”异常。服务本身是可选能力时，应改用 `Global.TryGetService<T>` 并根据返回值决定是否使用：

```csharp
if (Global.TryGetService<MiniCore.UI.IUIService>(this, out MiniCore.UI.IUIService ui))
{
    await ui.OpenAsync<LoginWindow>();
}
```

## 框架资源、配置与 UI 能力

下列旧组件已删除，业务代码必须改为按接口取得 AppService：

| 旧类型（已删除） | 当前实现 | 对外接口 | 依赖 |
| --- | --- | --- | --- |
| `YooAssetResourceComponent` | `YooAssetResourceService` | `IResourceService` | 无 |
| 旧通用资产门面 | 已删除 | 统一使用 `IResourceService` | 无重复封装 |
| CSV 配置链 | `ConfigurationService` | `IConfigurationService` | `IResourceService` |
| `TagsComponent` / `SceneBindingService` | 已删除 | 无 | UI Root 不查找场景 Tag |
| `UIFactoryComponent` 和旧 UIService | `UIService` | `MiniCore.UI.IUIService` | `IResourceService` |

```csharp
IResourceService resources = Global.GetService<IResourceService>(this);
IConfigurationService configurations = Global.GetService<IConfigurationService>(this);
MiniCore.UI.IUIService ui = Global.GetService<MiniCore.UI.IUIService>(this);

await resources.PreloadAssetAsync<GameObject>("Prefabs/Login");
LoginConfig config = await configurations.LoadJsonAsync<LoginConfig>("Login", "Configs/Login");
await ui.OpenAsync<LoginWindow>();

// 在当前 owner 不再需要服务时统一归还引用。
Global.ReleaseAll(this);
```

`IResourceService` 会按 Address 合并并发加载、维护资源与实例引用，并要求调用方分别使用 `ReleaseAsset` / `ReleaseInstance` 完成配对释放。`ConfigurationService` 缓存真实 JSON/Protobuf 反序列化结果，同一 Key 会锁定 Address、类型和格式；`Release(key)` 同时清除配置缓存与资源引用。

`UIService` 从 `UIProjectProfile` 指定的地址自动加载持久化 `ApplicationUIRoot`。框架 Registry 与 Session 位于 `MiniCore.Unity`，业务生成代码通过 `ProjectUIWindowRegistration.Register(UIWindowRegistry.Project)` 注入 View/Presenter 构造委托，不再跨程序集声明 `partial UIWindowRegistry`。完整规则与 KCP 示例见 [UI 框架](UIFramework.md)。

GameObject 复用通过按需 AppModule `IGameObjectPool` 取得。Pool Key 固定为 `Address + ComponentType + Group`；Address 区分不同 Prefab，Group 区分同一 Prefab 的不同业务池，归还时模块通过 owners 表识别来源，不要求业务重复传 Key。

## 内置服务速查

右侧目录的服务说明来自 `[AppService(..., Description = "...")]`。当前内置 Provider 如下；是否启用由对应接口分组中的 Provider 下拉框决定。

| 显示名 | 接口 | 用途 |
| --- | --- | --- |
| YooAsset 资源 | `IResourceService` | 加载、预加载、实例化和释放 YooAsset 资源。 |
| UI 框架 | `MiniCore.UI.IUIService` | 加载 Profile/Root，并管理强类型窗口、导航、缓存和资源租约。 |
| 网络 | `INetworkService` | 管理多会话收发包、RPC、心跳和 Handler 派发。 |
| 计时器 | `ITimerService` | 管理由 `Global.Tick` 驱动的计时任务。 |
| 配置 | `IConfigurationService` | 加载并缓存 JSON、Protobuf 配置。 |
| HTTP | `IHttpService` | 发送 HTTP 请求，支持默认超时和幂等重试。 |
| 音频 | `IAudioService` | 播放并管理 BGM、音效和 UI 音频。 |
| 设备设置 | `IDeviceSettingsService` | 应用画质、分辨率、帧率和垂直同步。 |
| 加密存档 | `ISaveService` | 加密并校验版本化本地存档。 |
| 本地存储路径 | `IStoragePathService` | 在 persistentDataPath 下为存档和本地运行数据提供开发者可配置的相对根目录。 |
| 客户端设置 | `ISettingsService` | 加载、保存并通知偏好设置变化。 |
| 本地运行数据记录 | `ITelemetryService` | 将运行指标、业务事件和异常写入本地 NDJSON 文件。 |

“本地运行数据记录”不会上传数据。它写入本地存储根目录下的 `Telemetry` 子目录，按日创建 NDJSON 滚动文件；写入失败会被隔离，不影响游戏主流程。

## 本地存储路径

`StoragePathService` 是存档和本地运行数据的共同根目录 Provider。启用“本地存储路径”后，在其启动参数 `RelativePath` 中填写开发者定义的相对目录，例如 `ProjectA` 或 `Company/ProjectB`。最终根目录固定为 `Application.persistentDataPath/RelativePath`；代码默认值为兼容旧项目的 `MiniCore`。

服务会在根目录下创建受控一级子目录：`Saves`、`Telemetry`。`RelativePath` 必须为非空相对目录，禁止绝对路径、`.` 与 `..`，因此运行时不能让玩家跳出当前产品的持久化目录。应在项目启动配置中确定该值；更改后会切换到另一套本地数据位置，旧数据不会自动迁移。

## HTTP 地址与启动参数

`HttpServiceInitArgs` 只保留服务级默认值：`DefaultTimeoutSeconds`、`MaxRetryCount` 和 `RetryBackoffMilliseconds`。`BaseUrl` 已移除，不会显示在启动编辑器，也不会由服务拼接相对路径。

所有 `HttpRequest.Url`、`IHttpService.SendJsonAsync` 和 `IHttpService.SendProtobufAsync` 调用都必须传入完整的 HTTP 或 HTTPS 绝对地址：

```csharp
IHttpService http = Global.GetService<IHttpService>(this);
LoginResponse response = await http.SendJsonAsync<LoginRequest, LoginResponse>(
    "https://api.example.com/v1/login",
    new LoginRequest { UserName = userName });
```

环境、区服或登录后下发的地址应由调用业务或项目自己的运行时 Endpoint Provider 管理，不能写入通用 HTTP 服务的启动参数。

## Protobuf 网络与业务 JSON 共存

网络消息默认继续使用 `ProtobufSerializer`，`INetworkService.SetSerializer` 和现有网络测试接口保持不变。这个选择只约束网络消息的编码方式，不会把整个项目锁定为只能使用 Protobuf，也不需要在项目启动配置中增加系统级“JSON/PB Provider”。

任何引用 `MiniCore.Serialization` 的业务程序集都可以在普通数据、调试工具或非网络持久化场景中直接创建 `NewtonsoftJsonSerializer` 进行 JSON 序列化和反序列化。配置服务同时提供 `LoadJsonAsync` 与 `LoadProtobufAsync`，HTTP 服务也同时提供 `SendJsonAsync` 与 `SendProtobufAsync`；这些调用可以和 PB 网络会话同时存在，彼此不覆盖。

## 加密存档

`EncryptedSaveService` 对外保存二进制数据，Protobuf 业务使用 `SaveProtobufAsync` / `LoadProtobufAsync`。服务通过启动参数 `EncryptionKey` 得到 32 字节主密钥，再按逻辑槽位分别派生加密密钥和认证密钥，最后使用 AES-CBC 加密并以 HMAC-SHA256 校验完整性。摘要不能替代加密：AES 隐藏明文，HMAC 检测篡改。

底层存储通过 `IStorageBackend` 的逻辑键 API 选择：普通原生 Player 使用 `StoragePathService` 下的 `Storage` 文件后端，浏览器 WebGL 由平台程序集注册 IndexedDB。`EncryptedSaveService` 显式声明 `IStoragePathService` 启动依赖，使 Editor 模拟和原生平台一定先完成文件回退能力装配；浏览器 Player 仍优先使用预先注册的 IndexedDB 后端，不会把浏览器存档改成文件路径。业务和存档服务不取得浏览器文件路径。未来微信或抖音平台包只需注册对应存储后端，无须修改 Protobuf 和保护格式。

启用流程：

1. 在 **AppService** 中启用“本地存储路径”，满足默认保护存档的原生后端依赖与确定性启动顺序。如需改用产品数据目录，取消 `RelativePath` 的“使用 Args 代码默认值”，再填写目录名；浏览器实际字节由预先注册的 IndexedDB 后端接管。
2. 启用“加密存档”，展开其启动参数，取消 `EncryptionKey` 的“使用 Args 代码默认值”，然后填写一个稳定、非空的开发者口令。
3. 如需自动加载客户端设置，再启用“客户端设置”。
4. 保存启动参数并生成代码。生成器会先启动本地存储路径服务，再启动加密存档及其下游服务。

`EncryptionKey` 变更后，使用旧值保存的所有加密存档都无法读取。参数会以明文保存在 `MiniCoreStartupSettings.asset`；勾选“覆盖默认值”后也会明文出现在生成的启动代码中。因此这套默认实现适合防止简单篡改和统一本地存档格式，**不能**防御逆向、篡改客户端或拥有本机文件访问权限的攻击者。

若发行项目需要更强的密钥保护、跨设备迁移或服务端授权，应实现自己的 `ISaveService`，并通过 `[AppService]` 取代默认保护存档实现；不要把私钥、访问令牌或服务端凭据填写进启动配置。默认保护格式不兼容旧 JSON 存档，不保留静默迁移分支。

## 编写项目启动逻辑

客户端在 [GameStartup.cs](../Assets/Scripts/MiniCore/HotUpdate/Entry/GameStartup.cs) 的 `StartAsync()` 中进入首个业务流程。它只编入 `MiniCore.HotUpdate.Client`，并在客户端 AppService、协议与 Handler 全部装配完成后调用：

```csharp
public sealed class GameStartup : AGameStartup
{
    public override async MTask StartAsync()
    {
        MiniBomberClientStartupComponent client = Global.GetOrAdd<MiniBomberClientStartupComponent>(this);
        await client.InitializeAsync();
    }
}
```

服务端业务由 `MiniCoreServerStartup` 在控制面完成后创建 `MiniBomberServerStartupComponent`。该组件读取已经解析好的 `DedicatedServerRuntimeContext.ActiveRoles`：Match 创建匹配队列 Component，Lobby/Game 创建对应权威业务运行时，Coordinator-only 不创建玩法组件。

两侧的 Startup Component 都只是普通 `AComponent`，不是 AppService。它们负责具体游戏的业务装配；`IServiceDiscoveryService` 才是每个 Dedicated Server 强制存在、负责注册发现的框架 AppService。

## 传统启动模块

`MiniCoreStartupModule` 是遗留的常驻组件启动机制。当前启动配置窗口左侧只编辑 AppService，不再展示普通启动组件；新系统级能力优先建模为 `AppService`，按需能力优先建模为 `AppModule` 或普通 `AComponent`。已有项目中的该特性仍可被生成器识别，但应逐步迁移，避免新增依赖于旧的普通组件启动流程。

```csharp
[MiniCoreStartupModule(
    "排行榜",
    Description = "加载并维护排行榜业务数据。")]
public sealed class RankingComponent : AComponent<RankingComponentInitArgs>
{
    protected override void OnDispose()
    {
        // 只清理组件自身资源；AComponent 会自动归还 Global 引用。
    }
}

public sealed class RankingComponentInitArgs : ComponentInitArgs
{
    public string Endpoint { get; set; } = "ranking";
    public int RetryCount { get; set; } = 3;
}
```

传统 Args 类型仍限于 `string`、`bool`、`int`、`long`、`float`、`double` 和 `enum` 的 public 字段或可写属性；新服务只在左侧编辑它们自己的 Args。YooAsset 资源使用地址或 GUID 字符串作为参数值。密钥、令牌、私钥和动态服务地址不属于启动 Args。
