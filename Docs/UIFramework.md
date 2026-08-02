# MiniCore UI 框架

本文说明 MiniCore 当前 UGUI 框架的 Root、窗口层级、CanvasScaler、安全区域、Authoring、Preset、运行时生命周期和 KCP 示例。代码实现与本文不一致时视为缺陷。

## 1. 结论与资源位置

- `ApplicationUIRoot` 由 `UIService` 自动加载并持久化，不能放进 `HotUpdateScene` 或业务场景。
- 业务场景不需要应用 Canvas，也不需要 EventSystem；Bootstrap 更新界面不属于 Application UI。
- 窗口 Prefab 直接成为目标 Layer Canvas 的子节点，没有 `WindowMount`、`FullScreenRoot`、`SafeAreaRoot` 或 `PoolRoot`。
- 窗口自己的背景、标题、按钮和文本仍全部制作在自己的 Prefab 中，Root 只决定窗口之间的渲染顺序。
- Overlay 与 Screen Space Camera 各有一个根 Canvas 和一个 CanvasScaler；Layer Canvas 只负责隔离重建和排序，不重复缩放。

| 内容 | 位置 |
| --- | --- |
| Root Prefab | `Assets/AssetRes/UI/Framework/ApplicationUIRoot.prefab` |
| UI Profile | `Assets/AssetRes/UI/Profiles/UIProjectProfile.asset` |
| 窗口 Prefab | `Assets/AssetRes/UI/Windows` |
| CanvasScaler 与 Transition Preset | `Assets/Settings/MiniCore/UI/Presets` |
| View、Root、布局、动画和输入组件 | `Assets/Scripts/MiniCore/Unity/UI` |
| UIService、Session、Presenter、路由和注册表 | `Assets/Scripts/MiniCore/HotUpdate/UI` |
| Inspector、创建向导和生成器 | `Assets/Scripts/MiniCore/Editor/UI` |

## 2. ApplicationUIRoot 的职责

`ApplicationUIRoot` 只管理三类内容：渲染空间、窗口层级和全局 UI 基础设施。固定结构如下：

```text
ApplicationUIRoot
├── EventSystem
├── UICamera
├── OverlayRootCanvas                 Canvas + CanvasScaler
│   ├── BackgroundLayer               Canvas + UILayerHost
│   ├── ScreenLayer                   Canvas + UILayerHost
│   ├── HudLayer                      Canvas + UILayerHost
│   ├── WindowLayer                   Canvas + UILayerHost
│   ├── PopupLayer                    Canvas + UILayerHost
│   ├── TooltipLayer                  Canvas + UILayerHost
│   ├── ToastLayer                    Canvas + UILayerHost
│   ├── GuideLayer                    Canvas + UILayerHost
│   ├── DragLayer                     Canvas + UILayerHost
│   ├── TransitionLayer               Canvas + UILayerHost
│   │   └── LoadingOverlay            框架的实际 UI
│   ├── SystemLayer                   Canvas + UILayerHost
│   └── DebugLayer                    Canvas + UILayerHost
└── CameraRootCanvas                  Canvas + CanvasScaler
    └── 与 Overlay 相同的 12 个 Layer Canvas
```

这些节点的作用是：

| 节点 | 作用 |
| --- | --- |
| `OverlayRootCanvas` | 承载普通 Screen Space Overlay 窗口。 |
| `CameraRootCanvas` | 承载必须使用 UI Camera 的 Screen Space Camera 窗口。 |
| `UICamera` | 只供 `CameraRootCanvas` 使用。 |
| `EventSystem` | 统一处理应用 UI 输入；Root 初始化时暂时禁用场景中的重复 EventSystem，销毁时恢复。 |
| `UILayerHost` | 保存 `Canvas、RenderSpace、Layer、SortingOrder`；它自己的 RectTransform 就是窗口父节点。 |
| `LoadingOverlay` | 全局延迟 Loading，是 Transition 层中的实际 UI，不是额外挂载节点。 |

Layer 的实际渲染顺序固定为：

```text
Background 0 → Screen 100 → Hud 200 → Window 300 → Popup 400
→ Tooltip 500 → Toast 600 → Guide 700 → Drag 800
→ Transition 900 → System 1000 → Debug 1100
```

Layer Canvas 是有意义的性能边界：某层发生布局或顶点变化时，不必重建整个应用 UI。它们不是新的设计坐标空间，也不是要求每个窗口再复制一套节点。窗口直接挂到 Layer，窗口内部怎样组织仍由窗口自己决定。

框架没有 World Space Layer。角色血条、场景提示等 World Space UI 应属于后续的场景/对象锚点体系，不挂入 `ApplicationUIRoot`。

## 3. Canvas 与 CanvasScaler

只有两个根 Canvas 带 CanvasScaler：

| 根 Canvas | Render Mode | 默认配置 |
| --- | --- | --- |
| `OverlayRootCanvas` | Screen Space Overlay | Scale With Screen Size，1920×1080，Match 0.5，PPU 100 |
| `CameraRootCanvas` | Screen Space Camera | Scale With Screen Size，1920×1080，Match 0.5，PPU 100 |

每个 Layer 都有嵌套 Canvas，用于排序与隔离 Canvas rebuild，但没有 CanvasScaler。嵌套 Canvas 会继承根 Canvas 的缩放结果，因此给它添加 CanvasScaler 不会产生第二套有效缩放，反而会显示 `Non-root Canvases will not be scaled`。这条提示正是在说明 CanvasScaler 被错误地挂到了非根 Canvas。

根 CanvasScaler 的序列化值是唯一运行配置。`UIProjectProfile` 不再保存第二套设计分辨率、Match 或缩放上下限，`UIResolutionService` 也不会在运行时改写 `scaleFactor`。在 Prefab Mode 和运行时看到的根 CanvasScaler 参数完全一致。

Overlay 和 Camera 的参数可以不同。例如主 UI 使用 Landscape Preset，而 Camera UI 使用 Camera Space Preset。调整时只修改两个根 Canvas，不修改 Layer 或窗口。

普通窗口根节点不应带 Canvas。只有窗口内部确实需要独立排序或局部 rebuild 时，才在子节点添加 Canvas 和 `UISubCanvas`。

## 4. 场景与 Root 创建菜单

`UIService` 启动时按 `UIProjectProfile.ApplicationRootAddress` 从 YooAsset 加载 `ApplicationUIRoot`，初始化后调用 `DontDestroyOnLoad`。因此：

- `HotUpdateScene` 不放 Application Canvas。
- 正式业务场景不放 Application Canvas。
- 不要为了防止 Prefab 丢失而在场景放一份 Root。
- Bootstrap 更新 Canvas 可以存在，但它在 HotUpdate DLL 启动前工作，不属于这套应用 UI。

以下两个入口调用同一套幂等“创建或修复”逻辑：

- Hierarchy 右键：`MiniCore/RootCanvas`
- 顶部菜单：`MiniCore/UI/RootCanvas`

它们只创建或校验 `ApplicationUIRoot.prefab` 并在 Project 中选中该资产，永远不会向当前场景写入对象。修复 Root 时保留合法的用户 CanvasScaler 参数；缺失或非法时才恢复默认值。

完整项目资产可通过 `MiniCore/UI/Generate Project UI Assets` 生成。YooAsset 只收集运行时使用的 `UIFramework、UIProfiles、UIWindows` 三组；`.preset` 是编辑器工具资产，不进入 YooAsset。

## 5. 窗口 Prefab 怎样组织

按钮、标题、文本、背景和面板都留在自己的窗口 Prefab 中。窗口打开时，框架把整个 Prefab 根节点直接挂到配置的 Layer Canvas。以 Screen 为例：

```text
ScreenWindowView                  AUIWindowView + CanvasGroup
├── BackgroundRoot               全屏背景，不受安全区约束
└── ContentRoot                  SafeAreaTarget
    ├── Title
    ├── Buttons
    └── 其他业务控件
```

不是把控件从窗口“移动到 Root 的 SafeArea 节点”，而是由 `AUIWindowView.SafeAreaTarget` 引用窗口自己的 `ContentRoot`。设备安全区变化时，View 只修改这个目标的锚点。

创建向导的模板结构为：

| 模板 | 默认节点与行为 |
| --- | --- |
| Screen | `BackgroundRoot + ContentRoot`，ContentRoot 是安全区目标。 |
| Hud | `ContentRoot`。 |
| FloatingWindow | `ContentRoot/PanelRoot`，PanelRoot 是动画目标。 |
| ModalPopup | `ContentRoot/PanelRoot`，PanelRoot 是动画目标；Session 运行时创建同层遮罩。 |
| Toast | `ContentRoot/ToastRoot`。 |
| Guide / System | `BackgroundRoot + ContentRoot`。 |
| Custom | 只创建根节点，不添加无意义子节点，不自动添加动画。 |

每个模板只是可直接使用的起点，不是要求所有 UI 窗体复制复杂约束。业务可以继续在这些节点下自由拼装界面。

## 6. AUIWindowView Authoring

窗口定义直接序列化在所有 View 的基类 `AUIWindowView` 中，不再有单独的 `UIWindowAuthoring` 组件。派生 View 只声明控件引用和显示逻辑：

```csharp
public sealed class InventoryWindowView : AUIWindowView
{
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform itemRoot;

    public Button CloseButton => closeButton;
    public Transform ItemRoot => itemRoot;
}
```

基类 Inspector 管理以下配置：

- WindowId、Route、YooAsset Address
- Template、Render Space、Layer
- Instance、Duplicate Open、Cache 策略
- Presenter/ViewModel 类型
- Modal 与遮罩点击策略
- 可选 Transition Driver
- Safe Area 策略与目标
- Navigation Group 和缓存数量

注册表生成器直接读取 Prefab 根节点的 `AUIWindowView`。窗口只允许一个 View 定义源，不扫描或依赖旧 Authoring 组件。`Transition Driver = null` 表示无动画，不需要空 Driver 占位。

推荐流程：

1. 选择 `MiniCore/UI/Create Window`，填写名称、模板和实例策略。
2. 向导先生成 View 与 Presenter；编译后自动创建 Prefab。
3. 在 Prefab 的 View Inspector 选择逻辑类型、渲染空间、Layer、安全区和动画。
4. 在派生 View 中声明控件字段，并在 Prefab 中绑定引用。
5. 保存 Prefab，然后执行 `MiniCore/UI/Generate Window Registry`；脚本重载也会自动同步。

生成文件为 `UIWindowRoutes.Generated.cs` 和 `UIWindowRegistry.Generated.cs`。Player 运行时直接构造 Presenter 并获取强类型 View，不扫描程序集、不使用字符串反射或 `Activator`。

## 7. 安全区域

安全区域是窗口策略，不是 Root 层级。Session 打开窗口时调用 View 绑定 `UIResolutionService`；关闭或缓存时自动解绑。策略如下：

| 策略 | 行为 |
| --- | --- |
| `Inherit` | 使用 `UIProjectProfile.DefaultSafeAreaPolicy`。 |
| `ConstrainContent` | 修改 `SafeAreaTarget` 锚点，通常目标是窗口自己的 ContentRoot；目标必填。 |
| `ConstrainWindow` | 修改窗口根 RectTransform 锚点，不需要额外目标。 |
| `Ignore` | 框架不修改布局，适合全屏背景、遮罩和转场。 |
| `Custom` | 框架不修改布局，由业务自行处理。 |

`ConstrainContent` 未配置 `SafeAreaTarget` 时，Inspector/注册表/构建校验会直接报错。安全区换算由无状态 `UISafeAreaUtility` 完成，不要求窗口挂 `UISafeAreaFitter`。

安全区和窗体内容的常见组合：

- Screen：背景留在 `BackgroundRoot` 全屏显示，按钮和标题放进 `ContentRoot`。
- Popup：需要整个弹窗避开刘海时使用 `ConstrainWindow`；只约束内部按钮时使用 `ConstrainContent`。
- Loading、遮罩和必须铺满屏幕的背景：使用 `Ignore`。
- 软键盘遮挡由 `UIKeyboardAvoidance` 单独处理，不改变 SafeArea 定义。

## 8. Unity Preset

项目使用 Unity 原生 Preset，不使用运行时 Preset 资源引用。预置资产位于 `Assets/Settings/MiniCore/UI/Presets`：

- CanvasScaler：Landscape 1920×1080、Portrait 1080×1920、Tablet 2048×1536、Camera Space。
- Transition：Fade、Popup Scale、Slide Left、Slide Right、Toast。

使用方法：

1. 选中 `CanvasScaler` 或 `UIPresetTransition` 组件。
2. 点击组件标题栏右上角的 Unity 原生 Preset 图标。
3. 在 Preset Selector 中选择预置并应用。
4. 检查结果并保存 Prefab。

这就是 Unity/XR 组件使用的同一套 Preset Selector。Preset 只在编辑器把参数复制到组件，不会与已应用对象保持运行时关联；修改 `.preset` 后，已有窗口需要重新应用。

Transition Preset 明确排除了 `Target` 和 `CanvasGroup` 字段，因此应用 Fade、Popup 或 Slide 时只改变时长、曲线、透明度、缩放和位移参数，不会破坏窗口对象引用。

## 9. 窗口生命周期、缓存和 Modal

固定状态流为：

```text
Loading → Staging → Opening → Active → Closing → Cached / Destroyed
                  └────────────────────→ Closing
任意未终结状态 ───────────────────────→ Failed
```

缓存 View 第一次实例化时就以最终 Layer 为父节点。关闭时原地禁用并进入缓存栈，不移动到隐藏 Pool；复用时重新激活并放到正确的同层顺序。Prefetch 也直接创建在最终 Layer 下。

Modal 遮罩由 Session 在窗口同一 Layer 中动态创建，拉伸铺满 Layer，Sibling 顺序始终位于窗口正下方。遮罩和窗口一起关闭；是否允许点击遮罩关闭由 View 配置决定。

Presenter/ViewModel 使用 Session 任务域；View 每次激活创建独立任务域。关闭、加载失败、动画异常和服务退出进入同一清理路径，绑定、安全区订阅、逻辑、遮罩和资源租约都会释放。

## 10. 强类型 API

业务通过 `Global` 获取接口：

```csharp
MiniCore.UI.IUIService ui = Global.GetService<MiniCore.UI.IUIService>(this);

UIWindowHandle bag = await ui.OpenAsync<BagWindow>();
UIWindowHandle detail = await ui.OpenAsync<PlayerDetailWindow>(
    new PlayerDetailOpenArgs(playerId));

await ui.NavigateAsync<HomeWindow>();
await ui.PrefetchAsync<BagWindow>(2);
ui.Focus(detail);
await ui.CloseAsync(detail);
```

调用方不传 Prefab 地址、View/Presenter 类型、Canvas Layer、动画或缓存参数；这些值全部来自生成注册表。

`WindowId` 是 View 中序列化的稳定 128 位身份。`RouteName` 是开发者可读的公共类型名，普通 Prefab 重命名不会自动改变 Route。`UIWindowHandle` 包含 WindowId、实例键和代次，旧代句柄不能误操作缓存复用后的窗口。

## 11. KCP 可运行示例

当前端到端示例为 `KcpTestWindow`：

| 内容 | 路径 |
| --- | --- |
| View | `Assets/Scripts/MiniCore/HotUpdate/UI/Test/View/KcpTestWindowView.cs` |
| Presenter | `Assets/Scripts/MiniCore/HotUpdate/UI/Test/Presenter/KcpTestWindowPresenter.cs` |
| Prefab | `Assets/AssetRes/UI/Windows/KcpTestWindow.prefab` |
| 生成路由 | `Assets/Scripts/MiniCore/HotUpdate/UI/Generated/UIWindowRoutes.Generated.cs` |

`GameStartup` 使用 `await ui.OpenAsync<KcpTestWindow>()` 打开示例。运行 `HotUpdateScene` 后，可在窗口中启动 KCP Server、连接 Client、发送 Normal/RPC 消息。它同时演示：

- View 基类内置 Authoring 配置；
- `ContentRoot` 作为 SafeAreaTarget；
- `UIPresetTransition` 的 Target/CanvasGroup 绑定；
- Presenter 事件绑定；
- 强类型 Route 和生成注册表。

## 12. 动画扩展

内置 Driver：

- `UIPresetTransition`：透明度、缩放、位移、Curve 和 Unscaled Time。
- `UIAnimatorTransition`：Animator Enter/Exit 状态、超时和打断收敛。
- 自定义组件：实现 `IUITransitionDriver` 后直接赋给 View。

无动画窗口将 Transition Driver 留空即可。自定义 Driver 必须在缓存前恢复 Transform、Alpha、Animator/Tween 状态，并保证被打断的旧动画不会继续回调已复用的 View。

## 13. 构建校验

构建前会校验：

- Root 双 Canvas、固定 Layer 和 CanvasScaler 配置；
- View 的 WindowId、Route、Address、Presenter 和生成表一致；
- 普通窗口根节点无 Canvas，子 Canvas 必须声明 `UISubCanvas`；
- `ConstrainContent` 已配置 SafeAreaTarget；
- YooAsset 包含 Root、Profile 和全部 Window 地址；
- Transition Driver 为空或确实实现 `IUITransitionDriver`。

Preset 不进入 YooAsset，也不参与运行时加载。
