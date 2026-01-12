# MiniCore v1.0

## 框架结构说明

MIniCore大部分功能都写成了Component的形式，通过Global.Com.Add<'AComponent>和Global.Com.Get<'AComponent>的形式可以添加或者获取组件；
目前实现的组件有：CSV表读取、文件操作、UI窗体（MVP的形式）、网络组件（Tcp、Kcp、Udp）等；
整个框架分为3个模块，分别是MiniCore.Model、MiniCore.Core和MiniCore.HotUpdate;

- MiniCore.Model：主要存储一些基础的实体、接口等定义；
- MiniCore.Core：主要是一些具体的组件的实现、工具类等；
- MiniCore.HotUpdate：用于热更新的程序集，可以通过HybridCLR等进行热更新；

UI资源加载通过MVP的方式，可以通过Unity上方菜单栏 MiniCore ==》UI Window Generator输入窗体名称通过自定义模板（可自由更改）一键生成View和Presenter，并且通过UIFactoryComponent直接加载窗体；
网络组件目前支持Tcp\Kcp\Udp，并且可以自定义序列化，可以灵活的使用Json、Protocol等。
Kcp目前是通过AI将[Kcp C#版](https://github.com/KumoKyaku/kcp)引入的方式生成的，由于只审查了接入的Session层面，因此对于潜在的bug还没及时修复，**目前已知问题是：在KCP测试界面点击连接服务端，无论连接成功与否都会显示连接成功。**

## 引用的插件

1. 官方插件：

- [Newtonsoft Json]：目前主要用于默认网络报文序列化

2. 三方插件：

- [UniTask](https://github.com/Cysharp/UniTask?tab=readme-ov-file#upm-package)

## 总结：

整个框架使用简单，便捷，可以快速上手，由于个人经验以及时间的原因，可能存在一些问题，欢迎各位大佬指正。

---