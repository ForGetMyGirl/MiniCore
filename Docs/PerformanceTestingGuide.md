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

## 第二项测试：Protobuf 正式路径与 JSON 对比基线

`NetworkMessageComponent` 在未显式设置 serializer 时默认使用 `ProtobufSerializer`，这是当前正式网络路径。`NewtonsoftJsonSerializer` 仅保留为迁移和性能对比实现；JSON 测试不代表当前客户端或 Dedicated Server 的默认配置。

先运行 Protobuf 序列化基准 `Assets/Tests/Editor/ProtobufSerializationPerformanceTests.cs`：

| 测试名 | Sample Group | 测量内容 |
| --- | --- | --- |
| `ProtobufSerializer_SerializesMessage` | `Network.Protobuf.Serialize` | Protobuf 消息序列化的时间与 GC。 |
| `ProtobufSerializer_DeserializesMessage` | `Network.Protobuf.Deserialize` | 通过生成 Parser Registry 反序列化的时间与 GC。 |

保留的 JSON 对比测试在 `Assets/Tests/Editor/NetworkJsonSerializationPerformanceTests.cs`：

| 测试名 | 测量问题 | 固定输入 |
| --- | --- | --- |
| `NewtonsoftJsonSerializer_SerializesMediumProtocolMessage` | 发送侧将协议对象编码为 UTF-8 JSON 字节需要多少时间、是否产生 GC。 | 一个携带固定中等长度文本的 `TestNetworkData`。 |
| `NewtonsoftJsonSerializer_DeserializesMediumProtocolMessage` | 收包侧将固定 JSON 字节还原为协议对象需要多少时间、是否产生 GC。 | 与发送侧相同协议生成的 JSON 字节。 |

在 Test Runner 的 `EditMode` 中分别运行 Protobuf 和 JSON 测试，每条各运行三次并导出 CSV。每组包含 `10,000` 次操作，报告中的时间是每 `10,000` 次的总耗时；计算单次成本时除以 `10,000`。它们都不包含 Socket、协议包封装、跨线程队列和 Handler 派发。

Protobuf 基线稳定后，第三项先测试网络线程到主线程的收包队列交接；随后分别运行 JSON 对比链路和 Protobuf 正式链路的完整入站包/RPC 基准。每次只新增一段链路，才能准确判断性能变化来自哪里。

## 第三项测试：网络收包队列交接

测试文件是 `Assets/Tests/Editor/NetworkIncomingQueuePerformanceTests.cs`，测试名为 `IncomingQueue_TransfersMediumPackets_BetweenNetworkAndMainThread`。

它用固定的 512 B 业务包模拟现有 `NetworkMessageComponent.EnqueueIncoming` 和 `ProcessQueueAsync` 中的内存路径：从传输层输入复制到 `ByteBufferPool` 租用的数组、放入 `ConcurrentQueue`、主线程出队、再归还数组。测试不启动 TCP/UDP/KCP，不做 JSON 或 Protobuf 反序列化，也不调用业务 Handler；因此它只回答“当前队列与缓冲池的基础交接成本是多少、是否产生异常 GC”。

每个测量组连续处理 `10,000` 个业务包。报告中的 `Network.IncomingQueue.MediumPacket` 是这一整组的总耗时，换算单包成本时除以 `10,000`。在 Test Runner 的 `EditMode` 下单独运行它三次；每次完成后可在 `MiniCore > Performance > History` 中查看并保留记录。

若完整交接测试显示稳定 GC 分配，再运行下面的归因测试，而不要立刻替换网络组件：

| 测试名 | Sample Group | 用途 |
| --- | --- | --- |
| `ByteBufferPool_CopiesMediumPackets_WithoutQueue` | `Network.IncomingQueue.BufferCopyOnly.MediumPacket` | 只测缓冲池租用、复制、归还，排除队列实现。 |
| `ConcurrentQueue_TransfersMediumPackets_WithoutBufferCopy` | `Network.IncomingQueue.ConcurrentQueueOnly.MediumPacket` | 只测当前 `ConcurrentQueue` 对收包结构体的入队与出队，排除复制与缓冲池。 |
| `BufferBlockCopy_CopiesMediumPackets_WithoutPool` | `Network.IncomingQueue.BlockCopyOnly.MediumPacket` | 只测固定数组间的字节复制，排除缓冲池容器操作。 |
| `ByteBufferPool_RentsAndReturnsMediumPackets_WithoutCopy` | `Network.IncomingQueue.BufferPoolOnly.MediumPacket` | 只测缓冲池的租用和归还，排除字节复制。 |

全部测试均使用相同的 `10,000` 包、512 B 输入和测量参数。2026-07-15 的第一次归因结果显示：`ConcurrentQueue` 测试为 `GC() = 5`，而包含 `ByteBufferPool.Rent/Return` 的测试为 `GC() = 10005`；因此当前优先怀疑缓冲池容器的入栈/出栈操作，而不是队列。

2026-07-15 的第二次归因结果显示：纯 `Buffer.BlockCopy` 为 `GC() = 5`，仅 `ByteBufferPool.Rent/Return` 为 `GC() = 10005`。这确认了分配来自旧版缓冲池的 `ConcurrentStack` 内部节点，而不是字节复制或收包队列。

当前 `ByteBufferPool` 已改为每桶内部加锁的 `byte[][]` 槽位栈，并设置单数组 1 MB、单桶 8 MB、全局 32 MB 的保留上限。下一次运行时，应重跑“仅租用/归还”和完整交接测试：稳定样本的 `GC()` 目标是接近当前测试环境的基础值 `5`。首次预热、池扩容或工作负载超过保留上限时仍可能发生数组分配，这是容量不足的正常诊断信号。

缓冲池改造后，除已有四条性能测试外，还应在 EditMode 中运行 `ByteBufferPoolTests`。它验证同尺寸数组会被复用，并验证多个线程同时执行 `Rent/Return` 时不会返回无效数组或产生并发异常。

## 第四项测试：关闭日志时的收包字符串分配

`NetworkMessageComponent.HandleIncoming` 当前会在调用 `LogSwitch.Info` 前格式化时间并构造插值字符串。`LogSwitch.EnableLog = false` 只能阻止日志输出，不能阻止已经发生的字符串创建。

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
