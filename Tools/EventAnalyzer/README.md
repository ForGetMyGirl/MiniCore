# MiniCore Eventing Analyzer

该目录保存强类型事件订阅诊断的 Roslyn Analyzer 源码与构建脚本。

在 macOS 和 Unity 2021.3 环境执行：

```bash
bash Tools/EventAnalyzer/BuildEventAnalyzer.sh
```

脚本仅引用 Unity Editor 内置的 Roslyn 组件，输出 Editor-only DLL 到 `Assets/Plugins/MiniCore/Eventing/Editor`。该 DLL 的 `.meta` 使用 `RoslynAnalyzer` 标签，因此 Unity 会在脚本编译与受支持 IDE 中启用 MCEVT001、MCEVT002、MCEVT003 诊断。

## 验证

```bash
bash Tools/EventAnalyzer/TestEventAnalyzer.sh
```

该脚本使用 Unity 自带 Roslyn 编译测试夹具，验证 MCEVT001、MCEVT002、MCEVT003 均会被报告，同时保留一条不应误报的命名方法订阅。
