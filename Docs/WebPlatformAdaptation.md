# WebGL 与小游戏平台适配

本文记录 MiniCore 面向普通浏览器 WebGL，以及后续微信、抖音等小游戏宿主的最终框架边界。框架按“运行能力”设计，不把 Client、Dedicated Server、Android、微信或抖音建模为互斥框架角色。

当前已落地普通浏览器 WebGL 的 WebSocket 客户端适配器与 IndexedDB 存储后端；微信和抖音 SDK 尚未接入。后续平台包只需注册各自的 WebSocket 客户端适配器、存储后端和宿主生命周期适配器，不改 MTask、网络协议、Session、RPC 或业务 Handler。

## 1. 最终分层

| 层级 | 当前职责 | 平台关系 |
| --- | --- | --- |
| `MiniCore.Runtime` | MTask、执行器契约、存储契约、对象池和生命周期 | 所有环境共用；无线程环境保留相同 MTask API |
| `MiniCore.Network` | Session、协议帧、RPC、收发队列、TCP/UDP/KCP/WebSocket | 同一程序集和命名空间；调用前查询传输能力 |
| `MiniCore.Platform.Browser` | 浏览器 WebSocket、IndexedDB 注册与 JavaScript 绑定 | 只进入 WebGL Player |
| 未来微信平台包 | 微信 WebSocket、文件/键值存储、前后台和 SDK 能力 | 可选择安装和引用，不进入其他构建 |
| 未来抖音平台包 | 抖音 WebSocket、Stark/文件存储、前后台和 SDK 能力 | 可选择安装和引用，不进入其他构建 |

Dedicated Server 只是 Unity 的无渲染运行形态，不是独立框架层，也没有 `MINICORE_SERVER` 一类框架宏。一个进程可以同时监听下游连接并作为客户端连接其他服务；因此 `INetworkService` 保持统一，不拆成互斥的 Client/Server 接口。

平台条件编译只允许出现在平台程序集、原生绑定和确实依赖运行时能力的底层实现中。业务代码通过 `MTaskExecutors.SupportsThreads`、`NetworkCapabilities` 和平台适配注册表查询能力，不散布 `UNITY_WEBGL`、微信或抖音宏。

## 2. MTask 与线程

MTask 始终是统一异步模型：有线程环境和无线程环境都能使用 `await MTask`、Delay、Yield、Owner 取消和主循环调度。

| API | 有托管线程环境 | 浏览器 WebGL |
| --- | --- | --- |
| `MTaskExecutors.Unity` | Unity 主循环 | Unity WebGL 主循环 |
| `MTaskExecutors.ThreadPool` | CLR 共享线程池 | 明确抛出 `PlatformNotSupportedException` |
| `TryGetThreadPool` | 返回共享执行器 | 返回 `false` |
| `CreateSingleThread(name)` | 创建一条模块独占、串行执行的工作线程 | 明确抛出 `PlatformNotSupportedException` |
| `TryCreateSingleThread` | 返回模块持有的执行器租约 | 返回 `false` |

`CreateSingleThread` 直接表示“创建一条顺序执行的后台工作线程”。网络模块调用 MTask 工厂取得 I/O 执行器并持有其生命周期租约；线程的创建、登记、调度、诊断和退出兜底仍全部由 MTask 实现。网络只负责在自身释放时归还租约，并把它注入监听器和 Transport。会话发送泵不要求线程亲和性：原生环境使用 MTask 共享线程池，避免与收包/KCP 更新争用；浏览器使用同一个 Unity 主循环执行器。

在浏览器中，`NetworkService` 无法取得独占线程时自动使用 Unity 主循环执行器。网络回调只把完整包放入固定容量队列，业务反序列化和 Handler 每帧受双重预算限制：默认最多 `256` 包或 `2 ms`。剩余包保留到后续帧；队列和单会话容量到顶会拒绝数据，持续拥塞会断开会话，而不是无限堆积内存。

## 3. 网络能力与协议

`MiniCore.Network` 保留统一 `INetworkService` 和 Transport 抽象。Transport 并不多余：它隔离连接、监听、收发和诊断，使上层 Session、RPC、心跳与 Handler 不需要知道底层是 TCP、UDP、KCP 还是 WebSocket。已删除的是没有提供额外职责的旧 `Transport/Entity` 目录层级。

当前能力矩阵：

| 环境 | TCP | UDP | KCP | WS/WSS 客户端 | WS/WSS 监听 |
| --- | --- | --- | --- | --- | --- |
| 原生 Player/Editor | 连接与监听 | 连接与监听 | 连接与监听 | 连接 | 监听 |
| 浏览器 WebGL | 不支持 | 不支持 | 不支持 | 支持 | 不支持 |

调用方使用 `NetworkCapabilities.SupportsConnect(kind)` 和 `SupportsListen(kind)` 决定暴露什么功能。不支持的调用会在创建 Socket 或线程前得到明确异常。

WebSocket 使用二进制消息承载与 TCP 一致的长度帧：

```text
4 字节大端 packetLength + 12 字节业务头(opcode + rpcId) + Protobuf Body
```

接收端按字节流累计并拆帧，不能假设一次 WebSocket 回调必然等于一个业务包。同一连接使用唯一派发泵保证跨回调顺序，并以最大待派发包数和字节数双重限制内存；超过上限时关闭拥塞连接，不无限堆积。这样可以直接连接 MiniCore 的 WS/WSS 监听器，也允许中间代理做不理解 Opcode 的透明转发。原生环境使用仓库固定提交的 `websocket-sharp` 客户端和监听器；per-message 压缩关闭，避免压缩协商差异、额外 CPU 和压缩侧信道面。实现同时校验 URL、路径、二进制消息、握手、关闭码与最大消息大小。浏览器环境使用 `.jslib` 调用宿主 WebSocket，并对 `bufferedAmount` 设置发送背压上限。

## 4. 网关是什么、谁来使用

MiniCore 不强制经过网关，也不在当前仓库内实现一个网关进程。开发和小规模部署可以让客户端直接连接游戏服务的 WSS 监听地址。

常见网关分两层：

| 类型 | 处理内容 | 典型使用者 | MiniCore 如何配合 |
| --- | --- | --- | --- |
| TLS/透明代理 | 证书、WSS 握手、连接转发、基础负载均衡 | 运维或部署系统 | 客户端把 URL 改为代理地址；代理按二进制流转发完整长度帧，不理解 Opcode |
| 业务网关 | 登录鉴权、限流、区服/房间路由、会话绑定、灰度与封禁 | 大规模在线业务 | 网关解析必要的接入层协议，再把连接或消息路由到对应游戏节点；这是独立部署系统 |

是否需要业务网关取决于规模，不取决于框架是否叫“小游戏框架”。接入顺序建议为：

1. 客户端直接以 WSS 连接单个游戏节点，先完成玩法闭环。
2. 需要统一证书或隐藏节点时，在前面部署成熟反向代理做 TLS 终止和透明转发。
3. 需要多节点横向扩容时，再增加登录票据、会话亲和、节点发现与房间路由；房间在一局内固定到同一节点。
4. 只有连接迁移、跨节点消息或精细限流成为真实需求时，才开发业务网关。

框架已经为透明转发准备好稳定长度帧和统一 WS Transport，但不会假设某种网关产品。Nginx、Envoy、云负载均衡或自研进程都可以位于外部；开发者/运维配置网关，客户端业务仍只调用 `INetworkService` 并使用下发的 WS/WSS URL。

## 5. 存储与存档

业务层只面对逻辑键二进制存储：`ReadAsync`、`WriteAsync`、`DeleteAsync`、`ExistsAsync`，不取得平台文件路径。原生环境使用文件后端，浏览器使用 IndexedDB；未来微信和抖音平台包可注册各自后端。

`ISaveService` 保存二进制数据，Protobuf 业务通过 `SaveProtobufAsync` / `LoadProtobufAsync` 使用生成消息。默认保护格式为：

```text
MCSB + 版本 + 随机 IV + AES-CBC 密文 + HMAC-SHA256 标签
```

加密密钥和认证密钥按槽位分别派生，并采用 Encrypt-then-MAC。摘要本身不是加密；HMAC 用于检测篡改，AES 用于避免明文直读。客户端内置口令可以提高修改成本，但不能抵抗客户端逆向或替代服务端权威校验。

## 6. 微信与抖音后续接入

后续平台适配应各自建立可选程序集，只实现平台边界：

- WebSocket 客户端适配器及发送缓冲/消息大小限制；
- 文件或键值存储后端；
- OnShow/OnHide、网络变化、内存告警等宿主生命周期；
- 安全区、键盘、音频、剪贴板、授权等 SDK 能力；
- 资源分包、缓存目录和域名白名单所需构建工具。

平台包启动时向现有注册表安装后端。Runtime、Network 和业务程序集不直接引用 WXSDK、TTSDK/Stark，也不要求所有项目同时安装多个平台 SDK。若同一 Unity WebGL 构建链会定义相同基础宏，由平台构建工具或独立 asmdef 决定实际进入哪个平台包，不能在业务层用宏猜测宿主。

## 7. 构建与验证

WebGL 构建前校验器会检查：浏览器程序集和 `.jslib` 是否存在、平台 asmdef 是否只进入 WebGL、原生 `websocket-sharp` 是否排除 WebGL，以及浏览器专属源码是否直接引用 Socket、Thread、ThreadPool、Timer、WaitHandle 或 `Task.Run`。`Interlocked`、`Volatile` 和取消状态只是在单线程环境中提供内存/状态语义，不等于创建线程，因此不机械禁止。

每次平台后端变化至少完成：Unity Editor C# 编译、WebGL 目标脚本编译、`.jslib` 链接，以及浏览器真机上的 WSS、前后台、断网重连、IndexedDB 和长时间队列观测。微信/抖音接入后再分别增加对应开发者工具与真机验证，不用反向污染普通浏览器实现。
