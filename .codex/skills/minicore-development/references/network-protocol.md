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

关键入口：`Network/Core/NetworkMessageComponent.cs`、`Network/Handler/AMHandler.cs`、`Network/Handler/ARpcHandler.cs`、`Protocol/Model/Opcode/OpcodeRegistry.cs`、`Editor/Protocol/ProtoCodeGenerator.cs`、`Editor/OpcodeRegistryGenerator.cs`。

- 新协议从 `Proto/` 开始；生成后不得手改 `Protocol/Generated/`。
- Opcode 由已编译的 HotUpdate Handler 反向绑定；不手改 `OpcodeManifest.json` 或 Handler 注册表。
- 标记、RPC 字段和生成时机以 `Docs/NetworkLayerAnalysis.md` 为准；回归入口见 `Docs/NetworkSmokeTesting.md` 与 `Assets/Tests/Editor`。
