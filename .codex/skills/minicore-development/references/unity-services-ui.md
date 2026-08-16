# Unity 适配、服务与 UI

读取本页处理 Unity 生命周期、资源/UI、设备、本地数据、HTTP、音频或游戏业务服务。

```text
Unity/
├── Driver/                       # Global 每帧驱动与启动
├── Mono/                         # Mono 行为、绑定与输入
├── Pooling/                      # GameObject Pool AppModule
├── Service/                      # 资源接口、配置、音频、设备、HTTP、存储、遥测
├── Startup/                      # MiniCoreStartupSettings 定义
├── Threading/                    # AMTaskBehaviour
└── UI/                           # AOT UI Runtime、Registry、Session 与基础 View/Presenter
Unity/YooAsset/
├── Resource/                     # YooAsset 资源 Provider
├── Scene/                        # YooAsset 场景 Provider
└── Threading/                    # YooAsset MTask 适配
HotUpdate/Demos/
├── MiniBomber/Client/UI/         # MiniBomber 业务窗口
└── NetworkLab/Client/UI/         # 网络实验业务窗口
```

关键入口：`Unity/Driver/UnityGlobalDriver.cs`、`Unity/Startup/MiniCoreStartupSettings.cs`、`Unity/UI/View/AUIWindowView.cs`、`Unity/UI/Interface/IUIService.cs`、`Unity/YooAsset/Resource/YooAssetResourceService.cs`、`Unity/YooAsset/Scene/YooAssetSceneService.cs`、`Unity/UI/Service/UIService.cs`。

- AppService 只能通过对应接口和 `Global.GetService<T>` 使用；客户端启动设置在 `Assets/Settings/MiniCoreStartupSettings.asset`，左侧只允许选择运行目标包含 `Client` 的 Provider。
- Dedicated Server 专用服务只显示在右侧只读能力目录，由固定宿主自动装配，不能写进客户端启动设置。
- 资源、UI、场景绑定使用当前服务契约，不重新引入旧组件名称。
- 配置文件可能包含敏感参数；不得复制其具体值到资料或日志。完整服务说明见 `Docs/StartupModules.md`。
