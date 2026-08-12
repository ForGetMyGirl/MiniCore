# MiniCore 第一次性能测试指南

这份指南面向第一次使用 Unity Performance Testing 的开发者。第一项测试已经放在 `Assets/Tests/Editor/NetworkHandlerDispatchPerformanceTests.cs`，它的名字是 `NormalHandlerInvoker_DispatchesMessage_WithoutReflection`。

## 第一次测什么

测试的是“一个普通消息已经反序列化完成后，网络框架把它交给对应 `AMHandler<TMessage>` 执行”的成本。

测试链路是：

```text
INetworkMessageHandlerInvoker.HandleAsync
    -> AMHandler<TMessage> 的类型检查
    -> 具体 BenchmarkMessageHandler.HandleAsync
```

它**不**测 TCP、UDP、KCP、网络线程队列、JSON 或 Protobuf。这样第一份数据只回答一个问题：当前无反射 Handler 派发本身花多少时间、会不会产生 GC。

## 测试参数

| 参数 | 当前值 | 含义 |
| --- | ---: | --- |
| 预热组数 | 5 | 让运行环境进入稳定状态，这些数据不写进结果。 |
| 正式测量组数 | 20 | 结果报告会保留 20 组样本，用于查看均值和波动。 |
| 每组派发次数 | 1,000,000 | 一次计时连续调用 1,000,000 次，降低极短方法的计时误差。 |
| 测量项 | 时间、GC 事件数 | 时间是主要指标；GC 应尽量保持为 0。 |

一次运行至少会真实调用 `25,000,000` 次 Handler：`5 x 1,000,000` 次预热，加 `20 x 1,000,000` 次正式测量。

## 在 Unity 中运行

1. 打开项目，等待右下角的脚本编译完成，确保 Console 没有红色编译错误。
2. 在顶部菜单打开 `Window > General > Test Runner`。
3. 切换到 `EditMode` 标签。
4. 搜索 `NormalHandlerInvoker_DispatchesMessage_WithoutReflection`。
5. 选中这一个测试，点击 `Run Selected`。第一次不要点击 `Run All`，避免其他功能测试混入结果。
6. 测试通过后，打开 `Window > General > Performance Test Report` 查看图表和统计值。
7. 点击报告窗口的 `Export CSV`，将本次结果以日期命名后保存到项目外的个人性能记录目录，例如 `2026-07-14-handler-baseline.csv`。

若菜单项没有立刻出现，先确认 Package Manager 中 `Performance Test Framework` 已安装，随后等待 Unity 完成一次脚本重编译。

## 看什么数据

优先记录报告中 `Network.NormalHandlerDispatch` 这一组：

| 要记录的值 | 为什么看它 |
| --- | --- |
| Median 或 Mean | 这是每 1,000,000 次派发的总耗时，作为之后改动前后的主要对比值；需要单次成本时再除以 1,000,000。 |
| Min / Max | 判断是否有明显的偶发抖动。 |
| Standard Deviation 或波动比例 | 波动较小才说明基线稳定；超过约 5% 时应在关闭其他程序后重跑。 |
| `Network.NormalHandlerDispatch.GC()` | 反映测量期间出现的 `GC.Alloc` 事件数；这条极短热路径的目标是 0。 |

不要把这项微基准的毫秒数直接当作“整个网络延迟”。真实网络延迟还包括收包、队列等待、反序列化、业务逻辑和渲染帧调度；它们会在后续的第二、第三项测试分别测量。

## 记录首次结果

完成后把报告中看到的数字填入下表，并将导出的 CSV 一起保留。以后每次优化都在相同机器、尽量相同 Unity 版本和相同测试参数下重跑，再对照这里的基线。

| 日期 | Unity 版本 | 机器与运行状态 | Median / Mean | Min / Max | 波动 | GC 事件 | CSV 文件名 | 备注 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 2026-07-14 | 2021.3.45f2c1 | Mac 编辑器，EditMode | `42.01 ms / 1,000,000 次` | 三次中位数 `41.33 ~ 42.05 ms` | 约 `1.7%` | 无每消息稳定 GC | 用户导出的三份 CSV | Handler 正式基线 |

## 自动保存与历史保存

Performance Testing 包每次运行结束后会自动覆盖写入两个“最近一次结果”文件：

```text
~/Library/Application Support/wangnamo/MiniCore/TestResults.xml
~/Library/Application Support/wangnamo/MiniCore/PerformanceTestResults.json
```

因此，Performance Testing 包本身只会保留最近一次结果，不会自动保留“第一次、第二次、第 N 次”的历史。MiniCore 已通过下方的自动归档工具补齐这一能力。原始性能结果通常不提交 Git；Git 中保留这份流程文档、测试代码以及关键结论即可。

## 自动归档与历史对比

项目已添加自动归档工具。每次 `[Performance]` 测试运行结束后，工具会等待 Performance Testing 包写完“最近一次结果”，然后自动创建一个时间戳目录：

```text
BenchmarkPerformance/History/20260714_224500_123/
  PerformanceTestResults.json
  PerformanceTestResults.csv
  TestResults.xml
```

不再需要为了保留历史而手动点击 Performance Test Report 的 `Export`。根目录中已有的手动 CSV 不会被覆盖；自动归档的 `History` 目录已加入 `.gitignore`，避免大量机器相关的原始数据进入 Git。

在 Unity 菜单打开 `MiniCore > Performance > History`，窗口会列出自动归档记录。可按“全部、今天、最近 7 天、最近 30 天”和测试方法名称筛选，并按时间正序或倒序查看。对两条记录分别点击“设为基准”和“设为对比”，下方会显示同名测试、同名 Sample Group 的中位数变化百分比。时间类指标中，正百分比表示候选记录更慢；GC 的基准值为零时不计算百分比，直接比较数值即可。

历史窗口支持“删除此记录”“删除当前筛选结果”和“清空全部历史”，每种删除都需要二次确认。它们只删除 `BenchmarkPerformance/History` 内的自动归档目录，不会删除 `BenchmarkPerformance` 根目录的手动 CSV 文件。

## MTask 稳态分配与执行器基线

MTask 的功能回归测试位于 `Assets/Tests/Editor/MTaskTests.cs`，覆盖单次消费、共享等待、父子取消、`.Forget()` 监督、组件两阶段释放、执行器切换及快速退出。它验证语义，不替代 Unity Profiler 的分配验收。

新增或修改 MTask、执行器、Owner 注入或网络线程交接时，应在预热后分别测量 `CompletedTask`、同步 `await`、`Yield`、`Delay`、`MTaskCompletionSource`、父子挂接、主线程投递和 Unity → 专用执行器 → Unity 往返。成功路径的目标是 `0 B GC Alloc`；跨线程场景必须同时查看创建线程与执行器线程，不能把分配隐藏到后台线程。

首次遇到某个 async 状态机类型、对象池扩容、异常、`.Share()` 共享状态，以及 BCL/第三方内部 `Task` 的分配必须单独记录，不可混入稳态成功路径。压测、场景反复进入退出和网络重连后，使用 `MTaskDiagnostics.Capture()` 对比 `ActiveNodes`、`ActiveTimers`、池命中、扩容和回收失败计数是否回到基线。应用快速退出只记录快照而不等待收敛；泄露判断应以运行期正常 Owner 释放后的稳定计数为准。

### 2026-08-05 独占执行器计时器唤醒回归

MiniBomber 本机回环测试中，准备、取消准备、房间设置和离开房间等 RPC 多次出现约 `979~1,018 ms` 的额外等待，也存在约 `13 ms` 的正常样本。服务端 Handler 没有等待战斗 Tick，KCP 更新间隔为 `10 ms`；进一步定位到 `MSingleThreadExecutor`：到期计时器被转移到就绪队列后，事件循环仍按“没有下一枚计时器”的旧兜底值等待 `1,000 ms`，到期续体只能在下一轮执行。

修复后，事件循环用三种返回值表达状态：已有到期续体时返回 `0` 并立即继续；存在未来计时器时返回距最近到期时间的正毫秒数；真正空闲时返回 `Timeout.Infinite` 并等待 `Post`、`Schedule` 或 `Dispose` 信号。该方案不增加轮询、线程或热路径分配，并保留单执行器内的顺序执行语义。

回归用例新增到 `MTaskTests`，分别覆盖“独占执行器短延迟不得落入空闲等待周期”“无限空闲仍可被新续体唤醒”和“独占线程释放自身后不再访问已释放的等待句柄”。`MiniCore.Runtime` 的 Player/Editor 编译、Player/Editor 的 Network、Unity、HotUpdate 依赖程序集以及 `MiniCore.EditorTests` 均编译通过。另用本次编译出的同一份 `MiniCore.Runtime.dll` 直接执行调度冒烟测试：两次运行中，请求 `10 ms` 的底层计时器在 `19~20 ms` 完成，经过 `MTask.SwitchTo → MTask.Delay(10) → SwitchTo` 的完整路径在 `27~28 ms` 完成，进入无限空闲后的新续体在记录精度内 `0 ms` 唤醒，自释放路径正常退出；计时项均低于回归上限 `500 ms`。完整 EditMode 套件仍应在 Unity Test Runner 中作为合入前例行检查运行。

## 第二项测试：Protobuf 正式路径与 JSON 对比基线

`NetworkService` 在未显式设置 serializer 时默认使用 `ProtobufSerializer`，这是当前正式网络路径。`NewtonsoftJsonSerializer` 仅保留为迁移和性能对比实现；JSON 测试不代表当前客户端或 Dedicated Server 的默认配置。

先运行 Protobuf 序列化基准 `Assets/Tests/Editor/ProtobufSerializationPerformanceTests.cs`：

| 测试名 | Sample Group | 测量内容 |
| --- | --- | --- |
| `ProtobufSerializer_SerializesMessage` | `Network.Protobuf.Serialize` | 将与 JSON 基准完全相同的 `TestNetworkData` 编码为 Protobuf 字节的时间与 GC。 |
| `ProtobufSerializer_DeserializesMessage` | `Network.Protobuf.Deserialize` | 将同一份 `TestNetworkData` 的 Protobuf 字节通过生成 Parser Registry 还原的时间与 GC。 |

保留的 JSON 对比测试在 `Assets/Tests/Editor/NetworkJsonSerializationPerformanceTests.cs`：

| 测试名 | 测量问题 | 固定输入 |
| --- | --- | --- |
| `NewtonsoftJsonSerializer_SerializesMediumProtocolMessage` | 发送侧将协议对象编码为 UTF-8 JSON 字节需要多少时间、是否产生 GC。 | 一个携带固定 `Id` 和中等长度文本的 `TestNetworkData`，与 Protobuf 基准相同。 |
| `NewtonsoftJsonSerializer_DeserializesMediumProtocolMessage` | 收包侧将固定 JSON 字节还原为协议对象需要多少时间、是否产生 GC。 | 与发送侧相同协议生成的 JSON 字节。 |

在 Test Runner 的 `EditMode` 中分别运行 Protobuf 和 JSON 测试，每条各运行三次并导出 CSV。每组包含 `10,000` 次操作，报告中的时间是每 `10,000` 次的总耗时；计算单次成本时除以 `10,000`。它们都不包含 Socket、协议包封装、跨线程队列和 Handler 派发。先运行 `NetworkSerializationComparisonTests.cs`，它会验证两条基准的字段值完全一致，并在 Test Runner 输出 JSON 与 Protobuf 的编码字节数；这两项是网络传输成本对比的一部分。

### 2026-07-27 基线结果

测试环境为 Mac 编辑器、Unity `2021.3.45f2c1`、`EditMode`；JSON 与 Protobuf 均使用同一个 `TestNetworkData` 固定输入。每个性能测试使用 `5` 组预热、`20` 组正式测量，每个测量组连续执行 `10,000` 次。下表的“三次原始中位数”是三次独立运行中 Performance Test Report 输出的中位数，`GC()` 为报告中的 GC 事件指标。

| 路径 | 三次原始中位数（ms / 10,000 次） | 三次 `GC()` | 汇总中位数（ms / 10,000 次） | 单次换算 | 汇总 `GC()` |
| --- | --- | --- | ---: | ---: | ---: |
| JSON Serialize | `25.739 / 25.760 / 26.006` | `12.000500 / 12.000500 / 12.000500` | `25.760` | `2.5760 µs` | `12.000500` |
| JSON Deserialize | `31.035 / 31.664 / 31.021` | `9.000500 / 9.000500 / 9.000500` | `31.035` | `3.1035 µs` | `9.000500` |
| Protobuf Serialize | `9.044 / 9.111 / 9.088` | `2.000500 / 2.000500 / 2.000500` | `9.088` | `0.9088 µs` | `2.000500` |
| Protobuf Deserialize | `6.908 / 6.914 / 6.922` | `3.000500 / 3.000500 / 3.000500` | `6.914` | `0.6914 µs` | `3.000500` |

同一固定输入的网络正文长度为 JSON `179 B`、Protobuf `161 B`，Protobuf 少 `18 B`（约 `10.1%`）。因此，在这个固定 `TestNetworkData` 的编解码微基准中，Protobuf 序列化约为 JSON 的 `2.83` 倍快，反序列化约为 `4.49` 倍快，并且两条路径的 `GC()` 指标均更低。该结论适用于当前序列化器、生成的 Parser Registry 和这份固定中等载荷；它不代表端到端网络延迟，也不包含 Socket、协议包封装、跨线程队列、Handler 派发、日志或业务逻辑。完整入站包、RPC 和真实传输场景仍应按后续基准分别验证。

Protobuf 基线稳定后，第三项先测试网络线程到主线程的收包队列交接；随后分别运行 JSON 对比链路和 Protobuf 正式链路的完整入站包/RPC 基准。每次只新增一段链路，才能准确判断性能变化来自哪里。

### 正式 Protobuf 链路的待归档基线

在同一台 Mac 的 `EditMode` 下，依次单独运行三次 `ProtobufInboundPacketPerformanceTests`、`ProtobufRpcPerformanceTests`、`NetworkIncomingQueuePerformanceTests` 与 `NetworkLoopbackIntegrationTests`。前两项分别记录 `Network.Protobuf.InboundPacket` 和 `Network.Protobuf.Rpc` 的三次原始中位数、汇总中位数和 `GC()`；队列测试同时记录完整交接、仅复制、仅队列、仅 `BlockCopy`、仅缓冲池五个 Sample Group。`NetworkSerializationComparisonTests` 输出的 JSON/Protobuf 正文大小，以及完整入站包和 RPC 测试构造出的固定输入包长度，也要随 CSV 一起写入性能记录。

`ByteBufferPoolTests` 与 `NetworkLoopbackIntegrationTests` 是进入 Android 压测前的功能前置条件：前者必须验证同尺寸复用、异常/清理与并发租还均通过；后者必须输出 `NETWORK_SMOKE: PASS (TCP / KCP / UDP)`。这些运行结果尚未在本文档中伪造为基线，完成实际 Unity Test Runner 运行后再填入具体数值。

### Android 端到端网络压测

Android Development Player 可传入 `-networkBenchmark` 启动完整本机回环压测；额外加入 `-networkBenchmarkQuit` 时，流程结束会以通过或失败退出码关闭 Player。该模式不会创建测试面板，只会启动 `NetworkBenchmarkRunner`，并自动关闭普通日志与 Payload 日志以外的 UI 干扰。

每种 TCP、KCP、UDP 传输均依次运行正常消息 `100`、`1,000`、`5,000` 条/秒三档负载：每档预热 `10` 秒、正式测量 `60` 秒、重复 `3` 次；随后运行 `64` 并发、`60` 秒、重复 `3` 次的 RPC 饱和样本，以及一次 `1,000` 条/秒负载后的主线程 `2` 秒停顿诊断。正常消息采用固定宽度序号与固定长度正文，仍通过真实 Protobuf、真实 Handler 与本机传输处理。

报告写入 `Application.persistentDataPath/NetworkBenchmark/<UTC 时间戳>/`，包含 `NetworkBenchmarkReport.json` 和 `NetworkBenchmarkReport.csv`。每条样本记录设备型号、系统、Unity 版本、构建类型、发送/接收/失败/未收消息数、吞吐、P50/P95/P99/最大端到端耗时、队列消息数与字节数峰值、单包主线程处理峰值、每帧 `GC Allocated In Frame` 峰值、断连数，以及主线程停顿后的恢复时间。只有中负载 P99 持续高于 `50 ms`、停止发送 `2` 秒后仍不能排空，或高负载下积压/内存持续增长时，才进入有界队列与主线程预算优化；在此之前不改动队列策略或 Handler 派发。

## 第三项测试：网络收包队列交接

测试文件是 `Assets/Tests/Editor/NetworkIncomingQueuePerformanceTests.cs`，完整交接测试名为 `IncomingQueue_TransfersMediumPackets_BetweenNetworkAndMainThread`。

它用固定的 512 B 业务包模拟现有 `NetworkService.EnqueueIncoming` 和 `ProcessQueueAsync` 中的内存路径：一个长驻后台线程从传输层输入复制到 `ByteBufferPool` 租用的数组、放入正式同预算的固定容量环形队列；测试主线程同时出队、再归还数组。队列容量为 `1024` 包或 `1 MiB`，与正式普通收包队列一致。测试不启动 TCP/UDP/KCP，不做 JSON 或 Protobuf 反序列化，也不调用业务 Handler；因此它只回答“固定队列与缓冲池的并发交接成本是多少、是否产生异常 GC”。

每个测量组连续处理 `10,000` 个业务包。报告中的 `Network.IncomingQueue.MediumPacket` 是这一整组的总耗时，换算单包成本时除以 `10,000`。在 Test Runner 的 `EditMode` 下单独运行它三次；每次完成后可在 `MiniCore > Performance > History` 中查看并保留记录。

若完整交接测试显示稳定 GC 分配，再运行下面的归因测试，而不要立刻替换网络组件：

| 测试名 | Sample Group | 用途 |
| --- | --- | --- |
| `ByteBufferPool_CopiesMediumPackets_WithoutQueue` | `Network.IncomingQueue.BufferCopyOnly.MediumPacket` | 只测缓冲池租用、复制、归还，排除队列实现。 |
| `FixedCapacityQueue_TransfersMediumPackets_ConcurrentlyWithoutBufferCopy` | `Network.IncomingQueue.FixedCapacityQueueOnly.MediumPacket` | 后台生产线程与主线程并发交接固定队列中的收包结构体，排除复制与缓冲池。 |
| `BufferBlockCopy_CopiesMediumPackets_WithoutPool` | `Network.IncomingQueue.BlockCopyOnly.MediumPacket` | 只测固定数组间的字节复制，排除缓冲池容器操作。 |
| `ByteBufferPool_RentsAndReturnsMediumPackets_WithoutCopy` | `Network.IncomingQueue.BufferPoolOnly.MediumPacket` | 只测缓冲池的租用和归还，排除字节复制。 |

全部测试均使用相同的 `10,000` 包、512 B 输入和测量参数。2026-07-15 的第一次归因结果显示：旧 `ConcurrentQueue` 测试为 `GC() = 5`，而包含 `ByteBufferPool.Rent/Return` 的测试为 `GC() = 10005`；因此当时优先怀疑缓冲池容器的入栈/出栈操作，而不是队列。

2026-07-15 的第二次归因结果显示：纯 `Buffer.BlockCopy` 为 `GC() = 5`，仅 `ByteBufferPool.Rent/Return` 为 `GC() = 10005`。这确认了分配来自旧版缓冲池的 `ConcurrentStack` 内部节点，而不是字节复制或收包队列。

当前 `ByteBufferPool` 已改为每桶内部加锁的 `byte[][]` 槽位栈，并设置单数组 1 MB、单桶 8 MB、全局 32 MB 的保留上限；正式收包队列也已替换为预分配、`lock` 保护的固定容量环形队列。下一次运行时，应重跑“仅租用/归还”、固定队列并发交接和完整交接测试：稳定样本的 `GC()` 目标是接近当前测试环境的基础值 `5`。首次预热、池扩容或工作负载超过保留上限时仍可能发生数组分配，这是容量不足的正常诊断信号。

缓冲池改造后，除已有四条性能测试外，还应在 EditMode 中运行 `ByteBufferPoolTests`。它验证同尺寸数组会被复用，并验证多个线程同时执行 `Rent/Return` 时不会返回无效数组或产生并发异常。

## 第四项测试：关闭日志时的收包字符串分配

`NetworkService.HandleIncoming` 当前会在调用 `LogSwitch.Info` 前格式化时间并构造插值字符串。`LogSwitch.EnableLog = false` 只能阻止日志输出，不能阻止已经发生的字符串创建。

测试文件 `NetworkIncomingLogPerformanceTests.cs` 使用相同的收包日志文本分别测量当前写法和候选优化写法：

| 测试名 | Sample Group | 测量内容 |
| --- | --- | --- |
| `IncomingLog_BuildsStrings_WhenLoggingDisabled` | `Network.IncomingLog.Disabled.Legacy` | 先创建时间与日志字符串，再进入已关闭的 `LogSwitch.Info`。 |
| `IncomingLog_SkipsStrings_WhenLoggingDisabled` | `Network.IncomingLog.Disabled.Guarded` | 先判断日志开关；关闭时不创建时间与日志字符串。 |

两条测试均在日志关闭状态下每组执行 `10,000` 次。2026-07-15 的最终确认结果如下：

| 路径 | 三次中位值 | GC 事件 |
| --- | --- | --- |
| `IncomingLog_BuildsStrings_WhenLoggingDisabled` | `22.116 / 22.168 / 22.318 ms` | 每次 `GC() = 10.0005` |
| `IncomingLog_SkipsStrings_WhenLoggingDisabled` | `0.034 / 0.036 / 0.036 ms` | 每次 `GC() = 0` |

因此已将网络收包、普通消息发送、RPC 请求和 RPC 响应的普通日志改为先判断开关再创建时间与字符串；日志开启时仍保留当前诊断信息。关闭日志时的收包日志热路径从旧路径中位数约 `22.168 ms / 10,000 次` 降至优化路径约 `0.036 ms / 10,000 次`，约减少 `99.84%`，且 `GC.Alloc` 采样归零。当前状态是框架阶段性能验证已确认，尚未做真实业务网络冒烟验证。

`LogSwitch` 在 Editor 和 Development Build 中默认开启，在正式非开发构建中默认关闭。Payload 日志始终需要同时开启 `EnableLog && EnablePayloadLog`，避免日志已关闭时仍把字节正文转换为 UTF-8 字符串。

## 第五项测试：完整入站普通业务包处理（JSON 对比）

`NetworkInboundPacketPerformanceTests` 使用项目现有 `TestNetworkData` 和 `NewtonsoftJsonSerializer`，在测量前生成固定的完整业务 packet。它保留为迁移对比基线，测量过程覆盖普通消息成功路径中的包头读取、opcode 查表、运行时类型 JSON 反序列化和无反射 Handler 派发。

测试名 `InboundPacket_ParsesDeserializesAndDispatchesNormalMessage` 的 Sample Group 为 `Network.InboundPacket.NormalMessage`。每组处理 `10,000` 个固定 512 B 级别的业务 packet；它不包含 Socket、跨线程队列、日志、真实业务 I/O 与 RPC 分支。

这项基线用于与 Protobuf 正式链路对比。其结果应与第二项 JSON 反序列化和第一项 Handler 派发结合阅读，而不是简单相加。

## 第六项测试：Protobuf 完整入站包与 RPC

以下测试覆盖当前正式协议路径，均在 `Assets/Tests/Editor`：

| 测试文件 | 测试名 | Sample Group | 覆盖范围 |
| --- | --- | --- | --- |
| `ProtobufInboundPacketPerformanceTests.cs` | `ProtobufInboundPacket_ParsesDeserializesAndDispatches` | `Network.Protobuf.InboundPacket` | 12 字节包头、Opcode、Protobuf 运行时类型反序列化、无反射普通 Handler 派发。 |
| `ProtobufRpcPerformanceTests.cs` | `ProtobufRpc_ParsesRequestAndBuildsResponsePacket` | `Network.Protobuf.Rpc` | RPC 请求包头读取、`RpcId` 运行时写入、响应 `Code/Msg` 填充、Protobuf 响应封包。 |

每组各处理 `10,000` 条固定输入。它们不包含真实 Socket、跨线程队列、日志和业务 I/O，目的是在可重复条件下比较框架编解码路径；端到端 TCP/UDP/KCP 冒烟仍需要单独场景验证。

详细的长期优化顺序见 [优化路线图](OptimizationRoadmap.md)。

## 2026-08-02 网络 GC 后期归因任务：正式基线峰值已归档，尚不直接优化

正式基线 `~/AndroidBenchmark/20260801_124241/` 的 `MaxGcAllocatedBytesPerFrame` 为单个 `60` 秒样本期间观察到的**最大** Unity 帧托管分配，不是平均值、累计分配量或网络模块专属计数。当前可重复峰值包括 TCP `1000/s` 的 `1.095–1.103 MB`、TCP `5000/s` 的 `1.630–1.654 MB`、TCP `64` 并发 RPC 的 `2.515–2.644 MB`、KCP `5000/s` 的 `1.770–1.786 MB` 和 UDP `5000/s` 的 `0.651–0.680 MB`。这些数值是后续归因输入，不是现阶段网络质量门禁，也不能推断每帧都发生同等分配。

已验证的稳定框架 GC 问题仍保持解决：旧 `ConcurrentStack<byte[]>` 缓冲池归还节点分配和日志关闭时的收包字符串分配均已消除。当前帧峰值还混合了压测生成的 Demo 消息/字符串、反序列化对象和 Protobuf 编码器等来源，尚不能归因于合包、队列或缓冲池。

该任务安排在 UI 主干完成后、真实多人房间协议的 Dedicated Server 多会话压测之前。届时先采集空载帧基线，再按“消息构造 → Protobuf 封包与 `TrySend` 入队 → TCP/UDP 合包 → 入站反序列化与 Handler”分段记录累计分配、平均/P95/P99/最大每帧分配和超阈值帧数；只有最大来源被数据确认后才优化并复跑专项与完整基线。

## 2026-08-01 完整 39 条回归：全部通过，冻结为正式性能基线

本轮先通过 Editor `NetworkLoopback_AllTransports_UseActualHandlersAndProtobuf`，随后在 Android Development Player 上运行无专项参数的完整 `39` 条本机回环。报告目录为 `~/AndroidBenchmark/20260801_124241/`，UTC 生成时间 `2026-08-01 12:42:41`，对应北京时间 `20:42:41`。全部 `39` 条均为零失败、零未收、零出站拒绝、零入站拒绝和零断线；该目录现冻结为当前网络实现的正式性能基线。

此前失败的九条 `1000/s` 这次全部通过：

| 传输 | 三轮端到端 P99 | 中位数 | 入站等待 P99 | 单包处理 P99 | 判定 |
| --- | ---: | ---: | ---: | ---: | --- |
| TCP | `39.315 / 39.278 / 39.038 ms` | `39.278 ms` | `37.250 / 37.250 / 37.250 ms` | `0.250 ms` | 通过。 |
| KCP | `48.611 / 48.784 / 48.600 ms` | `48.611 ms` | `40.000 / 40.000 / 40.000 ms` | `0.250 ms` | 通过。 |
| UDP | `38.907 / 38.720 / 38.661 ms` | `38.720 ms` | `36.250 / 36.000 / 36.000 ms` | `0.250 ms` | 通过。 |

主线程停顿恢复为 TCP `12.605 ms`、KCP `37.217 ms`、UDP `12.853 ms`，均低于 `50 ms`。TCP `5000/s` 每轮约 `300,000` 个完整帧仅发生约 `24,100–24,300` 次 Socket 接收，连续缓冲拆包仍正常；UDP `5000/s` 的 `300,000` 个逻辑包仅约 `50,500` 次实际 datagram 写入，约 `5.9` 包/datagram，`TrySend` 合包仍正常。KCP `5000/s` 的 P99 `57.875–61.012 ms` 不设固定 P99 门槛，且三轮均零可靠性异常。

本记录取代下方 `11:06:49` 完整回归的“当前失败候选”结论。它证明在当前带分段指标的构建和完整场景下，旧的共同 `70–85 ms` P99 **未被复现**；但此前与当前运行条件的差异尚未被单独控制，不能把一次通过倒推为某项网络代码修复了旧失败。保留入站等待和单包处理 P99 作为后续发布前完整回归的常规观测；只有未来完整回归再次失败时，才沿该边界补充帧级/序列号关联诊断。

## 2026-08-01 中负载分段诊断三轮：稳定通过，尾延迟落在入站主线程交接

在同一 Android Development 构建上，`-networkBenchmarkMediumNormalDiagnostic` 已连续运行三轮：`~/AndroidBenchmark/20260801_114351/`、`~/AndroidBenchmark/20260801_115133/`、`~/AndroidBenchmark/20260801_115843/`。对应报告 UTC 分别为 `11:43:51`、`11:51:33`、`11:58:43`，北京时间分别为 `19:43:51`、`19:51:33`、`19:58:43`。每一轮均为 TCP/KCP/UDP 各一条 `1000/s`；全部 `60000`（最后一轮 TCP 为 `60001`）尝试均成功接受并到达 Handler，零拒绝、零失败、零未收和零断线。

本记录以完整三轮结论取代下方“首轮、尚待复现稳定性”的临时状态；下方首轮保留为诊断过程证据。

| 传输 | 三轮端到端 P99 | 中位数 / 波动范围 | 三轮入站等待 P99 | 单包处理 P99 | 判定 |
| --- | ---: | ---: | ---: | ---: | --- |
| TCP | `38.048 / 38.812 / 38.628 ms` | `38.628 / 0.765 ms` | `36.250 / 37.000 / 36.750 ms` | `0.250–0.500 ms` | 稳定通过；尾延迟主要在入站等待。 |
| KCP | `47.808 / 48.072 / 48.139 ms` | `48.072 / 0.331 ms` | `38.500 / 39.000 / 39.750 ms` | `0.250 ms` | 稳定通过；尾延迟主要在入站等待。 |
| UDP | `37.161 / 37.701 / 37.802 ms` | `37.701 / 0.642 ms` | `34.500 / 35.750 / 35.750 ms` | `0.250 ms` | 稳定通过；尾延迟主要在入站等待。 |

这已确认最小三协议 `1000/s` 场景没有随机的 `70–85 ms` P99，也排除了单包 Protobuf、Handler 或事件发布本身造成此前共同尾延迟。网络 I/O 仍在网络执行器运行；这里的入站等待是网络线程完成入队后、按设计交给主线程执行 Handler 的帧调度边界。

不过它**不覆盖**完整 `39` 条回归：完整回归的九条 `1000/s` 失败仍是一个待复现的场景差异，不能据此宣称完整基线已修复，也不能直接调整队列、线程或 P99 门槛。下一步必须在当前含分段指标的构建上重跑完整 `39` 条；若失败时入站等待 P99 同步升高，则以同一边界继续增加帧级关联诊断；若完整回归通过，则再将此前失败归类为已记录但未复现的运行条件差异。

## 2026-08-01 中负载分段诊断首轮：入站到主线程交接主导尾延迟，尚待复现稳定性

报告目录为 `~/AndroidBenchmark/20260801_114351/`；UTC 生成时间 `2026-08-01 11:43:51`，对应北京时间 `19:43:51`。本轮使用 `-networkBenchmarkMediumNormalDiagnostic`，TCP/KCP/UDP 各运行一轮 `1000/s`，每轮均为 `60000 / 60000 / 0 / 60000`（尝试 / 接受 / 拒绝 / 接收），零失败、零未收、零入站拒绝、零断线，因此当前三条诊断样本均通过质量门槛。

| 传输 | 端到端 P50 / P95 / P99 | 入站等待 P50 / P95 / P99 | 单包处理 P50 / P95 / P99 | 结论 |
| --- | ---: | ---: | ---: | --- |
| TCP | `11.144 / 34.178 / 38.048 ms` | `9.250 / 32.750 / 36.250 ms` | `0.250 / 0.250 / 0.500 ms` | P99 几乎由网络入队到主线程开始处理的等待构成。 |
| KCP | `15.242 / 42.805 / 47.808 ms` | `9.250 / 34.750 / 38.500 ms` | `0.250 / 0.250 / 0.250 ms` | 主线程可见的入站交接是主要成分；剩余差值仍未细分。 |
| UDP | `11.108 / 33.700 / 37.161 ms` | `6.750 / 32.000 / 34.500 ms` | `0.250 / 0.250 / 0.250 ms` | P99 几乎由网络入队到主线程开始处理的等待构成。 |

这首次以数据排除了“单包 Protobuf 反序列化、Handler 或事件发布本身占据端到端尾延迟”的猜测，也没有证据表明 TCP 连续收包、UDP 合包或出站发送泵本身在该通过样本中退化。这里的入站等待是设计上网络线程交给主线程执行 Handler 的帧调度边界；它不等于网络 I/O 被主线程阻塞。

但这一轮只有每种传输一条通过样本，不能反推完整 `39` 条中九条 `1000/s` P99 失败已经消失，更不能称为稳定性结论。下一步保持同一构建和参数再运行两轮；若再次出现超标且入站等待 P99 同步升高，即确认原失败也发生在该交接边界；若两轮均通过，才将问题收敛为运行配置/时序相关波动，并在复现失败时补充主线程帧级和更细的关联诊断，仍不直接改网络实现。

## 2026-08-01 完整 39 条回归：可靠性通过，中负载共同 P99 未通过

本轮先在 Editor 运行 `NetworkLoopback_AllTransports_UseActualHandlersAndProtobuf`，TCP/KCP/UDP 的连接、`SendAsync`、16 条 `TrySend`、RPC 与断线均通过。随后 Android Development Player 运行不带专项参数的完整压测；报告目录为 `~/AndroidBenchmark/20260801_110649/`，UTC 生成时间 `2026-08-01 11:06:49`，对应北京时间 `2026-08-01 19:06:49`。报告包含完整 `39` 条样本，不是在第一个质量失败后中断。

全部样本均为零失败、零未收、零出站拒绝、零入站拒绝和零断线；三条主线程停顿恢复分别为 TCP `21.173 ms`、KCP `22.106 ms`、UDP `5.731 ms`，均低于 `50 ms`。TCP 连续接收也保持有效：`1000/s` 的约 `60,011` 个服务端完整帧只发生 `9,639` 次 Socket 接收；UDP `5000/s` 的约 `300,000` 个逻辑样本只发生约 `50,400` 次实际传输写入，说明 `TrySend` 数据报合包仍在生效。

但全部九条正常消息 `1000/s` 样本都超过中负载 P99 `50 ms` 门槛，故完整回归状态为失败：

| 传输 | 三轮 P99 | 可靠性 | 判定 |
| --- | --- | --- | --- |
| TCP | `69.970 / 70.054 / 69.775 ms` | 全部零失败/拒绝/未收/断线 | P99 失败。 |
| KCP | `85.193 / 85.218 / 85.430 ms` | 全部零失败/拒绝/未收/断线 | P99 失败。 |
| UDP | `80.856 / 80.480 / 80.983 ms` | 全部零失败/拒绝/未收/断线 | P99 失败。 |

该共同尾延迟尚未定位，不能归因于 TCP 连续收包、UDP 合包、队列容量、线程或 Handler。P50/P95/P99 来自同一组端到端样本：正常消息成功 `TrySend` 后记录同一开始 tick，真实 Handler 发布业务事件时记录同一结束 tick，由同一个预分配数组排序计算三个百分位；不存在“P99 用新发送方式而 P50 仍用旧方式”的分支。此前 TCP 与 UDP 专项通过仍是其各自配置下的真实结果，但不构成本轮完整基线通过证明。

下一步只增加压测期开启的入站等待与单包处理 P50/P95/P99，不改变发送、收包或线程模型。使用 `-networkBenchmarkMediumNormalDiagnostic` 运行 TCP/KCP/UDP 各一轮 `1000/s`（10 秒预热、60 秒测量），再以分段百分位决定是否需要补充更细的序列号边界诊断。

## 2026-08-01 UDP TrySend 数据报合包修复：5000/s 通过专项回归

报告目录为 `~/AndroidBenchmark/20260801_092609/`；报告 UTC 生成时间为 `2026-08-01 09:26:09`，对应北京时间 `2026-08-01 17:26:09`。本轮通过 `-networkBenchmarkUdpNormalQuick` 只运行 UDP 正常消息 `5000/s` 一轮，使用 Xiaomi 23116PN5BC、Android 16 API 36、Unity 2021.3.45f2 Development Player；此前已通过的 UDP `1000/s` 不重复运行。

失败报告 `~/AndroidBenchmark/20260801_085337/` 已定位的瓶颈是：同一会话的发送泵每个 `TrySend` 业务包都串行等待一次 UDP `Socket.SendToAsync`。该轮 `5000/s` 只接受并收到 `76003 / 299894`，出站队列拒绝 `223891`；然而单次传输写入平均仅 `0.617 ms`、入站拒绝为零，说明不是 Handler、入站队列或 UDP 收包导致的限制，而是每秒数千次独立 datagram 写入的串行成本。

修复只作用于 `TrySend` 的普通数据队列：发送泵从已存在的待发数据包中立即收集至多 `16` 个、总 datagram 不超过 `1200 B` 的业务包，写为内部 `MCUB` v1 批量 datagram；若当下只有一个包或单包过大，则仍原样发送。它**不会等待凑包**，因此低频消息不会因为合包被额外滞留。`SendAsync`、RPC、心跳和可靠控制队列始终保持单业务包单 datagram，并继续等待 OS 接受写入完成。接收端必须先校验整个批量 datagram，再按原业务包顺序交付；一个 UDP 批量 datagram 丢失时，所含的易失性 `TrySend` 数据会一并丢失，故该路径只适用于允许瞬时状态丢失的高频消息。

`MCUB` 是 UDP 传输层协议扩展，不能让旧接收端识别批量 datagram。升级时客户端与服务端必须同时采用这一版本；单包 raw datagram 仍兼容旧格式，但不能据此把混合版本通信视作完整兼容。

| 条目 | Offered / Accepted / Rejected / Received | 吞吐 | P50 / P95 / P99 | 实际 UDP 写入 | 判定 |
| --- | ---: | ---: | ---: | ---: | --- |
| UDP 普通 `5000/s` | `300000 / 300000 / 0 / 300000` | `4993.174/s` | `32.814 / 43.893 / 46.647 ms` | `50,705 / 300,012` | 通过；零失败、零未收、零入站拒绝、零断线。 |

`50,705` 次实际传输写入承载 `300,012` 个逻辑出站计时样本，平均约 `5.9` 个逻辑包/实际 datagram；这与吞吐恢复相互印证，不是误改了投递负载。TCP、KCP、RPC 未因本次 UDP 协议改动自动视为已经复测；当前只冻结 UDP 普通 `5000/s` 专项。发布前或共享网络路径继续变更后，仍需执行三协议 `NetworkLoopbackIntegrationTests`，并在需要长期正式基线时补同配置三轮中位数。

## 2026-08-01 TCP 连续收包拆包修复：5000/s 通过专项回归

报告目录为 `~/AndroidBenchmark/20260801_083926/`；报告 UTC 生成时间为 `2026-08-01 08:39:26`，对应北京时间 `2026-08-01 16:39:26`。本轮通过 `-networkBenchmarkTcpNormalQuick` 运行 TCP 正常消息 `1000/s`、`5000/s` 各一轮，使用 Xiaomi 23116PN5BC、Android 16 API 36、Unity 2021.3.45f2 Development Player。

前一份失败报告 `~/AndroidBenchmark/20260801_082139/` 已将问题定位到 TCP 接收循环，而不是主线程、入站队列或发送线程调度：`132,778` 个完整帧触发 `265,557` 次底层 Socket 接收，平均每帧约两次；每次平均等待 `0.217 ms`。旧实现对每个逻辑包串行等待长度头和正文，约 `0.434 ms/包` 的接收节奏与实测 `2140.175/s` 吞吐相符，并进一步使客户端 Socket 写入平均等待升至 `10.264 ms`。

修复后，`LengthPrefixedTcpTransportBase` 维持连续接收缓冲区：每次 Socket 接收后解析其中全部完整帧，仅将跨读取的大帧保留在缓冲区并按需扩容。它不改变四字节大端长度前缀、每帧回调顺序或业务正文缓冲的所有权。压测才启用 `ServerTransportReceiveOperation*` 和 `ClientSocketSendOperation*` 计数，用于以后区分 Socket I/O 与队列等待；正式收发不记录这些时间戳。

| 条目 | Offered / Accepted / Rejected / Received | 吞吐 | P99 | 服务端接收操作 | 判定 |
| --- | ---: | ---: | ---: | ---: | --- |
| TCP 普通 `1000/s` | `60000 / 60000 / 0 / 60000` | `999.766/s` | `38.098 ms` | `20,383 / 60,011` | 通过；满足中负载 P99 `<= 50 ms`。 |
| TCP 普通 `5000/s` | `300000 / 300000 / 0 / 300000` | `4995.950/s` | `38.159 ms` | `24,670 / 300,012` | 通过；零拒绝、零未收、零断线。 |

这轮推翻了先前“客户端单次 Socket 写入本身是 5000/s 的首要限制”的暂定判断：写入等待是服务端逐帧双接收造成 TCP 反压的结果。TCP `1000/s`、`5000/s` 已完成一次功能与性能专项回归；由于改动的是共享 TCP 传输实现，发布前仍需按 [网络冒烟测试](NetworkSmokeTesting.md) 执行三协议冒烟，并在需要正式长期基线时补同配置三轮中位数。UDP、KCP 与 RPC 不因本次 TCP 收包修复而自动视为已复测。

## 2026-07-31 TCP 专项回归：单帧合并达到 1000/s，逐包写出仍限制 5000/s

报告目录为 `~/AndroidBenchmark/20260730_161646/`；其 UTC 生成时间 `2026-07-30 16:16:46` 对应北京时间 `2026-07-31 00:16:46`。该轮通过 `-networkBenchmarkTcpNormalQuick` 只运行 TCP 正常消息 `1000/s`、`5000/s`，不重复 UDP、KCP、RPC 或主线程停顿。

| 条目 | Offered / Accepted / Rejected / Received | 吞吐 | P99 | 客户端出站等待 / 传输写入平均 | 判定 |
| --- | ---: | ---: | ---: | ---: | --- |
| TCP 普通 `1000/s` | `60000 / 60000 / 0 / 60000` | `999.273/s` | `61.652 ms` | `13.292 / 0.864 ms` | 可靠性与目标吞吐通过；P99 比 `50 ms` 中负载验收线高 `11.652 ms`，待优化后才可冻结。 |
| TCP 普通 `5000/s` | `300000 / 65678 / 234322 / 60318` | `972.769/s` | `8195.184 ms` | `928.021 / 0.912 ms` | 未通过；停止发送后仍有 `5360` 条处于已有积压的排空路径，非入站队列拒绝。 |

`5000/s` 的接受数由上一轮的 `42,371` 提升至 `65,678`，证明小包“长度头+正文”合并为一次写出有效。可是单次底层写入仍约 `0.9 ms`，一个会话逐包等待的自然上限约为 `1,000/s`；其 `928 ms` 的客户端出站等待与零入站拒绝进一步排除了主线程 Handler 或收包队列是此处主因。

下一版仅对 TCP 普通数据队列执行有界帧批量：单批不超过 `32` 包或 `32 KiB`，把多个已包含长度头的帧连续写入同一个池化缓冲并一次发送。可靠队列（`SendAsync`、RPC、心跳）仍逐包且优先，不改变公开的 `SendAsync` 完成语义；UDP/KCP 不实现该 TCP 字节流优化。批量失败时整批通知失败并归还全部缓冲，保持现有断线语义。先重跑同一条 TCP 专项快速回归；通过后才进行两档各三轮的正式基线。

新报告增加 `ClientTransportWriteCount`。它记录客户端实际提交到底层传输的写入次数：未批量时通常接近 `ClientOutboundTimingSampleCount`，TCP 普通消息批量生效后应明显更低，可直接验证吞吐提升是否来自减少写入操作而不是误改测试负载。

压测执行器现在也会将正常消息 `1000/s` 的 P99 高于 `50 ms` 记录为质量失败。此前该阈值只在文档中规定、没有进入自动 `PASS` 条件；此修正不改变网络行为，只防止以后把延迟超线的样本错误归为通过。

## 2026-07-28 Android 首次运行结论与校准

首次报告来自 Xiaomi 23116PN5BC、Android 16 API 36、Unity 2021.3.45f2 Development，共 `39` 条样本；TCP、KCP、UDP 均无失败、未收和断连，证明本机回环功能链路通过。原始 JSON/CSV 保存在开发机 `~/AndroidBenchmark/20260728_143449/`。

该版本的正常消息循环逐条 `await network.SendAsync(...)`。TCP/UDP 的等待包含底层异步 socket 写入，三个目标速率分别只约达到 `15` 与 `31` 条/秒；KCP 当时同步写入自身发送缓冲，才接近设定速率。因此这些 TCP/UDP 正常消息和停顿样本**不能**用作传输吞吐、入站队列容量或 Handler 性能结论。

后续基准改用 `TrySend` 固定速率投递，并记录 `OfferedCount`（尝试）、`SentCount`/接受数、`RejectedCount`（有界队列拒绝）和 `ReceivedCount`（Handler 已接收）。旧 `ConcurrentQueue` 微基准只覆盖预热后的特定模式，不能证明扩容路径不分配；正式收包队列必须以真正并发的固定队列压力测试验证稳定 GC。

## 2026-07-29 固定收包队列并发基线

在同一台 Mac 的 EditMode 中连续运行三次 `NetworkIncomingQueuePerformanceTests`。每个时间为处理 `10,000` 个 `512 B` 包的样本中位数；`GC()` 为 Performance Test Framework 采样计数。

| Sample Group | 三次中位值（ms） | 汇总中位值（ms） | 稳定 GC() |
| --- | ---: | ---: | ---: |
| `Network.IncomingQueue.FixedCapacityQueueOnly.MediumPacket` | `3.506 / 3.791 / 3.638` | `3.638` | `6 / 6 / 6` |
| `Network.IncomingQueue.BufferPoolOnly.MediumPacket` | `1.427 / 1.434 / 1.432` | `1.432` | `6 / 6 / 6` |
| `Network.IncomingQueue.MediumPacket` | `5.945 / 8.826 / 7.635` | `7.635` | `6 / 6 / 6` |

纯 `BlockCopy` 的三次中位值为 `0.161 / 0.154 / 0.158 ms`，可作为测试环境参考。第一次完整交接运行有一个 `GC() = 1037` 的单一样本，拉高了该次平均值，但其中位数仍为 `6`；后两次所有样本均稳定为 `6`。因此该单点视为首轮 Editor/性能框架预热扰动，不作为正式收包队列的稳定分配证据。

在当前测试条件下，固定队列的稳定 `GC()` 与纯复制、缓冲池测试的环境基线一致，且没有表现出运行期扩容分配。它证明的是此容量、包长和单生产者/单消费者交错模型下的稳定性；不等同于任意真实业务负载下永不分配，Android 端仍需用 `TrySend` 压测验证队列峰值与拒绝情况。

## 2026-07-29 Android 第二次运行：RPC 调度诊断，不计入基线

报告目录为开发机 `~/AndroidBenchmark/20260729_142339/`，Player 正常结束并导出 `39` 条样本。但该版本的 RPC 负载循环在每次补发后使用 `MTask.Yield()`；该 API 的语义是把续体重新放到当前执行器队列尾部，并不保证等到 Unity 下一帧。主线程执行器会在同一次 Drain 中继续取出该续体，因此 UDP 的 RPC 第 2、3 轮在约 `4.9 s` 内达到 `500,000` 条安全上限并全部失败，不能作为 RPC 吞吐或稳定性基线。

本轮采用的判定语义如下：目标速率完整达到、`RejectedCount/FailureCount/DroppedCount` 均为零且中压（`1,000` 条/秒）P99 不高于 `50 ms`，即为“通过并冻结”；高压 `5,000` 条/秒允许记录更高 P99，但不得拒绝或丢失。RPC 必须在预定测量时间内完成，且不能由压测执行器自身的调度方式制造失败；主线程停顿场景以停顿结束后的 `QueueRecoveryMilliseconds` 不高于 `50 ms` 为通过条件。

| 条目 | 结果 | 证据与结论 | 后续动作 |
| --- | --- | --- | --- |
| TCP 普通消息 `100/s`，三轮 | 通过并冻结 | 零拒绝、零失败、零丢失；P99 中位 `35.898 ms`。 | 不重测，除非改动 TCP/正式队列/序列化。 |
| KCP 普通消息 `100/1000/5000/s`，九轮 | 通过并冻结 | 三档均零拒绝、零失败、零丢失；`1000/s` P99 中位 `47.495 ms`；`5000/s` 达到约 `4,994/s`。高压 P99 中位 `58.536 ms` 作为基线记录。 | 不重测，除非改动 KCP/正式队列/序列化。 |
| KCP 主线程停顿 | 通过并冻结 | `2 s` 停顿后恢复 `15.895 ms`，全部 `2,999` 条已接收。 | 不重测，除非改动入站队列或主线程派发。 |
| UDP 普通消息 `100/s`，三轮 | 通过并冻结 | 零拒绝、零失败、零丢失；P99 中位 `36.339 ms`。 | 不重测，除非改动 UDP/正式队列/序列化。 |
| TCP 普通消息 `1000/5000/s` 与 TCP 主线程停顿 | 未通过 | 两档分别约 `58%/91%` 的 `TrySend` 被拒绝，P99 约 `2.6 s`；停顿恢复达到 `2 s` 超时上限。 | 先定位 TCP 出站发送器/传输写入吞吐，再只重测这三项。 |
| UDP 普通消息 `1000/5000/s` | 未通过 | 两档分别约 `13%/82%` 的 `TrySend` 被拒绝，P99 约 `1.2 s`。 | 先定位 UDP 出站发送器/传输写入吞吐，再只重测这两项。 |
| TCP/KCP/UDP RPC | 本轮无效，必须重测 | `MTask.Yield()` 在同一次 Drain 内恢复，错误制造 RPC 启动风暴；UDP 第 2、3 轮约 `4.9 s` 即达到 `500,000` 上限。 | 修正调度后先跑 RPC 快速回归，再跑 RPC 专项基线。 |
| TCP/UDP 主线程停顿（除 KCP 外） | 暂不判定 | 依赖未通过或无效的前置高频/RPC 状态；TCP 恢复超时，UDP 未能发送样本。 | 在对应普通消息或 RPC 问题修复后才重测。 |

后续版本将 RPC 补发改为 `await MTask.Delay(1)`，每次最多发起 `8` 个请求；每次补发前读取客户端可靠出站队列，达到 `16` 包高水位就等待。它通过执行器定时器至少等待一个调度间隔，既不在同一次 Drain 中制造任务风暴，又能保持 `64` 并发目标，并为心跳和服务端响应保留可靠队列容量。

日常修改不应重复运行完整 `39` 条 Android 样本。修改 RPC 或可靠出站队列后，先运行 `-networkBenchmark -networkBenchmarkRpcQuick`，它在三种传输上各运行一轮 `15` 秒 RPC，共导出 `3` 条结果；通过后如需正式 RPC 对比，再运行 `-networkBenchmark -networkBenchmarkRpcOnly` 导出 `9` 条结果。对于本轮 TCP/UDP 高频普通消息问题，修复后只运行对应传输和对应速率；默认完整 `39` 条只用于共享网络核心、序列化、正式入站/出站队列、传输实现变更后的版本基线归档。

已提供 `-networkBenchmark -networkBenchmarkRemainingNormalQuick`：仅运行 TCP/UDP 的 `1000/s`、`5000/s` 普通消息各一轮，并运行各自的主线程停顿诊断，共 `6` 条结果、约 `5` 分钟。它用于确认共享出站调度改动是否解决此前未通过条目；快速回归通过后，再仅对这 `6` 个场景执行三轮正式基线，不重跑 KCP、`100/s` 或已冻结 RPC。

仅改动 TCP 写出路径时，使用 `-networkBenchmark -networkBenchmarkTcpNormalQuick`。它只运行 TCP `1000/s`、`5000/s` 普通消息各一轮，共 `2` 条结果、约 `3` 分钟；不重复 UDP、KCP、RPC 和主线程停顿。该专项回归通过后，再执行相同两条 TCP 样本的三轮正式基线。

## 2026-07-29 RPC 快速回归：控制入站队列容量不足，不计入基线

报告目录为开发机 `~/AndroidBenchmark/20260729_145729/`，三种传输均走完流程并导出 `3` 条结果。但该版本把“流程未抛异常”显示为 `PASS`，并不等于 RPC 样本质量通过；报告中的 TCP 为 `278` 发起、`241` 成功、`37` 失败，KCP 为 `6,110` 发起、`6,047` 成功、`63` 失败，只有 UDP 为零失败。

TCP 与 KCP 的 `PeakQueuePacketCount` 都恰为旧的控制入站队列上限 `32`，分别出现约 `3.10 s` 与 `101 ms` 的 P99。结合 RPC 请求和响应在本机回环中共享同一个 `NetworkService` 的控制队列，这强烈指向 64 并发 RPC 的双向短时积压撞到了 32 槽上限；旧报告未记录控制队列拒绝数，因此仍须以修正后的快照复测确认。这不是 `MTask.Delay(1)` 的同 Drain 启动风暴复现，也不能被当作网络可靠性已通过。

后续修正把控制入站固定容量提高到 `256` 包（字节上限仍为 `64 KiB`，保持有界），并把控制/数据入站拒绝数写入快照与 CSV/JSON。RPC 快速与专项模式同时改为：只要任一 RPC 样本出现失败、未收或入站拒绝，仍然导出报告，但最终状态为 `NETWORK_BENCHMARK: FAIL`，退出码为 `1`。因此下一步只重跑 3 条 RPC 快速回归；它零失败、零未收、零入站拒绝后，才允许运行 9 条 RPC 专项基线。

修正后的快速报告为 `~/AndroidBenchmark/20260729_151846/`。TCP、KCP、UDP 均为零失败、零未收、零入站拒绝，控制队列峰值分别为 `55`、`41`、`17`，均低于 `256`；这确认旧的 `32` 包控制容量确实无法覆盖本机 64 并发 RPC 的双向短时积压。该轮只有一次 `15 s` 样本，且 TCP/UDP 的 P99 仍为 `3,796.680 ms` 与 `463.802 ms`（KCP 为 `102.118 ms`），因此它只冻结“可靠性恢复”结论，不冻结吞吐与延迟基线；下一步运行 9 条 RPC 专项，以三轮中位数决定 TCP/UDP 是否需要继续优化。

## 2026-07-29 RPC 专项基线：可靠性通过，TCP/UDP 仍需降延迟

报告目录为 `~/AndroidBenchmark/20260729_153127/`，Xiaomi 23116PN5BC、Android 16、Unity 2021.3.45f2 Development。三种传输各三轮 `60 s`、64 并发 RPC 均为零失败、零未收、零断线、零入站拒绝，因此控制队列 `256` 包 / `64 KiB` 作为当前压测配置的可靠性容量已通过并冻结；不再需要重复这项容量验证，除非修改入站队列、RPC 并发目标或协议包体大小。

| 传输 | 吞吐中位数（次/秒） | P50 中位数 | P95 中位数 | P99 中位数 | 队列峰值中位数 | 结论 |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| TCP | `16.921` | `3,828.608 ms` | `4,228.805 ms` | `4,393.796 ms` | `65` 包 | 可靠性通过，但本机回环延迟不可接受，必须优化。 |
| KCP | `461.096` | `66.840 ms` | `99.241 ms` | `102.056 ms` | `46` 包 | 可靠性通过；作为当前 KCP RPC 基线冻结。首轮单次 `1,076.929 ms` 最大值视为孤立扰动，三轮 P99 稳定。 |
| UDP | `48.382` | `368.176 ms` | `467.687 ms` | `501.099 ms` | `16` 包 | 可靠性通过，但本机回环延迟偏高，必须优化。 |

TCP 的连续 P50 约 `3.8 s`、UDP 的连续 P50 约 `368 ms`，与 KCP 的约 `67 ms` 形成数量级差异；它们不是队列容量问题，因为三者均零入站拒绝，且 TCP/KCP 峰值低于 `256`。当前根因假设是：会话出站发送器从 Unity 主线程启动，TCP/UDP 的底层 Socket 异步写入每次恢复都受主线程帧调度影响；KCP 的 `SendAsync` 同步交给自身发送缓冲，未经历同样的逐包等待。

随后曾进行“将全部出站发送泵切到既有 `MTaskExecutors.Network`”的快速验证，报告目录为 `~/AndroidBenchmark/20260729_155243/`。虽然三种传输均零失败、零未收、零拒绝，但 TCP / KCP / UDP 的 P99 分别为 `2,201.055 ms` / `1,898.605 ms` / `1,895.775 ms`；相比本节的专项基线，KCP 与 UDP 均显著恶化。因此该方案不成立，已撤回。结合 `MTaskExecutors.Network` 是接收循环、KCP 更新循环和该发送泵共用的单一专用执行器，最合理的解释是新增发送工作与既有网络任务发生了执行器争用；这是基于测量的推断。

独立出站线程方案没有直接证据证明能解决 TCP/UDP 的根因，已在进入下一次真机测试前撤回，不纳入当前架构。下一步先在原执行模型下记录客户端/服务端的出站排队等待、底层传输写入等待，以及入站队列等待；仅在这些分段指标证明调度位置是瓶颈后，才重新评估执行器隔离方案。该诊断只运行 3 条 RPC 快速回归，不进入性能基线。

诊断报告会新增以下字段，只有由 `NetworkBenchmarkRunner` 启动的压测会启用采样，正式业务默认不记录这些时间戳：

| 字段组 | 计时边界 | 用途 |
| --- | --- | --- |
| `ClientOutboundQueueWait*` | 客户端业务包进入出站队列 → 开始调用传输 `SendAsync` | 数值高表示客户端发送泵未被及时调度或前一包写入阻塞。 |
| `ClientTransportSend*` | 客户端调用传输 `SendAsync` → 该操作完成 | 数值高表示 TCP/UDP/KCP 传输写入路径本身或其续体等待较长。 |
| `ServerOutboundQueueWait*` / `ServerTransportSend*` | 本机服务端回包对应的相同两段 | 用来区分慢点是在 RPC 请求方向还是响应方向。 |
| `IncomingQueueWait*` | 网络线程复制入站包 → Unity 主线程开始处理 | 数值高表示主线程收包派发是瓶颈；该值合并客户端和服务端两个方向。 |

这四组数据不是端到端延迟的简单相加：它们不包含 Protobuf 编解码、RPC Handler 业务逻辑和 RPC 完成后的调度等待。但它们足以把下一步方向限定为“出站泵调度”“底层传输写入”或“主线程入站派发”之一，避免再凭现象修改线程模型。

## 2026-07-30 剩余普通消息快速回归：线程池已生效，瓶颈收敛为逐包底层写入

报告目录为 `~/AndroidBenchmark/20260730_155830/`；目录名为 UTC，生成时间 `2026-07-30 15:58:30 UTC` 对应北京时间 `2026-07-30 23:58:30`。此轮运行 `-networkBenchmarkRemainingNormalQuick`，只覆盖尚未冻结的 TCP/UDP `1000/s`、`5000/s` 普通消息及两项主线程停顿诊断；它是单轮快速回归，不替代后续三轮正式基线。

| 条目 | Offered / Accepted / Rejected / Received | 吞吐 | P99 | 客户端出站等待 / 传输写入平均 | 判定 |
| --- | ---: | ---: | ---: | ---: | --- |
| TCP 普通 `1000/s` | `60000 / 45452 / 14548 / 45452` | `739.426/s` | `1471.002 ms` | `1344.333 / 1.320 ms` | 未通过：队列保护正常拒绝，未发生网络丢失，但串行写出能力不足。 |
| TCP 普通 `5000/s` | `300000 / 42371 / 257629 / 42371` | `689.262/s` | `1497.411 ms` | `1442.381 / 1.414 ms` | 未通过。 |
| UDP 普通 `1000/s` | `59999 / 59999 / 0 / 59999` | `999.407/s` | `45.823 ms` | `11.379 / 0.722 ms` | 单轮通过；补三轮后可冻结。 |
| UDP 普通 `5000/s` | `300000 / 91364 / 208636 / 91364` | `1504.637/s` | `738.276 ms` | `665.088 / 0.654 ms` | 未通过。 |

TCP 与 UDP 的停顿样本均零拒绝、零失败、零丢失、零断线；停顿结束后的恢复分别为 `40.915 ms` 与 `20.044 ms`，均小于 `50 ms`。其 P99 包含刻意注入的 `2000 ms` Unity 主线程停顿，不能与正常消息 P99 混为性能回退。该两个单轮结果待对应传输实现发生变化或准备正式冻结时再复核。

这组数据也排除了“继续换线程”的猜测：客户端出站排队已分别堆积到 TCP `1.34–1.44 s`、UDP `665 ms`，而单个底层发送操作只占 TCP `1.32–1.41 ms`、UDP `0.65–0.72 ms`。当前每会话泵为保证顺序而逐包等待传输完成；本报告版本的 TCP 长度前缀和正文至少需要两次异步写入，UDP 则每个应用消息各发一个 datagram。下一轮先验证 TCP 的单帧合并：小包把长度头与正文写入同一池化数组并一次发送，大包仍分段发送。仅当 TCP `1000/s` 仍拒绝时，才评估多包有界批量；不得将 TCP 字节流批量写入直接套用到 UDP datagram，亦不得因本组数据创建全局专用发送线程。

## 2026-07-30 RPC 分段诊断：已定位为串行等待发送，不是队列容量或“需要新线程”

报告目录为 `~/AndroidBenchmark/20260729_161352/`；目录名使用 UTC，等价于北京时间 `2026-07-30 00:13:52`。三种传输均零失败、零未收、零拒绝，说明本轮仅用于定位性能，不存在可靠性回归。

| 传输 | 端到端 P50 | 客户端出站排队平均 | 客户端传输 `SendAsync` 平均 | 服务端出站排队平均 | 服务端传输 `SendAsync` 平均 | 入站队列平均等待 | 结论 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| TCP | `2,911.708 ms` | `642.259 ms` | `52.057 ms` | `0.023 ms` | `51.871 ms` | `1,003.050 ms` | 客户端发送泵排队与入站主线程等待均严重；`SendAsync` 完成路径约 52 ms，不能在入站循环中逐包等待。 |
| KCP | `66.825 ms` | `0.005 ms` | `0.021 ms` | `0.004 ms` | `0.013 ms` | `22.064 ms` | 所有出站段接近零，证明固定队列、缓冲池和 64 并发控制容量不是当前瓶颈。 |
| UDP | `364.990 ms` | `303.620 ms` | `19.381 ms` | `0.008 ms` | `0.167 ms` | `25.556 ms` | 首要慢点是客户端发送泵逐包等待，服务端回包和入站队列不是主因。 |

代码路径给出了可验证的因果关系：`ProcessQueueAsync` 逐包 `await HandleIncoming`；RPC 请求的 `HandleIncoming` 又在主线程入站处理循环中 `await session.SendOwnedAsync(...)`，直到底层传输 `SendAsync` 完成才处理下一包。TCP 服务端单包处理最大值为 `69.130 ms`，与其服务端发送完成路径 `51.871 ms` 同量级，直接佐证该等待阻塞了入站消费。客户端的 `NetworkOutboundQueue.DrainAsync` 同样逐包等待 `transport.SendAsync`，所以 TCP/UDP 的完成路径分别约 `52 ms` / `19 ms` 时，64 个并发请求会在客户端队列中形成约 `642 ms` / `304 ms` 的等待。

因此，本轮实施的第一项修复不是增加线程或复制 KCP 缓冲：RPC 响应在成功进入可靠出站队列后立刻释放入站处理循环；若可靠队列已满或会话断开，则记录错误并关闭该会话，不能静默丢弃。尚未改变客户端 `CallAsync` 和 `SendAsync` 等待实际写入完成的公开语义。修复后只重跑 3 条 RPC 快速回归，重点观察 TCP/UDP 的客户端出站排队、入站队列等待与端到端延迟是否下降；KCP 不应回退。

## 2026-07-30 RPC 快速回归：线程池出站泵通过，冻结为当前实现

报告目录为 `~/AndroidBenchmark/20260730_150625-threadpool/`。本轮仅改变 `NetworkOutboundQueue.DrainAsync` 的执行器：每个会话仍然只有一个串行发送泵、仍按可靠包优先的既有顺序发送，但泵及其 `await transport.SendAsync` 后续续体运行在共享 CLR 线程池，而不是 Unity 主线程；它没有创建专用线程，也没有复用会与接收/KCP 更新争用的 `MTaskExecutors.Network`。

三种传输均为零失败、零未收、零拒绝、零断线。与 `~/AndroidBenchmark/20260730_143728/`（服务端响应非阻塞后、仍由 Unity 主线程驱动发送泵）比较如下：

| 传输 | 吞吐（前 → 后） | P50（前 → 后） | P99（前 → 后） | 客户端出站排队平均（前 → 后） | 客户端传输等待平均（前 → 后） | 结论 |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| TCP | `21.155 → 302.717 次/秒` | `1,131.938 → 67.316 ms` | `1,431.219 → 101.464 ms` | `743.329 → 16.887 ms` | `46.771 → 2.468 ms` | 通过；主线程续体调度是此前积压的直接原因。 |
| KCP | `459.429 → 469.703 次/秒` | `66.920 → 66.869 ms` | `101.794 → 102.570 ms` | `0.004 → 0.408 ms` | `0.022 → 0.029 ms` | 保持当前基线；约 `0.8 ms` 的差异属于本轮扰动，未回退。 |
| UDP | `48.540 → 446.355 次/秒` | `369.069 → 66.518 ms` | `500.876 → 72.151 ms` | `319.748 → 11.348 ms` | `20.468 → 1.412 ms` | 通过；此前性能问题同样来自主线程续体调度。 |

这证明“发送完成要回到 Unity 主线程”是 TCP/UDP 性能问题的充分解释，而不是队列容量、缓冲池或新增专用线程的问题。保留共享线程池方案；已被验证会严重回退的 `MTaskExecutors.Network` 方案和无证据的独立出站线程方案均不采用。该结论适用于当前 Android 真机、64 并发 RPC、同机回环条件；修改 `MTask` 调度器、出站发送器、底层传输或 RPC 并发模型后，必须重跑本节 3 条快速回归。
