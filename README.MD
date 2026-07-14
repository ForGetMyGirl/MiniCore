# MiniCore

MiniCore 是基于 Unity 2021.3 的组件化游戏框架。项目将基础定义、核心能力和热更新业务拆分为独立程序集，当前包含资源加载、UI MVP、计时器、CSV 读取、文件工具和 TCP/UDP/KCP 网络模块。

## 模块

| 模块 | 职责 |
| --- | --- |
| `MiniCore.Model` | 基础实体、接口、网络协议、Transport、序列化、KCP 实现和生成代码。 |
| `MiniCore.Core` | Global 组件容器、资源、UI、网络、计时器和通用工具。 |
| `MiniCore.HotUpdate` | 热更新业务协议、Handler、场景入口、UI View 与 Presenter。 |

## 核心能力

- 组件化生命周期：`AComponent`、强类型 `ComponentInitArgs` 与 `AComponent<TArgs>`。
- Global owner 引用计数：直接返回组件实例，避免租约包装对象。
- UI：MVP 结构、窗口工厂、预加载与缓存。
- 资源：默认 YooAsset 接入，可通过 `IResourcesComponent` 替换实现。
- 网络：TCP、UDP、KCP，普通消息、RPC、心跳、客户端/服务端 Session。
- 网络派发：主线程收包处理、缓冲池复用、运行时热路径无 `MethodInfo.Invoke`。
- Opcode：稳定 manifest、自动生成映射和构建前校验。

## Global 组件使用

Global 组件必须声明 owner，通常传入 `this`。获取一次就对应释放一次；长期持有的对象应在 `Dispose`、`OnDestroy` 或解绑时调用 `ReleaseAll(this)`。

```csharp
// 基础设施：由 Global 根 owner 常驻持有，重复 Pin 不增加引用。
NetworkMessageComponent network = Global.Com.Pin<NetworkMessageComponent>();

// 临时或业务 owner：获取后负责 Remove / ReleaseAll。
AssetsComponent assets = Global.Com.Get<AssetsComponent>(this);
Global.Com.Remove<AssetsComponent>(this);

// 不存在时创建；初始化参数只在首次创建时生效。
YooAssetResourceComponent resources = Global.Com.GetOrAdd<YooAssetResourceComponent>(
    this,
    new YooAssetResourceComponentInitArgs("DefaultPackage"));

// owner 销毁时的兜底释放。
Global.Com.ReleaseAll(this);
```

`ForceRemove<T>()` 仅用于退出、切服等最高层中断流程；常规业务不要使用它替代 `Remove<T>(owner)`。

## 网络快速说明

- 普通消息实现 `IProtocol`，RPC 请求实现 `IRequest`，响应实现 `IResponse`。
- 普通消息使用 `AMHandler<TMessage>`，RPC 使用 `ARpcHandler<TRequest, TResponse>`。
- 业务协议和 Handler 放在 `MiniCore.HotUpdate`；opcode 映射由稳定 manifest 与生成文件维护。
- 默认 serializer 是 `UnityJsonSerializer`；示例入口配置为 `NewtonsoftJsonSerializer`。
- TCP 使用长度前缀，UDP 单个 datagram 对应单个 packet，KCP 在 UDP 上完成可靠重组。

详细链路、包格式、Session 和扩展步骤见：[网络层解析](Docs/NetworkLayerAnalysis.md)。

## 优化路线

后续优化应先压测再改造。第一次使用 Unity Performance Testing 时，先按[第一次性能测试指南](Docs/PerformanceTestingGuide.md)建立 Handler 派发基线；每次性能测试结果会自动归档到 `BenchmarkPerformance/History`，可在 Unity 菜单 `MiniCore > Performance > History` 查看和比较；阶段化待办、验收指标、风险与执行顺序见：[优化路线图](Docs/OptimizationRoadmap.md)。

当前优先级：建立性能基线和监控、入站队列背压、RPC 生命周期调度、缓冲池容量治理。

## 开发约定

- 公开方法、字段、属性使用中文 XML 注释；私有字段使用简洁中文行尾注释。
- 新建文件采用 UTF-8 无 BOM；已有文件保持原始编码。
- 高频路径避免临时分配；优先复用字段、缓存或已有对象池。
- 网络线程只负责接收与入队，Unity API 和业务 Handler 在主线程执行。

## 依赖

- Unity `2021.3.45f2c1`
- [UniTask](https://github.com/Cysharp/UniTask)
- [YooAsset](https://www.yooasset.com/docs/Introduce)
- Newtonsoft Json（Unity Package）

## 注意事项

- KCP 实现基于引入的 C# KCP 代码，修改算法前应补齐 TCP/UDP/KCP 的回归与压测。
- HybridCLR 可按项目需要接入；HotUpdate 程序集的网络协议、Handler 与生成代码需要一起验证。
- 项目包含辅助生成代码；提交前应审查生成结果和协议兼容性。
