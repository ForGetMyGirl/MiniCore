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
| UIService、Session、窗口定义和运行时注册表 | `Assets/Scripts/MiniCore/Unity/UI` |
| 业务 View/Presenter | `Assets/Scripts/MiniCore/HotUpdate/Demos/*/Client/UI` |
| 业务路由与构造注册代码 | `Assets/Scripts/MiniCore/HotUpdate/UI/Generated` |
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

`AUIWindowView.PrepareForOpen` 会在每次打开或从缓存恢复时，将窗口根 RectTransform 统一恢复为目标 Layer 的全拉伸子节点（Anchors 0～1、Offsets 0、Scale 1）。因此窗口根不应保存 1920×1080 等固定尺寸；需要固定宽高的弹窗面板应放在 `ContentRoot/PanelRoot`，之后再由安全区策略修改窗口根或 ContentRoot。

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

窗口定义直接序列化在所有 View 的基类 `AUIWindowView` 中，不再有单独的 `UIWindowAuthoring` 组件。派生 View 使用私有序列化字段持有控件，并通过语义方法向 Presenter 暴露用户意图、输入读取和界面刷新：

```csharp
public sealed class InventoryWindowView : AUIWindowView
{
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text countText;

    public void BindActions(UIBindingSet bindings, Action close)
    {
        if (close != null) bindings.Add(closeButton, close.Invoke);
    }

    public void RefreshCount(int count)
    {
        countText.text = count.ToString();
    }
}
```

Presenter 禁止直接访问 Unity 控件，也不接收完整业务 Model。简单界面使用明确参数；只有多个 Model 需要组合成复杂窗口展示时才建立窗口专用 ViewData，并且只包含该窗口实际使用的字段。ViewData 是一次展示投影，不回写业务状态，也不成为第二份权威数据。

基类 Inspector 管理以下配置：

- WindowId、Route、YooAsset Address
- Template、Render Space、Layer
- Instance、Duplicate Open、Cache 策略
- Presenter 类型
- Modal 与遮罩点击策略
- 可选 Transition Driver
- Safe Area 策略与目标
- Navigation Group 和缓存数量

同一个 Inspector 会在 `View Bindings` 区自动绘制派生 View 自己声明的 `Button`、`TMP_Text`、`GameObject` 等序列化字段。控件引用仍绑定在窗口自己的 View 上，不需要另挂 Authoring 组件。

注册表生成器直接读取 Prefab 根节点的 `AUIWindowView`。窗口只允许一个 View 定义源，不扫描或依赖旧 Authoring 组件。`Transition Driver = null` 表示无动画，不需要空 Driver 占位。

推荐流程：

1. 选择 `MiniCore/UI/Create Window`，填写名称、模板和实例策略。
2. 向导先生成 View 与 Presenter；编译后自动创建 Prefab。
3. 在 Prefab 的 View Inspector 选择逻辑类型、渲染空间、Layer、安全区和动画。
4. 在派生 View 中声明控件字段，并在 Prefab 中绑定引用。
5. 保存 Prefab，然后执行 `MiniCore/UI/Generate Window Registry`；脚本重载也会自动同步。

生成文件为 `UIWindowRoutes.Generated.cs` 和 `ProjectUIWindowRegistration.Generated.cs`。后者由客户端启动代码先注入 AOT `UIWindowRegistry.Project`，再初始化 `UIService`。Player 运行时通过构造委托直接创建 Presenter 并获取强类型 View，不扫描程序集、不使用字符串反射或 `Activator`。

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

`NavigateAsync` 负责同一 Navigation Group 内 Screen 到 Screen 的替换；当业务进入战斗等“不需要 Screen、只显示 Hud”的状态时，调用 `CloseNavigationAsync(group)` 明确关闭该组当前 Screen。Hud、Popup、System 等其他 Layer 不属于 Screen 导航组，必须由打开方保存 `UIWindowHandle` 并在业务状态退出时精确关闭。

Modal 遮罩由 Session 在窗口同一 Layer 中动态创建，拉伸铺满 Layer，Sibling 顺序始终位于窗口正下方。默认使用黑色 `Alpha 0.8`，确保弹窗和背景有明确层次；遮罩和窗口一起关闭，是否允许点击遮罩关闭由 View 配置决定。

Presenter 使用 Session 任务域；View 每次激活创建独立任务域。关闭、加载失败、动画异常和服务退出进入同一清理路径，绑定、安全区订阅、逻辑、遮罩和资源租约都会释放。

MiniCore 不提供空壳 ViewModel 或自动数据绑定。业务采用 `AComponent + MVP` 时，Model 是协议与 Unity 无关的长期业务数据，AComponent 是 Model 的唯一业务写入者，Presenter 负责订阅 Model 变化、协调 Component 命令并调用 View 语义方法；跨窗口和场景编排交给 Flow Component。PB 消息只在 Handler/Component 边界短暂存在，不能保存进 Model，也不能传给 Presenter 或 View。

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

`WindowId` 是 View 中序列化的稳定 128 位身份。`RouteName` 是开发者可读的公共类型名，普通 Prefab 重命名不会自动改变 Route。`UIWindowHandle` 是不可变引用句柄，包含 WindowId、实例键和代次；两个句柄按实例身份比较，旧代句柄不能误操作缓存复用后的窗口，`null` 表示没有有效窗口。使用引用句柄还能避免复杂值类型跨 HotUpdate `MTask<T>` 与 AOT 泛型边界时依赖额外的 HybridCLR adjustor thunk。

## 11. KCP 可运行示例

当前端到端示例为 `KcpTestWindow`：

| 内容 | 路径 |
| --- | --- |
| View | `Assets/Scripts/MiniCore/HotUpdate/Demos/NetworkLab/Client/UI/View/KcpTestWindowView.cs` |
| Presenter | `Assets/Scripts/MiniCore/HotUpdate/Demos/NetworkLab/Client/UI/Presenter/KcpTestWindowPresenter.cs` |
| Prefab | `Assets/AssetRes/UI/Windows/KcpTestWindow.prefab` |
| 生成路由 | `Assets/Scripts/MiniCore/HotUpdate/UI/Generated/UIWindowRoutes.Generated.cs` |
| 生成注册 | `Assets/Scripts/MiniCore/HotUpdate/UI/Generated/ProjectUIWindowRegistration.Generated.cs` |

KCP 示例入口和强类型路由仍保留，但 `GameStartup` 已改为启动 [MiniBomber 全链路 Demo](Demos/MiniBomber.md)，不再默认打开 `KcpTestWindow`。需要单独验证时，在自定义启动或测试入口调用 `await ui.OpenAsync<KcpTestWindow>()`。该窗口可启动 KCP Server、连接 Client、发送 Normal/RPC 消息，并演示：

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

## 14. 验证记录

### 2026-08-18（MVP 边界与 ViewModel 空壳清理）

- 约定：窗口 View 的 Unity 字段统一使用 `[SerializeField] private`，Presenter 只通过 `Bind/Get/TryGet/Refresh/Set/Show` 等语义方法访问 View。
- 数据边界：复杂窗口按需使用只含展示字段的 ViewData；禁止把完整 Model、PB 消息或协议集合传入 View。
- 框架清理：删除没有数据绑定能力且从未使用的 `AUIWindowViewModel<TView>`，Inspector、向导、生成器和生命周期说明统一只称 Presenter。
- 向导：新 View 模板明确生成私有 Unity 字段区与语义方法位置，Presenter 模板明确标出 Component 获取、Model 订阅和首次渲染位置。

### 2026-08-15（HotUpdate 程序集拆分后的窗口逻辑引用）

- 症状：执行 `MiniCore/UI/Generate Window Registry` 时，`LoginWindow` 等窗口的 Presenter 被判定为无效或不可直接构造。
- 根因：HotUpdate 拆分为 `MiniCore.HotUpdate.Shared`、`MiniCore.HotUpdate.Client` 和 `MiniCore.HotUpdate.Server` 后，12 个窗口 Prefab 的 `logicTypeName` 仍保存旧程序集限定名 `MiniCore.HotUpdate`；这些窗口的 Presenter 实际均编译进 `MiniCore.HotUpdate.Client`。
- 修复：将 11 个 MiniBomber 窗口和 `KcpTestWindow` 的 Presenter 程序集限定名统一更新为 `MiniCore.HotUpdate.Client`，不增加旧程序集名兼容或运行时回退。
- 验证：在已打开的 Unity Editor 中刷新资源并重新执行窗口注册表生成，Editor 日志确认共 12 个窗口生成成功，随后 `MiniCore.HotUpdate.Client` 完成重新编译；全项目静态扫描不再存在窗口 Prefab 对旧 `MiniCore.HotUpdate` 程序集名的引用。本次未运行测试或打包。

### 2026-08-05（Modal 遮罩可见性）

- 症状：Popup 自动创建的黑色 ModalMask 使用 `Alpha 0.55`，在较暗或复杂背景中层次不明显。
- 修复：Session 的唯一运行时遮罩默认值改为 `Alpha 0.8`；不修改 `SceneLoadingWindow` 等实际业务 UI 的背景透明度，也不增加 Prefab 配置项。
- 回归：增加默认值 Editor 测试，并要求在 MiniBomber 的 Register、CreateRoom 和 MatchResult 三个 Modal Popup 上进行运行态目视检查。

### 2026-08-05（派生 View 控件字段恢复显示）

- 症状：所有 `AUIWindowView` 派生组件的 Inspector 只显示 WindowId、Route、Logic、安全区等框架字段，看不到派生类声明的按钮、文本和平台操作根节点。
- 根因：`UIWindowViewEditor` 完全接管 Inspector 后只手工绘制了基类 Authoring 字段，没有继续绘制其余序列化属性；字段仍在 Prefab 数据和脚本中，不是 Unity 序列化失败。
- 修复：增加独立 `View Bindings` 区，统一绘制所有非框架字段，同时排除已经显示过的基类属性，适用于全部窗口派生类。
- 回归：新增 Inspector 字段分类测试，并运行 UI Framework Editor 定向测试与隔离 Unity 编译；没有改变窗口序列化格式、Prefab GUID 或生成注册表。
