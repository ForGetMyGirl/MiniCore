# MiniCore 文档入口

本目录是 MiniCore 当前架构的文档入口。阅读或让 AI 解析项目时，从本文件选择目标，不要使用迁移前目录结构推断当前设计。

| 需要回答的问题 | 优先阅读 |
| --- | --- |
| 框架有哪些程序集，它们能否引用 Unity，启动链如何工作 | [架构总览](Architecture.md) |
| `Global` 怎么拿、怎么释放组件，Client/Server 怎样启动 | [架构总览](Architecture.md#3-global-组件运行时) |
| 新项目如何勾选启动模块、填写 Args、编写唯一的 GameStartup | [项目启动模块配置](StartupModules.md) |
| Proto 放哪里，如何生成，Opcode 为什么由 Handler 决定 | [网络与协议](NetworkLayerAnalysis.md) |
| AI 修改代码时哪些边界不可突破，如何验证 | [AI 项目上下文](AI_CONTEXT.md) |
| 如何运行并解释性能测试 | [性能测试指南](PerformanceTestingGuide.md) |
| 如何运行 Editor / Player 网络冒烟 | [网络冒烟测试](NetworkSmokeTesting.md) |
| 哪些性能工作已完成，后续优化顺序是什么 | [优化路线图](OptimizationRoadmap.md) |

建议顺序：先读 [架构总览](Architecture.md)，再按任务进入具体文档；AI 或自动化代理还应完整读取 [AI 项目上下文](AI_CONTEXT.md)。
