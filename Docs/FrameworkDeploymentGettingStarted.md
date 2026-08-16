# MiniCore 框架部署入门

本文面向第一次部署 MiniCore 的开发者，说明如何把一个使用 MiniCore 的游戏项目发布为客户端、Coordinator、多 Role Dedicated Server，以及可选的独立认证服务和数据库服务。

本文只描述框架流程和示例拓扑，不记录任何真实服务器 IP、域名、SSH 用户、数据库地址、云厂商资源 ID、本机用户名或生产目录。项目仓库中的配置只能包含示例值；真实部署资料必须保存在仓库之外。

MiniBomber 只是本教程使用的示例业务。更换游戏项目后，框架启动顺序、Role、服务发现、协议边界和发布原则保持一致，业务场景、Handler、数据库模型与认证接口由具体项目替换。

开始操作前先阅读 [AI 项目上下文](AI_CONTEXT.md)、[打包与热更新流程](BuildAndHotUpdateWorkflow.md) 和 [Dedicated Server 架构](DedicatedServerArchitecture.md)；示例业务细节见 [MiniBomber 全链路 Demo](Demos/MiniBomber.md)。

## 1. 先理解最终部署结构

最小联网拓扑：

```text
客户端
  ├── HTTPS → AuthenticationServer
  ├── WSS   → Coordinator（查询 Lobby）
  └── WSS   → GameCluster（Lobby + Match + Game）

服务器内网
  ├── GameCluster → Coordinator
  ├── DatabaseServer → Coordinator
  ├── GameCluster → DatabaseServer
  ├── AuthenticationServer → 账号数据库
  └── DatabaseServer → 游戏数据库
```

各进程职责：

| 进程 | 必需 | 职责 |
| --- | --- | --- |
| Coordinator | 联网模式必需 | 注册、心跳、服务目录、Ready/Draining 状态和服务查询；不转发业务 RPC |
| GameCluster | 联网玩法必需 | 使用同一 DS Player 承载 Lobby、Match、Game 中的一种或多种 Role |
| AuthenticationServer | 示例实现，可替换 | HTTP 注册与登录；登录成功后返回 Coordinator 外网地址 |
| DatabaseServer | 可选 | 使用 MiniCore Inner RPC 提供游戏数据 Load/Save |
| MySQL | 使用示例认证或数据库服务时需要 | 账号库与游戏业务库应分开管理 |
| 反向代理 | 公网 HTTPS/WSS 时需要 | TLS 终止，并把认证、Coordinator 和 GameCluster 路径转发到本机监听端口 |
| 静态资源/CDN | WebGL 或在线热更新时需要 | 托管客户端文件和 YooAsset `DefaultPackage` |

推荐的最小进程拆分：

```text
进程 1：roles = [Coordinator]
进程 2：roles = [Lobby, Match, Game]
进程 3：AuthenticationServer
进程 4：DatabaseServer（persistenceMode=Database 时）
```

Coordinator 与 GameCluster 使用同一份 Dedicated Server Player，只是运行时 JSON 不同。以后需要扩容时，可以继续复制这份 Player，把 Lobby、Match、Game 拆成不同进程或启动多个相同 Role 实例。

## 2. 仓库与生产环境的安全边界

### 2.1 可以提交到 Git

- 示例配置结构；
- `example.com`、`127.0.0.1` 等示例地址；
- 框架默认端口；
- systemd、Caddy、Nginx、Docker 等无真实身份信息的模板；
- 不含密码的发布脚本；
- 数据库 Migration 源码；
- 构建和验证说明。

### 2.2 禁止提交到 Git

- 真实公网 IP、内网 IP和域名；
- SSH 用户、SSH 别名、私钥路径和云主机实例 ID；
- RDS/数据库真实地址、账号、密码和连接字符串；
- Token、证书私钥、Cookie、访问密钥和云厂商凭据；
- 生产服务器的完整目录清单和安全组细节；
- 带真实值的 `appsettings.Production.json`、`.env` 或 DS 运行配置；
- 含敏感命令历史或日志的截图。

### 2.3 生产配置应该放在哪里

推荐把发布目录与配置目录分离：

```text
<deploy-root>/
├── releases/
│   └── <version>/                 只读程序文件
├── current -> releases/<version> 当前版本软链接
├── config/                        服务器本机配置，不进入 Git
└── backups/                       回滚配置和旧版本
```

具体路径由部署者决定。文档、源码和 AI 输出都应使用 `<deploy-root>`、`<version>`、`<ssh-target>` 等占位符，不应把某个人的真实值写回仓库。

.NET 生产密钥可以使用以下任一种方式：

1. 服务器本地、权限为 `600` 的环境变量文件；
2. systemd Credentials；
3. 云厂商 Secret Manager；
4. 服务器本地的 `appsettings.Production.json`，并确保它不位于 Git 工作区。

公开客户端必然可以看到它实际连接的认证域名，因此认证入口不是密码；但仓库仍应默认使用示例地址，由项目发布配置或 CI 在构建环境中注入真实值。

## 3. AI 接入约定

每次让 AI 协助部署时，先给出以下约束：

```text
你正在协助部署 MiniCore。
先阅读 Docs/AI_CONTEXT.md、Docs/FrameworkDeploymentGettingStarted.md、
Docs/BuildAndHotUpdateWorkflow.md 和 Docs/DedicatedServerArchitecture.md。

规则：
1. 先只读检查，不修改服务器。
2. 不输出、复制或保存密码、连接字符串、Token、私钥和真实地址。
3. 所有真实值只使用环境变量名或 <placeholder> 表示。
4. 每次只执行当前阶段；先给出检查结果和风险，再等待授权进行写操作。
5. 不重写已有 systemd、反向代理或数据库配置，除非当前任务明确要求。
6. 任何删除、覆盖、数据库迁移和停服操作都必须单独确认。
7. 发生错误立即停止，不连续尝试破坏性修复。
```

AI 每个阶段应输出四项：

```text
已发现：当前真实状态，但敏感值必须遮蔽
准备执行：本阶段命令和影响范围
完成标准：什么结果代表本阶段通过
失败回传：开发者需要提供哪些脱敏输出
```

开发者给 AI 日志时，应先替换：

```text
真实域名       → <public-domain>
真实公网 IP    → <public-ip>
真实内网 IP    → <private-ip>
SSH 用户/别名  → <ssh-target>
数据库地址     → <database-host>
账号与密码     → <redacted>
Token/证书内容 → <redacted>
```

## 4. 部署阶段总览

严格按以下顺序推进：

```text
阶段 A：确定部署范围与版本
阶段 B：检查源码、配置边界和编译状态
阶段 C：构建 Dedicated Server
阶段 D：发布可选 .NET 服务
阶段 E：构建客户端或在线热更新资源
阶段 F：生成制品清单和校验值
阶段 G：准备服务器目录与私有配置
阶段 H：安装或更新进程守护配置
阶段 I：按依赖顺序启动
阶段 J：检查端口、日志和服务发现
阶段 K：验证故障恢复
阶段 L：记录发布结果和回滚点
```

首次部署需要完成全部阶段。日常更新只执行与改动范围有关的构建阶段，但服务器暂存、校验、切换、验收和回滚准备仍不能跳过。

## 5. 阶段 A：确定部署范围与版本

### 5.1 判断需要发布什么

| 改动 | 需要发布 |
| --- | --- |
| `MiniCore.Runtime/Network/Server/Unity` 等 AOT 代码 | 重新构建受影响的完整 Player |
| Control/Control.Inner 协议 | 重新构建客户端或 DS Player，不能只发热更 DLL |
| 业务 Common/Outer/Inner 协议 | 在兼容前提下可随 HybridCLR 业务包发布 |
| `MiniCore.HotUpdate.Shared/Client/Server` | 对应运行目标的热更新程序集和 YooAsset 包 |
| DS Role 或监听地址 | 修改部署副本的运行时 JSON，不需要重新编译 |
| AuthenticationServer | 重新 `dotnet publish` 认证项目 |
| DatabaseServer 或数据库业务协议 | 先发布兼容 DBServer，再发布 DS 业务程序集 |
| 数据库模型 | 单独评审 Migration；应用发布不能默认自动执行 Migration |
| 客户端业务资源 | 构建并发布新的 YooAsset 包 |

版本号应由项目自行制定，例如 `0.1.0` 或 Git Commit SHA。不要把示例版本当成框架要求。

### 5.2 AI 检查提示

```text
请只读检查当前 Git 改动，按 AOT、Control 协议、业务热更新、
Dedicated Server、AuthenticationServer、DatabaseServer、客户端资源分类，
给出本次必须发布的制品清单。不要运行构建，不要修改文件。
```

完成标准：开发者拿到一份明确的制品清单，并知道哪些服务不需要重启。

## 6. 阶段 B：本地构建前检查

### 6.1 工程要求

1. 使用项目声明的 Unity 版本打开工程。
2. 等待 Unity 导入和 C# 编译完成。
3. Console 中不能有 C# 编译错误。
4. 修改过 Proto 时，执行：

```text
MiniCore > Protocol > Generate All
```

5. 修改过窗口 Prefab、View 或 Presenter 时，执行：

```text
MiniCore > UI > Generate Window Registry
```

6. 检查客户端只包含 Common/Outer/Shared/Client，DS 包含 Common/Outer/Inner/Shared/Server。
7. 检查 Control/Control.Inner 没有进入 HybridCLR 热更新 DLL 目录。
8. 检查服务端运行配置没有放进客户端 `Assets/StreamingAssets`。

### 6.2 不敏感的源码配置

仓库中应只保留模板：

```text
Server/DedicatedServer/Config/MiniCoreServerRuntime.json
Server/AuthenticationServer/appsettings.json
Server/DatabaseServer/appsettings.json
```

这些文件用于说明结构和本地开发，不得填写生产密钥或真实基础设施地址。

### 6.3 AI 检查提示

```text
请只读检查 MiniCore 的目标程序集清单、asmdef 约束、DS 配置注入、
客户端泄漏校验、Proto 生成结果和 UI Registry 生成结果。
只报告编译或配置边界问题，不运行 Player、YooAsset 或测试构建。
```

完成标准：没有 C# 编译错误，生成文件与源码一致，客户端不存在 Server/Inner/DS 配置泄漏。

## 7. 阶段 C：构建 Dedicated Server

### 7.1 选择目标

1. 打开 `File > Build Settings`。
2. 选择 Dedicated Server 的 Linux x86_64 目标。
3. 正式发布时关闭 `Development Build`。
4. Scenes In Build 只保留项目的 DS 启动场景。MiniBomber 示例为：

```text
Assets/Scenes/Demos/MiniBomber/ServerBootstrapScene.unity
```

### 7.2 完整生成

首次构建、切换平台、修改 AOT/Control、修改 HybridCLR 配置或清理过生成目录时，执行：

```text
MiniCore > Build > DefaultPackage > 完整生成 (Generate All + Build)
```

该菜单已经包含 HybridCLR Generate All，不需要再重复执行。

只修改兼容的业务热更新代码或业务资源，并且平台、AOT、Control 与 Development 设置都没有变化时，才可以执行：

```text
MiniCore > Build > DefaultPackage > 热更编译 (Compile Active Target + Build)
```

### 7.3 构建 Player

完整生成成功后，在 Build Settings 中执行 Build，把产物输出到仓库外部：

```text
<release-root>/<version>/DedicatedServer/
```

至少检查：

```text
<game>.x86_64
UnityPlayer.so
GameAssembly.so
<game>_Data/StreamingAssets/MiniCoreServerRuntime.json
```

注入的 JSON 是构建模板。每个部署实例必须使用服务器本地的运行配置覆盖它。

### 7.4 AI 检查提示

```text
请检查 Dedicated Server 构建产物是否完整，并确认 StreamingAssets 中的
热更新程序集、AOT 元数据和 MiniCoreServerRuntime.json 存在。
不得读取或输出 JSON 的真实地址值，只报告结构和文件是否存在。
```

完成标准：DS Player 可以作为同一基础制品复制给 Coordinator 与所有业务 Role。

## 8. 阶段 D：发布独立 .NET 服务

MiniCore 示例服务位于：

```text
Server/AuthenticationServer
Server/DatabaseServer
```

使用工程 `Server/global.json` 指定的 SDK。通用发布命令：

```bash
dotnet publish Server/AuthenticationServer/AuthenticationServer.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o "<release-root>/<version>/AuthenticationServer"

dotnet publish Server/DatabaseServer/DatabaseServer.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o "<release-root>/<version>/DatabaseServer"
```

注意：尖括号是占位符，不能原样复制到终端。由开发者在仓库外选择真实输出目录。

发布目录中的 `appsettings.json` 是模板。生产环境必须使用服务器本地配置或环境变量覆盖：

```text
ConnectionStrings__Authentication
Authentication__CoordinatorWebSocketUrl
ConnectionStrings__GameDatabase
DatabaseServer__InstanceId
DatabaseServer__ListenHost
DatabaseServer__ListenPort
DatabaseServer__AdvertisedHost
DatabaseServer__CoordinatorHost
DatabaseServer__CoordinatorPort
DatabaseServer__MaximumConcurrency
```

DatabaseServer 使用 Inner RPC 注册 Coordinator；AuthenticationServer 是可替换业务系统，不向 Coordinator 注册。

### AI 检查提示

```text
请检查 Server/global.json 和两个 csproj，给出当前平台正确的 dotnet publish 命令。
不得读取或显示生产 appsettings、环境变量值或 Secret Manager 内容。
发布后只检查可执行文件、deps/runtimeconfig 和依赖是否完整。
```

完成标准：两个目录都有对应 Linux 可执行文件；生产密钥没有进入制品。

## 9. 阶段 E：构建客户端与热更新资源

### 9.1 客户端首包

1. 切换到目标客户端平台。
2. Scenes In Build 只保留客户端 Bootstrap 场景。MiniBomber 示例为：

```text
Assets/Scenes/HotUpdateScene.unity
```

3. 客户端业务配置只能包含认证入口，不包含 Coordinator、Lobby、Game、Match、Database 或 DS 内网地址。
4. AOT、Control、平台、Development Build 或启动器发生变化时执行完整生成。
5. 完整生成成功后构建 Player。

### 9.2 仅发布在线热更新

只有旧 Player 已具备新 DLL 所需 AOT 元数据和原生能力时，才能只发布 YooAsset 新版本。在线发布必须上传完整的新 `DefaultPackage` 版本目录，不能只上传某个 DLL。

### 9.3 WebGL

WebGL 静态站点通常包含：

```text
index.html
Build/
TemplateData/
StreamingAssets/yoo/DefaultPackage/
```

使用 Brotli 时，静态服务器或对象存储必须正确返回 Content-Encoding 与 Content-Type。示例域名只能使用：

```text
https://game.example.com/
https://api.example.com/
wss://api.example.com/minicore/coordinator
```

### 9.4 AI 检查提示

```text
请确认当前客户端目标、Development Build、HybridCLR 清单、YooAsset 包版本和
Bootstrap 场景一致。检查客户端产物中不存在 Inner、HotUpdate.Server、
Control.Inner 或 Dedicated Server 配置。不要上传任何文件。
```

完成标准：客户端只携带客户端允许的程序集与资源，服务端实现和部署拓扑未泄漏。

## 10. 阶段 F：制品与校验清单

每次发布至少生成：

```text
<release-root>/<version>/
├── DedicatedServer/
├── AuthenticationServer/       可选
├── DatabaseServer/             可选
├── Client/                     按目标平台命名
├── SHA256SUMS.txt
└── RELEASE-MANIFEST.txt
```

`RELEASE-MANIFEST.txt` 只记录非敏感元数据：

```text
Version=<version>
GitCommit=<commit>
UnityVersion=<unity-version>
BuildTarget=<target>
DevelopmentBuild=false
Artifacts=DedicatedServer,AuthenticationServer,DatabaseServer,WebGL
DatabaseMigration=none|reviewed-separately
```

压缩包中不得包含：

- `.git`；
- 本地缓存和 Library；
- `._*` AppleDouble 文件；
- 生产 `.env`；
- 真实 `appsettings.Production.json`；
- SSH Key；
- 云厂商凭据；
- 日志和命令历史。

### AI 检查提示

```text
请只读取制品文件名、大小和哈希，检查压缩包是否包含密钥、生产配置、
Git 元数据、日志或 AppleDouble 文件。不要展开或输出配置文件内容。
```

完成标准：制品清单完整、哈希可复验、没有敏感文件。

## 11. 阶段 G：服务器目录与私有配置

首次部署时，由运维人员在服务器上选择 `<deploy-root>`，为每个进程建立：

```text
<deploy-root>/coordinator/
<deploy-root>/game-cluster/
<deploy-root>/authentication/
<deploy-root>/database/
```

每个服务建议使用：

```text
releases/<version>/
current -> releases/<version>
config/
backups/
```

部署原则：

1. 上传到独立暂存目录；
2. 校验 SHA256；
3. 解压新版本；
4. 从服务器本地 `config/` 注入生产配置；
5. 检查所有权和执行权限；
6. 停止旧进程；
7. 原子切换 `current` 软链接；
8. 按依赖顺序启动；
9. 验收失败时把 `current` 指回旧版本。

### 11.1 DS 实例配置

Coordinator 示例：

```json
{
  "instanceId": "Coordinator-01",
  "roles": ["Coordinator"],
  "coordinator": {
    "innerHost": "127.0.0.1",
    "innerPort": 7000
  },
  "listeners": {
    "innerHost": "127.0.0.1",
    "innerPort": 7000,
    "outerHost": "127.0.0.1",
    "outerPort": 7001,
    "outerPath": "/minicore/coordinator"
  },
  "advertised": {
    "innerHost": "127.0.0.1",
    "innerPort": 7000,
    "outerWebSocketUrl": "wss://api.example.com/minicore/coordinator"
  },
  "persistenceMode": "None"
}
```

GameCluster 示例：

```json
{
  "instanceId": "GameCluster-01",
  "roles": ["Lobby", "Match", "Game"],
  "coordinator": {
    "innerHost": "127.0.0.1",
    "innerPort": 7000
  },
  "listeners": {
    "innerHost": "127.0.0.1",
    "innerPort": 7100,
    "outerHost": "127.0.0.1",
    "outerPort": 7101,
    "outerPath": "/minicore/game-cluster"
  },
  "advertised": {
    "innerHost": "127.0.0.1",
    "innerPort": 7100,
    "outerWebSocketUrl": "wss://api.example.com/minicore/game-cluster"
  },
  "persistenceMode": "Database"
}
```

同机部署可以使用 loopback；跨机器部署必须使用调用方可达的私网地址。真实地址只存在于服务器本机配置或私有部署系统中。

### 11.2 AI 检查提示

```text
请通过只读命令检查部署目录结构、软链接目标、文件所有权、执行权限和配置文件存在性。
配置内容只校验 JSON 语法与必需字段，不输出字段值。
```

完成标准：新版本位于独立 releases 目录，生产配置与程序制品分离，旧版本仍可回滚。

## 12. 阶段 H：进程守护与反向代理

### 12.1 systemd 原则

每个进程使用独立 unit：

```text
minicore-coordinator.service
minicore-game-cluster.service
minicore-authentication.service
minicore-database.service
```

unit 文件只引用稳定的 `current` 路径，不写数据库密码。敏感环境变量从权限受限的 EnvironmentFile、systemd Credentials 或 Secret Manager 加载。

依赖关系建议：

```text
DatabaseServer After/Wants Coordinator
GameCluster    After/Wants Coordinator 和可选 DatabaseServer
AuthenticationServer 独立于 Coordinator 启动
```

不要依赖 systemd 启动顺序代替应用重连。MiniCore 的普通 DS 和 DatabaseServer仍必须在 Coordinator 重启后自动重新注册。

### 12.2 反向代理原则

一个 HTTPS/WSS 域名可以按路径转发：

```text
/api/auth/*                 → AuthenticationServer HTTP
/minicore/coordinator       → Coordinator WebSocket
/minicore/game-cluster      → GameCluster WebSocket
```

只暴露 80/443。Inner TCP、数据库和本机业务端口只允许内网或 loopback 访问。

示例地址必须使用 `example.com`；真实证书、域名、DNS 和云安全组配置放在私有运维资料中。

### 12.3 AI 检查提示

```text
请只读检查 systemd unit、当前进程用户、WorkingDirectory、ExecStart、依赖关系、
重启策略和反向代理路由。输出时遮蔽真实主机名、用户名和目录。
不要执行 daemon-reload、restart、enable 或修改配置。
```

完成标准：所有 unit 语法有效，外网只暴露必要入口，服务间调用不经过 Coordinator 转发。

## 13. 阶段 I：启动顺序

首次部署或完整停机更新使用：

```text
1. Coordinator
2. DatabaseServer（启用 persistenceMode=Database 时）
3. AuthenticationServer（独立，可在 Coordinator 后启动）
4. GameCluster / 其他业务 Role
5. 客户端静态站点或在线资源版本
```

停止顺序相反：

```text
1. GameCluster / 其他业务 Role
2. AuthenticationServer
3. DatabaseServer
4. Coordinator
```

每启动一个服务都必须先确认：

- 进程为 active/running；
- 对应监听端口存在；
- 日志没有配置解析、程序集加载、Opcode、端口占用或数据库连接错误；
- 当前服务完成 Ready 后再启动依赖它的下一个服务。

### AI 指导提示

```text
请一次只指导启动一个服务。先给出 status、journal 和端口检查命令，
等开发者提供脱敏结果并确认 Ready 后，再给出下一个服务的命令。
不要一次性启动所有服务。
```

完成标准：Coordinator、可选 DatabaseServer、AuthenticationServer 和所有业务 Role 都处于稳定运行状态。

## 14. 阶段 J：部署验收

### 14.1 服务器检查

逐服务确认：

```text
systemd 状态
最近启动日志
监听端口
进程重启次数
Coordinator 服务目录
DatabaseServer Ready 状态
GameCluster Ready 状态
反向代理配置有效性
TLS 证书有效期
```

### 14.2 客户端链路

```text
客户端读取业务认证入口
→ HTTPS 登录
→ 响应返回 Coordinator 外网地址
→ WSS 连接 Coordinator
→ ResolveService(Lobby)
→ 返回 Ready 的 GameCluster 地址
→ 客户端直连 GameCluster
→ Lobby/Game 直接调用 DatabaseServer Inner RPC
```

验收时不能只检查“页面能打开”。必须确认认证、服务发现、业务直连和数据库读写完整贯通。

### 14.3 AI 检查提示

```text
请生成只读验收清单，覆盖 systemd、端口、最近日志、HTTP CORS、WSS 路径、
Coordinator 目录、Database Ready 和客户端完整登录链路。
命令中使用环境变量或占位符，不回显凭据。
```

完成标准：所有服务稳定运行，完整链路成功，日志中没有循环性异常。

## 15. 阶段 K：故障恢复验收

正式上线前至少验证：

1. 单个 RPC 超时不会断开健康 Session；
2. 重启 DatabaseServer 后，GameCluster 不重启即可重新发现并连接；
3. 重启 Coordinator 后，DatabaseServer 和普通 DS 自动重新注册并恢复 Ready；
4. Coordinator 短暂不可用时，业务 Listener 不被错误关闭；
5. 连接恢复后不会继续使用过期服务目录；
6. `NRestarts` 不持续增长；
7. 日志不循环出现 EOF、EndOfStream 或 Session 未连接错误。

当前默认时间：

```text
普通 RPC Timeout：10 秒
Ping 间隔：2 秒
Pong 失效时间：10 秒
Coordinator 租约心跳：5 秒
Coordinator 控制 RPC：3 秒
Database Load：5 秒
Database Save：8 秒
```

### AI 指导提示

```text
请先记录所有服务当前状态和重启次数，再一次只重启一个目标服务。
每次重启后检查依赖服务是否自动恢复，不主动重启依赖服务掩盖问题。
不得操作数据库实例、删除数据或修改网络安全策略。
```

完成标准：Coordinator 和 DatabaseServer 分别重启后，服务目录与业务连接都能自动恢复。

## 16. 阶段 L：回滚与发布记录

每次发布前必须保留：

- 上一版本程序目录；
- 上一版本私有配置备份；
- 客户端上一稳定版本或对象存储前缀；
- 数据库 Migration 的独立回滚评审结果；
- 当前 `current` 软链接目标；
- 发布前各服务状态和重启次数。

应用回滚：

```text
停止新版本依赖服务
→ current 指回上一版本
→ 恢复上一版本兼容配置
→ 按 Coordinator、DatabaseServer、AuthenticationServer、业务 Role 顺序启动
→ 重做完整验收
```

数据库回滚不能自动绑定应用回滚。发生 Schema 变化时，必须在发布前单独设计前向兼容、数据备份与恢复方案。

发布记录只能包含：

```text
版本号
Git Commit
制品 SHA256
目标环境代号
开始/完成时间
发布人
Migration 状态
验收结果
回滚点
```

不能包含密码、Token、真实连接字符串或私钥。

## 17. 首次部署与日常更新的区别

### 首次部署

- 创建服务器用户与目录；
- 安装 systemd unit；
- 配置私有环境变量或 Secret Manager；
- 配置数据库、最小权限账号和白名单；
- 配置 DNS、TLS 和反向代理；
- 构建并部署全部制品；
- 验证完整链路和故障恢复。

### 日常更新

- 根据改动范围选择完整 Player 或热更新包；
- 上传并校验新制品；
- 解压到新 releases 目录；
- 复用服务器本地私有配置；
- 原子切换 current；
- 只重启受影响服务；
- 完成验收后保留上一回滚版本。

日常更新不应反复创建用户、重写 unit、重配 Caddy、重新开放端口或重复执行初始数据库 SQL。

## 18. AI 分阶段提示词索引

开发者可以从任意阶段恢复，让 AI 快速接入。

### 18.1 接手现状

```text
请读取 Docs/FrameworkDeploymentGettingStarted.md，并只读检查当前发布状态。
按阶段 A-L 告诉我已经完成到哪一步、缺少什么证据，以及下一步是什么。
所有真实地址、用户、目录和凭据必须遮蔽。
```

### 18.2 生成本地制品

```text
请根据当前 Git 改动判断发布范围，然后逐步指导生成 MiniCore 制品。
一次只给一个构建目标；每一步写清 Unity 目标、场景、菜单、输出和完成标准。
不要运行测试或上传操作。
```

### 18.3 首次服务器安装

```text
请按照 Docs/FrameworkDeploymentGettingStarted.md 设计首次部署步骤。
先询问必要的非秘密选择，用 <placeholder> 输出配置模板。
密码和真实地址必须由我在服务器本地填写，不进入项目文件或聊天记录。
```

### 18.4 更新已有环境

```text
请只读检查当前服务、目录、软链接和配置存在性，设计无覆盖的暂存发布。
保留旧版本作为回滚点；在我确认前不停止服务、不切换 current、不执行 Migration。
```

### 18.5 排查启动失败

```text
请按 Coordinator、DatabaseServer、AuthenticationServer、GameCluster 的依赖顺序诊断。
先分析脱敏后的 status、journal 和监听端口，再提出最小修复。
不要重建整套环境，不修改数据库数据，不用重启所有服务掩盖根因。
```

### 18.6 发布验收

```text
请根据阶段 J-K 生成验收清单，一次只执行一个只读检查或一个明确授权的重启检查。
记录完成标准，不输出任何敏感配置值。
```

## 19. 最终检查表

- [ ] 仓库中没有真实服务器、SSH、数据库、证书或云资源信息。
- [ ] 生产配置位于仓库外，并具有最小读取权限。
- [ ] 本次发布范围已经按 AOT、Control、HotUpdate、.NET 和资源分类。
- [ ] Dedicated Server 与客户端分别在正确目标上生成。
- [ ] Coordinator 与 GameCluster 使用同一 DS 制品、不同运行配置。
- [ ] 客户端不包含 Server、Inner、Control.Inner 或 DS 配置。
- [ ] 制品具备版本、Commit、SHA256 和非敏感 Manifest。
- [ ] 新版本先进入 releases 暂存目录，没有直接覆盖 current。
- [ ] systemd 不保存数据库密码。
- [ ] 外网只暴露 HTTPS/WSS，Inner 与数据库端口未公开。
- [ ] 启动顺序和 Ready 检查正确。
- [ ] 认证、Coordinator 查询、GameCluster 直连和数据库链路全部通过。
- [ ] DatabaseServer 与 Coordinator 重启恢复通过。
- [ ] 上一程序版本、私有配置和客户端版本仍可回滚。
- [ ] 发布记录不包含任何秘密或个人基础设施信息。
