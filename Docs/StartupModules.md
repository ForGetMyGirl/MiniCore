# 项目启动与服务配置

MiniCore 通过“AppService 启动配置 + `GameStartup`”完成 HotUpdate 项目的初始化。资源、资产、场景绑定和 UI 已统一迁移到 `MiniCore.Service`；旧组件类型不再保留兼容包装。

## 项目启动流程

1. Bootstrap 场景的 `UpdateMainWindow` 初始化 YooAsset、HybridCLR 并加载 HotUpdate DLL。
2. Bootstrap 根据 Player 模式调用客户端或 Dedicated Server 的稳定入口。
3. `MiniCoreStartup.StartAsync()` 按 Player 模式为每个 AppService 接口选择一个 Provider，使用 `Global.RegisterAppService` 注册，并按依赖顺序启动。
4. 仅实现 `IAsyncAppService` 的服务会在注册后调用并等待 `InitializeAsync()`；没有异步服务的目标生成普通 `Task` 方法，不会生成无效的接口判断。
5. 服务启动完成后，调用项目唯一的 `GameStartup.StartAsync()`。

## 配置服务与能力目录

打开 Unity 菜单 `MiniCore > 项目启动配置`：

1. 在左侧 **AppService** 区按 Client / Server 显式勾选需要启动的服务；同一接口、同一目标只能勾选一个实现。新发现服务默认关闭。普通 `AComponent` 不再作为左侧启动项配置。
2. 展开服务的“启动参数”区域，可覆盖非敏感 Args 的代码默认值；未勾选覆盖时采用 Args 类中的默认值。
3. 右侧“项目能力目录”只读列出已发现的 Service、AppModule 和已标注具体职责的普通 AComponent，可折叠，用于查找项目当前可调用能力，不会改变启动配置。左侧会显示服务完整命名空间、接口、依赖、描述与可编辑 Args；右侧按类别显示用途、接口和完整类型名。
4. `AppServiceAttribute`、`AppModuleAttribute`、`ComponentCatalogAttribute` 和传统 `MiniCoreStartupModuleAttribute` 都支持命名参数 `Description`。未填写描述时目录会明确显示“未填写用途说明”。普通组件只有显式添加 `ComponentCatalogAttribute` 才会显示，避免把框架内部实现误当作可调用功能：

   ```csharp
   [ComponentCatalog("全局监听组件", Description = "集中注册指定节点及其子节点下的 IListener，并批量启动或停止全局监听。")]
   public class GlobalListenerComponent : AComponent
   {
   }
   ```

   服务和 AppModule 同样使用命名参数：`[AppService("网络", typeof(INetworkService), Description = "管理多会话网络通信。")]`、`[AppModule(typeof(IExampleModule), Description = "提供示例业务能力。")]`。
5. 点击“保存启动参数并生成代码”。控制台会指出冲突的接口、缺失依赖或无效 Args；生成后应等待 Unity 编译完成再运行。

服务 Provider 与其 Args 覆盖值保存于 `Assets/Settings/MiniCoreStartupSettings.asset`，启动代码生成到 `Assets/Scripts/MiniCore/HotUpdate/Generated/Startup/MiniCoreStartup.Generated.cs`。生成器会校验 AppService 接口、Provider、依赖和循环；不会因为目标是 Dedicated Server 而强制禁止 Unity 资源或 Canvas 服务。

默认配置应只启用项目实际需要的服务。例如客户端通常启用场景绑定、YooAsset 资源、资产管理、UI、网络和计时器；Dedicated Server 是否启用资源/UI 由项目部署方式决定。

## 已迁移的资源与 UI 服务

下列旧组件已删除，业务代码必须改为按接口取得 AppService：

| 旧类型（已删除） | 当前实现 | 对外接口 | 依赖 |
| --- | --- | --- | --- |
| `YooAssetResourceComponent` | `YooAssetResourceService` | `IResourceService` | 无 |
| `AssetsComponent` | `AssetService` | `IAssetService` | `IResourceService`、`ISceneBindingService` |
| `TagsComponent` | `SceneBindingService` | `ISceneBindingService` | 无 |
| `UIFactoryComponent` | `UIService` | `IUIService` | `IAssetService`、`ISceneBindingService` |

```csharp
IResourceService resources = Global.GetService<IResourceService>(this);
IAssetService assets = Global.GetService<IAssetService>(this);
IUIService ui = Global.GetService<IUIService>(this);

await resources.PreloadAssetAsync<GameObject>("Prefabs/Login");
await ui.OpenAsync<LoginWindow, LoginPresenter>("UI/Login", UICanvasLayer.Main);

// 在当前 owner 不再需要服务时统一归还引用。
Global.ReleaseAll(this);
```

`SceneBindingService` 继续提供主 Canvas、弹窗 Canvas、顶层 Canvas、底层 Canvas、预加载池和可复用对象池等约定场景节点。`AssetService` 与 `UIService` 只依赖上述接口，不依赖具体实现。

## 内置服务速查

右侧目录的服务说明来自 `[AppService(..., Description = "...")]`。当前内置 Provider 如下；是否启用仍由 Client/Server 勾选决定。

| 显示名 | 接口 | 用途 |
| --- | --- | --- |
| YooAsset 资源 | `IResourceService` | 加载、预加载、实例化和释放 YooAsset 资源。 |
| 资产管理 | `IAssetService` | 整合资源加载与场景绑定，管理资产预加载和实例化。 |
| 场景绑定 | `ISceneBindingService` | 提供 Canvas 与对象池根节点。 |
| UI | `IUIService` | 创建、显示、缓存和回收窗口，并绑定 Presenter。 |
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

## 加密存档

`EncryptedSaveService` 只依赖 `IStoragePathService`。它通过启动参数 `EncryptionKey` 接收开发者手动填写的稳定口令：服务先以 SHA-256 得到 32 字节主密钥，再以 HMAC-SHA256 按逻辑槽位派生工作密钥，最后使用 AES-CBC 加密并以 HMAC-SHA256 校验完整性。存档文件位于本地存储根目录的 `Saves` 子目录。

启用流程：

1. 在 **AppService** 中启用“本地存储路径”。如需改用产品数据目录，取消 `RelativePath` 的“使用 Args 代码默认值”，再填写目录名。
2. 启用“加密存档”，展开其启动参数，取消 `EncryptionKey` 的“使用 Args 代码默认值”，然后填写一个稳定、非空的开发者口令。
3. 如需自动加载客户端设置，再启用“客户端设置”。
4. 保存启动参数并生成代码。生成器会先启动本地存储路径服务，再启动加密存档及其下游服务。

`EncryptionKey` 变更后，使用旧值保存的所有加密存档都无法读取。参数会以明文保存在 `MiniCoreStartupSettings.asset`；勾选“覆盖默认值”后也会明文出现在生成的启动代码中。因此这套默认实现适合防止简单篡改和统一本地存档格式，**不能**防御逆向、篡改客户端或拥有本机文件访问权限的攻击者。

若发行项目需要更强的密钥保护、跨设备迁移或服务端授权，应实现自己的 `ISaveService`，并通过 `[AppService]` 取代默认加密存档实现；不要把私钥、访问令牌或服务端凭据填写进启动配置。

## 编写项目启动逻辑

在 [GameStartup.cs](../Assets/Scripts/MiniCore/HotUpdate/Entry/GameStartup.cs) 的 `StartAsync()` 中编写项目的首个业务动作，例如进入登录界面、加载存档或启动服务端监听：

```csharp
public sealed class GameStartup : AGameStartup
{
    public override async Task StartAsync()
    {
        if (Application.isBatchMode)
        {
            INetworkService network = Global.GetService<INetworkService>(this);
            await network.StartKcpServerAsync("0.0.0.0", 20000).AsTask();
            return;
        }

        // 客户端首个业务动作。
    }
}
```

## 传统启动模块

`MiniCoreStartupModule` 是遗留的常驻组件启动机制。当前启动配置窗口左侧只编辑 AppService，不再展示普通启动组件；新系统级能力优先建模为 `AppService`，按需能力优先建模为 `AppModule` 或普通 `AComponent`。已有项目中的该特性仍可被生成器识别，但应逐步迁移，避免新增依赖于旧的普通组件启动流程。

```csharp
[MiniCoreStartupModule(
    "排行榜",
    Description = "加载并维护排行榜业务数据。")]
public sealed class RankingComponent : AComponent<RankingComponentInitArgs>
{
    public override void Dispose()
    {
        Global.ReleaseAll(this);
        base.Dispose();
    }
}

public sealed class RankingComponentInitArgs : ComponentInitArgs
{
    public string Endpoint { get; set; } = "ranking";
    public int RetryCount { get; set; } = 3;
}
```

传统 Args 类型仍限于 `string`、`bool`、`int`、`long`、`float`、`double` 和 `enum` 的 public 字段或可写属性；新服务只在左侧编辑它们自己的 Args。YooAsset 资源使用地址或 GUID 字符串作为参数值。密钥、令牌、私钥和动态服务地址不属于启动 Args。
