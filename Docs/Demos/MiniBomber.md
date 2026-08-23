# MiniBomber 全链路 Demo 制作与验证

本文档对应 `Demos/MiniBomber` 的当前实现，用于展示独立认证服务、Coordinator 服务发现、多 Role Dedicated Server、数据库服务、服务器权威状态同步、UI Root、场景切换、YooAsset 和 HybridCLR 热更新链路。框架级部署约束见 [Dedicated Server 架构](../DedicatedServerArchitecture.md)，从零开始的通用操作流程见 [MiniCore 框架部署入门](../FrameworkDeploymentGettingStarted.md)。

## 1. 当前实现边界

已实现的代码链路：

- `Proto/Business/Outer/MiniBomber.proto` 中的客户端可见 Lobby/Game 业务协议，以及 `Proto/Business/Inner/Match.proto` 中只对服务端可见的匹配协议。
- Dedicated Server 权威房间和 `30Hz` 固定时间步战斗，客户端按 `30Hz` 发送输入，服务器按 `15Hz` 广播动态快照。
- 整数毫米权威坐标、整数余数移动、格子阻挡、炸弹、连锁爆炸、击杀、复活保护和服务器排名。
- 根目录 `Server/AuthenticationServer` 提供独立 HTTP 注册/登录，并在登录响应中下发 Coordinator WebSocket 地址。
- Coordinator 维护 DS 注册、心跳、Starting/Ready/Draining 与服务目录；不转发业务 RPC。
- Lobby、Match、Game Role 使用同一不可变 DS 制品，由每个实例的外部 JSON 决定当前进程注册哪些 Handler 和创建哪些业务 Component。
- 根目录 `Server/DatabaseServer` 使用 MiniCore Inner RPC、EF Core/Pomelo/MySQL，并以保留的 `FrameworkServiceIds.Database` 注册；DS 可以通过 `persistenceMode=None` 完全不使用它。
- 大厅创建/加入/刷新房间，房主设置、准备、开始、房主转移和战斗加载超时。
- Windows 键盘和 Android 虚拟摇杆/炸弹按钮的统一输入；Android 支持持续移动时同时放置炸弹，并在 Demo 运行期间保持屏幕常亮。
- 客户端业务资产 `MiniBomberClientNetworkProfile` 只保存认证 HTTPS 入口；Coordinator、Lobby、Game、Match、Database 地址均不进入客户端配置。
- 登录、大厅、房间、战斗、成绩和重连的 Client Flow、View 与 Presenter。
- 客户端采用 `AComponent + MVP`：纯 C# Model 保存长期业务数据，Component 负责 PB 转换、命令和唯一写入，Presenter 按需投影，View 只操作私有 Unity 控件。
- 客户端展示插值、简单本地预测、小误差收敛和超过 `0.75` 格的直接校正。预测不参与权威判定。

需要项目制作者完成的表现资产：

- 11 个 UGUI 窗口 Prefab 的美术层级和字段绑定。
- 玩家、炸弹、爆炸格、墙、木箱和道具的世界 Prefab。
- BattleScene 中的 Prefab、Input Actions、Camera 和地图引用。
- 每个平台的签名、图标、远程资源 URL 和实际构建产物。

### 源码与程序集边界

MiniBomber 手写运行时代码仍位于 `Assets/Scripts/MiniCore/HotUpdate/Demos/MiniBomber`，但通过 asmref 编入三个不同程序集：共享 DTO/规则进入 `MiniCore.HotUpdate.Shared`，客户端 UI/Flow/Handler 进入 `MiniCore.HotUpdate.Client`，服务端权威状态/Role Handler 进入 `MiniCore.HotUpdate.Server`。服务端程序集不会参与客户端 Player 编译或热更新清单。

Demo 的 Editor 工具物理位于 `HotUpdate/Demos/MiniBomber/Editor`，但通过 `MiniCore.Editor.asmref` 编入 `MiniCore.Editor`。客户端可见业务协议生成到热更新 `MiniCore.Protocol.Outer`，共享类型生成到热更新 `MiniCore.Protocol.Common`，Match 和 Database RPC 生成到热更新 `MiniCore.Protocol.Inner`；Coordinator 控制面单独生成到 AOT Control/Control.Inner。场景和 Prefab 继续位于 `Assets/Scenes/Demos/MiniBomber` 与 `Assets/AssetRes/Demos/MiniBomber`。

### 客户端 `AComponent + MVP` 边界

```text
PB Message（短生命周期）
→ Handler / AComponent 复制或增量归并
→ Account/Lobby/Room/Battle/Flow Model（长期纯数据）
→ Presenter 按需提取与投影
→ 简单参数或窗口专用 ViewData
→ View 刷新私有 Unity 控件
```

- PB 不池化，不保存到 Model，不进入 Presenter 或 View；网络字节缓冲池保持原实现。
- Model 不引用 PB、Unity、网络服务或 UI。集合只读暴露，所属 Component 负责写入并发布变化事件。
- `AccountSessionComponent、LobbyComponent、RoomComponent、BattleClientComponent` 按业务域维护 Model，不按窗口拆 Component。
- `MiniBomberClientFlowComponent` 只维护场景、窗口、重连和跨业务流程 Model，不复制账号、房间或战斗数据，也不保存按钮状态和格式化 UI 文本。
- Lobby、Room、MatchResult 使用窗口专用 ViewData；Login、Register、CreateRoom、Loading、Reconnect、Toast、NetworkDebug 使用明确参数。Battle HUD 按时间、排名、击杀事件和性能诊断分区刷新。
- ViewData 只含当前窗口使用的字段，Presenter 不把完整 Model 传给 View；ViewData 不回写 Component，也不是权威业务状态。

## 2. 启动与场景时序

Bootstrap 只负责 YooAsset、AOT 补充元数据和 HotUpdate DLL。加载 DLL 后它直接调用 `MiniCoreStartup` 和 `GameStartup`，不再先进入一个中转业务场景。

```text
客户端 HotUpdateScene
  → 更新资源与加载 HotUpdate DLL
  → MiniCoreStartup.StartAsync
  → GameStartup.StartAsync
  → MiniBomberClientStartupComponent
  → 加载 MiniBomberClientNetworkProfile
  → EnableNetwork=true 时创建 MiniBomber Client Components
  → YooAssetSceneService 切换 LoginScene
  → UIService 自动加载 ApplicationUIRoot
  → 打开 LoginWindow
```

```text
Dedicated ServerBootstrapScene
  → 更新资源与加载 HotUpdate DLL
  → MiniCoreServerStartup.StartAsync
  → 把 MiniBomberDedicatedServerApplication 交给 AOT DedicatedServerHost
  → AOT 宿主读取 --minicore-config 指定的外部 MiniCoreServerRuntime.json
  → AOT 注册网络、控制面与服务发现，业务注册 Role Handler
  → 启动监听并启动/注册 Coordinator 控制面
  → MiniBomberServerStartupComponent
  → 按 Role 创建 Match/Lobby/Game 业务 Component
  → 报告 Ready
```

Dedicated Server 通常以 `-batchmode` 运行，但运行形态不是由这个参数决定：服务端 Player 的目标程序集清单直接指向薄 `MiniCoreServerStartup`，系统级注册发现由 AOT `DedicatedServerHost` 在调用业务前完成；`GameStartup` 只存在于客户端程序集。

- 客户端：`MiniBomberClientStartupComponent` 加载共享规则和仅含认证入口的网络 Profile；`EnableNetwork=false` 时不进入任何 MiniBomber 网络流程。
- Coordinator：创建 `CoordinatorRegistryComponent`，维护控制面目录；Coordinator-only 不创建玩法运行时。
- Match：创建普通业务组件 `MiniBomberMatchServerComponent`；Match Handler 只在包含 Match Role 的进程注册。
- Lobby/Game：加载共享规则与地图，再创建当前权威业务运行时。
- 监听地址、广播地址、Role 与 Coordinator 内网地址全部来自实例外部 `MiniCoreServerRuntime.json`，不读取客户端资产，也不需要重新编译。

客户端流程窗口的完成边界不是“场景句柄加载结束”，而是“场景加载、目标数据刷新和目标窗口进入 Active 全部完成”。`SceneLoadingWindow` 在此边界之后才关闭，因此登录窗口不会在大厅尚未可用时重新露出。创建房间同样先完成 RoomWindow 导航，再关闭发起操作的 Popup。

客户端和服务端 Startup Component 都是无 `[ComponentCatalog]` 的普通业务 `AComponent`，不是框架新服务。框架级强制能力是 DS 专用的 `IServiceDiscoveryService`；它负责当前进程的注册、心跳、目录和状态上报，业务组件只消费已经准备好的 Role 与服务目录。

### 认证与地址发现

客户端默认连接流程：

```text
MiniBomberClientNetworkProfile.AuthenticationBaseUrl
  → HTTPS POST /api/auth/login
  → 返回 AccountId、SessionToken、CoordinatorWebSocketUrl
  → 客户端临时直连 Coordinator，ResolveService(Lobby)
  → Coordinator 返回 LobbyWebSocketUrl
  → 客户端关闭 Coordinator 会话并直连 Lobby
```

`AuthenticationServer` 是可替换的 MiniBomber 业务实现，不属于 MiniCore 启动依赖，也不注册 Coordinator。它使用独立账号库，不通过 DatabaseServer。当前账号密码使用 PBKDF2；会话令牌的跨服务校验/轮换和传输加密强化不在本阶段范围。

MiniCore 框架本身没有 `AuthenticationBaseUrl`、Coordinator 或 Lobby 配置。其他离线项目既不需要创建 `MiniBomberClientNetworkProfile`，也可以完全不启用网络与 HTTP AppService。

## 3. 一次性生成默认资产

在 Unity 执行：

```text
MiniCore > Demos > MiniBomber > Create Default Assets
```

菜单是幂等的，只创建缺失项，不覆盖已修改场景或配置。它会创建：

- `MiniBomberRuntimeConfig.asset`
- `MiniBomberClientNetworkProfile.asset`
- `MiniBomberRuleConfig.asset`
- `MiniBomberDefaultMap.asset`，默认 `17×13`
- `LoginScene.unity`
- `LobbyScene.unity`
- `BattleScene.unity`
- `ServerBootstrapScene.unity`，只有 `UpdateMainWindow`，没有 Camera、Canvas 或 EventSystem

地图资产有自定义网格 Inspector，可直接在 Unity 中编辑 Road、Solid 和 Breakable。客户端和服务器必须加载同一地图版本。

## 4. 场景制作

### LoginScene 与 LobbyScene

保留下列结构即可：

```text
LoginScene / LobbyScene
├── Environment
├── MainCamera
└── Lighting
```

不要放 Canvas、EventSystem、ApplicationUIRoot 或业务启动脚本。业务 UI 由常驻 `ApplicationUIRoot` 挂载，场景只负责背景和灯光。

### BattleScene

```text
BattleScene
├── Environment
│   ├── Ground
│   └── Decorations
├── GameplayRoot                         BomberBattleSceneBinding
│   ├── MapRoot                              BomberMapView
│   │   ├── SolidBlockRoot
│   │   ├── BreakableBlockRoot
│   │   └── PickupRoot
│   ├── Input                                 BomberInputComponent
│   ├── PlayerRoot
│   ├── BombRoot
│   └── EffectRoot
├── CameraRig                            BomberCameraController
│   └── MainCamera
└── Lighting
```

`BomberBattleSceneBinding` 必须绑定：

| 字段 | 引用 |
| --- | --- |
| Map View | `MapRoot/BomberMapView` |
| Input | `Input/BomberInputComponent` |
| Camera Controller | `CameraRig/BomberCameraController` |
| Player/Bomb/Effect Root | 同名节点 |
| Player Prefab | `PlayerAvatar.prefab` |
| Bomb Prefab | `Bomb.prefab` |
| Explosion Prefab | `ExplosionCell.prefab` |
| Pickup Prefab | 默认道具 Prefab；V1 可先配置占位资产 |

`BomberMapView` 绑定默认地图、墙/木箱 Prefab 和三个根节点。`BomberCameraController` 绑定 `MainCamera`，并按地图范围配置跟随高度与边界。

`BomberInputComponent.Actions` 必须绑定：

```text
Assets/AssetRes/Demos/MiniBomber/Config/MiniBomberGameplay.inputactions
```

Windows 使用 WASD/方向键和 Space。Android 控件通过 `OnScreenStick` 与 `OnScreenButton` 写入虚拟 Gamepad：

```text
MobileControlRoot
├── MoveJoystick
│   ├── Background
│   └── Handle                 OnScreenStick
└── BombButton                     OnScreenButton
```

- `OnScreenStick.Control Path` = `<Gamepad>/leftStick`
- `OnScreenStick.Movement Range` = `100`
- `OnScreenButton.Control Path` = `<Gamepad>/buttonSouth`
- `MobileControlRoot` 与 `DesktopHintRoot` 同时绑给 `BomberInputComponent`，运行时会根据平台二选一。

## 5. 世界 Prefab

放在 `Assets/AssetRes/Demos/MiniBomber/Prefabs`：

| Prefab | 必须脚本 | 用途 |
| --- | --- | --- |
| `PlayerAvatar` | `BomberPlayerView` | 显示服务器位置、本地预测、死亡和保护状态 |
| `Bomb` | `BomberBombView` | 显示炸弹格与引信 |
| `ExplosionCell` | `BomberExplosionView` | 显示服务器下发的爆炸格 |
| `IndestructibleBlock` | 无必需业务脚本 | 固定墙表现 |
| `DestructibleBlock` | 无必需业务脚本 | 可破坏木箱表现 |
| `BombCountPowerUp` | `BomberPickupView` | V2 炸弹数道具 |
| `BombRangePowerUp` | `BomberPickupView` | V2 范围道具 |

Collider、Animator 和 Transform 只负责客户端表现，不得将客户端物理碰撞、Transform 或动画事件作为击杀结果。

## 6. UI Prefab 制作

所有 Prefab 放在：

```text
Assets/AssetRes/Demos/MiniBomber/UI
```

不在业务场景中创建 Canvas。Prefab 根节点直接挂对应 `AUIWindowView` 派生类，并在其 Authoring Inspector 中配置 Route、Presenter、Layer 和安全区。`Asset Address` 与 Prefab 文件名一致。

| Prefab / Route | View / Presenter | Layer | 建议策略 |
| --- | --- | --- | --- |
| `LoginWindow` | `LoginWindowView` / `LoginWindowPresenter` | Screen | `ConstrainContent`，ContentRoot |
| `RegisterWindow` | `RegisterWindowView` / `RegisterWindowPresenter` | Popup | Modal + `ConstrainWindow` |
| `LobbyWindow` | `LobbyWindowView` / `LobbyWindowPresenter` | Screen | `ConstrainContent`，ContentRoot |
| `CreateRoomPopup` | `CreateRoomPopupView` / `CreateRoomPopupPresenter` | Popup | Modal + `ConstrainWindow` |
| `RoomWindow` | `RoomWindowView` / `RoomWindowPresenter` | Screen | `ConstrainContent`，ContentRoot |
| `BattleHudWindow` | `BattleHudWindowView` / `BattleHudWindowPresenter` | Hud | `ConstrainContent`，ContentRoot |
| `MatchResultWindow` | `MatchResultWindowView` / `MatchResultWindowPresenter` | Popup | Modal + `ConstrainWindow` |
| `SceneLoadingWindow` | `SceneLoadingWindowView` / `SceneLoadingWindowPresenter` | Transition | `Ignore` |
| `ReconnectOverlay` | `ReconnectOverlayView` / `ReconnectOverlayPresenter` | System | `Ignore` |
| `MessageToastWindow` | `MessageToastWindowView` / `MessageToastWindowPresenter` | Toast | `ConstrainContent` |
| `NetworkDebugWindow` | `NetworkDebugWindowView` / `NetworkDebugWindowPresenter` | Debug | `ConstrainContent` |

每个窗口必须：

1. Prefab 根节点有 `RectTransform + CanvasGroup + 对应 View`，根节点不挂 Canvas。
2. `Route Name` 与表格第一列完全一致。
3. `Presenter Type` 选择表格中对应 Presenter。
4. Screen/Hud 的全屏背景放 `BackgroundRoot`，需要避开刘海的控件放 `ContentRoot`，并将 `SafeAreaTarget` 绑定为 `ContentRoot`。
5. 使用 `ConstrainContent` 时不能留空 SafeAreaTarget；Loading 和重连遮罩必须全屏，所以使用 `Ignore`。
6. 无动画可将 Transition Driver 留空；有动画时挂 `UIPresetTransition`，绑定 Target/CanvasGroup，再应用 `Assets/Settings/MiniCore/UI/Presets` 中的 Unity Preset。

推荐层级：

```text
LoginWindow / LobbyWindow / RoomWindow
├── BackgroundRoot
└── ContentRoot
```

```text
RegisterWindow / CreateRoomPopup / MatchResultWindow
├── ContentRoot
│   └── PanelRoot
└── Transition Target = PanelRoot
```

```text
BattleHudWindow
├── ContentRoot
│   ├── RemainingTimeText
│   ├── RankingText
│   ├── KillFeedText
│   └── performanceText
├── MobileControlRoot
└── DesktopHintRoot
```

各 View 的控件字段均为 `[SerializeField] private`，仍会显示在 Inspector，并保持既有字段名和 Prefab 序列化绑定：

- Login：Account、Password、Login/Register Button、Prompt。
- Register：Account、Password、Confirm Password、Player Name、Submit/Close Button、Prompt。
- Lobby：Player Name、Online Count、Room List Text、Join Room Id、Refresh/Create/Join/Logout Button、Prompt。
- Create Room：Room Name、Duration Dropdown、Submit/Cancel Button、Prompt。Dropdown 选项顺序必须为 `2、5、10 分钟`。
- Room：Title、Member List、Room Name、Duration Dropdown、Apply/Ready/Start/Leave Button、Prompt。
- Battle HUD：Remaining Time、Ranking、Kill Feed、Performance Text、Mobile Control Root、Desktop Hint Root。Performance Text 每半秒显示 `FPS: 59.98  RTT: 10 ms`；RTT 暂不可用时显示 `RTT: --`。
- Result：Results、Return Countdown、Close Button。
- Loading：Progress Slider、Prompt。
- Reconnect：Status。
- Toast：Message。
- Debug：Diagnostics。

登录、注册、大厅和房间的命令互斥由 Presenter 协调，按钮文本、交互和显隐由 View 的语义方法更新。不要再给这些按钮额外挂一份网络请求；服务器成功但客户端窗口切换失败时，Prompt 会明确提示“重新登录可恢复服务器状态”。

完成 Prefab 后执行：

```text
MiniCore > UI > Generate Window Registry
```

手动执行后 Console 必须出现 `UI Window Registry 生成成功`，并显示窗口数量、生成文件是“已更新”还是“已是最新”，以及两个输出路径；校验失败时则明确输出 `UI Window Registry 生成失败` 和首个错误原因。自动导入触发的后台生成保持安静，避免每次脚本重载刷屏。

如果某个 Prefab 还没制作，Client Flow 会记录“缺少路由”警告并继续场景流程，方便先单独验证世界场景。完整联调时上表 11 个路由都应存在。

## 7. 网络与状态同步

MiniBomber 是服务器权威状态同步，不是帧同步：

```text
客户端 BomberInputFrame
  → 30Hz Outer RPC 输入
  → 服务器 30Hz 整数权威模拟
  → 15Hz 世界快照 + 即时炸弹/爆炸/击杀/复活事件
  → 客户端插值、预测与校正
```

服务器 Update 卡顿时每次最多补执行 2 个固定步，超出部分丢弃并告警，不无限堆积。网络不等待所有玩家输入。

玩家无新输入时，服务器最多沿用方向 `100ms`。超时后调用停止逻辑：

- 只将 MoveX/MoveZ、移动余数和待放置炸弹输入清零。
- 玩家保持当前服务器权威坐标。
- 不回原点、不回出生点。
- 炸弹、死亡、复活和保护时间继续正常更新。

权威数值不使用 `Vector3`、Rigidbody 或确定性浮点库。位置是整数毫米，时间是 Tick/整数毫秒，输入量化到 `-1000..1000`。客户端 float 只影响显示。

## 8. 多进程部署

推荐最小结构：

```text
AuthenticationServer (.NET 10 Web)
  └── 账号 MySQL

DatabaseServer (.NET 10 Worker，可选)
  └── 游戏数据 MySQL

同一份 Dedicated Server 不可变制品
  ├── Coordinator-01  roles=[Coordinator]
  ├── Lobby-01        roles=[minibomber.lobby]
  ├── Match-01        roles=[minibomber.match]
  └── Game-01         roles=[minibomber.game]
```

1. 编辑 `Server/AuthenticationServer/appsettings.json`，配置账号库、令牌参数和对外 Coordinator WebSocket 地址。
2. 需要持久化时编辑 `Server/DatabaseServer/appsettings.json`，配置游戏库、Inner RPC 监听和 Coordinator 内网地址；不需要时将所有 DS 副本设为 `persistenceMode=None`，无需启动 DBServer。
3. 使用 MiniCore Deploy 为每个实例生成仓库外 `MiniCoreServerRuntime.json`，通过 `--minicore-config` 启动。每个实例使用唯一 `instanceId` 和监听端口，`advertised` 填写其他进程或客户端实际可达的地址。
4. 先启动唯一 Coordinator，再启动可选 DBServer，以及 Lobby、Match、Game。普通 DS 会先以 `Starting` 注册，业务组件就绪后自动报告 `Ready`。
5. MiniBomber 客户端 Profile 只配置 AuthenticationServer HTTPS 入口；其余地址都由登录响应和 Coordinator 动态下发。

运行期间重启 DatabaseServer 不要求重启 GameCluster：DatabaseServer 会自动重新注册 Ready，GameCluster 的下一次 Load 会重新发现并连接。重启 Coordinator 后，DatabaseServer 与普通 DS 都会自动重连、重新注册和恢复原状态；Coordinator 不保存持久目录，因此短暂恢复窗口内客户端可能暂时查询不到服务。

Role 配置示例：

```json
{
  "instanceId": "Match-01",
  "roles": ["minibomber.match"],
  "coordinator": { "innerHost": "10.0.1.10", "innerPort": 7000 },
  "listeners": {
    "innerHost": "0.0.0.0",
    "innerPort": 7200,
    "outerHost": "0.0.0.0",
    "outerPort": 7201,
    "outerPath": "/minicore"
  },
  "advertised": {
    "innerHost": "10.0.1.12",
    "innerPort": 7200,
    "outerWebSocketUrl": "wss://match.example.com/minicore"
  },
  "persistenceMode": "Database"
}
```

Coordinator 不转发 Lobby、Match、Game 或 Database 业务消息。调用方先从服务目录取得 Ready 实例，再使用现有 MiniCore RPC 直连目标 Inner/Outer 地址。完整字段与启动顺序见 [Dedicated Server 架构](../DedicatedServerArchitecture.md)。

## 9. 多端构建

首次构建、切换平台、改变 Development Build 或修改 AOT/Unity 层代码时，每个平台都执行：

1. 先选定平台和 Development Build 状态。
2. `HybridCLR > Generate > All`。
3. `MiniCore > Build > DefaultPackage > 完整生成 (Generate All + Build)`。
4. 等待 Console 确认 DefaultPackage 成功。
5. Unity `Build`。

### Dedicated Server

- MiniCore Deploy v1 支持 Linux x64 与 Windows x64 Dedicated Server。
- Build Settings 只把 `Assets/Scenes/Demos/MiniBomber/ServerBootstrapScene.unity` 作为第一启动场景。
- 运行时带 `-batchmode -nographics`。
- 构建处理器只向该目标注入与实例无关的 `ServerRoleCatalog.json`；实例配置位于部署根目录并由启动参数指定。
- 默认目标程序集不包含 Client UI/Handler；若开发环境确需同包客户端代码，可显式启用“DS 额外包含 Client”。

### Windows Client

- 在 Windows 笔记本上切换 `Windows x86_64 + IL2CPP`。
- Build Settings 以 `Assets/Scenes/HotUpdateScene.unity` 为第一启动场景。
- 完成 HybridCLR 和 DefaultPackage 后构建 Player，再将 Windows 平台的 YooAsset 输出复制到 Mac mini 发布目录。

### Android Client

- 在 Mac 上切换 Android，目标架构至少启用 ARM64。
- Build Settings 以 `Assets/Scenes/HotUpdateScene.unity` 为第一启动场景。
- 完成 HybridCLR 和 DefaultPackage 后构建 APK/AAB，手机与 Mac mini 位于同一局域网。

### WebGL Client

- 切换 WebGL 目标，Build Settings 以 `Assets/Scenes/HotUpdateScene.unity` 为第一启动场景。
- 浏览器使用 AuthenticationServer 的 HTTPS API 和 Coordinator/Lobby 的 WebSocket 外网地址。
- HTTPS 页面必须接收 `wss://` 地址；TLS 证书与反向代理属于部署层，DS JSON 的 `advertised.outerWebSocketUrl` 应填写客户端实际可达入口。

资源发布根目录按平台隔离：

```text
ReleaseRoot
├── MacServer/DefaultPackage
├── Windows64/DefaultPackage
├── Android/DefaultPackage
└── WebGL/DefaultPackage
```

YooAsset 是平台相关产物，Windows 和 Android 不得指向同一个包目录。完整通用流程见 [打包与热更新流程](../BuildAndHotUpdateWorkflow.md)。

## 10. V1 到 V2 热更新验证

V1：

- `MiniBomberRuleConfig.EnablePowerUps = false`。
- 协议与代码已预留道具类型和表现入口。
- 安装 Windows/Android 首包并完成登录到结算的一局验证。

V2：

1. 在规则配置开启道具，完成 BombCount/BombRange Prefab 与 HUD 显示。
2. 平台和 Development 状态不变时，执行 `MiniCore > Build > DefaultPackage > 热更编译 (Compile Active Target + Build)`。
4. 将新版本目录按原结构完整发布到 Mac mini HTTP 目录。
5. 重启 Server 和已安装的旧 Client，不重新安装 Windows Player/APK。
6. 验证 Bootstrap 获取新版本、下载资源并进入启用道具的游戏。

V2 不修改 Proto、AOT API、Input System 包或原生插件。如果修改了这些边界，必须重新生成 AOT 并发布新首包。

## 11. 联调顺序

1. 单机 Editor：完成 UI Prefab 后生成 Registry，确认 LoginScene 无 Canvas，LoginWindow 由 Root 打开。
2. 本机 Server + 一个 Client：验证注册、登录、错误提示和大厅。
3. 两个 Client：验证创建/加入房间、房主权限、准备重置和开局。
4. 战斗：验证移动、炸弹、连锁、击杀提示、`+2/-1`、复活和排名。
5. 断网：让离线角色保持当前位置，确认 `1/2/4/4/4` 秒重试和 `15` 秒恢复窗口。
6. 跨端：Windows 与 Android 同房比赛，对比双端得分、时间、事件和最终排名。
7. 热更新：已安装 V1 的设备只下载 V2 DefaultPackage，验证道具开关和资源更新。

## 12. 历史自动验证范围

Editor 回归测试位于 `Assets/Tests/Editor/Demos/MiniBomber`。以下是改造前后保留的主要业务覆盖；独立 AuthenticationServer 已删除原 Unity 本地密码仓库与对应密码测试，应在后续为 .NET 服务另行建立验证：

- 输入超时后只停止移动，权威位置不改变。
- 断线角色停在当前位置，不发生传送。
- 自杀死亡惩罚和服务器最终排名。
- 放置者的圆形占位完全离开炸弹格前保持穿出权限，离开后炸弹恢复阻挡。
- Android HUD 的 BombButton 通过 `<Gamepad>/buttonSouth` 接入 PlaceBomb Action，移动摇杆和平台根节点引用完整。
- 炸弹按钮的 `performed` 边沿独立缓存到下一次输入采样，持续移动时不会丢弃同时发生的放置操作；客户端启动和释放分别设置、恢复屏幕休眠策略。
- PlayerAvatar 使用 MiniBomber 专用 Animator Controller，BomberPlayerView 引用 Animator。
- BattleScene 的地面节点具有有效 Mesh、Material 和 Shader。
- Bomb Prefab 根节点保持原点，运行时强制落在目标格地面中心。
- 可靠的 `BlockDestroyed` 业务事件到达时立即隐藏木箱，不再等待下一帧 `15Hz` 世界快照。
- Battle HUD 的 `performanceText` 引用有效，并以半秒窗口显示客户端 FPS 和 KCP RTT。
- 项目自有 Linker 配置保留 YooAsset 动态加载的托管类型；AOT Bootstrap 在热更资源下载完成后显式调用公开保护入口，并通过真实 `AddComponent<SkinnedMeshRenderer>()` 调用建立缺失原生组件的可达依赖。

运行入口：Unity Test Runner 的 EditMode，筛选 `MiniCore.Tests.Editor.Demos.MiniBomber`。

## 13. 验证记录

### 2026-08-18（MiniBomber `AComponent + MVP` 干净切换）

- 数据层：新增账号、大厅、房间、战斗、比赛结果和客户端流程纯 C# Model；列表元素拆为独立 Model，集合只读暴露。
- 协议边界：PB 仅用于收发，Component 把响应和推送复制或增量归并到 Model；不池化 PB，也不保留 PB 对象或子集合引用。
- UI 边界：全部 MiniBomber View 保持原字段名并改为私有序列化字段；Presenter 不再直接访问 Unity 控件，也不再消费 PB。
- 刷新策略：Lobby、Room、MatchResult 使用窗口专用 ViewData；Battle HUD 使用修订号与相同值保护进行分区刷新，列表和战斗实体 Model 尽量复用。
- 流程边界：Flow 只编排场景、窗口、重连和跨业务流程，长期流程数据使用结构化 Model，不保存格式化 UI 文本。

### 2026-08-15（RPC 简化超时、长连接心跳与服务自动恢复）

- API：Unity `INetworkService` 与 .NET `MiniCoreRpcClient` 的 RPC 方法统一使用末尾可选 `timeoutSeconds=10`；没有引入 Options 或重复重载。客户端查询 Coordinator 为 `8` 秒，控制面为 `3` 秒，数据库 Load/Save 为 `5/8` 秒，Match 开始和场景就绪为 `15` 秒。
- 长连接：Unity 与 .NET 默认每 `2` 秒 Ping，连续 `10` 秒没有 Pong 才断开；.NET 客户端改为唯一接收循环、RpcId Pending 表和串行发送入口，迟到响应忽略，断线一次性结束全部 Pending RPC。
- 恢复：DatabaseServer 的 `7300` 业务 Listener 不再随 Coordinator 控制连接异常退出；DBServer 与普通 DS 均以 `1/2/4/8/15` 秒上限退避重新注册并恢复 Ready。GameCluster 的数据库 Load 可重连重试一次，Save 结果未知时先 Load 核验，最终统一返回 `503 DatabaseUnavailable`。
- 编译：.NET 10 解决方案为 `0` 错误、`0` 警告；Unity 2021.3.45f2 隔离副本分别以 WebGL 客户端和 Linux Dedicated Server 条件完成脚本编译，均无 C# 错误。只保留项目既有的 `ReferenceCollectorEditor` 未使用字段和 `MTaskTests` 未等待调用警告；未运行测试、Player、YooAsset/HybridCLR 构建或数据库迁移。

### 2026-08-15（多 Role DS、独立认证/数据库服务与客户端裁剪）

- 启动：客户端与 Dedicated Server 使用不同热更新入口；DS 读取自身 JSON，自动注册网络、Role Handler 和 Coordinator 服务发现后再进入业务组件。
- 地址：客户端 Profile 只保存 AuthenticationServer HTTPS 入口；认证响应下发 Coordinator，Coordinator 再下发 Ready Lobby，业务数据不经过 Coordinator 转发。
- 服务：根目录新增 .NET 10 AuthenticationServer 与可选 DatabaseServer；DBServer 复用 MiniCore Inner 帧/RPC，以保留的 Database ServiceId 注册并使用 EF Core/Pomelo/MySQL。
- 裁剪：Client/Server 业务代码、业务 Common/Outer/Inner 协议与 AOT Control/Control.Inner 均有独立程序集；客户端构建校验阻止服务端程序集、Inner 协议、Control.Inner、Handler 清单和 DS JSON 泄漏。
- 验证约束：本轮只进行 Unity 与 .NET 编译检查，不运行测试，不执行 Player、HybridCLR 或 YooAsset 构建。

### 2026-08-12（WebGL 与原生端共用 Dedicated Server 接入）

- 设计调整：登录窗口隐藏并停止绑定服务器地址、端口输入，客户端连接端点固定由 `MiniBomberConstants` 提供；恢复存档不再保存连接端点，原字段号永久保留而不复用。
- 传输选择：原生 PC/Android 按能力使用 KCP，浏览器 WebGL 按能力使用 WebSocket；业务层不引入 `UNITY_WEBGL` 条件编译。
- 服务端接入：同一 Dedicated Server 同时监听 UDP/KCP 与 TCP/WebSocket 的同一数值端口，二者复用协议注册、Session、RPC、Handler 和权威房间状态。
- 部署约束：当前 `ws://` 只适合 HTTP 测试页面；HTTPS 页面必须改用具有可信域名证书的 WSS 入口。
- 构建结果：Unity `2021.3.45f2` 隔离工程的协议生成、WebGL C#/IL2CPP、HybridCLR Generate All、DefaultPackage 与最终 WebGL Player 构建通过；未运行测试套件。

### 2026-08-06（Android 角色原生注册、多点操作、即时木箱与 HUD 性能信息）

- 角色证据：当前 Android IL2CPP 产物的 `UnityClassRegistration.cpp` 已包含 AnimationClip、Animator、Avatar、ParticleSystem 和 ParticleSystemRenderer，却没有 PlayerAvatar 实际使用的 SkinnedMeshRenderer；PlayerAvatar 自身的 Mesh、Material、Animator 和 Controller 引用均有效。因此本轮不改材质，先修复缺失的原生组件注册。
- 裁剪结论修正：上一条记录中“可达的 `Debug.Log(typeof(SkinnedMeshRenderer))` 足以保留原生实现”的判断不成立。`typeof` 只保证托管类型元数据可达，不能证明 Unity 原生引擎模块进入 Player；保护入口保留显式调用，但为 SkinnedMeshRenderer 增加禁用临时对象上的真实 `AddComponent` 调用。Android 仍需全量重新构建 Player，不能只更新 HotUpdate DLL 或 DefaultPackage。
- 多点输入证据：ApplicationUIRoot 的 Input System Pointer Behavior 为 `Single Mouse Or Pen But Multi Touch And Track`，Point/Click Action 为 Pass Through，已允许独立触点。炸弹边沿改为订阅 `PlaceBomb.performed` 后缓存到下一输入帧，移动 Value 与炸弹 Button 不再依赖同一个 MonoBehaviour Update 内的瞬时轮询。
- 木箱证据：服务器已即时发送 `BlockDestroyed`，旧客户端表现层只处理 ExplosionStarted，木箱要等下一次 `15Hz` 快照才隐藏。现由事件直接调用 `BomberMapView.HideBreakable`，后续快照继续作为权威状态兜底。
- 客户端体验：MiniBomber 客户端启动时设置 `SleepTimeout.NeverSleep`，释放时恢复系统设置；BattleHud 每 `500ms` 计算显示 FPS，并读取默认 KCP Session 的平滑 RTT，格式为 `FPS: xx.xx  RTT: n ms`。
- 回归：Unity `2021.3.45f2` 隔离工程完整编译通过，MiniBomber EditMode `15/15`、UI Framework EditMode `17/17` 通过。Android 原生注册与移动加炸弹的最终结果仍须使用重新构建安装的 APK 真机复验。

### 2026-08-06（Android 原生类型保护改为显式启动调用）

- 设计调整：`UnityEngineTypePreserver` 改为公开 AOT 类，`UpdateMainWindow` 在资源下载完成、加载 AOT 元数据和 HotUpdate DLL 之前明确调用 `ProtectDynamicContentTypes()`。
- 历史实现：保护方法曾仅通过 `Debug.Log(typeof(...))` 静态引用 AnimationClip、Avatar、SkinnedMeshRenderer、ParticleSystem 和 ParticleSystemRenderer。上方最新记录已通过 Android 生成产物证明这种方式不足以保留 SkinnedMeshRenderer 原生实现，并已用真实原生组件调用修正。
- 服务端边界：类型引用通过 `!UNITY_SERVER` 排除，Dedicated Server 不为 Demo 的角色动画和粒子引入客户端渲染模块；Editor 同样不输出保护日志。
- 回归：Unity `2021.3.45f2` 隔离工程编译通过，MiniBomber 表现资产 EditMode `6/6` 通过；Android 原生模块是否进入 Player 仍以全量 IL2CPP 重建后的真机 Class ID 日志为最终证据。
- 结论关系：本记录曾取代下方同日记录中的“`[Preserve]` 真实 API 类型锚点”，随后又被上方最新记录的真实 `AddComponent<SkinnedMeshRenderer>()` 依赖替代；Android 原生引擎代码裁剪根因不变。

### 2026-08-06（战斗加载 RPC、炸弹穿出、Android 原生裁剪与摇杆范围修复）

- Editor 症状：进入 BattleScene 时 `MiniBomberSceneReady` RPC 在十秒后超时，随后同一个 `rpcId` 响应到达并产生“未找到 opcode:200024 的处理器”。证据显示 `MiniBomberMatchPrepareHandler` 在串行收包队列中等待场景加载及嵌套 RPC，SceneReady 响应只能排在当前 Handler 后面，形成收包队列自锁。
- RPC 修复：MatchPrepare Handler 只把完整场景流程交给 `MiniBomberClientFlowComponent` 的任务域监督，立即结束当前消息派发；场景流程仍负责加载、HUD、SceneReady RPC 和 Loading 关闭，但不再占住收包队列。
- 炸弹症状与根因：放置者中心刚跨入相邻格时，旧逻辑立即关闭 `OwnerCanPass`；此时玩家半径仍与炸弹格相交，下一步会被自己的炸弹阻挡。修复为玩家圆形占位完全离开炸弹格后才关闭一次性穿出权限。
- Android 证据：真机日志稳定报告 `Could not produce class with ID 74/90/137/198/199`，对应 AnimationClip、Avatar、SkinnedMeshRenderer、ParticleSystem 和 ParticleSystemRenderer。Android 裁剪报告也没有原生 ParticleSystem 模块与 SkinnedMeshRenderer，确认角色不可见和粒子缺失属于 Unity 原生引擎代码裁剪。
- 历史修复：保留 `Strip Engine Code`；`MiniCore.link.xml` 继续负责托管成员，当时先以 `[Preserve]` 的真实 API 类型锚点补足原生依赖。该具体实现已被上方两条更新记录替代，但“仅有 `link.xml` 不足以保证整个原生模块进入 Player”的结论仍然成立。
- 移动端：BattleHud 的 `OnScreenStick.Movement Range` 从 `50` 调整为 `100`，移动范围扩大一倍。
- 回归：Unity `2021.3.45f2` 隔离工程完整编译通过，MiniBomber EditMode `13/13` 通过。Android 原生修复必须执行 Android `HybridCLR > Generate > All`、重新构建并安装 Player；随后用 ADB 确认上述五个 Class ID 不再出现，不能只更新 HotUpdate DLL 或 DefaultPackage。

### 2026-08-06（战斗窗口回收与炸弹地面坐标修复）

- 症状：从房间进入 BattleScene 后 RoomWindow 仍留在 Screen Layer；比赛结束返回 LobbyScene 后 BattleHudWindow 仍留在 Hud Layer；新炸弹显示在地面上方约八米。
- 证据与根因：战斗流程只加载场景并打开 HUD，没有关闭 `Main` 导航组当前 Screen，也没有保存 HUD 句柄供退出时关闭。Bomb Prefab 根节点保存了 `Y=8.004326`，`BomberBombView.Initialize` 又沿用该 Y 值，因此错误高度可以稳定复现；服务器格坐标没有错误。
- 修复：UIService 增加 `CloseNavigationAsync(group)`，用于进入“无 Screen”业务状态；MiniBomber 进入战斗前关闭 `Main` Screen，保存唯一 BattleHud 句柄，所有非战斗目的地先精确关闭 HUD。炸弹初始化强制使用格子中心 `(x+0.5, 0, z+0.5)`，Prefab 根节点恢复原点，模型自身的视觉高度仍由 VisualRoot 控制。
- 回归：Unity `2021.3.45f2` 隔离资源导入和全工程编译通过；MiniBomber 表现资产 EditMode `5/5`、完整 UI Framework EditMode `17/17` 通过。真实房间开局和赛后回房仍需使用 Editor/客户端与 Dedicated Server 完成一次端到端复验。

### 2026-08-05（战斗表现、移动端输入、窗口适配与场景句柄修复）

- BombButton：确认它使用 `OnScreenButton` 把 `<Gamepad>/buttonSouth` 注入 Input System，`PlaceBomb` Action 监听同一路径；这是控件自身到 Action 的连接，不需要在 `BattleHudWindowView` 增加一个不会被 Presenter 使用的重复按钮引用。新增资产测试锁定 HUD 根引用、控件组件和 Action 路径。
- 角色动画：`BomberPlayerView` 绑定 PlayerAvatar 的 Animator，并依据权威位置变化或本地预测输入平滑写入 `Speed` 参数；专用 `BomberMan.controller` 继续负责 Idle/Run 切换，不启用 Root Motion。
- UI 适配：窗口每次打开或从缓存恢复时，由 `AUIWindowView` 将根 RectTransform 恢复为全 Layer 拉伸，再应用 Content/Window 安全区策略，避免 Prefab 固定尺寸造成横屏上下裁切和左右留白。
- Android 裁剪：新增项目自有 `Assets/Linker/MiniCore.link.xml`，保留 YooAsset 动态 Prefab 需要的动画、Avatar、蒙皮网格、粒子和 On-Screen Control 引擎类型。该配置用于下一次 Player 构建；`LoadMetadataForAOTAssembly` 仍只补充托管泛型元数据，不能替代 Unity Player 的原生引擎类型保留。
- 场景切换：移除 `LoadSceneMode.Single` 后对旧 SceneHandle 的手动二次 Release；YooAsset 2.3.18 已在 `OnSceneUnloaded` 自动释放它，服务只在失败或自身退出时释放仍有效的句柄。
- 回归：Unity `2021.3.45f2` 隔离导入和全工程编译通过；MiniBomber 表现资产 EditMode `4/4`、窗口根拉伸 PlayMode `1/1` 通过。Android 真机仍需重新生成 HybridCLR、重新构建安装 Player，再复验角色、地面、特效、横屏 UI 和炸弹按钮。

### 2026-08-05（Android `NotSupportAdjustorThunk` 窗口句柄修复）

- 症状：Android IL2CPP 成功完成 YooAsset 更新、补充元数据和 HotUpdate DLL 加载，但 MiniBomber 启动到 `UIService.OpenCoreAsync` 时抛出 `System.ExecutionEngineException: NotSupportAdjustorThunk`。
- 证据：异常链经过 `MTask<UIWindowHandle>`；旧 `UIWindowHandle` 是约 40 字节的嵌套值类型，旧 MethodBridge 将该返回形状记录为 `s20u`，却没有对应的 `AdjustorThunk_s20u`。这不是缺少 AOT 元数据，也不是场景加载失败。
- 修复：将 `UIWindowHandle` 改为不可变引用类型，继续按 WindowId、实例键和代次执行值相等比较；无窗口统一使用 `null`，UIService 和 MiniBomber 调用点增加空句柄保护。
- AOT 边界：`UIWindowHandle` 位于 `MiniCore.Unity`，本次属于 AOT 公共 API 变化。必须为 Android 再执行一次 `HybridCLR > Generate > All` 并重新构建、安装 Player；只更新 HotUpdate DLL 或 DefaultPackage 不会替换旧 Player 中的 AOT 类型布局。
- 回归：Unity `2021.3.45f2` 隔离编译通过，UIFramework EditMode `16/16` 通过；隔离 Android `Generate All` 成功，新主 MethodBridge 已不再包含旧 `s20u / AdjustorThunk_s20u` 形状。Android 真机启动链路仍需使用新首包复验。

### 2026-08-05（缓存窗口重新打开状态重置与 Registry 结果提示）

- 症状：从大厅退出并返回缓存的 LoginWindow 后，登录/注册按钮仍保持上次导航期间的禁用状态，Prompt 仍显示“正在进入游戏”；同类问题也可能出现在注册、大厅、创建房间和房间界面。
- 根因：窗口缓存会复用 View；上一 Presenter 在成功导航前关闭了命令按钮，但新 Presenter 的 `OnBind` 只重新绑定事件，没有初始化 View 的临时交互状态。
- 修复：上述五个 Presenter 每次绑定时都清空旧 Prompt、重置命令互斥标记，并按当前身份恢复按钮；RoomWindow 的房主按钮仍由当前权威房间快照控制，非房主不会获得修改或开始权限。输入框内容不强制清空。
- 工具反馈：手动执行 `MiniCore > UI > Generate Window Registry` 后输出明确的成功/失败、窗口数量、是否产生文件变化和输出路径；自动导入调用仍不输出无意义的成功日志。
- 回归：使用 Unity `2021.3.45f2` 当前 Bee 参数隔离编译 `MiniCore.HotUpdate` 与 `MiniCore.Editor` 均通过；仅保留既有未使用字段警告。运行态账号—大厅—创建房间—离开—重新进入链路仍需在 Editor 与 Dedicated Server 中复验。

### 2026-08-05（窗口命令状态与完整 Loading 边界）

- 症状：登录/注册和大厅按钮缺少稳定的请求中反馈；登录成功后 Loading 先消失，旧 LoginWindow 短暂重新显示；创建房间偶发空引用，随后重新登录又恢复到已经创建的房间。
- 证据：客户端日志显示创建房间 RPC 在约 `17ms` 内成功返回，服务器房间快照已经写入；异常发生在 `CreateRoomPopupPresenter.SubmitAsync` 关闭自身窗口之后继续访问已由 `OnDispose` 清空的 Flow 引用，因此不是丢包。
- 修复：所有账号、大厅和房间命令增加同步 Prompt、单命令互斥、按钮禁用和阶段化失败提示；同一端点的活动 KCP 连接直接复用；创建房间先完成 RoomWindow 导航再关闭 Popup。
- Loading 边界：`MiniBomberClientFlowComponent` 现在由完整目的地切换持有 Loading，覆盖场景加载、Lobby Snapshot、目标窗口创建与激活，最后使用精确句柄关闭。
- 生命周期保护：HotUpdate Presenter 在自身记录窗口释放状态，异步响应恢复时先确认 View 仍有效；窗口已关闭时不再访问失效引用，不增加 AOT API。
- 回归：使用 Unity `2021.3.45f2` 当前 Bee 响应参数隔离编译 `MiniCore.Unity` 与 `MiniCore.HotUpdate` 均通过；仅保留既有 `KcpTestWindowPresenter.localJoined` 未使用警告。运行态的注册、登录、创建房间和重复点击仍需在 Editor + Dedicated Server 联调复验。

### 2026-08-04（开发账号摘要简化）

- 变更：删除 MiniBomber 密码算法版本、旧 PBKDF2 分支、登录时迁移和异步线程切换；开发账号统一使用 `16` 字节随机盐与 SHA-256 摘要。
- 兼容策略：不迁移开发阶段旧账号数据库；已有旧格式账号需要清理 `MiniBomberServerAccounts` 存档槽并重新注册。
- 安全边界：密码不落明文且使用固定时间比较；账号数据库继续由 `ISaveService` 整体加密。本方案仅服务内网 Demo 联调，不作为生产账号方案。
- 编译症状：隔离编译同时暴露战斗增量/重同步消息缺少生成角色，以及 Worker 命令的 `Input` 属性和同名工厂冲突。Proto 注解已存在，因此确认是角色文件未同步，而不是协议定义错误。
- 修复：通过项目官方 Proto 生成器补齐角色与 Parser 表，将工厂改名为 `CreateInput`，再通过官方同步入口恢复 Opcode 和 HotUpdate Handler 直接注册表。
- 回归：MiniBomber EditMode `6/6` 通过，其中账号摘要 `3/3`、权威模拟 `3/3`；隔离工程和最终正式工程 Unity 编译均通过。未改动已有 ADB 退出告警和既有 EventBus 测试基础设施。

### 2026-08-04（Provider 单选与 Demo 启动入口）

- 变更：项目启动窗口改为按 `IAppService` 接口分组单选 Provider；MiniBomber 客户端与服务端装配迁入 `Demos/MiniBomber/Entry`，`GameStartup` 只保留运行形态和自动测试分流。
- 序列化边界：网络默认 Protobuf、`INetworkService.SetSerializer`、业务直接 JSON、配置和 HTTP 的 JSON/PB 双 API 均未修改。隔离生成的 `MiniCoreStartup.Generated.cs` 与变更前逐字一致。
- 回归：隔离 Unity 编译通过；Provider 配置测试 `4/4`、服务/序列化定向回归 `3/3`、MiniBomber 权威模拟 `3/3` 通过；KCP 示例 Window Registry 已完成二阶段恢复生成。
- 完整套件：EditMode 为 `80/83`。剩余三项仍是下方记录中的既有 EventBus/MTask 测试域初始化问题，与本次 Provider、GameStartup 或序列化改动无关；单独运行 EventBus 仍可复现相同结果，本次不扩大范围修改该测试基础设施。

### 2026-08-04

- 症状：完整 EditMode 测试最初把 MiniBomber 的三个 ScriptableObject 判定为无效资产，UI 源码扫描也发现 BatchMode 分支。
- 根因：三个 Unity 资产类型同处一个脚本文件，隔离生成后没有稳定 MonoScript 引用；UIService 同时承担了运行形态判断和 UI 生命周期。
- 修复：将 RuntimeConfig、RuleConfig、MapDefinition 拆成同名独立脚本；新增通用 `AppServiceAttribute.RunInBatchMode`，由生成启动代码在 BatchMode 跳过 UIService 注册。
- 回归：UIFramework `13/13`、MiniBomber 权威模拟 `3/3`、现有 UI PlayMode `3/3` 通过；完整 EditMode 为 `76/79`。
- 已知限制：剩余三项是既有 EventBus 测试问题。`NetworkLoopbackIntegrationTests` 的 TearDown 关闭 MTask 应用域后，后续 EventBus 用例没有重新初始化任务域；其中取消断言还使用了要求异常精确类型的 `Assert.Throws<OperationCanceledException>`，而运行时返回其派生类 `MTaskCanceledException`。本次未改动该独立测试基础设施。

## 14. 常见问题

| 现象 | 检查 |
| --- | --- |
| 进入 LoginScene 但没有 UI | 确认 11 个 Prefab 位于 Demo UI 目录，Route/Presenter 正确，然后生成 Window Registry。 |
| 业务场景出现两个 EventSystem | 删除业务场景中的 EventSystem；唯一 EventSystem 属于 ApplicationUIRoot。 |
| CanvasScaler 提示 Non-root Canvas | 只保留 OverlayRootCanvas/CameraRootCanvas 的 Scaler，不给 Layer Canvas 或窗口添加 Scaler。 |
| Android 摇杆没输入 | 确认 Input Handling = Both，Actions 已绑定，OnScreen 路径是虚拟 Gamepad 路径。 |
| Android 炸弹按钮没输入 | BombButton 不需要绑定到 View 字段；确认它挂有 OnScreenButton，Control Path 为 `<Gamepad>/buttonSouth`，PlaceBomb Action 监听相同路径。 |
| Android 移动时不能同时放炸弹 | 确认客户端使用订阅 `PlaceBomb.performed` 的新输入代码，并检查 ApplicationUIRoot 的 Pointer Behavior 没有被改为单指模式；只更新 Prefab 而未发布对应 HotUpdate DLL 不会生效。 |
| Android 动态角色或特效缺失并报告 class ID | 确认 `Assets/Linker/MiniCore.link.xml` 存在，且 `UpdateMainWindow` 在下载完成后显式调用公开的 `UnityEngineTypePreserver.ProtectDynamicContentTypes()`；然后重新生成 HybridCLR、构建并安装 Player，只更新热更包或补充 AOT 元数据不够。 |
| 客户端认证成功但找不到 Lobby | 检查认证响应中的 Coordinator 外网地址、Lobby 是否已注册 `Ready`，以及 Lobby 的 `advertised.outerWebSocketUrl` 是否对客户端可达。 |
| DS 无法启动 | 检查 `--minicore-config` 指定的绝对路径、配置 SHA-256、Role Catalog、实例 ID、监听端口和 Coordinator 内网地址。 |
| Lobby/Game 找不到 Database | 确认 `persistenceMode=Database`、DatabaseServer 已以保留 Database ServiceId 注册 Ready；不使用数据库时应明确设为 `None`。 |
| 登录后显示 `DatabaseUnavailable` | 检查 DatabaseServer 进程与 `7300` Listener、Coordinator 中 Database-01 是否 Ready，以及 GameCluster 到该内网地址是否可达；框架会自动重连，不应再显示底层 `Session MiniBomber.Database not connected`。 |
| 重连后角色回原点 | 这不是设计行为；检查服务器是否使用新 HotUpdate DLL，并验证输入超时测试。 |
| 设备仍运行 V1 | 确认执行 CompileDll/Prepare，上传完整新版本，Host URL 指向对应平台。 |
| Android 报 `NotSupportAdjustorThunk` | 先确认异常是否来自复杂值类型跨 HotUpdate/AOT 泛型边界；若 AOT 公共类型已经修改，执行 Android `HybridCLR > Generate > All` 后重新构建并安装 Player，不能只发布热更包。 |

通用 UI 规则见 [UI 框架](../UIFramework.md)，协议与 Handler 规则见 [网络与协议](../NetworkLayerAnalysis.md)。
