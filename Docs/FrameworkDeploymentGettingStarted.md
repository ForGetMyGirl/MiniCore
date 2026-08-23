# MiniCore 框架部署入门

本文面向第一次部署 MiniCore 的开发者。当前推荐入口是独立桌面应用 **MiniCore Deploy**；完整界面、状态机和目录规范见[MiniCore Deploy 自动构建与发布](MiniCoreDeploy.md)。本文重点说明第一次配置时需要作出的框架级选择。

MiniBomber 仅作为业务示例。项目自己的 Role、认证、数据库、房间、匹配和玩家 Drain 规则不应进入 MiniCore Runtime。

## 1. 先选择拓扑

生产默认：

```text
Coordinator（独立 DS 进程）
  ├── 项目业务 DS 1..N
  ├── DatabaseServer（可选）
  └── 客户端可发现的公开业务服务

AuthenticationServer（可选、独立，不注册 Coordinator）
静态内容 / WebGL / YooAsset（按项目需要）
```

小项目可以让 Coordinator 与多个业务 Role 同进程。两种拓扑使用同一份 DS 制品和外部配置模型。自动发布流程成熟后，生产环境优先使用独立 Coordinator，便于明确更新顺序和故障边界。

框架级必需关系：

| 组件 | 框架是否必需 | 说明 |
| --- | --- | --- |
| Coordinator | 使用框架服务发现时必需 | 唯一特殊控制面；不转发业务 RPC |
| 普通 Dedicated Server | 按项目需要 | 承载项目定义的一个或多个 Role |
| AuthenticationServer | 否 | MiniBomber HTTP 认证只是可替换示例 |
| DatabaseServer | 否 | `persistenceMode=None` 时 DS 不依赖它 |
| MySQL | 否 | 只在选择示例 Auth/DB 实现时需要 |

## 2. 安全边界

仓库可以提交 Role Catalog、示例配置结构、构建模板、服务模板和文档。以下内容必须保存在仓库外的 MiniCore Deploy 应用数据目录、目标服务器本地配置或秘密管理系统：

- 真实服务器地址、SSH 用户和私钥路径；
- 私钥内容、密码、Token 和连接字符串；
- 生产环境实例拓扑与发布历史；
- 证书私钥和云厂商凭据。

SSH 主机必须固定人工确认过的指纹。管理 Token 由工具在目标服务器生成，只允许 ServerCtl 在该主机通过回环管理端使用，不回传到桌面应用。

## 3. 准备桌面应用

构建或取得以下任一应用：

```text
Tools/MiniCore.Deploy/Artifacts/Desktop/win-x64/MiniCore.Deploy.Desktop.exe
Tools/MiniCore.Deploy/Artifacts/Desktop/osx-<arch>/MiniCore Deploy.app
```

首次打开后配置：

1. Unity 可执行程序、项目根目录和仓库外制品目录；
2. 客户端与 Dedicated Server 的显式 Bootstrap 场景；
3. 开发、测试或生产环境与统一 ReleaseVersion；
4. Linux/Windows 主机、SSH、固定指纹、部署根目录和运行账户；
5. 服务拓扑、实例 ID、Role、端口和自动重启策略；
6. 需要构建的 DS、客户端、Auth 与 DB 目标。

界面会检测当前 Unity 的平台模块并禁用不可用目标。若 Unity Editor 正在打开同一项目，一键构建会停止并要求先关闭，不会强制结束编辑器。

## 4. 定义项目 Role

项目在 Server 热更新程序集定义稳定 `ulong` Role，并使用 `ServerRoleDefinitionAttribute` 提供稳定键。已经发布的位值和键不得复用。

运行 Role Catalog 生成后，确认：

```text
Server/DedicatedServer/Config/ServerRoleCatalog.json
Assets/Scripts/MiniCore/HotUpdate/Client/Generated/PublicServiceIds.Generated.cs
```

只有 `clientDiscoverable` 服务进入客户端常量。新增 Role 需要重建 Server 热更新程序集和 DS 制品，但不需要修改 MiniCore AOT Runtime，也不要求全环境同时停服；是否能滚动发布由控制协议兼容性决定。

## 5. 配置主机与实例

Linux 默认根目录 `/opt/minicore`，Windows 默认根目录 `C:\ProgramData\MiniCore`。每个实例需要：

- 环境内唯一 InstanceId；
- 承载主机；
- Coordinator、普通 DS、Auth、DB 或 StaticContent 组件类型；
- 一个或多个稳定 Role 键；
- 不冲突的 Inner、Outer 和管理端口；
- 自动重启策略；
- Auth 的 HTTP 监听、内网公布地址、客户端公开 URL 与账号库参数；
- DB 的内网 RPC 监听/公布地址、并发上限与游戏库参数。

Auth/DB 的数据库参数包括地址、端口、数据库名称、账号、当前会话密码和 SSL 模式。密码不会保存到方案或日志，重新打开应用后必须再次输入。Coordinator 地址由当前拓扑自动写入各服务配置，不需要重复填写。

推荐先使用界面预设：

- “生产默认：独立 Coordinator”；
- “小项目：单机一体化”。

预设只生成可编辑拓扑，不把 MiniBomber 的 Lobby/Match/Game 固定进框架。

## 6. 选择发布方式

| 操作 | 是否重新构建 | 主要影响 |
| --- | --- | --- |
| 首次完整发布 | 是 | 构建、上传、安装服务并按依赖启动 |
| 全量版本更新 | 是 | 对整个环境执行安全滚动或维护窗口更新 |
| 业务内容更新 | 是 | 构建当前目标 HotUpdate/YooAsset 和相关制品 |
| 维护窗口全停更新 | 是 | 控制协议不兼容或无冗余时，人工确认后全停并统一切换 |
| 横向扩容 | 否 | 使用环境当前制品新增实例，不影响现有实例 |
| 配置更新 | 否 | 生成新配置哈希，只重启受影响实例 |
| 单实例修复 | 否 | 修复制品、配置、服务定义或运行状态差异 |
| 回滚 | 否 | 切换到历史完整 ReleaseManifest |
| 下线实例 | 否 | Drain、确认、停止和注销，不删除数据 |

执行前必须生成计划预览。计划列出新增、上传、切换、停止、启动、风险确认和未执行条件；配置变化后必须重新预览。

## 7. 自动构建规则

一键构建会依次完成代码生成、HybridCLR、YooAsset、Unity Player、可选 .NET 服务、SHA-256 和 `ReleaseManifest.json`。

- 首次/全量发布：完整 GenerateAll；
- 业务发布：当前目标 HotUpdate 编译与 YooAsset 构建；
- DS：Linux x64、Windows x64；
- 客户端：Windows、macOS、Android APK/AAB、WebGL；
- Auth/DB：按目标系统自包含 `dotnet publish`。

生产环境要求 Git 工作区干净。开发和测试允许脏工作区，但 Manifest 会记录提交号和差异 SHA-256。任一平台失败时，其他平台的成功结果仍保留，整体明确显示部分失败。

## 8. 外部实例配置

实例配置不进入 DS 制品，也不在构建后修改 StreamingAssets。工具在目标服务器生成：

```text
instances/<instance-id>/config/MiniCoreServerRuntime.json
instances/<instance-id>/config/management.token
instances/<instance-id>/logs/
```

DS 服务使用：

```text
--minicore-config <absolute-path>/MiniCoreServerRuntime.json
```

同一版本 DS 制品可被多个实例共享。每个实例仅有不同的配置、端口、日志、服务名和 `current/<instance-id>` 指针。

## 9. 首次发布顺序

MiniCore Deploy 自动执行：

1. SSH、指纹、系统、架构、磁盘、权限和端口预检；
2. 上传统一 Release Bundle 并校验哈希；
3. 写入外部配置和本地 Token；
4. 安装 systemd 或 Windows ServiceHost；
5. 启动 Coordinator 并等待 Ready；
6. 按启用情况启动 DB 与 Auth；
7. 启动业务 DS，检查版本、配置哈希和 Coordinator 注册；
8. 发布 WebGL/YooAsset 静态目录；
9. 写入远程状态和本地历史。

启用 Auth/DB 时，计划会先校验账号库或游戏库参数完整。执行器为每个实例生成服务实际读取的 `appsettings.json`，不会在结果与历史日志中输出密码或连接字符串；Linux 配置文件权限设为 `0600`。

数据库 Migration 不在此流程内。DatabaseServer 参与构建时，界面要求对当前 `ReleaseVersion` 单独确认迁移评审，并把迁移源码 SHA-256 写入 Manifest；未确认时计划无法生成。备份、数据库连接和 Migration 执行仍由项目单独完成。

## 10. 滚动更新与停服确认

冗余业务实例会逐个执行：

```text
Drain -> 等待排空 -> 停止 -> 切换 -> 启动 -> 健康检查 -> 重新注册
```

以下步骤必须人工确认：

- 更新 Coordinator；
- 更新某 Role 的最后一个实例；
- Drain 超时；
- 业务仍报告玩家、房间、比赛或持久化任务；
- 控制协议不兼容或更新将造成实际停服。

Coordinator 默认最后更新。稳定状态只允许一个 ReleaseVersion；兼容滚动期间可以短暂并存旧、新版本，操作结束后必须收敛。

## 11. 回滚与中断恢复

每个计划步骤写入仓库外 JSONL 历史，记录主机、实例、耗时、状态、错误码、日志和恢复建议。已经预览的计划也保存在仓库外；应用重启后仅恢复配置指纹相同的计划。状态机跳过已完成的非幂等步骤，但强制重跑预检和制品校验。

回滚切换到上一份完整 Manifest。控制协议不兼容时要求维护窗口。配置与日志默认保留；下线和回滚都不自动删除数据。

## 12. 第一版不负责的事项

MiniCore Deploy v1 不购买或创建云资源，不配置 DNS/TLS/CDN/反向代理/安全组，不执行数据库迁移，不签名或公证 macOS App，也不引入 Nomad、Ansible、Docker 或 Kubernetes。

发布执行包含进程健康、Ready、Coordinator 注册和 Drain 状态检查，但不运行单元测试、集成测试、回环测试或自动化玩法验证。

## 13. 故障处理

优先查看执行中心中的失败主机、失败步骤、错误码、日志位置和恢复建议。常见执行前拦截包括：

| 现象 | 处理 |
| --- | --- |
| Unity 平台目标不可选 | 在 Unity Hub 安装对应平台模块，然后重新选择 Unity 路径 |
| 提示项目被占用 | 保存并关闭正在打开该项目的 Unity Editor |
| 生产工作区不干净 | 审查并提交代码与生成文件后重新生成计划 |
| SSH 主机指纹不匹配 | 停止连接，人工核对服务器密钥，不自动接受新指纹 |
| 端口或 InstanceId 冲突 | 在实例配置中分配唯一值后重新预览 |
| Drain 未完成 | 查看业务阻塞项；确认不会影响在线玩家后再决定是否继续 |
| 控制协议不兼容 | 使用维护窗口全停计划，不强制滚动 |
| 存在数据库迁移 | 退出应用发布，单独完成迁移评审与操作 |

## 14. 最终检查表

- [ ] Auth/DB 的启用状态符合业务需要，未启用时不阻塞 Coordinator/DS。
- [ ] 启用 Auth/DB 时，账号库/游戏库参数完整，当前会话密码已经输入且数据库安全组由外部完成。
- [ ] Role 键与位值稳定，客户端只包含允许发现的 ServiceId。
- [ ] 生产工作区干净，ReleaseVersion 明确。
- [ ] DatabaseServer 参与时，当前 ReleaseVersion 的迁移已经单独评审并确认。
- [ ] Unity 模块、场景、主机指纹、端口和磁盘已在预检通过。
- [ ] 计划预览与当前配置指纹一致。
- [ ] 制品具有 Manifest 与 SHA-256，没有实例配置或秘密。
- [ ] 新版本先暂存并校验，没有覆盖运行中文件。
- [ ] 风险步骤经过人工确认，Drain 阻塞项已经处理。
- [ ] 环境最终收敛到一个 ReleaseVersion。
- [ ] 上一完整 Manifest 与私有配置仍可回滚。
- [ ] 发布历史不包含密码、私钥、Token 或完整连接密钥。
