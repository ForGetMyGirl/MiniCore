# 网络冒烟测试

`NetworkSmokeTestRunner` 使用真实的 `NetworkService`、生成的 HotUpdate Handler、Protobuf 以及 TCP/KCP/UDP 传输，顺序验证本机回环的连接探测、`SendAsync` 普通消息、16 条 `TrySend` 高速普通消息、RPC 回显和业务断开。

## Editor

1. 等待 Unity 完成脚本编译，确认 Console 没有 C# 编译错误。
2. 打开 `Window > General > Test Runner`，切到 `EditMode`。
3. 搜索并运行 `NetworkLoopbackIntegrationTests`。
4. 通过时，Console 最后一条为 `NETWORK_SMOKE: PASS (TCP / KCP / UDP)`。

测试会要求每种传输的 16 条 `TrySend` 均返回 `Accepted`，且均到达真实业务 Handler；因此它覆盖了出站数据队列接收、单会话串行发送与入站处理。它不刻意把队列填满，不验证拥堵断线阈值；该项由 Android 压测中的 `RejectedCount`、队列峰值和断连数验证。

测试使用本机端口 `25001`、`25002`、`25003`。运行前请确认它们未被其他程序占用；每个协议阶段最长等待 5 秒，整条 EditMode 用例最长 30 秒。

## Player

1. 按[打包与热更新流程](BuildAndHotUpdateWorkflow.md)执行 HybridCLR 产物准备与 `MiniCore > Build > Prepare DefaultPackage`，随后构建普通客户端 Player。
2. 给 Player 追加 `-networkSmokeTest` 启动参数。它会在 HotUpdate DLL 加载、`MiniCoreStartup` 注册 Handler 且 `GameStartup` 完成后自动运行。
3. Player 左上角与日志会显示阶段状态；通过时输出 `NETWORK_SMOKE: PASS (TCP / KCP / UDP)`，失败时输出 `NETWORK_SMOKE: FAIL`，并包含 `protocol`、`stage`、`sessionId` 和错误摘要。

在 macOS 上，可从终端运行：

```text
/path/to/MiniCore.app/Contents/MacOS/MiniCore -networkSmokeTest
```

如需以退出码给自动化系统判定结果，额外传入 `-networkSmokeQuit`；通过退出码为 `0`，失败为 `1`。

## Android 端到端性能压测

构建 Android Development Player 后，以 `-networkBenchmark` 启动即可运行 TCP、KCP、UDP 的本机回环性能压测；它与冒烟测试共用真实 `NetworkService`、生成 Handler 和 Protobuf，但不会创建交互测试面板。若用于设备农场或命令行自动化，再追加 `-networkBenchmarkQuit`，通过时退出码为 `0`，失败为 `1`。

每种传输会运行正常消息 `100`、`1,000`、`5,000` 条/秒三个档位（每档 `10` 秒预热、`60` 秒正式测量、重复三次）、`64` 并发 RPC（`60` 秒、重复三次）和一次 `2` 秒主线程停顿诊断。RPC 每次最多补发 `8` 个请求，且至少等待 `1 ms` 再补下一次；每次补发前读取可靠出站队列，达到 `16` 包高水位就暂停，始终为心跳和响应保留至少一半的 `32` 槽容量。这样既避免自定义 `MTask.Yield()` 在同一轮执行器 Drain 中被立即再次调度，也能维持 `64` 并发目标。压测开始时关闭普通与 Payload 日志，避免 UI 面板与手工日志影响样本；结果会导出 JSON/CSV 到 `Application.persistentDataPath/NetworkBenchmark/<UTC 时间戳>/`。

不要在每次修改后都运行完整 `39` 条样本。三个启动范围如下：

| 目的 | Unity 参数（均需同时传入 `-networkBenchmark`） | 预计结果数 | 适用时机 |
| --- | --- | ---: | --- |
| RPC 快速回归 | `-networkBenchmarkRpcQuick` | `3` | 改动 RPC 发起节奏、可靠出站队列或 RPC 超时处理后；三种传输各 1 轮、15 秒。 |
| RPC 专项基线 | `-networkBenchmarkRpcOnly` | `9` | RPC 快速回归通过后，需要三种传输各 3 轮、60 秒的可比较 RPC 数据时。 |
| 完整基线 | 无额外范围参数 | `39` | 发布前，或改动共享网络核心、Protobuf 封包、正式入站/出站队列、传输实现时。 |

报告包含设备/系统/Unity/构建类型、发送/接收/失败/未收数量、吞吐、P50/P95/P99、队列消息与字节积压峰值、入站队列拒绝数、单包主线程处理峰值、`GC Allocated In Frame` 峰值、断连数及停顿恢复耗时。RPC 样本只要出现失败、丢失或入站拒绝，仍会写出完整报告，但最终状态为 `NETWORK_BENCHMARK: FAIL`，不能进入基线。每次运行后应将目录从设备取回，并将三次重复样本的中位数补入性能测试指南；它是决定是否引入有界入站队列和主线程处理预算的依据，不替代真实服务端和公网链路压测。

## 范围

该测试验证客户端 HotUpdate 启动后的本机回环。Dedicated Server 仍需通过独立 Server Player 与 Client Player 的 KCP 测试覆盖。

高频普通消息压测使用 `TrySend`，报告必须区分尝试投递、成功入队、队列拒绝和 Handler 已接收。流程通过仅表示链路功能完成，不代表达到目标吞吐；TCP/UDP/KCP 的吞吐、P99、队列峰值与 GC 必须结合报告判读。

## Dedicated Server + Client KCP

该验证使用同一份 Player 的两个独立进程：`-batchmode` 进程由 `MiniCoreStartup` 启动 Server 模块列表，普通进程启动 Client 模块列表。macOS 请直接执行 `.app/Contents/MacOS/MiniCore`，不要双击同一个 `.app`，否则系统会激活已有实例。

1. 按[打包与热更新流程](BuildAndHotUpdateWorkflow.md)执行 HybridCLR 产物准备与 `MiniCore > Build > Prepare DefaultPackage`，然后构建 Player。
2. 在终端 A 启动服务端，并等待 READY 日志：

```text
/path/to/MiniCore.app/Contents/MacOS/MiniCore -batchmode -nographics -dedicatedServerSmokeTest -serverPort 20000 -logFile -
```

3. 在终端 B 启动客户端自检：

```text
/path/to/MiniCore.app/Contents/MacOS/MiniCore -dedicatedClientSmokeTest -dedicatedClientSmokeQuit -serverHost 127.0.0.1 -serverPort 20000 -logFile -
```

通过时 Client 输出 `DEDICATED_CLIENT_SMOKE: PASS protocol:KCP` 并以退出码 `0` 结束；Server 输出 READY，且事件日志包含 `[dedicated-smoke] normal`、`[dedicated-smoke] rpc` 与 `[dedicated-smoke] close`。结束服务端时在终端 A 按 `Ctrl+C`。

## 退出与清理检查

运行期正常关闭网络服务时，`OnDisposing()` 应先关闭监听器、Socket 与会话，随后由 MTask 取消并等待 finally 退场；这是检测资源没有遗留的路径。关闭 Player、按 `Ctrl+C` 结束 Dedicated Server 或停止 Editor Play Mode 则采用 MTask 快速退出：主线程不会等待网络专用线程 Join，也不保证未完成收发全部执行完。

Development/Editor 退出日志可能记录当时仍在退场的任务和计时器数量；这是非阻塞退出的诊断快照，不会单独造成运行期泄露。若同一网络流程在正常服务释放后仍持续出现活动任务/计时器，才应检查 Socket 是否在 `OnDisposing()` 关闭、任务是否遗漏 `.Forget()` 监督或外部回调是否未解绑。
