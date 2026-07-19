# 网络冒烟测试

`NetworkSmokeTestRunner` 使用真实的 `NetworkService`、生成的 HotUpdate Handler、Protobuf 以及 TCP/KCP/UDP 传输，顺序验证本机回环的连接探测、普通消息、RPC 回显和业务断开。

## Editor

1. 等待 Unity 完成脚本编译，确认 Console 没有 C# 编译错误。
2. 打开 `Window > General > Test Runner`，切到 `EditMode`。
3. 搜索并运行 `NetworkLoopbackIntegrationTests`。
4. 通过时，Console 最后一条为 `NETWORK_SMOKE: PASS (TCP / KCP / UDP)`。

测试使用本机端口 `25001`、`25002`、`25003`。运行前请确认它们未被其他程序占用；每个协议阶段最长等待 5 秒，整条 EditMode 用例最长 30 秒。

## Player

1. 先执行 HybridCLR 热更新 DLL 编译，再执行 `MiniCore > Build > Prepare DefaultPackage`，随后构建普通客户端 Player。
2. 给 Player 追加 `-networkSmokeTest` 启动参数。它会在 HotUpdate DLL 加载、`MiniCoreStartup` 注册 Handler 且 `GameStartup` 完成后自动运行。
3. Player 左上角与日志会显示阶段状态；通过时输出 `NETWORK_SMOKE: PASS (TCP / KCP / UDP)`，失败时输出 `NETWORK_SMOKE: FAIL`，并包含 `protocol`、`stage`、`sessionId` 和错误摘要。

在 macOS 上，可从终端运行：

```text
/path/to/MiniCore.app/Contents/MacOS/MiniCore -networkSmokeTest
```

如需以退出码给自动化系统判定结果，额外传入 `-networkSmokeQuit`；通过退出码为 `0`，失败为 `1`。

## 范围

该测试验证客户端 HotUpdate 启动后的本机回环。Dedicated Server 仍需通过独立 Server Player 与 Client Player 的 KCP 测试覆盖。

## Dedicated Server + Client KCP

该验证使用同一份 Player 的两个独立进程：`-batchmode` 进程由 `MiniCoreStartup` 启动 Server 模块列表，普通进程启动 Client 模块列表。macOS 请直接执行 `.app/Contents/MacOS/MiniCore`，不要双击同一个 `.app`，否则系统会激活已有实例。

1. 先执行 HybridCLR DLL 编译与 `MiniCore > Build > Prepare DefaultPackage`，然后构建 Player。
2. 在终端 A 启动服务端，并等待 READY 日志：

```text
/path/to/MiniCore.app/Contents/MacOS/MiniCore -batchmode -nographics -dedicatedServerSmokeTest -serverPort 20000 -logFile -
```

3. 在终端 B 启动客户端自检：

```text
/path/to/MiniCore.app/Contents/MacOS/MiniCore -dedicatedClientSmokeTest -dedicatedClientSmokeQuit -serverHost 127.0.0.1 -serverPort 20000 -logFile -
```

通过时 Client 输出 `DEDICATED_CLIENT_SMOKE: PASS protocol:KCP` 并以退出码 `0` 结束；Server 输出 READY，且事件日志包含 `[dedicated-smoke] normal`、`[dedicated-smoke] rpc` 与 `[dedicated-smoke] close`。结束服务端时在终端 A 按 `Ctrl+C`。
