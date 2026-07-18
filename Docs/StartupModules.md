# 重构后的项目启动方式

MiniCore 现在通过“启动模块配置 + `GameStartup`”完成 HotUpdate 项目的初始化。

## 项目启动流程

1. Bootstrap 场景的 `UpdateMainWindow` 初始化 YooAsset、HybridCLR 并加载 HotUpdate DLL。
2. Bootstrap 根据 Player 模式调用客户端或 Dedicated Server 的稳定入口。
3. `MiniCoreStartup.StartAsync()` 根据 Player 模式执行自动生成的模块启动代码：按依赖顺序 `Global.Pin` 已选组件，网络模块同时注册业务 Handler。
4. 模块初始化完成后，调用项目的 `GameStartup.StartAsync()`。

## 配置启动模块

打开 Unity 菜单 `MiniCore > 项目启动配置`：

1. 勾选 Client 或 Server 需要启动的组件。
2. 展开带 `AComponent<TArgs>` 的组件，填写启动参数；保持“使用代码默认值”时采用 Args 类中的默认值。
3. 点击“保存启动参数并生成代码”。

每个模块都可以独立勾选 Client 和 Server。配置保存于 `Assets/Settings/MiniCoreStartupSettings.asset`，启动代码生成到 `Assets/Scripts/MiniCore/HotUpdate/Generated/Startup/MiniCoreStartup.Generated.cs`。

默认示例中，客户端加载场景标签、YooAsset 资源、资产管理、UI 工厂、网络消息和计时器；服务端加载网络消息和计时器。

## 编写项目启动逻辑

在 [GameStartup.cs](../Assets/Scripts/MiniCore/HotUpdate/Entry/GameStartup.cs) 的 `StartAsync()` 中编写项目的首个业务动作，例如进入登录界面、加载存档或启动服务端监听：

```csharp
public sealed class GameStartup : AGameStartup
{
    public override async Task StartAsync()
    {
        if (Application.isBatchMode)
        {
            NetworkMessageComponent network = Global.Get<NetworkMessageComponent>(this);
            await network.StartKcpServerAsync("0.0.0.0", 20000).AsTask();
            return;
        }

        // 客户端首个业务动作。
    }
}
```

## 新增启动模块

给常驻组件添加 `MiniCoreStartupModule` 特性，并声明启动目标和依赖：

```csharp
[MiniCoreStartupModule(
    "排行榜",
    DependsOn = new[] { typeof(NetworkMessageComponent) })]
public sealed class RankingComponent : AComponent<RankingComponentInitArgs>
{
    protected override void Awake(RankingComponentInitArgs args)
    {
        NetworkMessageComponent network = Global.Get<NetworkMessageComponent>(this);
    }

    public override void Dispose()
    {
        Global.ReleaseAll(this);
        base.Dispose();
    }
}

public sealed class RankingComponentInitArgs : ComponentInitArgs
{
    public string Endpoint { get; set; } = "ranking";
    public int RetryCount { get; set; } = 3;
}
```

编辑器可配置 `string`、`bool`、`int`、`long`、`float`、`double` 和 `enum` 类型的 public 字段或可写属性。YooAsset 资源使用地址或 GUID 字符串作为参数值。
