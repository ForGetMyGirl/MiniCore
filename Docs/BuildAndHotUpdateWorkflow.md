# 打包与热更新流程

本文档说明 MiniCore 当前由 **HybridCLR + YooAsset** 组成的 Android/Player 打包与后续热更新流程。它以项目中的实际菜单命令和启动代码为准，适用于 `DefaultPackage`。

日常手动菜单仍可用于本地开发；正式的多目标构建、清单、压缩与远程发布应使用 [MiniCore Deploy](MiniCoreDeploy.md)。桌面工具通过 BatchMode 调用同一组生成与构建能力，不要求开发人员手动切换 Build Settings。

当前项目登记的是有运行目标的程序集清单，而不是一份客户端与服务端共享的 DLL 列表：

| 运行目标 | 默认热更新程序集 |
| --- | --- |
| 客户端 | `MiniCore.Protocol.Common`、`MiniCore.Protocol.Outer`、`MiniCore.HotUpdate.Shared`、`MiniCore.HotUpdate.Client` |
| Dedicated Server | `MiniCore.Protocol.Common`、`MiniCore.Protocol.Outer`、`MiniCore.Protocol.Inner`、`MiniCore.HotUpdate.Shared`、`MiniCore.HotUpdate.Server` |

Dedicated Server 可以通过项目设置中的“DS 额外包含 Client”显式加入 Client 业务程序集；客户端目标没有对称开关，永远不能加入 Server 业务程序集。业务 Common/Outer/Inner 是按运行侧裁剪的 HybridCLR 协议：客户端加载 Common/Outer，DS 加载 Common/Outer/Inner。AOT 程序集不能反向引用这些热更新程序集。

`MiniCore.Protocol.Control/Control.Inner`、`MiniCore.Unity`、`MiniCore.Unity.YooAsset` 和 `MiniCore.Server` 是稳定 AOT 程序集，不生成热更新 DLL。Control 进入客户端与 DS，Control.Inner 只进入 DS。Smoke/Benchmark Runner 位于 `MiniCore.Development.Network`，只在 Editor 或 Development Build 编译，也不属于业务热更新清单。

## 1. 先理解两个构建入口

| 操作 | 产物与职责 | 何时需要 |
| --- | --- | --- |
| `MiniCore/Build/DefaultPackage/完整生成 (Generate All + Build)` | 执行 HybridCLR `Generate All`，同步热更 DLL 与 AOT 元数据，并构建、校验 `DefaultPackage`。 | 首次构建、构建目标或 Development 设置改变、HybridCLR/AOT 配置或泛型使用改变，以及产物缺失时。 |
| `MiniCore/Build/DefaultPackage/热更编译 (Compile Active Target + Build)` | 仅编译当前平台热更新 DLL，复用现有 AOT 产物，并构建、校验 `DefaultPackage`。 | 平台与 Development 设置不变时，日常修改热更新代码或资源。 |

两个入口都会校验热更新 DLL、AOT 元数据、生成的地址表和首包清单是否一致；菜单成功不代表 Android Player 已构建完成，验证首包时仍需执行 Unity 的 Build 或 Build And Run。在线模式可以直接发布新资源版本，客户端只下载哈希发生变化的 Bundle。

## 2. 操作前的共同检查

1. 等待 Unity 脚本编译完成，确认 Console 没有 C# 编译错误。
2. 在 `File > Build Settings` 选定目标平台；Android 包必须先切换到 Android。
3. 确认本次 Player 是否为 Development Build。HybridCLR 生成产物与这个选择必须一致。
4. 确认 `UpdateMainWindow` 的 `packageName` 为 `DefaultPackage`，并按本次目的选择资源模式：本地验证使用 Offline，在线热更验证使用 Host。

> 关键规则：先确定“平台 + Development Build”，再执行 HybridCLR 命令。执行 `Generate/All` 后才改 Development Build，属于产物不匹配，必须重新执行 `Generate/All`。

## 3. 首次构建、切平台或底层改动：完整流程

以下情况必须走完整流程：首次出包、切换 Android/iOS/桌面目标、切换 Development Build、升级 Unity/HybridCLR、调整 HybridCLR 设置/热更新程序集列表、修改 AOT 泛型使用或清理过 `Library`/HybridCLR 产物。

1. 在 Unity Build Settings 设置目标平台与 Development Build。
2. 执行 `MiniCore > Build > DefaultPackage > 完整生成 (Generate All + Build)`。
3. 检查 Console 出现“`MiniCore DefaultPackage 完整构建完成`”。
4. 执行 Unity 的 `Build` 或 `Build And Run`。
5. 在目标设备完成启动验证；网络改动还应执行对应的 Editor 冒烟测试与 Android 压测范围，详见[网络冒烟测试](NetworkSmokeTesting.md)。

本轮网络框架、队列或压测运行器等改动后，为避免使用旧 AOT 产物，也按此完整流程构建一次。

## 4. 日常修改热更新代码：最短安全流程

适用范围：只修改业务 Common/Outer/Inner 协议或 `MiniCore.HotUpdate.Shared/Client/Server` 中的业务/Handler；修改保持向后兼容；平台、运行目标、Development Build、Control 协议和 AOT 泛型需求不变。

1. 执行 `MiniCore > Build > DefaultPackage > 热更编译 (Compile Active Target + Build)`。
2. 根据目的选择：
   - **验证首包**：执行 Unity `Build` / `Build And Run`，安装新 APK 或 Player。
   - **准备在线热更**：不要立即重打 APK，按[第 6 节](#6-发布在线热更新包)发布新 `DefaultPackage`。

`ActiveBuildTarget` 是日常首选：它直接采用当前 Unity 的平台和 Development Build 设置，最不容易把 Debug/Release 产物混在一起。

### CompileDll 各选项怎么选

| 选项 | 用法 |
| --- | --- |
| `ActiveBuildTarget` | 默认选择。当前已切到 Android 且 Development 勾选状态正确时，就用它。 |
| `Android` | 仅在当前活动目标已是 Android 时使用，实际效果与 `ActiveBuildTarget` 等价。 |
| `ActiveBuildTarget_Development` / `Android_Development` | 强制生成 Development 产物。仅在之后的 Unity Build 也明确勾选 Development Build 时使用。 |
| `ActiveBuildTarget_Release` / `Android_Release` | 强制生成 Release 产物。仅在之后的 Unity Build 明确取消 Development Build 时使用。 |

不要用强制 `_Development` 产物去构建 Release Player，也不要用 `_Release` 产物去构建 Development Player；不确定时回到 `ActiveBuildTarget`。

## 5. 不需要构建 DefaultPackage 的情况

仅修改以下内容且不需要改变 YooAsset 资源时，通常可以直接 Build：

- Unity Player 设置、签名、图标或渠道配置；
- 未打入 `DefaultPackage` 的编辑器脚本。

修改 Player/AOT 侧运行时代码时，即使没有改热更新程序集，也可能改变热更 DLL 所需的裁剪 AOT 元数据；按第 3 节执行“完整生成”菜单，不要把它当成“直接 Build”的情形。

只要修改了热更新 DLL 或 `Assets/AssetRes` 中会随 `DefaultPackage` 发布的资源，就执行“热更编译”菜单。若修改属于第 3 节的底层/AOT 情况，则改用“完整生成”菜单。

## 6. 发布在线热更新包

项目启动时由 `UpdateMainWindow` 初始化 `DefaultPackage`，在 Host 模式依次请求远端版本、更新清单、下载缺失资源，然后加载 AOT 元数据，并按当前运行目标登记的依赖顺序加载业务协议和业务代码热更新 DLL。Control 协议已编入 AOT Player。在线热更发布的是 **YooAsset 的完整新包版本**，不是单独上传一个 DLL。

### Dedicated Server 不可变制品与外部配置

`Server/DedicatedServer/Config/MiniCoreServerRuntime.json` 只保留为本地开发结构示例。构建 DS Player 时不再注入实例配置；`DedicatedServerConfigBuildProcessor` 只将与实例无关的 `ServerRoleCatalog.json` 注入 StreamingAssets。

线上 DS 必须使用 `--minicore-config <absolute-path>` 读取服务器本地实例配置。InstanceId、Role、Coordinator、端口、日志路径、管理 Token 和配置哈希都位于制品之外，因此同一个只读 DS 制品可以被多个实例共享。

客户端 Player 构建预处理会同时阻止以下泄漏：

- 服务端实例运行配置被注入或误放进 `Assets/StreamingAssets`；
- `MiniCore.Protocol.Inner` 或 `MiniCore.HotUpdate.Server` 出现在客户端目标清单；
- Control/Control.Inner、业务 Inner、Server DLL bytes 或服务端 Handler 清单出现在客户端热更新资源目录。

因此复制客户端包或反编译客户端程序集都得不到 DS 内网监听、Coordinator 内网地址、完整内部 Role Catalog、Inner DTO 或服务端 Handler。部署时无需复制并修改 DS 目录；多个实例通过不同外部配置引用同一个版本目录。

1. 按改动范围执行第 3 节“完整生成”或第 4 节“热更编译”菜单。
2. 在 YooAsset 构建输出中找到本次 Android / `DefaultPackage` / 时间戳版本目录；两个菜单都使用 UTC `yyyyMMddHHmmss` 作为包版本。
3. 将该版本目录中的清单、版本文件和全部资源文件按 YooAsset 的原始目录结构上传到 Host 模式 `resourcesServerURL` 指向的资源根目录；备用源 `fallbackServerURL` 应部署相同内容。
4. 不要只传变动 DLL，也不要重命名哈希资源文件；客户端先读取版本文件，再按清单下载对应文件。
5. 使用一台已安装旧首包的测试设备启动应用，确认它能获取新版本、下载资源、加载热更新 DLL 并进入游戏。
6. 验证通过后再扩大发布范围；保留上一稳定版本的完整目录，以便服务器侧回退版本文件/资源指向。

MiniCore Deploy 已能通过 SSH 上传 WebGL/YooAsset 静态版本目录、校验哈希并原子切换版本指针；第一版不配置 CDN、TLS 或 Web Server。使用对象存储/CDN 的项目应在自己的 Provider 中完成上传和缓存刷新。

DS、.NET 服务、systemd、反向代理、客户端资源与回滚的通用操作流程见 [MiniCore 框架部署入门](FrameworkDeploymentGettingStarted.md)。文档只提供无真实基础设施信息的模板；生产参数必须保存在仓库之外。

### 在线热更新不能覆盖的改动

以下改动必须通过新的 Player 首包（APK/IPA/桌面包）发布，不能仅发资源包：

- 修改 AOT/Player 侧代码、Unity 原生插件、Android Manifest、签名或 Player 设置；
- 修改 `MiniCore.Protocol.Control/Control.Inner` 的消息、Opcode 注册或 DTO；
- 对业务 Common/Outer/Inner 做字段改号、Opcode 复用等破坏性变更，或旧 Player 缺少新业务 DLL 所需的 AOT 元数据；
- 需要新增或改变 AOT 泛型/元数据，而旧 Player 不具备兼容基础；
- 修改启动器 `UpdateMainWindow`、首包资源模式或远端地址配置；
- Unity、HybridCLR 或底层原生库升级。

是否可以只发热更包的判断原则是：旧 Player 是否已经具备运行新热更 DLL 所需的 AOT 元数据、原生能力与启动逻辑。不确定时按首包发布处理。

## 7. 验证清单

### 首包 / APK 验证

- 对应的“完整生成”或“热更编译”菜单成功；
- Player 正常启动，日志中无 AOT 元数据或热更新 DLL 加载失败；
- 涉及网络时，先通过 `NetworkLoopbackIntegrationTests`，再按[网络冒烟测试](NetworkSmokeTesting.md)选择 RPC 快速、专项或完整 Android 压测；
- 记录 APK 版本、包版本、目标平台和 Development 状态。

### 在线热更新验证

- 新包已上传，主/备用源均可访问；
- 旧首包设备启动后，日志显示版本请求、清单更新和下载成功；
- 客户端实际加载的是新热更 DLL，目标功能可用；
- 网络/协议改动按相应回归范围通过；
- 保留上一个可用远端版本，确认能够回退。

## 8. 常见问题

| 现象 | 原因与处理 |
| --- | --- |
| “热更编译”提示缺少 HotUpdate DLL | 确认当前活动平台正确后重试；若属于首次、切平台或 AOT 改动，改用“完整生成”。 |
| 提示缺少 AOT 元数据或 `AOTGenericReferences` | 执行“完整生成 (Generate All + Build)”。 |
| 修改热更代码后设备仍是旧逻辑 | 多数是未执行“热更编译”菜单，或在线模式未上传新包版本。重新按第 4 节处理并确认版本文件已更新。 |
| Development 与 Release 包行为异常 | 确认 Build Settings 的 Development Build 与 HybridCLR 产物一致；改变勾选状态后执行“完整生成”。 |
| 在线客户端不下载新资源 | 检查 `UpdateMainWindow` 是否为 Host 模式、主/备用 URL 是否正确、版本文件和新版本目录是否已完整上传。 |

## 9. 日常决策速查

```text
是否改了平台、Development Build、Control 协议、Player/AOT 运行时代码、HybridCLR/AOT 配置或泛型/AOT依赖？
  是 → 完整生成 (Generate All + Build) → Build 首包 或 发布完整新资源包
  否 → 是否改了任一当前目标 HotUpdate 业务程序集或 DefaultPackage 资源？
         是 → 热更编译 (Compile Active Target + Build) → Build 首包 或 发布新资源包
         否 → 直接 Build（如仅改 Player 设置、签名或图标）
```

## 10. 验证记录

### 2026-08-24（MiniCore Deploy 主机地址与构建目标同步）

- 地址：主机独立保存 VPC/内网公布地址，实例留空时继承、显式覆盖时保留；监听地址与公布地址继续严格分离。
- 安全：生产策略阻止非 HTTPS/WSS、localhost、回环、通配和私网客户端入口；非生产使用这些入口时需要人工确认。
- 目标：动态新增、删除、禁用或切换 Auth/DB 实例会立即同步构建目标；“仅服务端”包含当前已启用的 DS/Auth/DB。
- 回归：新增依赖零第三方测试框架的自动化回归检查源码；本次仅执行解决方案编译检查，不运行测试、应用或发布流程。

### 2026-08-23（MiniCore Deploy 生产发布安全链）

- 构建：每个目标构建前清理独立输出；`MaintenanceRelease` 执行协议、Opcode、Handler、HybridCLR/AOT 等完整必要生成；拓扑未启用的 Auth/DB 不允许构建或发布旧产物。
- 制品：本地提交和远端解压均使用独立临时目录与原子改名；上传前重验大小和 SHA-256；同版本异内容拒绝；`ContentOnly` 没有完整基线时禁止发布。
- 执行：环境级远程锁覆盖预检到状态持久化；取消可终止构建与上传，版本/服务切换在完成当前原子段后停止。
- 健康：Auth 验证账号库，DatabaseServer 验证游戏库、Coordinator 注册和 RPC；启动失败自动恢复前一版本、配置和服务定义。
- 配置：`configVersion` 独立于 `ReleaseVersion`，DS 配置哈希使用部署器和 Unity/Newtonsoft 均可重建的固定规范字节。
- 验证范围：只执行 C#、Avalonia XAML 和解决方案编译检查；未执行 Player、HybridCLR、YooAsset 构建或任何测试。

### 2026-08-23（MiniCore Deploy、通用 Role 与外部实例配置）

- 构建：新增独立 Avalonia 桌面应用和 JSON BatchMode 桥接，可按顺序生成 Proto、Startup、UI、Handler、HybridCLR、YooAsset 与多个 Player 目标。
- 制品：DS Player、HotUpdate、YooAsset 与 Role Catalog 组成不可变制品；实例配置不再注入 StreamingAssets。
- Role：框架改用通用 `ServerRoleMask` / `ServiceId`，MiniBomber 的 Lobby/Match/Game 位于业务 Server 热更新程序集。
- 发布：生成 ReleaseManifest 与 SHA-256，并支持首次发布、扩容、滚动更新、配置更新、修复、回滚和下线。
- 验证范围：Unity 脚本、MiniCore Server 和 MiniCore Deploy 编译与 Windows/macOS 自包含发布；未运行任何测试。

### 2026-08-15（控制面 AOT、业务协议恢复 HybridCLR）

- 修正：上一版把 Common/Outer/Inner 整体固定为 AOT，虽然切断了链接错误，却同时失去了业务协议热更新能力；本记录取代该过渡结论。
- 边界：Coordinator 查询、注册、心跳、状态和目录同步进入 AOT Control/Control.Inner；MiniBomber、NetworkLab、Match 和 Database 协议进入业务 Common/Outer/Inner 热更新程序集。
- 依赖：固定 `MiniCore.Server` 只引用 Control/Control.Inner；构建设置新增通用 AOT → HotUpdate 反向依赖校验，并禁止 Control DLL bytes 进入热更新资源。
- 兼容：删除的认证 RPC 只在 Opcode Manifest 中永久保号；AuthenticationServer HTTP DTO 不进入 MiniCore RPC。
- 验证范围：执行协议/启动代码生成及 Unity 客户端、Dedicated Server、.NET Server 编译；未执行 Player、HybridCLR、YooAsset 构建或测试。

### 2026-08-15（过渡方案：协议程序集固定为 AOT）

- 症状：Dedicated Server 目标执行“完整生成”时，HybridCLR 的 `GenerateStrippedAOTDlls` 内部 Player 构建在 UnityLinker 阶段报告无法解析 `MiniCore.Protocol.Inner`。
- 根因：固定 AOT `MiniCore.Server` 静态引用 Common/Outer/Inner 控制面协议，但三个协议程序集同时被登记为 HybridCLR 热更新 DLL；生成剥离 AOT DLL 时 HybridCLR 会从临时 Player 移除热更新程序集，形成不合法的 AOT → HotUpdate 反向依赖。
- 过渡修复：曾将 Common/Outer/Inner 整体改为 AOT；该做法已由上面的 Control/Business 分层替代，不再是当前发布规则。
- 验证范围：仅执行 Unity C# 编译检查，不执行 Player、HybridCLR、YooAsset 构建或测试；实际完整生成由发布人员在目标 Editor 中重新执行。

### 2026-08-15（客户端/服务端目标程序集与 DS 配置隔离，历史记录）

- 改造：HotUpdate 拆分为 Shared、Client、Server，项目协议拆分为 Common、Outer、Inner；构建命令按运行目标过滤并生成独立加载清单。
- 配置：当时曾将 DS JSON 源文件移至 `Server/DedicatedServer/Config` 并注入 StreamingAssets；该做法已由 2026-08-23 的外部实例配置取代。
- 防泄漏：客户端构建前校验 Server/Inner 程序集、服务端 Handler 和 DS JSON 均不存在。
- 本次约束：只完成 C# 编译检查；未执行 Player、YooAsset 或 HybridCLR 构建，因此实际产物仍应在后续正式构建流程中确认。

### 2026-08-12（完整生成内部构建与最终资源校验分阶段）

- 症状：点击“完整生成”时，HybridCLR 在 `GenerateStripedAOTDlls` 的内部 Player 构建阶段报告 `MiniCore.Protocol.dll.bytes` 尚未同步，流程在真正复制热更新 DLL 前中断。
- 根因：项目的通用 Player 构建预处理器错误地把 HybridCLR 用于生成裁剪 AOT DLL 的临时构建当成最终 Player 构建，提前检查了只有后续同步阶段才会生成的 YooAsset `.dll.bytes`。
- 修复：MiniCore 发起 `PrebuildCommand.GenerateAll` 时显式进入产物生成阶段；该阶段的内部构建仍同步并校验程序集登记，但跳过最终资源一致性检查。离开生成阶段后先同步 DLL、AOT 元数据和 Bootstrap 地址，再构建并校验 `DefaultPackage`；普通 Player 构建继续执行完整校验。
- 验证：Unity `2021.3.45f2` 隔离工程脚本编译通过。按本次验证约束未执行完整 Player/DefaultPackage 构建；需要在原项目重新点击“完整生成”，以实际产物链确认后续 HybridCLR 与 YooAsset 阶段。

### 2026-08-12（实例协议注册与多热更新程序集生成链）

- 症状：最终隔离编译报错，项目内 Google.Protobuf 版本无法使用 `CodedOutputStream(byte[], offset, length)` 构造函数。
- 根因：该版本只提供整数组写入构造函数；直接写调用方数组头部又会破坏网络帧前缀。
- 修复：`ProtobufSerializer.SerializeInto` 先校验精确正文长度，再使用 `ArrayPool<byte>` 池化缓冲编码并复制到目标区间；没有保留不兼容构造函数，也不产生每包临时数组。
- 验证：Unity `2021.3.45f2` 隔离工程完整脚本编译通过；PB 生成成功，覆盖 `3` 个项目 Proto 和 `39` 个网络协议注册项；Handler 二阶段生成确认 `26` 项且无需更新；开发导航重新生成。按本次要求未运行测试，实际 Player、多 DLL YooAsset 产物和端到端加载仍需在对应构建流程中验证。

相关文档：[架构总览](Architecture.md)、[网络与协议](NetworkLayerAnalysis.md)、[网络冒烟测试](NetworkSmokeTesting.md)、[性能测试指南](PerformanceTestingGuide.md)。
