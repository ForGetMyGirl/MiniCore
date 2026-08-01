# Editor、生成器、工具与验证

读取本页处理 Unity 菜单、代码生成、项目辅助工具、Editor 测试或性能回归。

```text
Assets/Scripts/MiniCore/Editor/
├── Protocol/                     # Proto 生成与发布校验
├── HybridCLR/                    # DLL 同步、YooAsset 构建与校验
├── Performance/                  # 性能历史工具
├── AI/                           # 开发导航生成器
└── *Generator*.cs                # Opcode、启动、UI 等生成器
Assets/Tests/Editor/              # 生命周期、服务、网络和性能测试
Tools/MTaskCodeGen/               # MTask Owner IL 后处理器
Tools/EventAnalyzer/              # 事件订阅分析器
```

关键入口：`Editor/Protocol/ProtoCodeGenerator.cs`、`Editor/OpcodeRegistryGenerator.cs`、`Editor/MiniCoreStartupCodeGenerator.cs`、`Editor/HybridCLR/HybridClrYooAssetBuildCommand.cs`。

- 修改 Proto、Handler 或启动设置时遵循既有生成器，不编辑 Generated 产物。
- 结构变动后点击 `MiniCore/AI/Generate Development Navigation` 更新本 Skill 的自动页。
- 网络和性能变动先阅读 `Docs/NetworkSmokeTesting.md`、`Docs/PerformanceTestingGuide.md` 与 `Docs/OptimizationRoadmap.md`；故障按项目的 evidence-driven-debugging Skill 处理。
