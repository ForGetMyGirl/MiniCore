# 网络、协议与 Handler

读取本页处理协议演进、RPC、收发、心跳或 TCP/UDP/KCP。协议和网络均是纯 C# 程序集，业务 Handler 位于 HotUpdate。

```text
Proto/                            # .proto 源码与 OpcodeManifest
Assets/Scripts/MiniCore/
├── Protocol/                     # 消息角色、Opcode、Proto 生成代码
├── Serialization/                # Protobuf 默认序列化与 JSON 对比实现
├── Network/                      # NetworkService、Session、Handler 基类、传输
└── HotUpdate/Network/Handler/    # 业务 AMHandler / ARpcHandler
```

关键入口：`Network/Core/NetworkService.cs`、`Network/Protocol/NetworkProtocolBuilder.cs`、`Network/Protocol/NetworkProtocolRegistry.cs`、`Network/Handler/AMHandler.cs`、`Network/Handler/ARpcHandler.cs`、`Editor/Protocol/ProtoCodeGenerator.cs`、`Editor/Protocol/Opcode/OpcodeRegistryGenerator.cs`。

- 新协议从 `Proto/` 开始；只有 `//[INormalMessage]`、`//[IRpcRequest]`、`//[IRpcResponse]` 标记的消息进入网络注册。生成后不得手改 `Protocol/Generated/`。
- `ProtoCodeGenerator` 按消息完整类型名维护 `Proto/Manifest/OpcodeManifest.json`：Normal 使用 `[100001, 200001)`，RPC 使用 `[200001, uint.MaxValue)`。已登记类型的角色不能跨区间变化，已删除编号不复用。
- `MiniCore.Protocol` 只保存 PB、角色 partial 和无状态消息登记；消息角色契约、Opcode 到类型的不可变 Registry 与 Handler 基类属于 `MiniCore.Network`，Protobuf Parser 适配属于 `MiniCore.Serialization`。
- 每个 `NetworkService` 通过 `NetworkProtocolBuilder` 合并项目协议登记与 Handler 直接登记并原子提交。Handler 不分配 Opcode；无 Handler 的合法出站消息仍可发送。
- `OpcodeHandlerRegistryInvalidator` 监视 `MiniCoreHotUpdateAssemblySettings` 中全部已登记程序集，源码导入、删除或移入移出时先写安全空表，编译后再由 `OpcodeRegistryGenerator` 扫描并生成直接构造代码。
- 不手改 `OpcodeManifest.json`、`ProjectSettings/MiniCoreProtocolGeneratedFiles.json` 或 Handler 注册表。所有权清单只允许清理已登记旧输出根目录中的固定生成文件，并验证生成标记。
- 标记、RPC 字段和生成时机以 `Docs/NetworkLayerAnalysis.md` 为准；回归入口见 `Docs/NetworkSmokeTesting.md` 与 `Assets/Tests/Editor`。
