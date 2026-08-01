---
name: minicore-development
description: Navigate and develop the MiniCore Unity project safely. Use when a task involves MiniCore Runtime, Global, MTask, events, networking, Proto, Handler or Opcode generation, Unity services or UI, HotUpdate, HybridCLR, YooAsset, Bootstrap, Editor tools, tests, or build workflows, and Codex needs to locate the correct module and project boundary before changing code.
---

# MiniCore 开发导航

## 工作方式

1. 先阅读 `Docs/AI_CONTEXT.md`，并以当前源码与 asmdef 为最终事实来源。
2. 按任务只读取下列一份或两份领域资料；不要一次加载全部资料。
3. 再打开资料中列出的入口脚本、关联文档与测试，确认现有实现后再修改。
4. 涉及目录、程序集、扩展点或生成流程的结构改动后，在 Unity 点击 `MiniCore/AI/Generate Development Navigation` 更新 `references/generated/`；不要手改这些生成页。

## 资料路由

- Global、组件、服务、MTask、事件：[runtime-core.md](references/runtime-core.md)
- Proto、Opcode、Handler、TCP/UDP/KCP：[network-protocol.md](references/network-protocol.md)
- Unity 生命周期、UI、资源、存档、HTTP、音频：[unity-services-ui.md](references/unity-services-ui.md)
- Bootstrap、HotUpdate、HybridCLR、YooAsset、启动与发布：[hotupdate-build.md](references/hotupdate-build.md)
- Editor 菜单、生成器、项目工具、测试与性能验证：[editor-generation-tests.md](references/editor-generation-tests.md)
- 路径、asmdef 引用或扩展点不确定时，再读取 `references/generated/` 中对应自动生成页。

## 必守边界

- 纯 C# 程序集不得使用 Unity API；依赖方向以 `Docs/Architecture.md` 和 asmdef 为准。
- 不手改 Protocol、HotUpdate、Bootstrap 下的 `Generated/` 文件；按项目既有生成流程维护。
- 资源、UI 与 AppService 通过公开接口和 `Global` 获取，不恢复已删除的旧组件类型。
- 导航资料不记录配置中的密钥、令牌或私有地址；只记录配置文件路径和用途。
