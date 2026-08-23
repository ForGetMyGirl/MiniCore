# MiniCore AI 项目上下文

这是一份给 AI、新成员和自动化工具的快速上下文。处理代码任务前先读本文件，再按需要阅读 [架构总览](Architecture.md)、[UI 框架](UIFramework.md)、[强类型事件中心](Eventing.md) 与 [网络与协议](NetworkLayerAnalysis.md)。更新 `Docs/` 时同时遵守 [文档维护约定](DocumentationConventions.md)。当代码与本文冲突时，以当前代码和程序集配置为准，并同步修正文档。

## 项目一句话

MiniCore 是 Unity 2021.3 项目：纯 C# 核心通过静态 `Global` 管理组件；网络能力跨运行形态复用，Coordinator 控制面协议固定为 AOT，MiniBomber 等业务协议和 Shared/Client/Server 业务代码由 HybridCLR 热更新。

## 不可违背的架构边界

```text
Runtime <- Serialization <- Network <- Protocol <- HotUpdate 业务
Runtime <- Unity <- Unity.YooAsset
Runtime / Network / Unity <- Server <- HotUpdate.Server 业务入口
Unity / Unity.YooAsset <- Project.Bootstrap -动态加载-> HotUpdate 业务
Runtime / Network <- Platform.Browser
```

- `MiniCore.Runtime`、`MiniCore.Serialization`、`MiniCore.Network`、`MiniCore.Protocol.Control/Control.Inner`、`MiniCore.Protocol.Common/Outer/Inner` 的 asmdef 为 `noEngineReferences: true`：不得使用 `UnityEngine`、`MonoBehaviour`、`UnityEditor` 或 Unity 特有 API。
- `MiniCore.Unity` 是框架级 Unity 服务、UI Runtime、配置与 GameObject Pool 的位置；`MiniCore.Unity.YooAsset` 只承载 YooAsset Provider 和异步适配。
- `MiniCore.Server` 是固定 Dedicated Server 宿主、配置读取、控制面协议和服务发现的位置，不允许引用 MiniBomber 等具体业务类型。
- `MiniCore.Platform.Browser` 只进入 WebGL，负责 JavaScript WebSocket 客户端适配器和 IndexedDB 存储后端；未来微信、抖音 SDK 必须放独立可选平台程序集，不得写入 Runtime/Network 或业务代码。
- 固定控制面生成到 AOT `MiniCore.Protocol.Control/Control.Inner`；项目业务按 `Common`、`Outer`、`Inner` 生成到三个 HybridCLR 程序集。客户端业务只携带 Common/Outer，DS 携带 Common/Outer/Inner。
- 业务按 `MiniCore.HotUpdate.Shared/Client/Server` 隔离。Client 不得引用 Server 或 Inner；Server 默认不携带 Client。
- `Project.Bootstrap` 是稳定程序集：不能静态引用 HotUpdate 类型；客户端反射调用 `MiniCoreStartup`，DS 反射调用 `MiniCoreServerStartup`。
- AOT `MiniCore.Server` 只能引用 Control/Control.Inner，不能引用业务 Common/Outer/Inner。控制面变化必须发布新 Player；兼容的业务协议变化可以随 HybridCLR/YooAsset 业务版本发布。
- 当前不接入 Actor。未来若接入，作为可选独立程序集，不能突破 Runtime/Network/Protocol 边界。

## Global 规则

- 业务直接用 `Global`，没有 `Global.Com`。
- 临时持有：`Global.GetOrAdd<T>(owner)`，owner 销毁时 `Global.Remove<T>(owner)` 或 `Global.ReleaseAll(owner)`。
- 成组生命周期：`using GlobalScope scope = Global.CreateScope("Name")`；Scope 释放时归还其全部引用。
- 常驻基础设施：`Global.Pin<T>()`；卸载时 `Global.Unpin<T>()`。
- 只有退出、切服等最高层中断可用 `Global.ForceRemove<T>()`。
- 每个 owner 获取一次，就必须释放一次；不要用静态字段或隐式单例绕开引用计数。
- Unity 每帧由 `UnityGlobalDriver.Update -> Global.Tick()` 驱动。不得在此链路每帧 new Context 或分配临时集合。
- 系统级能力使用 `AAppService + [AppService]`，调用方只能通过 `Global.GetService<TInterface>(owner)` 取得接口；不要用 `Global.Get<TConcreteService>` 绕过服务接口。
- `IResourceService` 是唯一资源契约，`YooAssetResourceService` 是独立 AOT Provider；`IConfigurationService`、`MiniCore.UI.IUIService` 与 `IGameObjectPool` 均属于 AOT 框架能力。重复资产门面、CSV 链路、单例式对象池、全局监听聚合器和对应兼容层均已删除，不得重新引入。
- 普通 `AComponent` 不在启动配置左侧登记。需要让开发者发现时使用 `[ComponentCatalog("名称", Description = "具体职责")]`；不要把框架内部装配组件标为目录能力。

## MTask 规则

- 业务层和公开服务契约统一返回 `MTask` / `MTask<T>`；System Task 和 `CancellationToken` 只能出现在 BCL/第三方适配边界。
- `AComponent`、AppService、AppModule 和 `AMTaskBehaviour` 自动拥有任务域。不要新增 Start Handle、Task Scope 或在业务方法间传递 Token。
- 普通 MTask 只等待一次；多个消费者必须显式 `.Share()`。不等待的长寿命任务必须 `.Forget()`，使它转移到 Owner 监督域。
- 长 CPU 循环无法被安全强杀，必须周期调用 `MTask.ThrowIfCancellationRequested()` 或 `await MTask.Yield()`。
- 任务池、Runner、执行器队列和计时器使用有上限池化；可通过 `MTaskDiagnostics.Capture()` 检查命中、扩容、回收失败、活动 Node 和 Timer。
- `AComponent.OnDisposing()` 发生在任务域取消前，只放立即解除阻塞的同步操作，例如关闭 Socket、监听器和外部 I/O；`OnDispose()` 只在任务 finally 退场后做最终清理。
- `MTaskExecutors.Unity` 只代表主线程队列；独立单线程必须由模块用 `CreateSingleThread(name)` 创建、持有和释放，短时无亲和性计算用 `ThreadPool`。无线程平台使用 `TryCreateSingleThread` / `TryGetThreadPool` 探测并回落主循环；`SwitchTo` 只切换到已有执行器，绝不隐式创建线程。
- Runtime 的 MTask 核心不依赖 UniTask、Burst 或 Cecil。Owner 自动注入是随项目交付的 Unity 2021.3 Editor-only 插件，不进入 Player，也不增加外部 UPM 依赖。
- 应用退出/停止 Play Mode 使用快速退出：只取消任务并抽取一次主线程队列，不等待 finally 或专用线程 Join。开发环境的未退场任务、计时器诊断是退出快照，不代表运行中的泄露；运行期正常释放仍必须收敛这些数量。

## 网络与协议规则

- Proto 根目录是 `Proto/`；`Control` 与 `Business` 先区分固定控制面和可热更新业务协议，其下再按 `Common`、`Outer`、`Inner` 分区。Request/Response 不得跨文件拆分。
- 需要网络传输的消息标记只允许写成 `//[INormalMessage]`、`//[IRpcRequest]`、`//[IRpcResponse]`；纯配置/存档 Protobuf 可以无网络角色注解，也不会进入 Opcode/Parser 网络注册表。
- RPC Response 必须拥有 `int32 code = 1;` 与 `string msg = 2;`。
- `RpcId` 位于网络 12 字节包头，不是 Proto 字段；生成 partial 的 `RpcId` 是运行时关联属性，不会被 Protobuf Body 序列化。
- Protobuf 是正式默认序列化方式；`NewtonsoftJsonSerializer` 保留用于迁移与性能对比；`UnityJsonSerializer` 不是正式网络路径。
- `INetworkService` 同时提供连接与监听能力，不按 Client/Server 拆接口；一个进程可以兼任上游客户端和下游服务端。调用前用 `NetworkCapabilities` 判断 TCP、UDP、KCP、WebSocket 的连接/监听能力。
- `CallAsync` 的末尾 `timeoutSeconds` 是单次 RPC 等待时间，默认 `10` 秒且必须大于零；长连接 Ping 每 `2` 秒发送一次，连续 `10` 秒无 Pong 才断开，两者不是同一个参数。控制面 RPC 使用 `3` 秒，客户端查询 Coordinator 使用 `8` 秒，Database Load/Save 分别使用 `5/8` 秒，Match/场景长流程使用 `15` 秒。
- WebSocket 与 TCP 共用 `4 字节大端长度 + 12 字节业务头 + Protobuf Body` 字节流帧；普通浏览器 WebGL 只支持 WS/WSS 客户端。网络入站主循环默认受每帧 `256` 包和 `2 ms` 双预算约束。
- 客户端 Handler 放入 Client 程序集；服务端 Handler 放入 Server 程序集并使用项目包装特性，例如 `[MiniBomberServerHandler(MiniBomberServerRole.X)]`。框架特性只保存通用 `ulong RequiredRoleMask`。
- 带网络角色的 Proto 消息生成稳定 Opcode、Parser 和角色注册；Handler 只做第二阶段处理绑定。无 Handler 的合法出站消息仍可发送。
- 每个 `NetworkService` 持有独立不可变 Registry；启动时由 Builder 原子合并项目协议和 Handler，提交前禁止连接、监听和收发。
- 已删除协议的编号保留在 `Proto/Manifest/OpcodeManifest.json`，绝不可重用或重排。
- 不手改项目配置的 PB 输出目录、`HotUpdate/Generated`、`OpcodeManifest.json`；通过生成器维护。

## 生成与构建规则

1. 修改 `.proto` 后执行 Unity 菜单 `MiniCore > Protocol > Generate All`。
2. Control PB 固定生成到 `Protocol/Control/Generated`；业务 PB、角色、Opcode 和协议注册代码生成到 `Protocol/Generated/Common|Outer|Inner`。两类协议共用一个 Opcode Manifest。
3. 修改/新增/删除 Handler 后，等待脚本编译完成；工具扫描全部已登记热更新程序集并只同步 Handler 直接注册代码。
4. 生成流程使用 `Proto/Tools/protoc-29.5` 中随仓库提交的 Windows x64、macOS x64、macOS arm64 工具。
5. 删除 Handler 时，Editor 先写安全空 Handler 表，使首轮编译不会被旧的直接 `new Handler()` 引用阻断；下一轮自动写入正确表。
6. 修改窗口 Prefab 或 UI View/Presenter 后生成 `UIWindowRoutes.Generated` 与业务侧 `ProjectUIWindowRegistration.Generated`；Player 不扫描程序集或使用 `Activator` 创建窗口逻辑。
7. 打包前必须让 Console 无 C# 编译错误；Proto、Handler、UI Registry、HybridCLR、YooAsset 与 WebGL 平台边界由构建校验器验证。

## 热更新与启动规则

- `UpdateMainWindow` 负责 YooAsset 初始化、版本/清单/下载、AOT 元数据加载、按依赖顺序加载全部已登记热更新 DLL，并在最后调用唯一启动程序集 Entry。
- AOT 元数据先于 HotUpdate DLL 加载。不要把所有剥离 DLL 盲目打入包；以生成的 HybridCLR AOT 地址表为准。
- 客户端热更清单是业务 Common、Outer、Shared、Client；DS 是业务 Common、Outer、Inner、Shared、Server。AOT Control 随客户端和 DS 固定携带，Control.Inner 仅进入 DS；切目标后必须重新生成对应产物。
- `MiniCoreStartup` 只装配客户端 AppService 并进入 `GameStartup`；热更新 `MiniCoreServerStartup` 只是薄入口，固定 AOT `DedicatedServerHost` 读取 DS JSON、装配网络/服务发现和控制面，再调用业务入口并报告 Ready。
- MiniCore 客户端框架不知道任何认证或游戏服务器地址。MiniBomber 客户端只从自己的 YooAsset Profile 读取认证入口；认证响应下发 Coordinator，Coordinator 再下发 Lobby 等地址。
- Coordinator 是控制面，不转发业务 RPC；DS 从 `IServiceDiscoveryService` 取得端点后用现有 `INetworkService` 直连。
- 网络默认 Protobuf 只约束网络消息编码；业务可以同时直接使用 `NewtonsoftJsonSerializer`，配置和 HTTP 也可分别调用 JSON/PB API，不设置系统级序列化 Provider。
- 启动配置只选择 AppService Provider 并覆盖非敏感 Args。每个接口只能启用一个 Provider；`RequiresServices` 必须在项目启动配置中可用。
- 只有实现 `MiniCore.Service.IAsyncAppService` 的 Provider 才会在生成代码中调用 `InitializeAsync()`；不要对所有具体服务生成接口模式匹配。
- `AppServiceAttribute`、`AppModuleAttribute`、`ComponentCatalogAttribute` 与 `MiniCoreStartupModuleAttribute` 都可填写 `Description`，供启动配置窗口的只读目录展示。
- HTTP 服务不再配置 `BaseUrl`，所有 HTTP 请求必须由调用方传入完整的 HTTP/HTTPS 绝对地址；密钥、令牌、私钥和动态服务地址不得写入启动 Args。
- 存档业务只使用 `IStorageBackend` 的逻辑键/字节 API。原生文件后端才通过 `IStoragePathService` 取得根目录；浏览器由 `MiniCore.Platform.Browser` 注册 IndexedDB 后端。不得向业务暴露或硬编码平台路径。
- `ISaveService` 保存二进制数据，Protobuf 类型通过扩展方法读写。`EncryptedSaveService` 派生独立 AES/HMAC 密钥并采用 Encrypt-then-MAC；配置口令只适合本地防误改，修改后旧存档不可读。`LocalTelemetryFileService` 只把运行数据写入原生本地目录，不上传数据。
- Base 程序集不依赖具体业务类；Entry 的反射创建仅发生一次，Handler 运行时注册使用生成的直接构造，不扫描 AppDomain，不用字符串/`Activator` 创建 Handler。

## C# 与仓库操作规则

- 修改 C# 前阅读并遵守 `.codex/skills/csharp-performance-conventions/SKILL.md`。
- 所有新增或修改方法都要有中文、多行 XML 注释；公共类/接口/属性/事件也写中文注释。
- 被修改的类使用访问级别 region；Unity 对象字段放在 `UnityProperty` region。
- 新 C# 文件 UTF-8 无 BOM；保留已有文件编码。
- 热路径避免 LINQ、闭包、字符串拼接、临时数组/集合、重复委托与装箱；优先已有对象池和缓存。
- 搜索优先 `rg`。手工编辑使用 `apply_patch`。不要用破坏性 git 命令，不要回退用户已有的脏工作区改动。
- 改动实现后必须做与风险匹配的验证；涉及 Unity C# 编译时，至少在隔离副本进行 Unity batchmode 编译检查，再交付。

## 阅读地图

| 任务 | 先读代码/文档 |
| --- | --- |
| Global、组件生命周期、纯 C# 服务 | `Runtime/Core/Global`、[架构总览](Architecture.md#3-global-组件运行时) |
| AppService、启动配置、资源/UI、配置、对象池、加密存档与 HTTP 规则 | `Runtime/Service`、`Unity/Service`、`Unity/UI`、`Unity/Pooling`、`Unity/YooAsset`、[项目启动与服务配置](StartupModules.md) |
| 网络收发、RPC、传输 | `Network/Core`、`Network/Transport`、[网络与协议](NetworkLayerAnalysis.md) |
| 浏览器 WebGL、小游戏平台边界、网关 | `Platform/Browser`、`Plugins/MiniCore/Browser`、[WebGL 与小游戏平台适配](WebPlatformAdaptation.md) |
| 新协议与 Proto | `Proto/`、`Editor/Protocol/ProtoCodeGenerator.cs`、[网络与协议](NetworkLayerAnalysis.md#2-proto-与生成流程) |
| Opcode/Handler 生成 | `Editor/Protocol`、项目 PB 输出目录、`HotUpdate/Generated/Network` |
| UI 窗口、Root、分辨率、安全区域和动画 | `Unity/UI`、`HotUpdate/Demos/*/Client/UI`、`HotUpdate/UI/Generated`、`Editor/UI`、[UI 框架](UIFramework.md) |
| MiniBomber 账号、大厅、房间、战斗、三端和热更新联调 | `Demos/MiniBomber`、`Proto/Business`、[MiniBomber 全链路 Demo](Demos/MiniBomber.md)、[框架部署入门](FrameworkDeploymentGettingStarted.md)、[多 Role 服务端架构](DedicatedServerArchitecture.md) |
| 桌面自动构建、不可变制品、SSH 发布、滚动更新与回滚 | `Tools/MiniCore.Deploy`、[MiniCore Deploy](MiniCoreDeploy.md)、[打包与热更新流程](BuildAndHotUpdateWorkflow.md) |
| 热更启动/打包 | `Project/Bootstrap/UpdateMainWindow.cs`、`HotUpdate/Entry`、`Editor/HybridCLR` |
| Development Runner 与性能测试 | `Assets/Scripts/MiniCore/Development`、`Assets/Tests/Editor`、[性能测试指南](PerformanceTestingGuide.md) |
| 文档维护 | [文档维护约定](DocumentationConventions.md) |

## 禁止的“省事”做法

- 不新增 `App`/`Context` 链式容器来替代 `Global`。
- 不重新引入 `Global.Com`、`MiniCore.Client`、`MiniCore.Game.Server`、`UnityClientHost` 或 `UnityServerHost`。
- 不让协议对象自行保存/硬编码 Opcode。
- 不由 Handler 反向决定 Opcode；只有带 Proto 网络角色的消息才分配 Opcode。
- 不在 Player/Base 程序集中静态引用 HotUpdate 业务类型。
- 不按 Dedicated Server、Client、微信或抖音拆互斥网络接口；业务程序集可以按运行侧裁剪，不让平台宏渗入业务代码。
- 不以反射扫描或 `Activator.CreateInstance` 替代 HotUpdate Handler 生成表。
- 不把 Proto、protobuf 工具、Client/Server 业务又放回旧的 `Assets/Scripts/MiniCore/Model`、`Core` 等迁移前目录。
