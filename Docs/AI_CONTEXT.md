# MiniCore AI 项目上下文

这是一份给 AI、新成员和自动化工具的快速上下文。处理代码任务前先读本文件，再按需要阅读 [架构总览](Architecture.md) 与 [网络与协议](NetworkLayerAnalysis.md)。当代码与本文冲突时，以当前代码和程序集配置为准，并同步修正文档。

## 项目一句话

MiniCore 是 Unity 2021.3 项目：纯 C# 核心通过静态 `Global` 管理组件；Unity 只是适配层；Client 与 Dedicated Server 从 YooAsset 加载同一份 `MiniCore.HotUpdate.dll`，由 HybridCLR 支持热更新。

## 不可违背的架构边界

```text
Runtime <- Protocol / Serialization <- Network <- HotUpdate
Runtime <- Unity                         <- HotUpdate
Unity  <- Project.Bootstrap               -动态加载-> HotUpdate
```

- `MiniCore.Runtime`、`MiniCore.Protocol`、`MiniCore.Serialization`、`MiniCore.Network` 的 asmdef 为 `noEngineReferences: true`：不得使用 `UnityEngine`、`MonoBehaviour`、`UnityEditor` 或 Unity 特有 API。
- `MiniCore.Unity` 是 Unity 时间、日志、驱动、Mono/UI 契约等适配代码的位置。
- `MiniCore.HotUpdate` 承载业务入口、资源/UI 业务与网络 Handler；不要把业务写回 Runtime/Network。
- `Project.Bootstrap` 是稳定程序集：不能静态引用 HotUpdate 类型；加载 DLL 后反射一次调用 `MiniCore.HotUpdate.MiniCoreStartup.StartAsync()`。
- 当前不接入 Actor。未来若接入，作为可选独立程序集，不能突破 Runtime/Network/Protocol 边界。

## Global 规则

- 业务直接用 `Global`，没有 `Global.Com`。
- 临时持有：`Global.GetOrAdd<T>(owner)`，owner 销毁时 `Global.Remove<T>(owner)` 或 `Global.ReleaseAll(owner)`。
- 成组生命周期：`using GlobalScope scope = Global.CreateScope("Name")`；Scope 释放时归还其全部引用。
- 常驻基础设施：`Global.Pin<T>()`；卸载时 `Global.Unpin<T>()`。
- 只有退出、切服等最高层中断可用 `Global.ForceRemove<T>()`。
- 每个 owner 获取一次，就必须释放一次；不要用静态字段或隐式单例绕开引用计数。
- Unity 每帧由 `UnityGlobalDriver.Update -> Global.Tick()` 驱动。不得在此链路每帧 new Context 或分配临时集合。

## 网络与协议规则

- Proto 根目录是 `Proto/`；业务文件按领域组织，不按 ClientToServer/ServerToClient 拆分。
- 标记只允许写成 `//[INormalMessage]`、`//[IRpcRequest]`、`//[IRpcResponse]`。
- RPC Response 必须拥有 `int32 code = 1;` 与 `string msg = 2;`。
- `RpcId` 位于网络 12 字节包头，不是 Proto 字段；生成 partial 的 `RpcId` 是运行时关联属性，不会被 Protobuf Body 序列化。
- Protobuf 是正式默认序列化方式；`NewtonsoftJsonSerializer` 保留用于迁移与性能对比；`UnityJsonSerializer` 不是正式网络路径。
- 业务 Handler 放在 `Assets/Scripts/MiniCore/HotUpdate`，继承 `AMHandler<T>` 或 `ARpcHandler<TRequest,TResponse>`。
- Opcode 由**已编译的 HotUpdate Handler**反向绑定。没有 Handler 的消息没有运行时 Opcode，不能被网络层发送。
- RPC Handler 会为 Request 与 Response 登记 Opcode。已删除协议的编号保留在 `Proto/Manifest/OpcodeManifest.json`，绝不可重用或重排。
- 不手改 `Protocol/Generated`、`HotUpdate/Generated`、`OpcodeManifest.json`；通过生成器维护。

## 生成与构建规则

1. 修改 `.proto` 后执行 Unity 菜单 `MiniCore > Protocol > Generate All`。
2. 修改/新增/删除 HotUpdate Handler 后，等待脚本编译完成；Opcode 与 Handler 注册表自动同步，**没有 Opcode 手动菜单**。
3. 生成流程使用 `Proto/Tools/protoc-29.5` 中随仓库提交的 Windows x64、macOS x64、macOS arm64 工具。
4. 删除 Handler 时，Editor 先写安全空 Handler 表，使首轮编译不会被旧的直接 `new Handler()` 引用阻断；下一轮自动写入正确表。
5. 打包前必须让 Console 无 C# 编译错误；Proto、Opcode、HybridCLR、YooAsset 与 Dedicated Server 边界由构建校验器验证。

## 热更新与启动规则

- `UpdateMainWindow` 负责 YooAsset 初始化、版本/清单/下载、AOT 元数据加载、`MiniCore.HotUpdate.dll` 加载，并在最后调用 Entry。
- AOT 元数据先于 HotUpdate DLL 加载。不要把所有剥离 DLL 盲目打入包；以生成的 HybridCLR AOT 地址表为准。
- `MiniCore.HotUpdate.dll` 必须在 YooAsset 包中，地址为 Bootstrap 配置使用的 `HotUpdate`。
- `MiniCoreStartup` 根据 `Application.isBatchMode` 选择生成的 Client 或 Server 模块列表；`GameStartup` 负责项目业务启动，服务端端口参数为 `-serverPort`，默认 `20000`。
- Base 程序集不依赖具体业务类；Entry 的反射创建仅发生一次，Handler 运行时注册使用生成的直接构造，不扫描 AppDomain，不用字符串/`Activator` 创建 Handler。

## C# 与仓库操作规则

- 修改 C# 前阅读并遵守 `.codex/skills/csharp-performance-conventions/SKILL.md`。
- 所有新增或修改方法都要有中文、多行 XML 注释；公共类/接口/属性/事件也写中文注释。
- 被修改的类使用访问级别 region；Unity 对象字段放在 `UnityProperty` region。
- 新 C# 文件 UTF-8 无 BOM；保留已有文件编码。
- 热路径避免 LINQ、闭包、字符串拼接、临时数组/集合、重复委托与装箱；优先已有对象池和缓存。
- 搜索优先 `rg`。手工编辑使用 `apply_patch`。不要用破坏性 git 命令，不要回退用户已有的脏工作区改动。
- 改动实现后必须做与风险匹配的验证；涉及 Unity C# 编译时，至少在隔离副本进行 Unity batchmode 编译检查，再交付。

## 阅读地图

| 任务 | 先读代码/文档 |
| --- | --- |
| Global、组件生命周期、纯 C# 服务 | `Runtime/Core/Global`、[架构总览](Architecture.md#3-global-组件运行时) |
| 网络收发、RPC、传输 | `Network/Core`、`Network/Transport`、[网络与协议](NetworkLayerAnalysis.md) |
| 新协议与 Proto | `Proto/`、`Editor/Protocol/ProtoCodeGenerator.cs`、[网络与协议](NetworkLayerAnalysis.md#2-proto-与生成流程) |
| Opcode/Handler 生成 | `Editor/Opcode*.cs`、`Protocol/Generated/Registry`、`HotUpdate/Generated/Network` |
| Unity 生命周期与 UI 适配 | `Unity/Driver`、`Unity/Mono`、`Unity/UI` |
| 热更启动/打包 | `Project/Bootstrap/UpdateMainWindow.cs`、`HotUpdate/Entry`、`Editor/HybridCLR` |
| 性能测试 | `Assets/Tests/Editor`、[性能测试指南](PerformanceTestingGuide.md) |

## 禁止的“省事”做法

- 不新增 `App`/`Context` 链式容器来替代 `Global`。
- 不重新引入 `Global.Com`、`MiniCore.Client`、`MiniCore.Game.Server`、`UnityClientHost` 或 `UnityServerHost`。
- 不让协议对象自行保存/硬编码 Opcode。
- 不给没有 Handler 的消息自动分配 Opcode。
- 不在 Player/Base 程序集中静态引用 HotUpdate 业务类型。
- 不以反射扫描或 `Activator.CreateInstance` 替代 HotUpdate Handler 生成表。
- 不把 Proto、protobuf 工具、Client/Server 业务又放回旧的 `Assets/Scripts/MiniCore/Model`、`Core` 等迁移前目录。
