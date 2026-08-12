# Editor、生成器、工具与验证

读取本页处理 Unity 菜单、代码生成、项目辅助工具、Editor 测试或性能回归。

```text
Assets/Scripts/MiniCore/Editor/
├── HotUpdateAssembly/            # 热更新程序集登记共享 Editor 配置
├── Protocol/                     # 独立 Proto 生成、稳定 Opcode 与发布校验
│   └── Opcode/                  # 独立 Handler 扫描、失效与直接注册生成
├── HybridCLR/                    # DLL 同步、YooAsset 构建与校验
├── Performance/                  # 性能历史工具
├── AI/                           # 开发导航生成器
├── Startup/                      # 启动配置与代码生成
└── UI/                           # UI Authoring、Registry 与构建校验
Assets/Tests/Editor/              # 生命周期、服务、网络和性能测试
Tools/MTaskCodeGen/               # MTask Owner IL 后处理器
Tools/EventAnalyzer/              # 事件订阅分析器
```

关键入口：`Editor/Protocol/ProtoCodeGenerator.cs`、`Editor/Protocol/Opcode/OpcodeRegistryGenerator.cs`、`Editor/HotUpdateAssembly/MiniCoreHotUpdateAssemblySettings.cs`、`Editor/Startup/MiniCoreStartupCodeGenerator.cs`、`Editor/UI/UIWindowRegistryGenerator.cs`、`Editor/HybridCLR/HybridClrYooAssetBuildCommand.cs`。

- `MiniCore.Protocol.Editor` 可在 HotUpdate 编译失败时独立运行 protoc、维护 Opcode 与生成 Protocol 代码；`MiniCore.Protocol.Handler.Editor` 在成功编译后扫描全部已登记热更新程序集，两者不反向引用主 `MiniCore.Editor`。
- `MiniCoreHotUpdateAssemblySettings` 是 Proto 输出归属、Handler 扫描、HybridCLR DLL 清单与 Bootstrap 加载顺序的共同来源；新增热更新模块使用 `MiniCore/HotUpdate/创建并登记热更新模块`。
- 修改 Proto、Handler、UI 或启动设置时遵循对应生成器，不编辑 Generated 产物。Proto 所有权清单带版本、生成器、旧输出目录和来源摘要，清理前还会检查固定文件名与生成标记。
- Player 构建前分别执行 Proto、Handler、UI、平台边界与 HybridCLR/YooAsset 校验；HybridCLR 校验由独立 `IPreprocessBuildWithReport` 入口触发，不依赖 Opcode 校验器调用。
- 结构变动后点击 `MiniCore/AI/Generate Development Navigation` 更新本 Skill 的自动页。
- 网络和性能变动先阅读 `Docs/NetworkSmokeTesting.md`、`Docs/PerformanceTestingGuide.md` 与 `Docs/OptimizationRoadmap.md`；故障按项目的 evidence-driven-debugging Skill 处理。
