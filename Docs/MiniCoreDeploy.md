# MiniCore Deploy 自动构建与发布

本文说明 `Tools/MiniCore.Deploy` 中已经实现的第一版桌面发布系统。它负责 MiniCore 服务器的构建、打包、上传、校验、解压、运行配置生成、版本切换、服务安装与启停、深度健康检查、续跑、自动恢复、回滚和发布记录。它不包含 AI Agent，也不负责 Caddy/Nginx、HTTPS/WSS、CORS、OSS/CDN、DNS、安全组/防火墙、数据库备份与 Migration 执行或客户端商店/下载站发布。

相关边界见[多 Role 与独立服务架构](DedicatedServerArchitecture.md)、[打包与热更新流程](BuildAndHotUpdateWorkflow.md)和[框架部署入门](FrameworkDeploymentGettingStarted.md)。

## 1. 应用组成

```text
Tools/MiniCore.Deploy/
├── MiniCore.Deploy.Core            发布模型、差异计划、状态机与恢复
├── MiniCore.Deploy.Infrastructure  Unity、Git、SSH/SFTP、哈希和服务管理
├── MiniCore.Deploy.Desktop         Avalonia 跨平台桌面界面
├── MiniCore.Deploy.ServiceHost     Windows 服务进程包装器
├── MiniCore.ServerCtl              DS 本机管理命令行
└── Packaging                       Windows/macOS 自包含打包入口
```

应用基于 .NET 10 与 Avalonia 12.1.1，不使用浏览器或 Unity EditorWindow。Windows 输出自包含 EXE；macOS 输出自包含 App，第一版不包含签名、公证或商店上传。

## 2. 构建桌面应用

在 `Tools/MiniCore.Deploy` 目录执行：

```bash
dotnet build MiniCore.Deploy.slnx --configuration Debug
bash Packaging/publish-desktop.sh
```

输出位置：

```text
Artifacts/Desktop/win-x64/MiniCore.Deploy.Desktop.exe
Artifacts/Desktop/osx-<arch>/MiniCore Deploy.app
```

发布脚本生成自包含单文件程序。macOS App 首次从未签名来源启动时可能需要操作系统允许；生产分发前应在项目自己的签名流水线中完成签名与公证。

## 3. 桌面界面

界面依次包含以下页面：

1. 主机管理：Linux/Windows、主机描述、SSH 登录地址、VPC/内网公布地址、登录用户、私钥/密码认证、主机指纹和部署目录。
2. 项目设置：Unity 可执行文件、项目目录、制品目录和显式启动场景。
3. 配置方案：自由创建、命名、复制、修改和删除彼此独立的方案。
4. 服务拓扑：新增 Coordinator、普通 DS、可选 Auth/DB 或静态内容实例。
5. 构建目标：Linux/Windows DS、Windows/macOS/Android/WebGL 客户端和可选 .NET 服务。
6. 发布方式：首次发布、全量更新、业务更新、维护窗口全停更新、扩容、配置更新、修复、回滚或下线。
7. 计划预览：展示所有步骤、目标主机和需要人工确认的风险。
8. 执行中心：显示等待、执行、成功、失败、跳过、错误码和恢复建议。
9. 发布历史：按环境、版本、实例、操作和结果筛选计划，查看失败步骤、重试次数、前一版本和自动回滚结果。
10. 帮助与文档：说明字段用途、推荐设置和安全边界。

Unity 路径改变后，界面直接检查 `PlaybackEngines`，未安装的平台目标会禁用；执行端还会再次拦截缺失模块。

服务拓扑发生变化时，构建目标立即重算：新增或切换为启用的 Auth/DB 实例会立刻开放相应目标；禁用、删除或改成其他组件后，会同时禁用并清除已经失效的 Build/Publish 选择。“仅服务端”表示选择当前 Unity 安装与拓扑共同允许的全部服务端制品，包括 DS 以及已启用的 Auth/DB，不是“仅 DS”。

“一键构建并发布”只能执行已经展示且配置指纹未改变的计划。执行前会重新计算完整配置指纹，任何未预览变化都会被拒绝；应用重启后只有指纹完全相同的最近计划可以恢复。

## 4. 配置保存位置

项目仓库只保存通用 Role Catalog、构建桥接和发布模板。以下信息保存在操作系统应用数据目录：

- 环境与主机地址；
- SSH 用户和私钥路径；
- 实例拓扑；
- 发布计划、步骤结果和恢复历史。

SSH 私钥认证与账号密码认证都必须填写 SSH 登录用户名。私钥是“如何证明客户端身份”，用户名是“以服务器上的哪个账号登录”，二者不是替代关系。阿里云 Linux 实例通常使用创建实例时选择的 `root` 或 `ecs-user`；建议生产环境使用 `ecs-user` 和密钥认证。Linux 首版会让 systemd 服务明确以该登录用户运行：填写 `ecs-user` 时不会把 DS 自动改成 root，只有填写 `root` 才以 root 运行。工具只保存私钥路径，不复制私钥内容；SSH 密码和加密私钥口令只驻留当前应用会话，并通过 JSON 忽略规则禁止写入方案。阿里云官方同时支持密码和密钥对登录，但推荐使用密钥和非 root 用户，详见[实例登录凭证管理](https://help.aliyun.com/zh/ecs/user-guide/instance-logon-credential-management)和[避免使用 root 登录](https://help.aliyun.com/zh/ecs/user-guide/avoid-logging-in-to-the-instance-as-root)。

主机指纹是 SSH 服务器公钥的 SHA-256 摘要，用来确认“当前地址确实还是原来的服务器”，不是用户需要计算或背诵的密码。界面的“获取并确认”会在 SSH 认证前自动读取指纹，并要求与阿里云控制台、服务器初始化记录或其他可信渠道核对；确认后随方案固定保存。后续指纹改变时连接会被拒绝，以防中间人冒充服务器。阿里云也明确要求首次连接核对主机指纹，见[使用 SSH 密钥对连接 Linux 实例](https://help.aliyun.com/zh/ecs/user-guide/connect-to-a-linux-instance-by-using-an-ssh-key-pair)。

密码、私钥内容、管理 Token 和数据库连接字符串不得进入项目、应用日志或本地方案文件。本机的 Unity、项目、输出、场景和私钥路径可通过输入框末尾的“…”打开系统选择窗。部署根目录和静态发布目录属于目标服务器路径，macOS 本地选择窗无法浏览它们，因此必须填写目标服务器绝对路径。

Auth 与 DB 不再使用一个含义不清的“数据库密钥环境文件”。服务拓扑直接编辑数据库地址、端口、数据库名称、账号、当前会话密码和 SSL 模式。账号、地址与库名可随方案保存；密码只驻留当前应用会话，并参与计划指纹以阻止“预览后换密码”绕过重新预览。执行时工具把全部参数安全转义后写入目标实例自己的 `appsettings.json`，Linux 权限设为 `0600`，不会把连接字符串写入步骤结果或历史日志。

主机卡片中的“测试连接”会使用当前 SSH 地址、登录用户、认证方式和固定主机指纹，依次建立 SSH 与 SFTP 连接。只有两种连接都成功才显示通过；该操作不会执行构建、发布或远程配置修改。主机的 `PrivateAddress` 是另一项独立配置：它保存服务间互访使用的 VPC IP 或内网 DNS，不参与 SSH 连接测试，也不会自动成为任何进程的监听地址。

## 5. Unity 自动构建桥接

桌面工具使用独立 Unity BatchMode 进程，不修改 `EditorBuildSettings`。构建前会检查：

- Unity 路径和平台模块；
- 项目目录及 `Temp/UnityLockfile` / `Library/EditorInstance.json`；
- 生产环境 Git 工作区是否干净；
- 开发、测试环境的提交号与差异 SHA-256；
- 显式客户端和服务端场景是否存在。

如果该项目正在 Unity Editor 中打开，构建停止并提示用户关闭，不会强制结束 Editor。

构建分成彼此独立的 Unity 进程：

1. 生成 Role Catalog、Proto、Startup 和 UI Registry；
2. 等待脚本域更新后生成 Handler 注册表；
3. 对每个已选平台分别执行 HybridCLR、YooAsset 与 `BuildPipeline.BuildPlayer`。

首次发布和全量发布执行完整 HybridCLR 生成；业务发布使用当前平台热更新编译。每个平台具有独立 JSON 请求、JSON 结果和日志；已经成功的平台产物不会因为另一平台失败而被伪装成失败或删除。

## 6. 不可变制品与发布清单

每个版本目录包含按目标压缩的制品和 `ReleaseManifest.json`。Manifest 记录：

- ReleaseVersion 与控制协议版本；
- Git 指纹和构建类型；
- Auth/DB Migration 源码 SHA-256 与已评审的 ReleaseVersion；
- 每个目标的压缩包、压缩长度、解压长度和 SHA-256；
- 完整 Release 标记和整份 Release 内容摘要；
- 制品兼容信息。

Dedicated Server Player、HotUpdate、YooAsset 与 `ServerRoleCatalog.json` 组成不可变制品。实例地址、端口、Role、日志和 Token 不写入制品。多个实例可以引用同一个只读版本目录，只拥有不同的外部配置与进程服务名。

Linux 目标目录：

```text
/opt/minicore/
├── releases/<release-version>/<component>/
├── instances/<instance-id>/config/
├── instances/<instance-id>/logs/
├── state/
└── current/<instance-id> -> releases/<release-version>/<component>/
```

Windows 默认使用 `C:\ProgramData\MiniCore` 的等价结构。本地打包完成后，工具会在上传前重新核对 Manifest 中每个压缩包的大小与 SHA-256。远端上传使用可取消的 `.part` 文件，解压进入计划独占的临时目录，完成空间、哈希和完整性校验后才原子改名为最终 Release 目录，再切换 `current`；不会覆盖正在运行的文件，也不会覆盖已有 Release。

同一个 `ReleaseVersion` 只能对应一份内容：目录不存在时创建；版本与整份内容摘要都相同可以复用；同版本不同摘要必须拒绝。磁盘预检按压缩包、解压空间、临时目录、旧版本保留和安全余量动态计算，不再使用固定阈值。实例 ID 转换为 systemd/Windows 服务名后还会再次做唯一性校验，`foo_bar`、`foo-bar` 或仅大小写不同等碰撞会在计划阶段失败。

`ContentOnly` 不能作为完整 DS 或 WebGL Release 激活。当前模型还没有记录可供合成的完整基线版本，因此只允许生成内容构建结果，不允许进入远程发布计划；将来实现“基于明确完整基线合成新的完整不可变版本”之前，不保留危险的目录替换旧行为。

## 7. 实例外部配置

DS 必须使用显式绝对路径启动：

```text
MiniCoreServer --minicore-config <absolute-path>/MiniCoreServerRuntime.json
```

配置包含环境、实例、版本、Role、Coordinator、监听、公布地址、管理端口、日志、持久化模式、配置版本和配置 SHA-256。运行时会校验配置哈希和随制品发布的 Role Catalog。服务拓扑界面不再从主机 SSH 地址猜测运行时网络地址，而是明确区分：

- `内网监听地址`：进程在本机绑定的地址，常用 `0.0.0.0`。
- `内网公布地址覆盖`：Coordinator、DS、Auth 或 DB 提供给其他服务器访问的 VPC IP/DNS；实例留空时继承所选主机的 `PrivateAddress`。
- `外网监听地址/端口/路径`：Coordinator 或 DS 实际监听 WebSocket 的位置。
- `外网公布地址`：客户端真正使用的完整 `ws://` 或 `wss://` 地址，可以与监听位置不同，例如经过负载均衡、域名和 TLS 终止。
- `管理端口`：只监听 `127.0.0.1`，供服务器本地 `ServerCtl` 使用。

不同组件只显示自己实际使用的字段：

| 组件 | Role | 运行时网络配置 |
| --- | --- | --- |
| Coordinator | 必须含框架 `Coordinator`，也可承载业务 Role | 内网/外网监听与公布地址、管理端口 |
| DedicatedServer | 一个或多个项目业务 Role | 内网/外网监听与公布地址、管理端口、可选 DB 依赖 |
| AuthenticationServer | 不使用 DS Role | HTTP 监听/内网公布地址、客户端访问 URL、账号库参数 |
| DatabaseServer | 不使用项目业务 Role；运行时自动使用框架固定 Database 服务标识 | 内网 RPC 监听/公布地址、并发上限、游戏库参数 |
| StaticContent | 无 Role、无进程、无端口 | 远程静态目录版本指针和可选外部 HTTP/HTTPS URL |

Auth 的客户端访问 URL 是客户端访问认证 API 的入口，内网公布地址是反向代理或服务器间访问入口；它从当前 Coordinator 拓扑自动取得外网 WebSocket URL。DB 从当前拓扑自动取得 Coordinator 内网地址，并以框架固定的 Database 服务标识注册。工具分别生成 Auth 与 DB 实际读取的 `appsettings.json`，两者不会误用同一套数据库。

### 7.1 主机继承与实例覆盖

实例的 `InnerAdvertisedHost` 留空具有明确语义，不代表缺失配置：执行器会使用当前所选主机的 `PrivateAddress`。切换主机后，留空实例自动跟随新主机；一旦填写实例覆盖值，切换主机不会静默修改它。清空覆盖值即可恢复跟随。计划预览和最终配置生成使用同一个解析规则，因此界面显示、配置哈希与服务器实际读取值不会分叉。

`PrivateAddress` 只解决“其他服务器连接到哪里”，不能作为 `ListenHost` 的默认值。跨主机场景通常让进程监听 `0.0.0.0`，再公布主机 VPC 地址；仅允许同机反向代理访问的 HTTP/WebSocket 监听可以显式使用 `127.0.0.1`。内网公布地址不能使用 localhost、回环地址或 `0.0.0.0`。

### 7.2 同机反向代理与多主机示例

同机 Caddy 反代示例（MiniCore Deploy 不安装或修改 Caddy）：

| 配置项 | 示例值 | 含义 |
| --- | --- | --- |
| 主机 SSH 地址 | `<ssh-public-address>` | 只供部署工具连接 |
| 主机 VPC 地址 | `<server-vpc-address>` | 实例留空覆盖时自动继承 |
| Coordinator 外网监听 | `127.0.0.1:<coordinator-port>` | 只接受同机 Caddy 转发 |
| Coordinator 客户端公布 URL | `wss://coordinator.example.com/minicore` | 客户端实际连接入口 |
| Auth HTTP 监听 | `127.0.0.1:<auth-port>` | 只接受同机 Caddy 转发 |
| Auth 客户端访问 URL | `https://auth.example.com` | 客户端实际访问入口 |

多 ECS/VPC 示例：每台 `HostDefinition` 分别填写自己的 `<host-vpc-address>`；Coordinator、DS、Auth、DB 的实例覆盖保持空白，便会随所选主机继承。服务间监听通常使用 `0.0.0.0`，服务发现和 DB 配置写入继承后的 VPC 地址。面向客户端的 Coordinator/Auth URL 仍填写负载均衡或反向代理后的 `wss://coordinator.example.com/...` 与 `https://auth.example.com`；DNS、TLS、反向代理和安全组仍由工具外部管理。

配置方案提供“生产公网地址安全校验”。开启后，客户端公布地址必须使用公网 `https://` 或 `wss://` 绝对地址；localhost、`127.0.0.0/8`、`0.0.0.0`、私网 IP 和未加密 HTTP/WS 会阻止生成计划。关闭时可用于本地联调，但预检步骤会明确标记风险并等待人工确认。Coordinator 与 Auth 同时发布时，两者面向客户端的入口都受此规则约束。

`Server/DedicatedServer/Config/MiniCoreServerRuntime.json` 只是本地开发结构示例，不再注入 Player。构建时只允许把与实例无关的 `ServerRoleCatalog.json` 注入 Dedicated Server 的 StreamingAssets。

## 8. 发布操作

### 8.1 首次完整发布

工具从预检开始在环境的稳定控制主机上持有远程部署互斥锁，锁记录计划 ID、操作人员和开始时间，直到远程/本地状态持久化或失败清理后才释放。另一份工具实例不能同时切换同一环境；超过 12 小时的占用会明确标记为疑似陈旧锁，但工具不会自动抢锁。

随后依次预检 SSH、主机指纹、系统、动态磁盘预算、权限和端口；构建统一版本；上传和校验制品；生成 DS 外部配置、Auth/DB `appsettings.json` 和本机 Token；安装 systemd 或 Windows ServiceHost；启动 Coordinator；按启用情况启动 DB/Auth；启动业务 DS；执行深度健康检查；最后写入远程状态和本地历史。

Auth 与 DB 都是可选组件。没有配置它们时，Coordinator 和普通 DS 仍可构建、启动并完成服务发现。只有业务明确选择数据库持久化模式时，业务 DS 才等待 DatabaseServer。

### 8.2 横向扩容

扩容不重新构建。工具使用环境当前 `ReleaseVersion` 的缓存制品，为新实例生成唯一 InstanceId、端口、配置和服务名，启动并等待健康与 Coordinator 注册。现有实例不会停止或重启。

### 8.3 滚动更新

稳定环境只允许一个 ReleaseVersion。兼容更新期间允许旧、新版暂时共存，完成后所有启用实例必须收敛到目标版本。

普通冗余实例按以下顺序更新：

```text
drain -> 等待业务排空 -> 停止 -> 切换版本/配置 -> 启动 -> 健康与重新注册
```

Coordinator 默认最后更新。更新 Coordinator、某 Role 最后一个实例、Drain 超时、业务仍有玩家/房间/比赛/持久化任务或实际停服时，执行中心要求人工确认。控制协议不兼容时不得滚动更新，应生成维护窗口计划。

`MaintenanceRelease` 是显式的维护窗口全停操作：先按 DS、Auth、DB、Coordinator 顺序 Drain/停止，再按 Coordinator、DB、Auth、DS 顺序统一切换和启动。停止与最后实例健康步骤都需要人工确认。

### 8.4 其他操作

- 配置更新：配置版本独立于程序 ReleaseVersion；纯应用配置变化更新配置并重启，运行用户、自动重启或启动参数变化还会刷新 systemd/Windows 服务定义。
- 单实例修复：排空并停止实例，重新部署、刷新服务定义、启动并完成深度健康检查；不会对仍在运行的旧进程只调用一次 `start`。
- 回滚：切换到历史完整 Manifest；协议不兼容时要求维护窗口。
- 下线：Drain、确认、停止并注销服务；保留配置与日志，不自动删除数据。
- 静态发布：上传版本目录并原子切换，不配置 CDN、TLS 或 Web Server。
- DB Migration：只要 DatabaseServer 参与构建，当前 `ReleaseVersion` 就必须单独勾选迁移评审；工具记录迁移源码指纹并阻止未评审版本，不自动连接数据库或执行迁移。

## 9. 生命周期管理

Dedicated Server 创建仅监听 `127.0.0.1` 的管理端。随制品发布的 `MiniCore.ServerCtl` 从服务器本机配置读取端口和 Token 文件：

```text
MiniCore.ServerCtl --config <absolute-path> status
MiniCore.ServerCtl --config <absolute-path> health
MiniCore.ServerCtl --config <absolute-path> drain
MiniCore.ServerCtl --config <absolute-path> drain-status
MiniCore.ServerCtl --config <absolute-path> shutdown
```

管理 Token 在目标服务器首次创建，权限受限，不上传回桌面工具。部署工具通过 SSH 在目标主机本地运行 ServerCtl，因此 Token 不离开服务器，管理端口也不需要对公网开放。

框架通过 `IDedicatedServerDrainParticipant` 只定义“停止接收新工作、报告剩余活动量和阻塞原因”。MiniBomber 提供玩家、房间、比赛与匹配队列的示例实现；其他项目应实现自己的业务排空规则。

## 10. systemd 与 Windows 服务

Linux 为每个实例安装独立 systemd unit，使用实例级服务名、稳定 `current` 链接和 `--minicore-config` 参数，并通过 `User=` 明确使用主机配置中的 SSH 登录用户。首次预检可通过免交互 `sudo` 创建部署根目录并把目录所有权交给该用户；异常退出是否重启由实例配置决定。

Windows 使用 `MiniCore.Deploy.ServiceHost` 托管 Unity DS 或其他子进程。每个 Windows 服务拥有独立描述文件、工作目录、日志目录和安全关闭命令；升级时切换实例的 `current` Junction，而不是覆盖正在运行的 EXE。

## 11. 中断、错误与恢复

每一步产生 `StepResult`，包含计划、时间、环境、操作人员、操作、主机、实例、目标/前一版本、耗时、状态、错误码、原因、重试、日志位置和恢复建议。每个步骤还会在应用数据目录生成独立结构化 JSON 日志；写入 JSONL 历史和步骤日志前统一脱敏密码、Token、私钥、口令与连接密钥。执行中心可直接打开步骤日志，历史页解析结果而不是只展示文件路径。

重新执行同一计划时，状态机加载已经成功的步骤并跳过；预检与制品暂存属于安全幂等步骤，会强制重跑以重新核对远程状态、协议和 SHA-256。已经完成的构建、服务安装或版本切换不会盲目重放。失败结果会保留成功制品和未执行步骤，不会显示为整体成功。

首次安装续跑时，端口预检会区分“本计划已经安装并启动的目标服务”和真正外部进程占用。取消构建会终止本地子进程；取消上传会关闭 SSH/SFTP、清理 `.part`；版本指针和服务切换属于不可中断安全段，界面会显示“正在取消，等待当前安全步骤完成”，待该段完成后停止。

Auth 只有在账号库可连接且 Coordinator 地址有效时才通过 `/ready`。DatabaseServer 只有在游戏库可连接、业务 RPC Ping/Pong 完成、Coordinator 注册并维持新鲜心跳时才产生就绪文件。DS 继续通过本机 `ServerCtl health` 验证配置版本、注册和框架健康。启动或深度健康检查最终失败时，部署器会停止新进程，恢复上一个版本指针、旧配置与旧服务定义，重新启动并验证；自动补偿失败时记录可执行的人工恢复步骤。首次安装没有前一版本，因此只能报告明确的人工恢复边界。

## 12. 第一版边界

第一版不负责：

- 云服务器购买、扩缩容 API 或安全组；
- DNS、证书、TLS、反向代理和 CDN；
- 数据库创建、备份或 Migration 执行；
- macOS 签名、公证和应用商店上传；
- Nomad、Ansible、Docker 或 Kubernetes；
- AI Agent 自动诊断与修复；
- 单元测试、集成测试、回环测试和运行时自动化测试。

这些边界不会阻止后续扩展：核心执行步骤、输入、结果和恢复建议都已结构化，未来 AI Agent 应调用同一套计划与执行接口，而不是绕过计划预览直接生成远程命令。

## 13. 实现与构建记录

### 2026-08-24 主机 VPC 继承、客户端地址安全与动态拓扑同步

- 主机增加独立 `PrivateAddress`；实例内网公布地址留空时继承所选主机，实例显式覆盖在切换主机时保持不变。
- 计划校验和 DS/Auth/DB 配置生成统一解析有效内网公布地址；`ListenHost` 保持独立，不会从 VPC 地址推导。
- 增加可选的生产公网端点安全策略；非生产方案使用私网、本机或未加密客户端地址时，预检必须人工确认。
- Auth/DB 实例新增、删除、启用、禁用或切换组件后，构建目标立即刷新并清除失效选择；“仅服务端”选择当前全部可用服务端目标。
- 增加无第三方测试依赖的自动化回归检查源码，覆盖主机继承、主机切换、实例覆盖、URL 风险和动态 Auth/DB 拓扑；按本次约束只编译，不执行测试或应用运行验证。

### 2026-08-23 生产发布安全、恢复与审计整改

- 增加环境级远程互斥锁、本地/远端不可变 Release 提交、同版本异内容拒绝、上传前本地复核、动态磁盘预算和服务名碰撞检查。
- 禁止把 `ContentOnly` 半包激活为完整服务版本；Auth/DB 构建与发布目标必须和启用拓扑一致；MaintenanceRelease 执行完整生成链。
- 修正首次安装续跑、Repair、ConfigurationUpdate、RemoveInstance 和安全取消语义；敏感配置从内存上传，远端临时文件先限制权限再写入。
- Auth/DatabaseServer 增加真实依赖就绪检查；启动失败自动恢复上一程序、配置和服务定义；`configVersion` 与 `ReleaseVersion` 解耦，DS 采用跨 JSON 库一致的固定规范字节计算哈希。
- 每个步骤写入结构化脱敏日志；执行中心可打开日志，历史页支持按环境、版本、实例、操作和结果筛选，并展示重试、失败步骤、前一版本和回滚结果。
- SSH 支持仅当前会话使用的加密私钥口令，不改变无口令私钥和密码认证行为。
- MiniCore Server 与 MiniCore Deploy 全部工程完成编译检查；按约束未执行任何测试或运行时自动化验证。

### 2026-08-23 Auth/DB 参数与主机连通性修正

- 依据实际 MiniBomber 部署配置，删除 Auth/DB“密钥环境文件”抽象，改为账号库与游戏库各自的地址、端口、库名、账号、当前会话密码和 SSL 模式。
- Auth 增加独立内网公布地址；监听地址、内网入口和客户端公开 URL 不再混为一个字段。
- 部署执行器按组件生成实际可读取的 `appsettings.json`；Coordinator 地址从拓扑自动写入，不要求重复输入。
- 数据库密码不持久化、不进入日志，但会参与计划指纹；Linux 远程配置以 `0600` 权限保存。
- 主机页增加 SSH 与 SFTP 双连接测试，只验证当前连接配置，不触发构建、上传或发布。
- Deploy 五个工程编译为零警告、零错误，并重新生成 Windows x64 EXE 与 macOS arm64 App；按约束未运行任何测试或启动应用。

### 2026-08-23 SSH 与组件配置补全

- 主机配置增加仅展示的描述，并把原“运行用户”明确拆解为必填的 SSH 登录用户和可选认证方式。
- Linux systemd unit 明确以 SSH 登录用户运行；选择 `ecs-user` 时不再隐式以 root 启动服务。
- SSH 支持本机私钥与当前会话密码；密码不持久化，私钥只保存路径。
- 主机指纹改为工具自动读取、用户核对后固定，不再要求手工输入 SHA-256。
- 所有本机路径增加系统文件或目录选择窗；服务器路径保留明确的远程绝对路径提示。
- 端口控件和计划层同时限制 `1-65535` 正整数，并检查同一主机端口冲突。
- Coordinator/DS、Auth、DB 与 StaticContent 使用各自的配置表单；Auth/DB/StaticContent 不再显示业务 Role。
- DS 恢复独立的内外网监听、公布地址与 WebSocket 路径；Auth 使用 HTTP 配置，DB 使用内网 RPC 配置。
- Linux 与 Windows 执行后端都会把界面配置的网络参数写入对应实例配置。
- MiniCore Deploy 解决方案编译为零警告、零错误；按约束未运行任何自动化测试。

### 2026-08-23 初版实现与构建

- 新增 .NET 10 + Avalonia 12.1.1 桌面发布解决方案、SSH/SFTP 后端、Linux systemd、Windows ServiceHost 和 ServerCtl。
- Role 从固定框架业务枚举改为稳定 `ulong` 位值、字符串键和项目 Role Catalog；MiniBomber Role 只是业务示例。
- Dedicated Server 改为外部实例配置、配置 SHA-256、回环管理端、Drain 扩展点和不可变制品。
- Control Proto 改为通用 `service_id` / `role_mask`，Coordinator 不理解 Lobby、Match 或 Game 语义。
- Unity 代码生成与脚本编译成功；MiniCore Server 与 MiniCore Deploy 解决方案编译为零错误。
- 发布计划快照保存在仓库外，配置指纹一致时可在应用重启后继续；继续前强制重新预检和校验远程制品。
- 生成 Windows x64 自包含 EXE 与 macOS arm64 自包含 App。
- 本次按约束没有运行单元、集成、回环或运行时自动化测试。
