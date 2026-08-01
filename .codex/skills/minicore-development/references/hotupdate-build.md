# HotUpdate、HybridCLR、YooAsset 与 Bootstrap

读取本页处理热更启动、AOT 元数据、资源包或发布流程。

```text
Assets/Scripts/Project/Bootstrap/ # YooAsset 初始化、DLL 加载、反射启动
Assets/Scripts/MiniCore/HotUpdate # 唯一热更新程序集与业务入口
Assets/Scripts/MiniCore/Editor/HybridCLR/
                                  # HybridCLR/YooAsset 构建与校验
Assets/AssetRes/Dlls/             # HotUpdate DLL 和 AOT 元数据资源
Assets/AssetBundleCollectorSetting.asset
ProjectSettings/HybridCLRSettings.asset
```

关键入口：`Project/Bootstrap/UpdateMainWindow.cs`、`HotUpdate/Generated/Startup/MiniCoreStartup.Generated.cs`、`Editor/HybridCLR/HybridClrYooAssetBuildCommand.cs`、`Editor/HybridCLR/HybridClrBuildValidator.cs`。

- Bootstrap 不静态引用 HotUpdate 类型；先加载 AOT 元数据，再加载 `MiniCore.HotUpdate.dll` 并反射调用统一入口。
- 首包资源包名、DLL 地址和收集规则必须与 Bootstrap、HybridCLR 生成地址表及 YooAsset 收集器一致。
- 生成的 AOT 地址表和 DLL 资产不可手工伪造；完整操作顺序见 `Docs/BuildAndHotUpdateWorkflow.md`。
