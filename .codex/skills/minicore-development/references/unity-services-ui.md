# Unity 适配、服务与 UI

读取本页处理 Unity 生命周期、资源/UI、设备、本地数据、HTTP、音频或游戏业务服务。

```text
Unity/
├── Driver/                       # Global 每帧驱动与启动
├── Mono/                         # Mono 行为、绑定与输入
├── Service/                      # 音频、设备、HTTP、存储、遥测
├── Startup/                      # MiniCoreStartupSettings 定义
├── Threading/                    # AMTaskBehaviour、YooAsset MTask 适配
└── UI/                           # Interface、Presenter、View、Model、State、动画与布局
HotUpdate/
├── Service/Resource/             # 资源与资产服务
├── Service/Scene/                # 场景服务
├── Client/                       # 配置、对象池、监听器
└── UI/                           # 热更新业务窗口
```

关键入口：`Unity/Driver/UnityGlobalDriver.cs`、`Unity/Startup/MiniCoreStartupSettings.cs`、`Unity/UI/View/AUIWindowView.cs`、`Unity/UI/Interface/IUIService.cs`、`HotUpdate/Service/Resource/YooAssetResourceService.cs`、`HotUpdate/Service/Scene/YooAssetSceneService.cs`、`HotUpdate/UI/Service/UIService.cs`。

- AppService 只能通过对应接口和 `Global.GetService<T>` 使用；启动设置在 `Assets/Settings/MiniCoreStartupSettings.asset`。
- 资源、UI、场景绑定使用当前服务契约，不重新引入旧组件名称。
- 配置文件可能包含敏感参数；不得复制其具体值到资料或日志。完整服务说明见 `Docs/StartupModules.md`。
