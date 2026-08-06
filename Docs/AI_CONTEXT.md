# MiniCore AI 项目上下文

这是一份给 AI、新成员和自动化工具的快速上下文。处理代码任务前先读本文件，再按需要阅读 [架构总览](Architecture.md)、[UI 框架](UIFramework.md)、[强类型事件中心](Eventing.md) 与 [网络与协议](NetworkLayerAnalysis.md)。更新 `Docs/` 时同时遵守 [文档维护约定](DocumentationConventions.md)。当代码与本文冲突时，以当前代码和程序集配置为准，并同步修正文档。

## 项目一句话

MiniCore 是 Unity 2021.3 项目：纯 C# 核心通过静态 `Global` 管理组件；Unity 只是适配层；Client 与 Dedicated Server 从 YooAsset 加载同一份 `MiniCore.HotUpdate.dll`，由 HybridCLR 支持热更新。

## 不可违背的架构边界

```text
Runtime <- Protocol / Serialization <- Network <- HotUpdate
Runtime <- Unity                         <- HotUpdate
Unity  <- Project.Bootstrap               -动态加载-> HotUpdate
```

- `MiniCore.Runtime`、`MiniCore.Protocol`、`MiniCore.Serialization`、`MiniCore.Network` 的 asmdef 为 `noEngineReferences: true`：不得使用 `UnityEngine`、`MonoBehaviour`、`UnityEditor` 或 Unity 特有 API。
- `MiniCore.Unity` 是 Unity 时间、日志、驱动、Mono/UI 契约等适配代码的位置。
- `MiniCore.HotUpdate` 承载业务入口、资源/UI 业务与网络 Handler；不要把业务写回 Runtime/Network。
- `Project.Bootstrap` 是稳定程序集：不能静态引用 HotUpdate 类型；加载 DLL 后反射一次调用 `MiniCore.HotUpdate.MiniCoreStartup.StartAsync()`。
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
- `IResourceService` / `YooAssetResourceService`、`IAssetService` / `AssetService`、`MiniCore.UI.IUIService` / `UIService` 已替换并删除旧的 `YooAssetResourceComponent`、`AssetsComponent`、`TagsComponent`、`SceneBindingService`、`UIFactoryComponent` 和旧 UI API；不得重新引入这些旧类型或兼容包装。
- 普通 `AComponent` 不在启动配置左侧登记。需要让开发者发现时使用 `[ComponentCatalog("名称", Description = "具体职责")]`；不要把框架内部装配组件标为目录能力。

## MTask 规则

- 业务层和公开服务契约统一返回 `MTask` / `MTask<T>`；System Task 和 `CancellationToken` 只能出现在 BCL/第三方适配边界。
- `AComponent`、AppService、AppModule 和 `AMTaskBehaviour` 自动拥有任务域。不要新增 Start Handle、Task Scope 或在业务方法间传递 Token。
- 普通 MTask 只等待一次；多个消费者必须显式 `.Share()`。不等待的长寿命任务必须 `.Forget()`，使它转移到 Owner 监督域。
- 长 CPU 循环无法被安全强杀，必须周期调用 `MTask.ThrowIfCancellationRequested()` 或 `await MTask.Yield()`。
- 任务池、Runner、执行器队列和计时器使用有上限池化；可通过 `MTaskDiagnostics.Capture()` 检查命中、扩容、回收失败、活动 Node 和 Timer。
- `AComponent.OnDisposing()` 发生在任务域取消前，只放立即解除阻塞的同步操作，例如关闭 Socket、监听器和外部 I/O；`OnDispose()` 只在任务 finally 退场后做最终清理。
- `MTaskExecutors.Unity` 只代表主线程队列；专用线程必须由模块用 `CreateDedicated(name)` 创建、持有和释放，短时无亲和性计算用 `ThreadPool`。`SwitchTo` 只切换到已有执行器，绝不隐式创建线程。
- Runtime 的 MTask 核心不依赖 UniTask、Burst 或 Cecil。Owner 自动注入是随项目交付的 Unity 2021.3 Editor-only 插件，不进入 Player，也不增加外部 UPM 依赖。
- 应用退出/停止 Play Mode 使用快速退出：只取消任务并抽取一次主线程队列，不等待 finally 或专用线程 Join。开发环境的未退场任务、计时器诊断是退出快照，不代表运行中的泄露；运行期正常释放仍必须收敛这些数量。

## 网络与协议规则

- Proto 根目录是 `Proto/`；业务文件按领域组织，不按 ClientToServer/ServerToClient 拆分。
- 标记只允许写成 `//[INormalMessage]`、`//[IRpcRequest]`、`//[IRpcResponse]`。
- RPC Response 必须拥有 `int32 code = 1;` 与 `string msg = 2;`。
- `RpcId` 位于网络 12 字节包头，不是 Proto 字段；生成 partial 的 `RpcId` 是运行时关联属性，不会被 Protobuf Body 序列化。
- Protobuf 是正式默认序列化方式；`NewtonsoftJsonSerializer` 保留用于迁移与性能对比；`UnityJsonSerializer` 不是正式网络路径。
- 业务 Handler 放在 `Assets/Scripts/MiniCore/HotUpdate`，继承 `AMHandler<T>` 或 `ARpcHandler<TRequest,TResponse>`。
- Opcode 由**已编译的 HotUpdate Handler**反向绑定。没有 Handler 的消息没有运行时 Opcode，不能被网络层发送。
- RPC Handler 会为 Request 与 Response 登记 Opcode。已删除协议的编号保留在 `Proto/Manifest/OpcodeManifest.json`，绝不可重用或重排。
- 不手改 `Protocol/Generated`、`HotUpdate/Generated`、`OpcodeManifest.json`；通过生成器维护。

## 生成与构建规则

1. 修改 `.proto` 后执行 Unity 菜单 `MiniCore > Protocol > Generate All`。
2. 修改/新增/删除 HotUpdate Handler 后，等待脚本编译完成；Opcode 与 Handler 注册表自动同步，**没有 Opcode 手动菜单**。
3. 生成流程使用 `Proto/Tools/protoc-29.5` 中随仓库提交的 Windows x64、macOS x64、macOS arm64 工具。
4. 删除 Handler 时，Editor 先写安全空 Handler 表，使首轮编译不会被旧的直接 `new Handler()` 引用阻断；下一轮自动写入正确表。
5. 修改窗口 Prefab 或 UI View/Presenter 后等待二阶段 `UIWindowRegistry.Generated` 自动生成；Player 不扫描程序集或使用 `Activator` 创建窗口逻辑。
6. 打包前必须让 Console 无 C# 编译错误；Proto、Opcode、UI Registry、HybridCLR、YooAsset 与 Dedicated Server 边界由构建校验器验证。

## 热更新与启动规则

- `UpdateMainWindow` 负责 YooAsset 初始化、版本/清单/下载、AOT 元数据加载、`MiniCore.HotUpdate.dll` 加载，并在最后调用 Entry。
- AOT 元数据先于 HotUpdate DLL 加载。不要把所有剥离 DLL 盲目打入包；以生成的 HybridCLR AOT 地址表为准。
- `MiniCore.HotUpdate.dll` 必须在 YooAsset 包中，地址为 Bootstrap 配置使用的 `HotUpdate`。
- `MiniCoreStartup` 在所有运行形态使用同一份已启用模块和 AppService 清单；启动编辑器按 `IAppService` 接口分组单选 Provider，具体实现的 Args 独立保留。`GameStartup` 根据打包目标负责项目业务启动，复杂玩法应委托给玩法目录下的普通 Startup Component；服务端端口参数为 `-serverPort`，默认 `20000`。
- 网络默认 Protobuf 只约束网络消息编码；业务可以同时直接使用 `NewtonsoftJsonSerializer`，配置和 HTTP 也可分别调用 JSON/PB API，不设置系统级序列化 Provider。
- 启动配置只选择 AppService Provider 并覆盖非敏感 Args。每个接口只能启用一个 Provider；`RequiresServices` 必须在项目启动配置中可用。
- 只有实现 `MiniCore.Service.IAsyncAppService` 的 Provider 才会在生成代码中调用 `InitializeAsync()`；不要对所有具体服务生成接口模式匹配。
- `AppServiceAttribute`、`AppModuleAttribute`、`ComponentCatalogAttribute` 与 `MiniCoreStartupModuleAttribute` 都可填写 `Description`，供启动配置窗口的只读目录展示。
- HTTP 服务不再配置 `BaseUrl`，所有 HTTP 请求必须由调用方传入完整的 HTTP/HTTPS 绝对地址；密钥、令牌、私钥和动态服务地址不得写入启动 Args。
- 本地存储必须通过 `IStoragePathService` 取得根目录。`StoragePathService.RelativePath` 只能填写开发者定义的相对目录；最终路径为产品专属 `persistentDataPath/RelativePath`，代码默认值为兼容旧项目的 `MiniCore`。存档和本地运行数据分别使用其 `Saves`、`Telemetry` 子目录，不能硬编码 `persistentDataPath/MiniCore`。
- `EncryptedSaveService` 通过启动参数 `EncryptionKey` 派生 AES/HMAC 密钥；此参数明文存储于启动配置和生成代码，只适合本地防误改，修改后旧存档不可读。`LocalTelemetryFileService` 的显示名为“本地运行数据记录”，只把指标、事件和异常写入本地存储根目录的 `Telemetry` 子目录，不上传数据。
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
| AppService、启动配置、资源/UI 迁移、加密存档与 HTTP 规则 | `Runtime/Service`、`HotUpdate/Service`、`Unity/Service`、[项目启动与服务配置](StartupModules.md) |
| 网络收发、RPC、传输 | `Network/Core`、`Network/Transport`、[网络与协议](NetworkLayerAnalysis.md) |
| 新协议与 Proto | `Proto/`、`Editor/Protocol/ProtoCodeGenerator.cs`、[网络与协议](NetworkLayerAnalysis.md#2-proto-与生成流程) |
| Opcode/Handler 生成 | `Editor/Opcode*.cs`、`Protocol/Generated/Registry`、`HotUpdate/Generated/Network` |
| UI 窗口、Root、分辨率、安全区域和动画 | `Unity/UI`、`HotUpdate/UI`、`Editor/UI`、[UI 框架](UIFramework.md) |
| MiniBomber 账号、大厅、房间、战斗、三端和热更新联调 | `Demos/MiniBomber`、`Proto/Demos/MiniBomber`、[MiniBomber 全链路 Demo](Demos/MiniBomber.md) |
| 热更启动/打包 | `Project/Bootstrap/UpdateMainWindow.cs`、`HotUpdate/Entry`、`Editor/HybridCLR` |
| 性能测试 | `Assets/Tests/Editor`、[性能测试指南](PerformanceTestingGuide.md) |
| 文档维护 | [文档维护约定](DocumentationConventions.md) |

## 禁止的“省事”做法

- 不新增 `App`/`Context` 链式容器来替代 `Global`。
- 不重新引入 `Global.Com`、`MiniCore.Client`、`MiniCore.Game.Server`、`UnityClientHost` 或 `UnityServerHost`。
- 不让协议对象自行保存/硬编码 Opcode。
- 不给没有 Handler 的消息自动分配 Opcode。
- 不在 Player/Base 程序集中静态引用 HotUpdate 业务类型。
- 不以反射扫描或 `Activator.CreateInstance` 替代 HotUpdate Handler 生成表。
- 不把 Proto、protobuf 工具、Client/Server 业务又放回旧的 `Assets/Scripts/MiniCore/Model`、`Core` 等迁移前目录。
