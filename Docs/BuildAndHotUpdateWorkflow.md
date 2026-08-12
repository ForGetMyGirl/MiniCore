# 打包与热更新流程

本文档说明 MiniCore 当前由 **HybridCLR + YooAsset** 组成的 Android/Player 打包与后续热更新流程。它以项目中的实际菜单命令和启动代码为准，适用于 `DefaultPackage`。

## 1. 先理解两个构建入口

| 操作 | 产物与职责 | 何时需要 |
| --- | --- | --- |
| `MiniCore/Build/DefaultPackage/完整生成 (Generate All + Build)` | 执行 HybridCLR `Generate All`，同步热更 DLL 与 AOT 元数据，并构建、校验 `DefaultPackage`。 | 首次构建、构建目标或 Development 设置改变、HybridCLR/AOT 配置或泛型使用改变，以及产物缺失时。 |
| `MiniCore/Build/DefaultPackage/热更编译 (Compile Active Target + Build)` | 仅编译当前平台热更新 DLL，复用现有 AOT 产物，并构建、校验 `DefaultPackage`。 | 平台与 Development 设置不变时，日常修改热更新代码或资源。 |

两个入口都会校验热更新 DLL、AOT 元数据、生成的地址表和首包清单是否一致；菜单成功不代表 Android Player 已构建完成，验证首包时仍需执行 Unity 的 Build 或 Build And Run。在线模式可以直接发布新资源版本，客户端只下载哈希发生变化的 Bundle。

## 2. 操作前的共同检查

1. 等待 Unity 脚本编译完成，确认 Console 没有 C# 编译错误。
2. 在 `File > Build Settings` 选定目标平台；Android 包必须先切换到 Android。
3. 确认本次 Player 是否为 Development Build。HybridCLR 生成产物与这个选择必须一致。
4. 确认 `UpdateMainWindow` 的 `packageName` 为 `DefaultPackage`，并按本次目的选择资源模式：本地验证使用 Offline，在线热更验证使用 Host。

> 关键规则：先确定“平台 + Development Build”，再执行 HybridCLR 命令。执行 `Generate/All` 后才改 Development Build，属于产物不匹配，必须重新执行 `Generate/All`。

## 3. 首次构建、切平台或底层改动：完整流程

以下情况必须走完整流程：首次出包、切换 Android/iOS/桌面目标、切换 Development Build、升级 Unity/HybridCLR、调整 HybridCLR 设置/热更新程序集列表、修改 AOT 泛型使用或清理过 `Library`/HybridCLR 产物。

1. 在 Unity Build Settings 设置目标平台与 Development Build。
2. 执行 `MiniCore > Build > DefaultPackage > 完整生成 (Generate All + Build)`。
3. 检查 Console 出现“`MiniCore DefaultPackage 完整构建完成`”。
4. 执行 Unity 的 `Build` 或 `Build And Run`。
5. 在目标设备完成启动验证；网络改动还应执行对应的 Editor 冒烟测试与 Android 压测范围，详见[网络冒烟测试](NetworkSmokeTesting.md)。

本轮网络框架、队列或压测运行器等改动后，为避免使用旧 AOT 产物，也按此完整流程构建一次。

## 4. 日常修改热更新代码：最短安全流程

适用范围：只修改 `MiniCore.HotUpdate` 中的业务、Handler、测试或协议处理代码；平台和 Development Build 不变；没有改动 HybridCLR/AOT 配置或新的 AOT 泛型使用。

1. 执行 `MiniCore > Build > DefaultPackage > 热更编译 (Compile Active Target + Build)`。
2. 根据目的选择：
   - **验证首包**：执行 Unity `Build` / `Build And Run`，安装新 APK 或 Player。
   - **准备在线热更**：不要立即重打 APK，按[第 6 节](#6-发布在线热更新包)发布新 `DefaultPackage`。

`ActiveBuildTarget` 是日常首选：它直接采用当前 Unity 的平台和 Development Build 设置，最不容易把 Debug/Release 产物混在一起。

### CompileDll 各选项怎么选

| 选项 | 用法 |
| --- | --- |
| `ActiveBuildTarget` | 默认选择。当前已切到 Android 且 Development 勾选状态正确时，就用它。 |
| `Android` | 仅在当前活动目标已是 Android 时使用，实际效果与 `ActiveBuildTarget` 等价。 |
| `ActiveBuildTarget_Development` / `Android_Development` | 强制生成 Development 产物。仅在之后的 Unity Build 也明确勾选 Development Build 时使用。 |
| `ActiveBuildTarget_Release` / `Android_Release` | 强制生成 Release 产物。仅在之后的 Unity Build 明确取消 Development Build 时使用。 |

不要用强制 `_Development` 产物去构建 Release Player，也不要用 `_Release` 产物去构建 Development Player；不确定时回到 `ActiveBuildTarget`。

## 5. 不需要构建 DefaultPackage 的情况

仅修改以下内容且不需要改变 YooAsset 资源时，通常可以直接 Build：

- Unity Player 设置、签名、图标或渠道配置；
- 未打入 `DefaultPackage` 的编辑器脚本。

修改 Player/AOT 侧运行时代码时，即使没有改热更新程序集，也可能改变热更 DLL 所需的裁剪 AOT 元数据；按第 3 节执行“完整生成”菜单，不要把它当成“直接 Build”的情形。

只要修改了热更新 DLL 或 `Assets/AssetRes` 中会随 `DefaultPackage` 发布的资源，就执行“热更编译”菜单。若修改属于第 3 节的底层/AOT 情况，则改用“完整生成”菜单。

## 6. 发布在线热更新包

项目启动时由 `UpdateMainWindow` 初始化 `DefaultPackage`，在 Host 模式依次请求远端版本、更新清单、下载缺失资源，然后加载 AOT 元数据，并按项目登记的依赖顺序加载 `MiniCore.Protocol`、`MiniCore.HotUpdate` 及其他热更新 DLL。因此，在线热更发布的是 **YooAsset 的完整新包版本**，不是单独上传一个 DLL。

1. 按改动范围执行第 3 节“完整生成”或第 4 节“热更编译”菜单。
2. 在 YooAsset 构建输出中找到本次 Android / `DefaultPackage` / 时间戳版本目录；两个菜单都使用 UTC `yyyyMMddHHmmss` 作为包版本。
3. 将该版本目录中的清单、版本文件和全部资源文件按 YooAsset 的原始目录结构上传到 Host 模式 `resourcesServerURL` 指向的资源根目录；备用源 `fallbackServerURL` 应部署相同内容。
4. 不要只传变动 DLL，也不要重命名哈希资源文件；客户端先读取版本文件，再按清单下载对应文件。
5. 使用一台已安装旧首包的测试设备启动应用，确认它能获取新版本、下载资源、加载热更新 DLL 并进入游戏。
6. 验证通过后再扩大发布范围；保留上一稳定版本的完整目录，以便服务器侧回退版本文件/资源指向。

当前项目没有把构建产物自动上传 CDN/对象存储的脚本，也没有在仓库内定义发布服务器目录规范。因此第 3 步应交由现有的部署渠道执行；在接入自动发布前，发布人必须记录包版本、目标平台、Development/Release 状态、资源根地址和验证设备结果。

### 在线热更新不能覆盖的改动

以下改动必须通过新的 Player 首包（APK/IPA/桌面包）发布，不能仅发资源包：

- 修改 AOT/Player 侧代码、Unity 原生插件、Android Manifest、签名或 Player 设置；
- 需要新增或改变 AOT 泛型/元数据，而旧 Player 不具备兼容基础；
- 修改启动器 `UpdateMainWindow`、首包资源模式或远端地址配置；
- Unity、HybridCLR 或底层原生库升级。

是否可以只发热更包的判断原则是：旧 Player 是否已经具备运行新热更 DLL 所需的 AOT 元数据、原生能力与启动逻辑。不确定时按首包发布处理。

## 7. 验证清单

### 首包 / APK 验证

- 对应的“完整生成”或“热更编译”菜单成功；
- Player 正常启动，日志中无 AOT 元数据或热更新 DLL 加载失败；
- 涉及网络时，先通过 `NetworkLoopbackIntegrationTests`，再按[网络冒烟测试](NetworkSmokeTesting.md)选择 RPC 快速、专项或完整 Android 压测；
- 记录 APK 版本、包版本、目标平台和 Development 状态。

### 在线热更新验证

- 新包已上传，主/备用源均可访问；
- 旧首包设备启动后，日志显示版本请求、清单更新和下载成功；
- 客户端实际加载的是新热更 DLL，目标功能可用；
- 网络/协议改动按相应回归范围通过；
- 保留上一个可用远端版本，确认能够回退。

## 8. 常见问题

| 现象 | 原因与处理 |
| --- | --- |
| “热更编译”提示缺少 HotUpdate DLL | 确认当前活动平台正确后重试；若属于首次、切平台或 AOT 改动，改用“完整生成”。 |
| 提示缺少 AOT 元数据或 `AOTGenericReferences` | 执行“完整生成 (Generate All + Build)”。 |
| 修改热更代码后设备仍是旧逻辑 | 多数是未执行“热更编译”菜单，或在线模式未上传新包版本。重新按第 4 节处理并确认版本文件已更新。 |
| Development 与 Release 包行为异常 | 确认 Build Settings 的 Development Build 与 HybridCLR 产物一致；改变勾选状态后执行“完整生成”。 |
| 在线客户端不下载新资源 | 检查 `UpdateMainWindow` 是否为 Host 模式、主/备用 URL 是否正确、版本文件和新版本目录是否已完整上传。 |

## 9. 日常决策速查

```text
是否改了平台、Development Build、Player/AOT 运行时代码、HybridCLR/AOT 配置或泛型/AOT依赖？
  是 → 完整生成 (Generate All + Build) → Build 首包 或 发布完整新资源包
  否 → 是否改了 MiniCore.HotUpdate 或 DefaultPackage 资源？
         是 → 热更编译 (Compile Active Target + Build) → Build 首包 或 发布新资源包
         否 → 直接 Build（如仅改 Player 设置、签名或图标）
```

## 10. 验证记录

### 2026-08-12（实例协议注册与多热更新程序集生成链）

- 症状：最终隔离编译报错，项目内 Google.Protobuf 版本无法使用 `CodedOutputStream(byte[], offset, length)` 构造函数。
- 根因：该版本只提供整数组写入构造函数；直接写调用方数组头部又会破坏网络帧前缀。
- 修复：`ProtobufSerializer.SerializeInto` 先校验精确正文长度，再使用 `ArrayPool<byte>` 池化缓冲编码并复制到目标区间；没有保留不兼容构造函数，也不产生每包临时数组。
- 验证：Unity `2021.3.45f2` 隔离工程完整脚本编译通过；PB 生成成功，覆盖 `3` 个项目 Proto 和 `39` 个网络协议注册项；Handler 二阶段生成确认 `26` 项且无需更新；开发导航重新生成。按本次要求未运行测试，实际 Player、多 DLL YooAsset 产物和端到端加载仍需在对应构建流程中验证。

相关文档：[架构总览](Architecture.md)、[网络与协议](NetworkLayerAnalysis.md)、[网络冒烟测试](NetworkSmokeTesting.md)、[性能测试指南](PerformanceTestingGuide.md)。
